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
using System.Linq;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Document
{
	[TestFixture]
	public class HeightTests
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
		
		[Test]
		public void SimpleCheck()
		{
			CheckHeights();
		}
		
		[Test]
		public void TestLinesRemoved()
		{
			document.Remove(5, 4);
			CheckHeights();
		}
		
		[Test]
		public void TestHeightChanged()
		{
			heightTree.SetHeight(document.GetLineByNumber(4), 100);
			CheckHeights();
		}
		
		[Test]
		public void TestLinesInserted()
		{
			document.Insert(0, "x\ny\n");
			heightTree.SetHeight(document.Lines[0], 100);
			heightTree.SetHeight(document.Lines[1], 1000);
			heightTree.SetHeight(document.Lines[2], 10000);
			CheckHeights();
		}
		
		void CheckHeights()
		{
			CheckHeights(document, heightTree);
		}
		
		internal static void CheckHeights(TextDocument document, HeightTree heightTree)
		{
			double[] heights = document.Lines.Select(l => heightTree.GetIsCollapsed(l.LineNumber) ? 0 : heightTree.GetHeight(l)).ToArray();
			double[] visualPositions = new double[document.LineCount+1];
			for (int i = 0; i < heights.Length; i++) {
				visualPositions[i+1]=visualPositions[i]+heights[i];
			}
			foreach (DocumentLine ls in document.Lines) {
				Assert.AreEqual(visualPositions[ls.LineNumber-1], heightTree.GetVisualPosition(ls));
			}
			Assert.AreEqual(visualPositions[document.LineCount], heightTree.TotalHeight);
		}
	}
}
