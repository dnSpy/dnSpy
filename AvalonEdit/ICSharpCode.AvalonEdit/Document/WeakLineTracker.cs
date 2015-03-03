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

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Allows registering a line tracker on a TextDocument using a weak reference from the document to the line tracker.
	/// </summary>
	public sealed class WeakLineTracker : ILineTracker
	{
		TextDocument textDocument;
		WeakReference targetObject;
		
		private WeakLineTracker(TextDocument textDocument, ILineTracker targetTracker)
		{
			this.textDocument = textDocument;
			this.targetObject = new WeakReference(targetTracker);
		}
		
		/// <summary>
		/// Registers the <paramref name="targetTracker"/> as line tracker for the <paramref name="textDocument"/>.
		/// A weak reference to the target tracker will be used, and the WeakLineTracker will deregister itself
		/// when the target tracker is garbage collected.
		/// </summary>
		public static WeakLineTracker Register(TextDocument textDocument, ILineTracker targetTracker)
		{
			if (textDocument == null)
				throw new ArgumentNullException("textDocument");
			if (targetTracker == null)
				throw new ArgumentNullException("targetTracker");
			WeakLineTracker wlt = new WeakLineTracker(textDocument, targetTracker);
			textDocument.LineTrackers.Add(wlt);
			return wlt;
		}
		
		/// <summary>
		/// Deregisters the weak line tracker.
		/// </summary>
		public void Deregister()
		{
			if (textDocument != null) {
				textDocument.LineTrackers.Remove(this);
				textDocument = null;
			}
		}
		
		void ILineTracker.BeforeRemoveLine(DocumentLine line)
		{
			ILineTracker targetTracker = targetObject.Target as ILineTracker;
			if (targetTracker != null)
				targetTracker.BeforeRemoveLine(line);
			else
				Deregister();
		}
		
		void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
		{
			ILineTracker targetTracker = targetObject.Target as ILineTracker;
			if (targetTracker != null)
				targetTracker.SetLineLength(line, newTotalLength);
			else
				Deregister();
		}
		
		void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
		{
			ILineTracker targetTracker = targetObject.Target as ILineTracker;
			if (targetTracker != null)
				targetTracker.LineInserted(insertionPos, newLine);
			else
				Deregister();
		}
		
		void ILineTracker.RebuildDocument()
		{
			ILineTracker targetTracker = targetObject.Target as ILineTracker;
			if (targetTracker != null)
				targetTracker.RebuildDocument();
			else
				Deregister();
		}
		
		void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
		{
			ILineTracker targetTracker = targetObject.Target as ILineTracker;
			if (targetTracker != null)
				targetTracker.ChangeComplete(e);
			else
				Deregister();
		}
	}
}
