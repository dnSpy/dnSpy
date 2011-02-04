// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Utils
{
	[TestFixture]
	public class CompressingTreeListTests
	{
		[Test]
		public void EmptyTreeList()
		{
			CompressingTreeList<string> list = new CompressingTreeList<string>(string.Equals);
			Assert.AreEqual(0, list.Count);
			foreach (string v in list) {
				Assert.Fail();
			}
			string[] arr = new string[0];
			list.CopyTo(arr, 0);
		}
		
		[Test]
		public void CheckAdd10BillionElements()
		{
			const int billion = 1000000000;
			CompressingTreeList<string> list = new CompressingTreeList<string>(string.Equals);
			list.InsertRange(0, billion, "A");
			list.InsertRange(1, billion, "B");
			Assert.AreEqual(2 * billion, list.Count);
			try {
				list.InsertRange(2, billion, "C");
				Assert.Fail("Expected OverflowException");
			} catch (OverflowException) {
				// expected
			}
		}
		
		[Test]
		public void AddRepeated()
		{
			CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
			list.Add(42);
			list.Add(42);
			list.Add(42);
			list.Insert(0, 42);
			list.Insert(1, 42);
			Assert.AreEqual(new[] { 42, 42, 42, 42, 42 }, list.ToArray());
		}
		
		[Test]
		public void RemoveRange()
		{
			CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
			for (int i = 1; i <= 3; i++) {
				list.InsertRange(list.Count, 2, i);
			}
			Assert.AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
			list.RemoveRange(1, 4);
			Assert.AreEqual(new[] { 1, 3 }, list.ToArray());
			list.Insert(1, 1);
			list.InsertRange(2, 2, 2);
			list.Insert(4, 1);
			Assert.AreEqual(new[] { 1, 1, 2, 2, 1, 3 }, list.ToArray());
			list.RemoveRange(2, 2);
			Assert.AreEqual(new[] { 1, 1, 1, 3 }, list.ToArray());
		}
		
		[Test]
		public void RemoveAtEnd()
		{
			CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
			for (int i = 1; i <= 3; i++) {
				list.InsertRange(list.Count, 2, i);
			}
			Assert.AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
			list.RemoveRange(3, 3);
			Assert.AreEqual(new[] { 1, 1, 2 }, list.ToArray());
		}
		
		[Test]
		public void RemoveAtStart()
		{
			CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
			for (int i = 1; i <= 3; i++) {
				list.InsertRange(list.Count, 2, i);
			}
			Assert.AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
			list.RemoveRange(0, 1);
			Assert.AreEqual(new[] { 1, 2, 2, 3, 3 }, list.ToArray());
		}
		
		[Test]
		public void RemoveAtStart2()
		{
			CompressingTreeList<int> list = new CompressingTreeList<int>((a, b) => a == b);
			for (int i = 1; i <= 3; i++) {
				list.InsertRange(list.Count, 2, i);
			}
			Assert.AreEqual(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
			list.RemoveRange(0, 3);
			Assert.AreEqual(new[] { 2, 3, 3 }, list.ToArray());
		}
	}
}
