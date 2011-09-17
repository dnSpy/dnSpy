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
using System.IO;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser
{
	/// <summary>
	/// Test fixture that parses NRefactory itself,
	/// ensures that there are no parser crashes while doing so,
	/// and that the returned positions are consistent.
	/// </summary>
	[TestFixture]
	public class ParseSelfTests
	{
		string[] fileNames;
		
		[TestFixtureSetUp]
		public void SetUp()
		{
			string path = Path.GetFullPath (Path.Combine ("..", "..", ".."));
			if (!File.Exists(Path.Combine(path, "NRefactory.sln")))
				throw new InvalidOperationException("Test cannot find the NRefactory source code in " + path);
			fileNames = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
		}
		
		[Test]
		public void GenerateTypeSystem()
		{
			SimpleProjectContent pc = new SimpleProjectContent();
			CSharpParser parser = new CSharpParser();
			parser.GenerateTypeSystemMode = true;
			foreach (string fileName in fileNames) {
				CompilationUnit cu;
				using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)) {
					cu = parser.Parse(fs);
				}
				TypeSystemConvertVisitor cv = new TypeSystemConvertVisitor(pc, fileName);
				pc.UpdateProjectContent(null, cv.Convert(cu));
			}
		}
		
		#region ParseAndCheckPositions
		string currentFileName;
		ReadOnlyDocument currentDocument;
		
		[Test]
		public void ParseAndCheckPositions()
		{
			CSharpParser parser = new CSharpParser();
			foreach (string fileName in fileNames) {
				this.currentDocument = new ReadOnlyDocument(File.ReadAllText(fileName));
				CompilationUnit cu = parser.Parse(currentDocument.CreateReader());
				if (parser.HasErrors)
					continue;
				this.currentFileName = fileName;
				CheckPositionConsistency(cu);
				CheckMissingTokens(cu);
			}
		}

		void PrintNode (AstNode node)
		{
			Console.WriteLine ("Parent:" + node.GetType ());
			Console.WriteLine ("Children:");
			foreach (var c in node.Children)
				Console.WriteLine (c.GetType () +" at:"+ c.StartLocation +"-"+ c.EndLocation + " Role: "+ c.Role);
			Console.WriteLine ("----");
		}
		
		void CheckPositionConsistency (AstNode node)
		{
			string comment = "(" + node.GetType ().Name + " at " + node.StartLocation + " in " + currentFileName + ")";
			var pred = node.StartLocation <= node.EndLocation;
			if (!pred)
				PrintNode (node);
			Assert.IsTrue(pred, "StartLocation must be before EndLocation " + comment);
			var prevNodeEnd = node.StartLocation;
			var prevNode = node;
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				bool assertion = child.StartLocation >= prevNodeEnd;
				if (!assertion) {
					PrintNode (prevNode);
					PrintNode (node);
					
				}
				Assert.IsTrue(assertion, currentFileName + ": Child " + child.GetType () +" (" + child.StartLocation  + ")" +" must start after previous sibling " + prevNode.GetType () + "(" + prevNode.StartLocation + ")");
				CheckPositionConsistency(child);
				prevNodeEnd = child.EndLocation;
				prevNode = child;
			}
			Assert.IsTrue(prevNodeEnd <= node.EndLocation, "Last child must end before parent node ends " + comment);
		}
		
		void CheckMissingTokens(AstNode node)
		{
			if (node is IRelocatable) {
				Assert.IsNull(node.FirstChild, "Token nodes should not have children");
			} else {
				var prevNodeEnd = node.StartLocation;
				var prevNode = node;
				for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
					CheckWhitespace(prevNode, prevNodeEnd, child, child.StartLocation);
					CheckMissingTokens(child);
					prevNode = child;
					prevNodeEnd = child.EndLocation;
				}
				CheckWhitespace(prevNode, prevNodeEnd, node, node.EndLocation);
			}
		}
		
		void CheckWhitespace(AstNode startNode, TextLocation whitespaceStart, AstNode endNode, TextLocation whitespaceEnd)
		{
			if (whitespaceStart == whitespaceEnd || startNode == endNode)
				return;
			int start = currentDocument.GetOffset(whitespaceStart.Line, whitespaceStart.Column);
			int end = currentDocument.GetOffset(whitespaceEnd.Line, whitespaceEnd.Column);
			string text = currentDocument.GetText(start, end - start);
			bool assertion = string.IsNullOrWhiteSpace(text);
			if (!assertion) {
				if (startNode.Parent != endNode.Parent)
					PrintNode (startNode.Parent);
				PrintNode (endNode.Parent);
			}
			Assert.IsTrue(assertion, "Expected whitespace between " + startNode.GetType () +":" + whitespaceStart + " and " + endNode.GetType () + ":" + whitespaceEnd
			              + ", but got '" + text + "' (in " + currentFileName + " parent:" + startNode.Parent.GetType () +")");
		}
		#endregion
	}
}
