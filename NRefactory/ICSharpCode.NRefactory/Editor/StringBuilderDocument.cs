// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.IO;
using System.Text;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// Document based on a string builder.
	/// This class serves as a reference implementation for the IDocument interface.
	/// </summary>
	public class StringBuilderDocument : IDocument
	{
		readonly StringBuilder b;
		readonly TextSourceVersionProvider versionProvider = new TextSourceVersionProvider();
		
		/// <summary>
		/// Creates a new StringBuilderDocument.
		/// </summary>
		public StringBuilderDocument()
		{
			b = new StringBuilder();
		}
		
		/// <summary>
		/// Creates a new StringBuilderDocument with the specified initial text.
		/// </summary>
		public StringBuilderDocument(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			b = new StringBuilder(text);
		}
		
		/// <summary>
		/// Creates a new StringBuilderDocument with the initial text copied from the specified text source.
		/// </summary>
		public StringBuilderDocument(ITextSource textSource)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			b = new StringBuilder(textSource.TextLength);
			textSource.WriteTextTo(new StringWriter(b));
		}
		
		/// <inheritdoc/>
		public event EventHandler<TextChangeEventArgs> TextChanging;
		
		/// <inheritdoc/>
		public event EventHandler<TextChangeEventArgs> TextChanged;
		
		/// <inheritdoc/>
		public event EventHandler ChangeCompleted;
		
		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get { return versionProvider.CurrentVersion; }
		}
		
		#region Line<->Offset
		/// <inheritdoc/>
		public int LineCount {
			get { return CreateDocumentSnapshot().LineCount; }
		}
		
		/// <inheritdoc/>
		public IDocumentLine GetLineByNumber(int lineNumber)
		{
			return CreateDocumentSnapshot().GetLineByNumber(lineNumber);
		}
		
		/// <inheritdoc/>
		public IDocumentLine GetLineByOffset(int offset)
		{
			return CreateDocumentSnapshot().GetLineByOffset(offset);
		}
		
		/// <inheritdoc/>
		public int GetOffset(int line, int column)
		{
			return CreateDocumentSnapshot().GetOffset(line, column);
		}
		
		/// <inheritdoc/>
		public int GetOffset(TextLocation location)
		{
			return CreateDocumentSnapshot().GetOffset(location);
		}
		
		/// <inheritdoc/>
		public TextLocation GetLocation(int offset)
		{
			return CreateDocumentSnapshot().GetLocation(offset);
		}
		#endregion
		
		#region Insert/Remove/Replace
		/// <inheritdoc/>
		public void Insert(int offset, string text)
		{
			Replace(offset, 0, text);
		}
		
		/// <inheritdoc/>
		public void Insert(int offset, ITextSource text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			Replace(offset, 0, text.Text);
		}
		
		/// <inheritdoc/>
		public void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
		{
			if (offset < 0 || offset > this.TextLength)
				throw new ArgumentOutOfRangeException("offset");
			if (text == null)
				throw new ArgumentNullException("text");
			if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion)
				PerformChange(new InsertionWithMovementBefore(offset, text));
			else
				Replace(offset, 0, text);
		}
		
		/// <inheritdoc/>
		public void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			Insert(offset, text.Text, defaultAnchorMovementType);
		}
		
		[Serializable]
		sealed class InsertionWithMovementBefore : TextChangeEventArgs
		{
			public InsertionWithMovementBefore(int offset, string newText) : base(offset, string.Empty, newText)
			{
			}
			
			public override int GetNewOffset(int offset, AnchorMovementType movementType)
			{
				if (offset == this.Offset && movementType == AnchorMovementType.Default)
					return offset;
				else
					return base.GetNewOffset(offset, movementType);
			}
		}
		
		/// <inheritdoc/>
		public void Remove(int offset, int length)
		{
			Replace(offset, length, string.Empty);
		}
		
		/// <inheritdoc/>
		public void Replace(int offset, int length, string newText)
		{
			if (offset < 0 || offset > this.TextLength)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0 || length > this.TextLength - offset)
				throw new ArgumentOutOfRangeException("length");
			if (newText == null)
				throw new ArgumentNullException("newText");
			PerformChange(new TextChangeEventArgs(offset, b.ToString(offset, length), newText));
		}
		
		/// <inheritdoc/>
		public void Replace(int offset, int length, ITextSource newText)
		{
			if (newText == null)
				throw new ArgumentNullException("newText");
			Replace(offset, length, newText.Text);
		}
		
		bool isInChange;
		
		void PerformChange(TextChangeEventArgs change)
		{
			// Ensure that all changes take place inside an update group.
			// Will also take care of throwing an exception if isInChange is set.
			StartUndoableAction();
			try {
				isInChange = true;
				try {
					if (TextChanging != null)
						TextChanging(this, change);
					
					// Perform changes to document and Version property
					documentSnapshot = null;
					cachedText = null;
					b.Remove(change.Offset, change.RemovalLength);
					b.Insert(change.Offset, change.InsertedText.Text);
					versionProvider.AppendChange(change);
					
					// Update anchors and fire Deleted events
					UpdateAnchors(change);
					
					if (TextChanged != null)
						TextChanged(this, change);
				} finally {
					isInChange = false;
				}
			} finally {
				EndUndoableAction();
			}
		}
		#endregion
		
		#region Undo
		int undoGroupNesting = 0;
		
		/// <inheritdoc/>
		public void StartUndoableAction()
		{
			// prevent changes from within the TextChanging/TextChanged event handlers
			if (isInChange)
				throw new InvalidOperationException();
			undoGroupNesting++;
		}
		
		/// <inheritdoc/>
		public void EndUndoableAction()
		{
			undoGroupNesting--;
			if (undoGroupNesting == 0) {
				if (ChangeCompleted != null)
					ChangeCompleted(this, EventArgs.Empty);
			}
		}
		
		/// <inheritdoc/>
		public IDisposable OpenUndoGroup()
		{
			StartUndoableAction();
			return new CallbackOnDispose(EndUndoableAction);
		}
		#endregion
		
		#region CreateSnapshot/CreateReader
		ReadOnlyDocument documentSnapshot;
		
		/// <inheritdoc/>
		public IDocument CreateDocumentSnapshot()
		{
			if (documentSnapshot == null)
				documentSnapshot = new ReadOnlyDocument(this, this.FileName);
			return documentSnapshot;
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return new StringTextSource(this.Text, versionProvider.CurrentVersion);
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new StringTextSource(GetText(offset, length));
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader()
		{
			return new StringReader(this.Text);
		}
		
		/// <inheritdoc/>
		public TextReader CreateReader(int offset, int length)
		{
			return new StringReader(GetText(offset, length));
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			writer.Write(this.Text);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(TextWriter writer, int offset, int length)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			writer.Write(GetText(offset, length));
		}
		#endregion
		
		#region GetText / IndexOf
		string cachedText;
		
		/// <inheritdoc/>
		public string Text {
			get {
				if (cachedText == null)
					cachedText = b.ToString();
				return cachedText;
			}
			set {
				Replace(0, b.Length, value);
			}
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return b.Length; }
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			return b[offset];
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return b.ToString(offset, length);
		}
		
		/// <inheritdoc/>
		public string GetText(ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			return b.ToString(segment.Offset, segment.Length);
		}
		
		/// <inheritdoc/>
		public int IndexOf(char c, int startIndex, int count)
		{
			return this.Text.IndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return this.Text.IndexOfAny(anyOf, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return this.Text.IndexOf(searchText, startIndex, count, comparisonType);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(char c, int startIndex, int count)
		{
			return this.Text.LastIndexOf(c, startIndex + count - 1, count);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return this.Text.LastIndexOf(searchText, startIndex + count - 1, count, comparisonType);
		}
		#endregion
		
		#region CreateAnchor
		readonly List<WeakReference> anchors = new List<WeakReference>();
		
		/// <inheritdoc/>
		public ITextAnchor CreateAnchor(int offset)
		{
			var newAnchor = new SimpleAnchor(this, offset);
			for (int i = 0; i < anchors.Count; i++) {
				if (!anchors[i].IsAlive)
					anchors[i] = new WeakReference(newAnchor);
			}
			anchors.Add(new WeakReference(newAnchor));
			return newAnchor;
		}
		
		void UpdateAnchors(TextChangeEventArgs change)
		{
			// First update all anchors, then fire the deleted events.
			List<int> deletedAnchors = new List<int>();
			for (int i = 0; i < anchors.Count; i++) {
				var anchor = anchors[i].Target as SimpleAnchor;
				if (anchor != null) {
					anchor.Update(change);
					if (anchor.IsDeleted)
						deletedAnchors.Add(i);
				}
			}
			deletedAnchors.Reverse();
			foreach (var index in deletedAnchors) {
				var anchor = anchors[index].Target as SimpleAnchor;
				if (anchor != null)
					anchor.RaiseDeletedEvent();
				anchors.RemoveAt(index);
			}
		}
		
		sealed class SimpleAnchor : ITextAnchor
		{
			readonly StringBuilderDocument document;
			int offset;
			
			public SimpleAnchor(StringBuilderDocument document, int offset)
			{
				this.document = document;
				this.offset = offset;
			}
			
			public event EventHandler Deleted;
			
			public TextLocation Location {
				get {
					if (IsDeleted)
						throw new InvalidOperationException();
					return document.GetLocation(offset);
				}
			}
			
			public int Offset {
				get {
					if (IsDeleted)
						throw new InvalidOperationException();
					return offset;
				}
			}
			
			public AnchorMovementType MovementType { get; set; }
			
			public bool SurviveDeletion { get; set; }
			
			public bool IsDeleted {
				get { return offset < 0; }
			}
			
			public void Update(TextChangeEventArgs change)
			{
				if (SurviveDeletion || offset <= change.Offset || offset >= change.Offset + change.RemovalLength) {
					offset = change.GetNewOffset(offset, MovementType);
				} else {
					offset = -1;
				}
			}
			
			public void RaiseDeletedEvent()
			{
				if (Deleted != null)
					Deleted(this, EventArgs.Empty);
			}
			
			public int Line {
				get { return this.Location.Line; }
			}
			
			public int Column {
				get { return this.Location.Column; }
			}
		}
		#endregion
		
		/// <inheritdoc/>
		public virtual object GetService(Type serviceType)
		{
			return null;
		}
		
		/// <inheritdoc/>
		public virtual event EventHandler FileNameChanged { add {} remove {} }
		
		/// <inheritdoc/>
		public virtual string FileName {
			get { return string.Empty; }
		}
	}
}
