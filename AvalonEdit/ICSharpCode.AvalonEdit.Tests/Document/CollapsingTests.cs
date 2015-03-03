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
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Document
{
	[TestFixture]
	public class CollapsingTests
	{
		TextDocument document;
		HeightTree heightTree;
		
		[SetUp]
		public void Setup()
		{
			document = new TextDocument();
			document.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
			heightTree = new HeightTree(document, 10);
			foreach (DocumentLine line in document.Lines) {
				heightTree.SetHeight(line, line.LineNumber);
			}
		}
		
		CollapsedLineSection SimpleCheck(int from, int to)
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(from), document.GetLineByNumber(to));
			for (int i = 1; i < from; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			for (int i = from; i <= to; i++) {
				Assert.IsTrue(heightTree.GetIsCollapsed(i));
			}
			for (int i = to + 1; i <= 10; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
			return sec1;
		}
		
		[Test]
		public void SimpleCheck()
		{
			SimpleCheck(4, 6);
		}
		
		[Test]
		public void SimpleUncollapse()
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(4), document.GetLineByNumber(6));
			sec1.Uncollapse();
			for (int i = 1; i <= 10; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
		}
		
		[Test]
		public void FullCheck()
		{
			for (int from = 1; from <= 10; from++) {
				for (int to = from; to <= 10; to++) {
					try {
						SimpleCheck(from, to).Uncollapse();
						for (int i = 1; i <= 10; i++) {
							Assert.IsFalse(heightTree.GetIsCollapsed(i));
						}
			CheckHeights();
					} catch {
						Console.WriteLine("from = " + from + ", to = " + to);
						throw;
					}
				}
			}
		}
		
		[Test]
		public void InsertInCollapsedSection()
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(4), document.GetLineByNumber(6));
			document.Insert(document.GetLineByNumber(5).Offset, "a\nb\nc");
			for (int i = 1; i < 4; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			for (int i = 4; i <= 8; i++) {
				Assert.IsTrue(heightTree.GetIsCollapsed(i));
			}
			for (int i = 9; i <= 12; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
		}
		
		[Test]
		public void RemoveInCollapsedSection()
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(7));
			int line4Offset = document.GetLineByNumber(4).Offset;
			int line6Offset = document.GetLineByNumber(6).Offset;
			document.Remove(line4Offset, line6Offset - line4Offset);
			for (int i = 1; i < 3; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			for (int i = 3; i <= 5; i++) {
				Assert.IsTrue(heightTree.GetIsCollapsed(i));
			}
			for (int i = 6; i <= 8; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
		}
		
		[Test]
		public void RemoveEndOfCollapsedSection()
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(6));
			int line5Offset = document.GetLineByNumber(5).Offset;
			int line8Offset = document.GetLineByNumber(8).Offset;
			document.Remove(line5Offset, line8Offset - line5Offset);
			for (int i = 1; i < 3; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			for (int i = 3; i <= 5; i++) {
				Assert.IsTrue(heightTree.GetIsCollapsed(i));
			}
			for (int i = 6; i <= 7; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
		}
		
		[Test]
		public void RemoveCollapsedSection()
		{
			CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(3));
			int line3Offset = document.GetLineByNumber(3).Offset;
			document.Remove(line3Offset - 1, 1);
			for (int i = 1; i <= 9; i++) {
				Assert.IsFalse(heightTree.GetIsCollapsed(i));
			}
			CheckHeights();
			Assert.AreSame(null, sec1.Start);
			Assert.AreSame(null, sec1.End);
			// section gets uncollapsed when it is removed
			Assert.IsFalse(sec1.IsCollapsed);
		}
		
		void CheckHeights()
		{
			HeightTests.CheckHeights(document, heightTree);
		}
	}
}
