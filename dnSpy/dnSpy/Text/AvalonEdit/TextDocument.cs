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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace dnSpy.Text.AvalonEdit {
	/// <summary>
	/// This class is the main class of the text model. Basically, it is a <see cref="System.Text.StringBuilder"/> with events.
	/// </summary>
	/// <remarks>
	/// <b>Thread safety:</b>
	/// <inheritdoc cref="VerifyAccess"/>
	/// <para>However, there is a single method that is thread-safe: <see cref="CreateSnapshot()"/> (and its overloads).</para>
	/// </remarks>
	sealed class TextDocument : ITextSource {
		#region Thread ownership
		readonly object lockObject = new object();
		Thread owner = Thread.CurrentThread;

		/// <summary>
		/// Verifies that the current thread is the documents owner thread.
		/// Throws an <see cref="InvalidOperationException"/> if the wrong thread accesses the TextDocument.
		/// </summary>
		/// <remarks>
		/// <para>The TextDocument class is not thread-safe. A document instance expects to have a single owner thread
		/// and will throw an <see cref="InvalidOperationException"/> when accessed from another thread.
		/// It is possible to change the owner thread using the <see cref="SetOwnerThread"/> method.</para>
		/// </remarks>
		public void VerifyAccess() {
			if (owner != null && Thread.CurrentThread != owner)
				throw new InvalidOperationException("TextDocument can be accessed only from the thread that owns it.");
		}

		/// <summary>
		/// Transfers ownership of the document to another thread. This method can be used to load
		/// a file into a TextDocument on a background thread and then transfer ownership to the UI thread
		/// for displaying the document.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="VerifyAccess"/>
		/// <para>
		/// The owner can be set to null, which means that no thread can access the document. But, if the document
		/// has no owner thread, any thread may take ownership by calling <see cref="SetOwnerThread"/>.
		/// </para>
		/// </remarks>
		public void SetOwnerThread(Thread newOwner) {
			// We need to lock here to ensure that in the null owner case,
			// only one thread succeeds in taking ownership.
			lock (lockObject) {
				if (owner != null) {
					VerifyAccess();
				}
				owner = newOwner;
			}
		}
		#endregion

		#region Fields + Constructor
		readonly Rope<char> rope;
		readonly DocumentLineTree lineTree;
		readonly LineManager lineManager;

		public int LineCount => lineTree.LineCount;
		public IList<DocumentLine> Lines => lineTree;
		public DocumentLine GetLineByNumber(int lineNumber) {
			if (lineNumber < 1 || lineNumber > lineTree.LineCount)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			return lineTree.GetByNumber(lineNumber);
		}
		public DocumentLine GetLineByOffset(int offset) {
			if ((uint)offset > (uint)rope.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			return lineTree.GetByOffset(offset);
		}

		/// <summary>
		/// Create a new text document with the specified initial text.
		/// </summary>
		public TextDocument(IEnumerable<char> initialText) {
			if (initialText == null)
				throw new ArgumentNullException("initialText");
			rope = new Rope<char>(initialText);
			lineTree = new DocumentLineTree(this);
			lineManager = new LineManager(lineTree, this);

			FireChangeEvents();
		}
		#endregion

		#region Text
		void ThrowIfRangeInvalid(int offset, int length) {
			if (offset < 0 || offset > rope.Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
			if (length < 0 || offset + length > rope.Length) {
				throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset(" + offset + ")+length <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
		}

		/// <inheritdoc/>
		public string GetText(int offset, int length) {
			VerifyAccess();
			return rope.ToString(offset, length);
		}

		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count) {
			DebugVerifyAccess(); // frequently called (NewLineFinder), so must be fast in release builds
			return rope.IndexOfAny(anyOf, startIndex, count);
		}

		/// <inheritdoc/>
		public char GetCharAt(int offset) {
			DebugVerifyAccess(); // frequently called, so must be fast in release builds
			return rope[offset];
		}

		/// <inheritdoc/>
		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => rope.CopyTo(sourceIndex, destination, destinationIndex, count);

		/// <inheritdoc/>
		public char[] ToCharArray(int startIndex, int length) {
			var array = new char[length];
			CopyTo(startIndex, array, 0, length);
			return array;
		}

		WeakReference cachedText;

		/// <summary>
		/// Gets/Sets the text of the whole document.
		/// </summary>
		public string Text {
			get {
				VerifyAccess();
				string completeText = cachedText != null ? (cachedText.Target as string) : null;
				if (completeText == null) {
					completeText = rope.ToString();
					cachedText = new WeakReference(completeText);
				}
				return completeText;
			}
		}

		/// <inheritdoc/>
		public int TextLength {
			get {
				VerifyAccess();
				return rope.Length;
			}
		}

		/// <summary>
		/// Creates a snapshot of the current text.
		/// </summary>
		/// <remarks>
		/// <para>This method returns an immutable snapshot of the document, and may be safely called even when
		/// the document's owner thread is concurrently modifying the document.
		/// </para><para>
		/// This special thread-safety guarantee is valid only for TextDocument.CreateSnapshot(), not necessarily for other
		/// classes implementing ITextSource.CreateSnapshot().
		/// </para><para>
		/// </para>
		/// </remarks>
		public ITextSource CreateSnapshot() {
			lock (lockObject) {
				return new RopeTextSource(rope);
			}
		}

		/// <inheritdoc/>
		public void WriteTextTo(System.IO.TextWriter writer) {
			VerifyAccess();
			rope.WriteTo(writer, 0, rope.Length);
		}

		/// <inheritdoc/>
		public void WriteTextTo(System.IO.TextWriter writer, int offset, int length) {
			VerifyAccess();
			rope.WriteTo(writer, offset, length);
		}
		#endregion

		#region BeginUpdate / EndUpdate
		int beginUpdateCount;

		/// <summary>
		/// <para>Begins a group of document changes.</para>
		/// <para>Some events are suspended until EndUpdate is called.</para>
		/// <para>Calling BeginUpdate several times increments a counter, only after the appropriate number
		/// of EndUpdate calls the events resume their work.</para>
		/// </summary>
		public void BeginUpdate() {
			VerifyAccess();
			if (inDocumentChanging)
				throw new InvalidOperationException("Cannot change document within another document change.");
			beginUpdateCount++;
			if (beginUpdateCount == 1) {
			}
		}

		/// <summary>
		/// Ends a group of document changes.
		/// </summary>
		public void EndUpdate() {
			VerifyAccess();
			if (inDocumentChanging)
				throw new InvalidOperationException("Cannot end update within document change.");
			if (beginUpdateCount == 0)
				throw new InvalidOperationException("No update is active.");
			if (beginUpdateCount == 1) {
				// fire change events inside the change group - event handlers might add additional
				// document changes to the change group
				FireChangeEvents();
				beginUpdateCount = 0;
			}
			else {
				beginUpdateCount -= 1;
			}
		}
		#endregion

		#region Fire events after update
		int oldTextLength;
		int oldLineCount;
		bool fireTextChanged;

		/// <summary>
		/// Fires TextChanged, TextLengthChanged, LineCountChanged if required.
		/// </summary>
		internal void FireChangeEvents() {
			// it may be necessary to fire the event multiple times if the document is changed
			// from inside the event handlers
			while (fireTextChanged) {
				fireTextChanged = false;

				int textLength = rope.Length;
				if (textLength != oldTextLength)
					oldTextLength = textLength;
				int lineCount = lineTree.LineCount;
				if (lineCount != oldLineCount)
					oldLineCount = lineCount;
			}
		}
		#endregion

		#region Insert / Remove  / Replace
		internal bool inDocumentChanging;

		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="text">The new text.</param>
		public void Replace(int offset, int length, string text) {
			if (text == null)
				throw new ArgumentNullException("text");
			var textSource = new StringTextSource(text);

			// Ensure that all changes take place inside an update group.
			// Will also take care of throwing an exception if inDocumentChanging is set.
			BeginUpdate();
			try {
				// protect document change against corruption by other changes inside the event handlers
				inDocumentChanging = true;
				try {
					// The range verification must wait until after the BeginUpdate() call because the document
					// might be modified inside the UpdateStarted event.
					ThrowIfRangeInvalid(offset, length);

					DoReplace(offset, length, textSource);
				}
				finally {
					inDocumentChanging = false;
				}
			}
			finally {
				EndUpdate();
			}
		}

		void DoReplace(int offset, int length, ITextSource newText) {
			if (length == 0 && newText.TextLength == 0)
				return;

			ITextSource removedText;
			if (length == 0) {
				removedText = StringTextSource.Empty;
			}
			else if (length < 100) {
				removedText = new StringTextSource(rope.ToString(offset, length));
			}
			else {
				// use a rope if the removed string is long
				removedText = new RopeTextSource(rope.GetRange(offset, length));
			}

			cachedText = null; // reset cache of complete document text
			fireTextChanged = true;

			lock (lockObject) {
				// now update the textBuffer and lineTree
				if (offset == 0 && length == rope.Length) {
					// optimize replacing the whole document
					rope.Clear();
					if (newText is RopeTextSource newRopeTextSource)
						rope.InsertRange(0, newRopeTextSource.GetRope());
					else
						rope.InsertText(0, newText.Text);
					lineManager.Rebuild();
				}
				else {
					rope.RemoveRange(offset, length);
					lineManager.Remove(offset, length);
#if DEBUG
					lineTree.CheckProperties();
#endif
					if (newText is RopeTextSource newRopeTextSource)
						rope.InsertRange(offset, newRopeTextSource.GetRope());
					else
						rope.InsertText(offset, newText.Text);
					lineManager.Insert(offset, newText);
#if DEBUG
					lineTree.CheckProperties();
#endif
				}
			}
		}
		#endregion

		#region Debugging
		[Conditional("DEBUG")]
		internal void DebugVerifyAccess() => VerifyAccess();
		#endregion
	}
}
