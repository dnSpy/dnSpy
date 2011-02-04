// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Document
{
	[TestFixture]
	public class TextUtilitiesTests
	{
		#region GetWhitespaceAfter
		[Test]
		public void TestGetWhitespaceAfter()
		{
			Assert.AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
		}
		
		[Test]
		public void TestGetWhitespaceAfterDoesNotSkipNewLine()
		{
			Assert.AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
		}
		
		[Test]
		public void TestGetWhitespaceAfterEmptyResult()
		{
			Assert.AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceAfter(new StringTextSource("a b"), 2));
		}
		
		[Test]
		public void TestGetWhitespaceAfterEndOfString()
		{
			Assert.AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceAfter(new StringTextSource("a "), 2));
		}
		
		[Test]
		public void TestGetWhitespaceAfterUntilEndOfString()
		{
			Assert.AreEqual(new SimpleSegment(2, 3), TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \t"), 2));
		}
		#endregion
		
		#region GetWhitespaceBefore
		[Test]
		public void TestGetWhitespaceBefore()
		{
			Assert.AreEqual(new SimpleSegment(1, 3), TextUtilities.GetWhitespaceBefore(new StringTextSource("a\t \t b"), 4));
		}
		
		[Test]
		public void TestGetWhitespaceBeforeDoesNotSkipNewLine()
		{
			Assert.AreEqual(new SimpleSegment(2, 1), TextUtilities.GetWhitespaceBefore(new StringTextSource("a\n b"), 3));
		}
		
		[Test]
		public void TestGetWhitespaceBeforeEmptyResult()
		{
			Assert.AreEqual(new SimpleSegment(2, 0), TextUtilities.GetWhitespaceBefore(new StringTextSource(" a b"), 2));
		}
		
		[Test]
		public void TestGetWhitespaceBeforeStartOfString()
		{
			Assert.AreEqual(new SimpleSegment(0, 0), TextUtilities.GetWhitespaceBefore(new StringTextSource(" a"), 0));
		}
		
		[Test]
		public void TestGetWhitespaceBeforeUntilStartOfString()
		{
			Assert.AreEqual(new SimpleSegment(0, 2), TextUtilities.GetWhitespaceBefore(new StringTextSource(" \t a"), 2));
		}
		#endregion
	}
}
