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
		public void EmptyDeletionPossibleWhenNothingIsReadOnly()
		{
			var result = provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(10, result[0].Offset);
			Assert.AreEqual(0, result[0].Length);
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
		public void EmptyDeletionImpossibleInReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(11, 0)).ToList();
			Assert.AreEqual(0, result.Count);
		}
		
		[Test]
		public void EmptyDeletionPossibleAtStartOfReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(10, result[0].Offset);
			Assert.AreEqual(0, result[0].Length);
		}
		
		[Test]
		public void EmptyDeletionPossibleAtEndOfReadOnlySegment()
		{
			segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
			var result = provider.GetDeletableSegments(new SimpleSegment(15, 0)).ToList();
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(15, result[0].Offset);
			Assert.AreEqual(0, result[0].Length);
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
