// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Document
{
	[TestFixture]
	public class TextSegmentTreeTest
	{
		Random rnd;
		
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			int seed = Environment.TickCount;
			Console.WriteLine("TextSegmentTreeTest Seed: " + seed);
			rnd = new Random(seed);
		}
		
		class TestTextSegment : TextSegment
		{
			internal int ExpectedOffset, ExpectedLength;
			
			public TestTextSegment(int expectedOffset, int expectedLength)
			{
				this.ExpectedOffset = expectedOffset;
				this.ExpectedLength = expectedLength;
				this.StartOffset = expectedOffset;
				this.Length = expectedLength;
			}
		}
		
		TextSegmentCollection<TestTextSegment> tree;
		List<TestTextSegment> expectedSegments;
		
		[SetUp]
		public void SetUp()
		{
			tree = new TextSegmentCollection<TestTextSegment>();
			expectedSegments = new List<TestTextSegment>();
		}
		
		[Test]
		public void FindInEmptyTree()
		{
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(0));
			Assert.AreEqual(0, tree.FindSegmentsContaining(0).Count);
			Assert.AreEqual(0, tree.FindOverlappingSegments(10, 20).Count);
		}
		
		[Test]
		public void FindFirstSegmentWithStartAfter()
		{
			var s1 = new TestTextSegment(5, 10);
			var s2 = new TestTextSegment(10, 10);
			tree.Add(s1);
			tree.Add(s2);
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(-100));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(0));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(4));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(5));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(6));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(9));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(10));
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(11));
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(100));
		}
		
		[Test]
		public void FindFirstSegmentWithStartAfterWithDuplicates()
		{
			var s1 = new TestTextSegment(5, 10);
			var s1b = new TestTextSegment(5, 7);
			var s2 = new TestTextSegment(10, 10);
			var s2b = new TestTextSegment(10, 7);
			tree.Add(s1);
			tree.Add(s1b);
			tree.Add(s2);
			tree.Add(s2b);
			Assert.AreSame(s1b, tree.GetNextSegment(s1));
			Assert.AreSame(s2b, tree.GetNextSegment(s2));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(-100));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(0));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(4));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(5));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(6));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(9));
			Assert.AreSame(s2, tree.FindFirstSegmentWithStartAfter(10));
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(11));
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(100));
		}
		
		[Test]
		public void FindFirstSegmentWithStartAfterWithDuplicates2()
		{
			var s1 = new TestTextSegment(5, 1);
			var s2 = new TestTextSegment(5, 2);
			var s3 = new TestTextSegment(5, 3);
			var s4 = new TestTextSegment(5, 4);
			tree.Add(s1);
			tree.Add(s2);
			tree.Add(s3);
			tree.Add(s4);
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(0));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(1));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(4));
			Assert.AreSame(s1, tree.FindFirstSegmentWithStartAfter(5));
			Assert.AreSame(null, tree.FindFirstSegmentWithStartAfter(6));
		}
		
		TestTextSegment AddSegment(int offset, int length)
		{
//			Console.WriteLine("Add " + offset + ", " + length);
			TestTextSegment s = new TestTextSegment(offset, length);
			tree.Add(s);
			expectedSegments.Add(s);
			return s;
		}
		
		void RemoveSegment(TestTextSegment s)
		{
//			Console.WriteLine("Remove " + s);
			expectedSegments.Remove(s);
			tree.Remove(s);
		}
		
		void TestRetrieval(int offset, int length)
		{
			HashSet<TestTextSegment> actual = new HashSet<TestTextSegment>(tree.FindOverlappingSegments(offset, length));
			HashSet<TestTextSegment> expected = new HashSet<TestTextSegment>();
			foreach (TestTextSegment e in expectedSegments) {
				if (e.ExpectedOffset + e.ExpectedLength < offset)
					continue;
				if (e.ExpectedOffset > offset + length)
					continue;
				expected.Add(e);
			}
			Assert.IsTrue(actual.IsSubsetOf(expected));
			Assert.IsTrue(expected.IsSubsetOf(actual));
		}
		
		void CheckSegments()
		{
			Assert.AreEqual(expectedSegments.Count, tree.Count);
			foreach (TestTextSegment s in expectedSegments) {
				Assert.AreEqual(s.ExpectedOffset, s.StartOffset /*, "startoffset for " + s*/);
				Assert.AreEqual(s.ExpectedLength, s.Length /*, "length for " + s*/);
			}
		}
		
		[Test]
		public void AddSegments()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			CheckSegments();
		}
		
		void ChangeDocument(OffsetChangeMapEntry change)
		{
			tree.UpdateOffsets(change);
			foreach (TestTextSegment s in expectedSegments) {
				int endOffset = s.ExpectedOffset + s.ExpectedLength;
				s.ExpectedOffset = change.GetNewOffset(s.ExpectedOffset, AnchorMovementType.AfterInsertion);
				s.ExpectedLength = Math.Max(0, change.GetNewOffset(endOffset, AnchorMovementType.BeforeInsertion) - s.ExpectedOffset);
			}
		}
		
		[Test]
		public void InsertionBeforeAllSegments()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(5, 0, 2));
			CheckSegments();
		}
		
		[Test]
		public void ReplacementBeforeAllSegmentsTouchingFirstSegment()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(5, 5, 2));
			CheckSegments();
		}
		
		[Test]
		public void InsertionAfterAllSegments()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(45, 0, 2));
			CheckSegments();
		}
		
		[Test]
		public void ReplacementOverlappingWithStartOfSegment()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(9, 7, 2));
			CheckSegments();
		}
		
		[Test]
		public void ReplacementOfWholeSegment()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(10, 20, 30));
			CheckSegments();
		}
		
		[Test]
		public void ReplacementAtEndOfSegment()
		{
			TestTextSegment s1 = AddSegment(10, 20);
			TestTextSegment s2 = AddSegment(15, 10);
			ChangeDocument(new OffsetChangeMapEntry(24, 6, 10));
			CheckSegments();
		}
		
		[Test]
		public void RandomizedNoDocumentChanges()
		{
			for (int i = 0; i < 1000; i++) {
//				Console.WriteLine(tree.GetTreeAsString());
//				Console.WriteLine("Iteration " + i);
				
				switch (rnd.Next(3)) {
					case 0:
						AddSegment(rnd.Next(500), rnd.Next(30));
						break;
					case 1:
						AddSegment(rnd.Next(500), rnd.Next(300));
						break;
					case 2:
						if (tree.Count > 0) {
							RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
						}
						break;
				}
				CheckSegments();
			}
		}
		
		[Test]
		public void RandomizedCloseNoDocumentChanges()
		{
			// Lots of segments in a short document. Tests how the tree copes with multiple identical segments.
			for (int i = 0; i < 1000; i++) {
				switch (rnd.Next(3)) {
					case 0:
						AddSegment(rnd.Next(20), rnd.Next(10));
						break;
					case 1:
						AddSegment(rnd.Next(20), rnd.Next(20));
						break;
					case 2:
						if (tree.Count > 0) {
							RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
						}
						break;
				}
				CheckSegments();
			}
		}
		
		[Test]
		public void RandomizedRetrievalTest()
		{
			for (int i = 0; i < 1000; i++) {
				AddSegment(rnd.Next(500), rnd.Next(300));
			}
			CheckSegments();
			for (int i = 0; i < 1000; i++) {
				TestRetrieval(rnd.Next(1000) - 100, rnd.Next(500));
			}
		}
		
		[Test]
		public void RandomizedWithDocumentChanges()
		{
			for (int i = 0; i < 500; i++) {
//				Console.WriteLine(tree.GetTreeAsString());
//				Console.WriteLine("Iteration " + i);
				
				switch (rnd.Next(6)) {
					case 0:
						AddSegment(rnd.Next(500), rnd.Next(30));
						break;
					case 1:
						AddSegment(rnd.Next(500), rnd.Next(300));
						break;
					case 2:
						if (tree.Count > 0) {
							RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
						}
						break;
					case 3:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), rnd.Next(50), rnd.Next(50)));
						break;
					case 4:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), 0, rnd.Next(50)));
						break;
					case 5:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), rnd.Next(50), 0));
						break;
				}
				CheckSegments();
			}
		}
		
		[Test]
		public void RandomizedWithDocumentChangesClose()
		{
			for (int i = 0; i < 500; i++) {
//				Console.WriteLine(tree.GetTreeAsString());
//				Console.WriteLine("Iteration " + i);
				
				switch (rnd.Next(6)) {
					case 0:
						AddSegment(rnd.Next(50), rnd.Next(30));
						break;
					case 1:
						AddSegment(rnd.Next(50), rnd.Next(3));
						break;
					case 2:
						if (tree.Count > 0) {
							RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
						}
						break;
					case 3:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), rnd.Next(10), rnd.Next(10)));
						break;
					case 4:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), 0, rnd.Next(10)));
						break;
					case 5:
						ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), rnd.Next(10), 0));
						break;
				}
				CheckSegments();
			}
		}
	}
}
