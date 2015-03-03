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
#if NREFACTORY
using ICSharpCode.NRefactory.Editor;
#endif
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
			ITextSource snapshot1 = document.CreateSnapshot();
			ITextSource snapshot2 = document.CreateSnapshot();
			Assert.AreEqual(0, snapshot1.Version.CompareAge(snapshot2.Version));
			Assert.AreEqual(0, snapshot1.Version.GetChangesTo(snapshot2.Version).Count());
			Assert.AreEqual(document.Text, snapshot1.Text);
			Assert.AreEqual(document.Text, snapshot2.Text);
		}
		
		[Test]
		public void ForwardChanges()
		{
			TextDocument document = new TextDocument("initial text");
			ITextSource snapshot1 = document.CreateSnapshot();
			document.Replace(0, 7, "nw");
			document.Insert(1, "e");
			ITextSource snapshot2 = document.CreateSnapshot();
			Assert.AreEqual(-1, snapshot1.Version.CompareAge(snapshot2.Version));
			TextChangeEventArgs[] arr = snapshot1.Version.GetChangesTo(snapshot2.Version).ToArray();
			Assert.AreEqual(2, arr.Length);
			Assert.AreEqual("nw", arr[0].InsertedText.Text);
			Assert.AreEqual("e", arr[1].InsertedText.Text);
			
			Assert.AreEqual("initial text", snapshot1.Text);
			Assert.AreEqual("new text", snapshot2.Text);
		}
		
		[Test]
		public void BackwardChanges()
		{
			TextDocument document = new TextDocument("initial text");
			ITextSource snapshot1 = document.CreateSnapshot();
			document.Replace(0, 7, "nw");
			document.Insert(1, "e");
			ITextSource snapshot2 = document.CreateSnapshot();
			Assert.AreEqual(1, snapshot2.Version.CompareAge(snapshot1.Version));
			TextChangeEventArgs[] arr = snapshot2.Version.GetChangesTo(snapshot1.Version).ToArray();
			Assert.AreEqual(2, arr.Length);
			Assert.AreEqual("", arr[0].InsertedText.Text);
			Assert.AreEqual("initial", arr[1].InsertedText.Text);
			
			Assert.AreEqual("initial text", snapshot1.Text);
			Assert.AreEqual("new text", snapshot2.Text);
		}
	}
}
