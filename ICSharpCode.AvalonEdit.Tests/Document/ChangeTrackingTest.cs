// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Document
{
	[TestFixture]
	public class ChangeTrackingTest
	{
		[Test]
		public void NoChanges()
		{
			TextDocument document = new TextDocument("initial text");
			ChangeTrackingCheckpoint checkpoint1, checkpoint2;
			ITextSource snapshot1 = document.CreateSnapshot(out checkpoint1);
			ITextSource snapshot2 = document.CreateSnapshot(out checkpoint2);
			Assert.AreEqual(0, checkpoint1.CompareAge(checkpoint2));
			Assert.AreEqual(0, checkpoint1.GetChangesTo(checkpoint2).Count());
			Assert.AreEqual(document.Text, snapshot1.Text);
			Assert.AreEqual(document.Text, snapshot2.Text);
		}
		
		[Test]
		public void ForwardChanges()
		{
			TextDocument document = new TextDocument("initial text");
			ChangeTrackingCheckpoint checkpoint1, checkpoint2;
			ITextSource snapshot1 = document.CreateSnapshot(out checkpoint1);
			document.Replace(0, 7, "nw");
			document.Insert(1, "e");
			ITextSource snapshot2 = document.CreateSnapshot(out checkpoint2);
			Assert.AreEqual(-1, checkpoint1.CompareAge(checkpoint2));
			DocumentChangeEventArgs[] arr = checkpoint1.GetChangesTo(checkpoint2).ToArray();
			Assert.AreEqual(2, arr.Length);
			Assert.AreEqual("nw", arr[0].InsertedText);
			Assert.AreEqual("e", arr[1].InsertedText);
			
			Assert.AreEqual("initial text", snapshot1.Text);
			Assert.AreEqual("new text", snapshot2.Text);
		}
		
		[Test]
		public void BackwardChanges()
		{
			TextDocument document = new TextDocument("initial text");
			ChangeTrackingCheckpoint checkpoint1, checkpoint2;
			ITextSource snapshot1 = document.CreateSnapshot(out checkpoint1);
			document.Replace(0, 7, "nw");
			document.Insert(1, "e");
			ITextSource snapshot2 = document.CreateSnapshot(out checkpoint2);
			Assert.AreEqual(1, checkpoint2.CompareAge(checkpoint1));
			DocumentChangeEventArgs[] arr = checkpoint2.GetChangesTo(checkpoint1).ToArray();
			Assert.AreEqual(2, arr.Length);
			Assert.AreEqual("", arr[0].InsertedText);
			Assert.AreEqual("initial", arr[1].InsertedText);
			
			Assert.AreEqual("initial text", snapshot1.Text);
			Assert.AreEqual("new text", snapshot2.Text);
		}
	}
}
