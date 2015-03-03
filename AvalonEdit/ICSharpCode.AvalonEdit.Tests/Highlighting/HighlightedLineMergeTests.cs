// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory.Editor;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	[TestFixture]
	public class HighlightedLineMergeTests
	{
		IDocument document = new TextDocument(new string(' ', 20));
		
		[Test]
		public void SimpleMerge1()
		{
			HighlightedLine baseLine = new HighlightedLine(document, document.GetLineByNumber(1));
			baseLine.Sections.Add(MakeSection(0, 1, "B"));
			
			HighlightedLine additionalLine = new HighlightedLine(document, document.GetLineByNumber(1));
			additionalLine.Sections.Add(MakeSection(0, 2, "A"));
			
			baseLine.MergeWith(additionalLine);
			// The additional section gets split up so that it fits into the tree structure
			Assert.That(baseLine.Sections, Is.EqualTo(
				new[] {
					MakeSection(0, 1, "B"),
					MakeSection(0, 1, "A"),
					MakeSection(1, 2, "A")
				}).Using(new SectionComparer()));
		}
		
		[Test]
		public void SimpleMerge2()
		{
			HighlightedLine baseLine = new HighlightedLine(document, document.GetLineByNumber(1));
			baseLine.Sections.Add(MakeSection(0, 1, "B"));
			baseLine.Sections.Add(MakeSection(0, 1, "BN"));
			
			HighlightedLine additionalLine = new HighlightedLine(document, document.GetLineByNumber(1));
			additionalLine.Sections.Add(MakeSection(0, 2, "A"));
			
			baseLine.MergeWith(additionalLine);
			// The additional section gets split up so that it fits into the tree structure
			Assert.That(baseLine.Sections, Is.EqualTo(
				new[] {
					MakeSection(0, 1, "B"),
					MakeSection(0, 1, "BN"),
					MakeSection(0, 1, "A"),
					MakeSection(1, 2, "A")
				}).Using(new SectionComparer()));
		}
		
		HighlightedSection MakeSection(int start, int end, string name)
		{
			return new HighlightedSection { Offset = start, Length = end - start, Color = new HighlightingColor { Name = name }};
		}
		
		class SectionComparer : IEqualityComparer<HighlightedSection>
		{
			public bool Equals(HighlightedSection a, HighlightedSection b)
			{
				return a.Offset == b.Offset && a.Length == b.Length && a.Color.Name == b.Color.Name;
			}
			
			public int GetHashCode(HighlightedSection obj)
			{
				return obj.Offset;
			}
		}
		
		#region Automatic Test
		/*
		const int combinations = 6 * 3 * 4 * 3 * 3 * 4;
		HighlightingColor[] baseLineColors = {
			new HighlightingColor { Name = "Base-A" },
			new HighlightingColor { Name = "Base-B" },
			new HighlightingColor { Name = "Base-N" },
			new HighlightingColor { Name = "Base-C" }
		};
		HighlightingColor[] additionalLineColors = {
			new HighlightingColor { Name = "Add-A" },
			new HighlightingColor { Name = "Add-B" },
			new HighlightingColor { Name = "Add-N" },
			new HighlightingColor { Name = "Add-C" }
		};
		
		HighlightedLine BuildHighlightedLine(int num, HighlightingColor[] colors)
		{
			// We are build a HighlightedLine with 4 segments:
			// A B C (top-level) and N nested within B.
			// These are the integers controlling the generating process:
			
			int aStart = GetNum(ref num, 5); // start offset of A
			int aLength = GetNum(ref num, 2); // length of A
			
			int bDistance = GetNum(ref num, 3); // distance from start of B to end of A
			int bStart = aStart + aLength + bDistance;
			int nDistance = GetNum(ref num, 2); // distance from start of B to start of N, range 0-2
			int nLength = GetNum(ref num, 2); // length of N
			int bEndDistance = GetNum(ref num, 2); // distance from end of N to end of B
			int bLength = nDistance + nLength + bEndDistance;
			
			int cDistance = GetNum(ref num, 3); // distance from end of B to start of C
			int cStart = bStart + bLength + cDistance;
			int cLength = 1;
			Assert.AreEqual(0, num);
			
			var documentLine = document.GetLineByNumber(1);
			HighlightedLine line = new HighlightedLine(document, documentLine);
			line.Sections.Add(new HighlightedSection { Offset = aStart, Length = aLength, Color = colors[0] });
			line.Sections.Add(new HighlightedSection { Offset = bStart, Length = bLength, Color = colors[1] });
			line.Sections.Add(new HighlightedSection { Offset = bStart + nDistance, Length = nLength, Color = colors[2] });
			line.Sections.Add(new HighlightedSection { Offset = cStart, Length = cLength, Color = colors[3] });
			
			return line;
		}
		
		/// <summary>
		/// Gets a number between 0 and max (inclusive)
		/// </summary>
		int GetNum(ref int num, int max)
		{
			int result = num % (max+1);
			num = num / (max + 1);
			return result;
		}
		
		[Test]
		public void TestAll()
		{
			for (int c1 = 0; c1 < combinations; c1++) {
				HighlightedLine line1 = BuildHighlightedLine(c1, additionalLineColors);
				for (int c2 = 0; c2 < combinations; c2++) {
					HighlightedLine line2 = BuildHighlightedLine(c2, baseLineColors);
					HighlightingColor[] expectedPerCharColors = new HighlightingColor[document.TextLength];
					ApplyColors(expectedPerCharColors, line2);
					ApplyColors(expectedPerCharColors, line1);
					try {
						line2.MergeWith(line1);
					} catch (InvalidOperationException ex) {
						throw new InvalidOperationException(string.Format("Error for c1 = {0}, c2 = {1}", c1, c2), ex);
					}
					
					HighlightingColor[] actualPerCharColors = new HighlightingColor[document.TextLength];
					ApplyColors(actualPerCharColors, line2);
					Assert.AreEqual(expectedPerCharColors, actualPerCharColors, string.Format("c1 = {0}, c2 = {1}", c1, c2));
				}
			}
		}
		
		void ApplyColors(HighlightingColor[] perCharColors, HighlightedLine line)
		{
			foreach (var section in line.Sections) {
				for (int i = 0; i < section.Length; i++) {
					perCharColors[section.Offset + i] = section.Color;
				}
			}
		}
		*/
		#endregion
	}
}
