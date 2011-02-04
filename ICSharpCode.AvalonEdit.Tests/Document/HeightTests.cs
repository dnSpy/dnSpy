// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
