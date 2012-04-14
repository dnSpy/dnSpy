// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.ConsistencyCheck.Xml;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Xml;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	/// <summary>
	/// Tests incremental tag soup parser.
	/// </summary>
	public class IncrementalXmlParserTests
	{
		static Random sharedRnd = new Random();
		
		public static void Run(string fileName)
		{
			Run(new StringTextSource(File.ReadAllText(fileName)));
		}
		
		public static void Run(ITextSource originalXmlFile)
		{
			int seed;
			lock (sharedRnd) {
				seed = sharedRnd.Next();
			}
			Console.WriteLine(seed);
			Random rnd = new Random(seed);
			
			AXmlParser parser = new AXmlParser();
			StringBuilder b = new StringBuilder(originalXmlFile.Text);
			IncrementalParserState parserState = null;
			var versionProvider = new TextSourceVersionProvider();
			int totalCharactersParsed = 0;
			int totalCharactersChanged = originalXmlFile.TextLength;
			TimeSpan incrementalParseTime = TimeSpan.Zero;
			TimeSpan nonIncrementalParseTime = TimeSpan.Zero;
			Stopwatch w = new Stopwatch();
			for (int iteration = 0; iteration < 100; iteration++) {
				totalCharactersParsed += b.Length;
				var textSource = new StringTextSource(b.ToString(), versionProvider.CurrentVersion);
				w.Restart();
				var incrementalResult = parser.ParseIncremental(parserState, textSource, out parserState);
				w.Stop();
				incrementalParseTime += w.Elapsed;
				w.Restart();
				var nonIncrementalResult = parser.Parse(textSource);
				w.Stop();
				nonIncrementalParseTime += w.Elapsed;
				CompareResults(incrementalResult, nonIncrementalResult);
				
				incrementalResult.AcceptVisitor(new ValidationVisitor(textSource));
				
				// Randomly mutate the file:
				
				List<TextChangeEventArgs> changes = new List<TextChangeEventArgs>();
				int modifications = rnd.Next(0, 25);
				int offset = 0;
				for (int i = 0; i < modifications; i++) {
					if (i == 0 || rnd.Next(0, 10) == 0)
						offset = rnd.Next(0, b.Length);
					else
						offset += rnd.Next(0, Math.Min(10, b.Length - offset));
					int originalOffset = rnd.Next(0, originalXmlFile.TextLength);
					int insertionLength;
					int removalLength;
					switch (rnd.Next(0, 21) / 10) {
						case 0:
							removalLength = 0;
							insertionLength = rnd.Next(0, Math.Min(50, originalXmlFile.TextLength - originalOffset));
							break;
						case 1:
							removalLength = rnd.Next(0, Math.Min(20, b.Length - offset));
							insertionLength = rnd.Next(0, Math.Min(20, originalXmlFile.TextLength - originalOffset));
							break;
						default:
							removalLength = rnd.Next(0, b.Length - offset);
							insertionLength = rnd.Next(0, originalXmlFile.TextLength - originalOffset);
							break;
					}
					string removedText = b.ToString(offset, removalLength);
					b.Remove(offset, removalLength);
					string insertedText = originalXmlFile.GetText(originalOffset, insertionLength);
					b.Insert(offset, insertedText);
					versionProvider.AppendChange(new TextChangeEventArgs(offset, removedText, insertedText));
					totalCharactersChanged += insertionLength;
				}
			}
			Console.WriteLine("Incremental parse time:     " + incrementalParseTime + " for " + totalCharactersChanged + " characters changed");
			Console.WriteLine("Non-Incremental parse time: " + nonIncrementalParseTime + " for " + totalCharactersParsed + " characters");
		}
		
		static void CompareResults(IList<AXmlObject> result1, IList<AXmlObject> result2)
		{
			for (int i = 0; i < Math.Min(result1.Count, result2.Count); i++) {
				CompareResults(result1[i], result2[i]);
			}
			if (result1.Count != result2.Count)
				throw new InvalidOperationException();
		}
		
		static void CompareResults(AXmlObject obj1, AXmlObject obj2)
		{
			if (obj1.GetType() != obj2.GetType())
				throw new InvalidOperationException();
			if (obj1.StartOffset != obj2.StartOffset)
				throw new InvalidOperationException();
			if (obj1.EndOffset != obj2.EndOffset)
				throw new InvalidOperationException();
			
			if (obj1.MySyntaxErrors.Count() != obj2.MySyntaxErrors.Count())
				throw new InvalidOperationException();
			foreach (var pair in obj1.MySyntaxErrors.Zip(obj2.MySyntaxErrors, (a,b) => new { a, b })) {
				if (pair.a.StartOffset != pair.b.StartOffset)
					throw new InvalidOperationException();
				if (pair.a.EndOffset != pair.b.EndOffset)
					throw new InvalidOperationException();
				if (pair.a.Description != pair.b.Description)
					throw new InvalidOperationException();
			}
			
			if (obj1 is AXmlText) {
				var a = (AXmlText)obj1;
				var b = (AXmlText)obj2;
				if (a.ContainsOnlyWhitespace != b.ContainsOnlyWhitespace)
					throw new InvalidOperationException();
				if (a.Value != b.Value)
					throw new InvalidOperationException();
			} else if (obj1 is AXmlTag) {
				var a = (AXmlTag)obj1;
				var b = (AXmlTag)obj2;
				if (a.OpeningBracket != b.OpeningBracket)
					throw new InvalidOperationException();
				if (a.ClosingBracket != b.ClosingBracket)
					throw new InvalidOperationException();
				if (a.Name != b.Name)
					throw new InvalidOperationException();
			} else if (obj1 is AXmlAttribute) {
				var a = (AXmlAttribute)obj1;
				var b = (AXmlAttribute)obj2;
				if (a.Name != b.Name)
					throw new InvalidOperationException();
				if (a.Value != b.Value)
					throw new InvalidOperationException();
			} else if (obj1 is AXmlElement) {
				var a = (AXmlElement)obj1;
				var b = (AXmlElement)obj2;
				if (a.Name != b.Name)
					throw new InvalidOperationException();
				if (a.IsProperlyNested != b.IsProperlyNested)
					throw new InvalidOperationException();
				if (a.HasEndTag != b.HasEndTag)
					throw new InvalidOperationException();
			} else if (obj1 is AXmlDocument) {
				// only compare the children
			} else {
				throw new NotSupportedException();
			}
			
			CompareResults(obj1.Children, obj2.Children);
		}
		
		sealed class ValidationVisitor : AXmlVisitor
		{
			readonly ITextSource textSource;
			
			public ValidationVisitor(ITextSource textSource)
			{
				this.textSource = textSource;
			}
			
			public override void VisitTag(AXmlTag tag)
			{
				if (textSource.GetText(tag.StartOffset, tag.OpeningBracket.Length) != tag.OpeningBracket)
					throw new InvalidOperationException();
				if (textSource.GetText(tag.NameSegment) != tag.Name)
					throw new InvalidOperationException();
				if (textSource.GetText(tag.EndOffset - tag.ClosingBracket.Length, tag.ClosingBracket.Length) != tag.ClosingBracket)
					throw new InvalidOperationException();
				base.VisitTag(tag);
			}
			
			public override void VisitAttribute(AXmlAttribute attribute)
			{
				if (textSource.GetText(attribute.NameSegment) != attribute.Name)
					throw new InvalidOperationException();
				base.VisitAttribute(attribute);
			}
		}
	}
}
