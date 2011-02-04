// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Editing
{
	[TestFixture]
	public class TextSegmentReadOnlySectionTests
	{
		TextSegmentCollection<TextSegment> segments;
		TextSegmentReadOnlySectionProvider<TextSegment> provider;
		
		[SetUp]
		public void SetUp()
		{
			segments = new TextSegmentCollection<TextSegment>();
			provider = new TextSegmentReadOnlySectionProvider<TextSegment>(segments);
		}
		
		[Test]
		public void InsertionPossibleWhenNothingIsReadOnly()
		{
			Assert.IsTrue(provider.CanInsert(0));
			Assert.IsTrue(provider.CanInsert(100));
		}
		
		[Test]
		public void DeletionPossibleWhenNothingIsReadOnly()
		{
			var result = provider.GetDeletableSegments(new SimpleSegment(10, 20)).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(10, result[0].Offset);
			Assert.AreEqual(20, result[0].Length);
		}
		
		[Test]
		public void InsertionPossibleBeforeReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
			Assert.IsTrue(provider.CanInsert(5));
		}
		
		[Test]
		public void InsertionPossibleAtStartOfReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
			Assert.IsTrue(provider.CanInsert(10));
		}
		
		[Test]
		public void InsertionImpossibleInsideReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
			Assert.IsFalse(provider.CanInsert(11));
			Assert.IsFalse(provider.CanInsert(12));
			Assert.IsFalse(provider.CanInsert(13));
			Assert.IsFalse(provider.CanInsert(14));
		}
		
		[Test]
		public void InsertionPossibleAtEndOfReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
			Assert.IsTrue(provider.CanInsert(15));
		}
		
		[Test]
		public void InsertionPossibleBetweenReadOnlySegments()
		{
			segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
			segments.Add(new TextSegment { StartOffset = 15, EndOffset = 20 });
			Assert.IsTrue(provider.CanInsert(15));
		}
		
		[Test]
		public void DeletionImpossibleInReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(11, 2)).ToList();
			Assert.AreEqual(0, result.Count);
		}
		
		[Test]
		public void DeletionAroundReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(15, 16)).ToList();
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(15, result[0].Offset);
			Assert.AreEqual(5, result[0].Length);
			Assert.AreEqual(25, result[1].Offset);
			Assert.AreEqual(6, result[1].Length);
		}
		
		[Test]
		public void DeleteLastCharacterInReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(24, 1)).ToList();
			Assert.AreEqual(0, result.Count);
			/* // we would need this result for the old Backspace code so that the last character doesn't get selected:
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(25, result[0].Offset);
			Assert.AreEqual(0, result[0].Length);*/
		}
		
		[Test]
		public void DeleteFirstCharacterInReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(20, 1)).ToList();
			Assert.AreEqual(0, result.Count);
			/* // we would need this result for the old Delete code so that the first character doesn't get selected:
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(2, result[0].Offset);
			Assert.AreEqual(0, result[0].Length);*/
		}
		
		[Test]
		public void DeleteWholeReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(20, 5)).ToList();
			Assert.AreEqual(0, result.Count);
		}
	}
}
