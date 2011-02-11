// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory;

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// Wraps the AvalonEdit TextDocument to provide the IDocument interface.
	/// </summary>
	public class AvalonEditDocumentAdapter : IDocument
	{
		internal readonly TextDocument document;
		
		/// <summary>
		/// Creates a new AvalonEditDocumentAdapter instance.
		/// </summary>
		/// <param name="document">The document to wrap.</param>
		public AvalonEditDocumentAdapter(TextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.document = document;
		}
		
		/// <summary>
		/// Creates a new document.
		/// </summary>
		public AvalonEditDocumentAdapter()
		{
			this.document = new TextDocument();
		}
		
		sealed class LineAdapter : IDocumentLine
		{
			readonly TextDocument document;
			readonly DocumentLine line;
			
			public LineAdapter(TextDocument document, DocumentLine line)
			{
				Debug.Assert(document != null);
				Debug.Assert(line != null);
				this.document = document;
				this.line = line;
			}
			
			public int Offset {
				get { return line.Offset; }
			}
			
			public int Length {
				get { return line.Length; }
			}
			
			public int EndOffset {
				get { return line.EndOffset; }
			}
			
			public int TotalLength {
				get { return line.TotalLength; }
			}
			
			public int DelimiterLength {
				get { return line.DelimiterLength; }
			}
			
			public int LineNumber {
				get { return line.LineNumber; }
			}
			
			public string Text {
				get { return document.GetText(line); }
			}
		}
		
		public int TextLength {
			get { return document.TextLength; }
		}
		
		public int TotalNumberOfLines {
			get { return document.LineCount; }
		}
		
		public string Text {
			get { return document.Text; }
			set { document.Text = value; }
		}
		
		public event EventHandler TextChanged {
			add { document.TextChanged += value; }
			remove { document.TextChanged -= value; }
		}
		
		public IDocumentLine GetLine(int lineNumber)
		{
			return new LineAdapter(document, document.GetLineByNumber(lineNumber));
		}
		
		public IDocumentLine GetLineForOffset(int offset)
		{
			return new LineAdapter(document, document.GetLineByOffset(offset));
		}
		
		public int PositionToOffset(int line, int column)
		{
			try {
				return document.GetOffset(new TextLocation(line, column));
			} catch (ArgumentOutOfRangeException e) {
				// for UDC: re-throw exception so that stack trace identifies the caller (instead of the adapter)
				throw new ArgumentOutOfRangeException(e.ParamName, e.ActualValue, e.Message);
			}
		}
		
		public Location OffsetToPosition(int offset)
		{
			try {
				return ToLocation(document.GetLocation(offset));
			} catch (ArgumentOutOfRangeException e) {
				// for UDC: re-throw exception so that stack trace identifies the caller (instead of the adapter)
				throw new ArgumentOutOfRangeException(e.ParamName, e.ActualValue, e.Message);
			}
		}
		
		public static Location ToLocation(TextLocation position)
		{
			return new Location(position.Column, position.Line);
		}
		
		public static TextLocation ToPosition(Location location)
		{
			return new TextLocation(location.Line, location.Column);
		}
		
		public void Insert(int offset, string text)
		{
			document.Insert(offset, text);
		}
		
		public void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
		{
			if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion) {
				document.Replace(offset, 0, text, OffsetChangeMappingType.KeepAnchorBeforeInsertion);
			} else {
				document.Insert(offset, text);
			}
		}
		
		public void Remove(int offset, int length)
		{
			document.Remove(offset, length);
		}
		
		public void Replace(int offset, int length, string newText)
		{
			document.Replace(offset, length, newText);
		}
		
		public void Replace(int offset, int length, string newText, AnchorMovementType defaultAnchorMovementType)
		{
			document.Replace(offset, length, newText);
		}
		
		public char GetCharAt(int offset)
		{
			return document.GetCharAt(offset);
		}
		
		public string GetText(int offset, int length)
		{
			return document.GetText(offset, length);
		}
		
		public System.IO.TextReader CreateReader()
		{
			return document.CreateSnapshot().CreateReader();
		}
		
		public System.IO.TextReader CreateReader(int offset, int length)
		{
			return document.CreateSnapshot(offset, length).CreateReader();
		}
		
		#region Snapshots and ITextBufferVersion
		public ITextBuffer CreateSnapshot()
		{
			ChangeTrackingCheckpoint checkpoint;
			ITextSource textSource = document.CreateSnapshot(out checkpoint);
			return new Snapshot(textSource, checkpoint);
		}
		
		public ITextBuffer CreateSnapshot(int offset, int length)
		{
			return new AvalonEditTextSourceAdapter(document.CreateSnapshot(offset, length));
		}
		
		public ITextBufferVersion Version {
			get {
				return new SnapshotVersion(ChangeTrackingCheckpoint.Create(document));
			}
		}
		
		sealed class Snapshot : AvalonEditTextSourceAdapter
		{
			readonly ITextBufferVersion version;
			
			public Snapshot(ITextSource textSource, ChangeTrackingCheckpoint checkpoint)
				: base(textSource)
			{
				this.version = new SnapshotVersion(checkpoint);
			}
			
			public override ITextBuffer CreateSnapshot()
			{
				// Snapshot is immutable
				return this;
			}
			
			public override ITextBufferVersion Version {
				get { return version; }
			}
		}
		
		sealed class SnapshotVersion : ITextBufferVersion
		{
			readonly ChangeTrackingCheckpoint checkpoint;
			
			public SnapshotVersion(ChangeTrackingCheckpoint checkpoint)
			{
				Debug.Assert(checkpoint != null);
				this.checkpoint = checkpoint;
			}
			
			public bool BelongsToSameDocumentAs(ITextBufferVersion other)
			{
				SnapshotVersion otherVersion = other as SnapshotVersion;
				return otherVersion != null && checkpoint.BelongsToSameDocumentAs(otherVersion.checkpoint);
			}
			
			public int CompareAge(ITextBufferVersion other)
			{
				SnapshotVersion otherVersion = other as SnapshotVersion;
				if (otherVersion == null)
					throw new ArgumentException("Does not belong to same document");
				return checkpoint.CompareAge(otherVersion.checkpoint);
			}
			
			public IEnumerable<TextChangeEventArgs> GetChangesTo(ITextBufferVersion other)
			{
				SnapshotVersion otherVersion = other as SnapshotVersion;
				if (otherVersion == null)
					throw new ArgumentException("Does not belong to same document");
				return checkpoint.GetChangesTo(otherVersion.checkpoint).Select(c => new TextChangeEventArgs(c.Offset, c.RemovedText, c.InsertedText));
			}
			
			public int MoveOffsetTo(ITextBufferVersion other, int oldOffset, AnchorMovementType movement)
			{
				SnapshotVersion otherVersion = other as SnapshotVersion;
				if (otherVersion == null)
					throw new ArgumentException("Does not belong to same document");
				switch (movement) {
					case AnchorMovementType.AfterInsertion:
						return checkpoint.MoveOffsetTo(otherVersion.checkpoint, oldOffset, ICSharpCode.AvalonEdit.Document.AnchorMovementType.AfterInsertion);
					case AnchorMovementType.BeforeInsertion:
						return checkpoint.MoveOffsetTo(otherVersion.checkpoint, oldOffset, ICSharpCode.AvalonEdit.Document.AnchorMovementType.BeforeInsertion);
					default:
						throw new NotSupportedException();
				}
			}
		}
		#endregion
		
		public void StartUndoableAction()
		{
			document.BeginUpdate();
		}
		
		public void EndUndoableAction()
		{
			document.EndUpdate();
		}
		
		public IDisposable OpenUndoGroup()
		{
			return document.RunUpdate();
		}
		
		public ITextAnchor CreateAnchor(int offset)
		{
			return new AnchorAdapter(document.CreateAnchor(offset));
		}
		
		#region AnchorAdapter
		sealed class AnchorAdapter : ITextAnchor
		{
			readonly TextAnchor anchor;
			
			public AnchorAdapter(TextAnchor anchor)
			{
				this.anchor = anchor;
			}
			
			#region Forward Deleted Event
			EventHandler deleted;
			
			public event EventHandler Deleted {
				add {
					// we cannot simply forward the event handler because
					// that would raise the event with an incorrect sender
					if (deleted == null && value != null)
						anchor.Deleted += OnDeleted;
					deleted += value;
				}
				remove {
					deleted -= value;
					if (deleted == null)
						anchor.Deleted -= OnDeleted;
				}
			}
			
			void OnDeleted(object sender, EventArgs e)
			{
				// raise event with correct sender
				if (deleted != null)
					deleted(this, e);
			}
			#endregion
			
			public Location Location {
				get { return ToLocation(anchor.Location); }
			}
			
			public int Offset {
				get { return anchor.Offset; }
			}
			
			public ILSpy.Debugger.AvalonEdit.Editor.AnchorMovementType MovementType {
				get {
					return (ILSpy.Debugger.AvalonEdit.Editor.AnchorMovementType)anchor.MovementType;
				}
				set {
					anchor.MovementType = (ICSharpCode.AvalonEdit.Document.AnchorMovementType)value;
				}
			}
			
			public bool SurviveDeletion {
				get { return anchor.SurviveDeletion; }
				set { anchor.SurviveDeletion = value; }
			}
			
			public bool IsDeleted {
				get { return anchor.IsDeleted; }
			}
			
			public int Line {
				get { return anchor.Line; }
			}
			
			public int Column {
				get { return anchor.Column; }
			}
		}
		#endregion
		
		#region Changing/Changed events
		EventHandler<TextChangeEventArgs> changing, changed;
		bool eventsAreAttached;
		
		void AttachEvents()
		{
			if (!eventsAreAttached && (changing != null || changed != null)) {
				eventsAreAttached = true;
				document.Changing += document_Changing;
				document.Changed += document_Changed;
			}
		}
		
		void DetachEvents()
		{
			if (eventsAreAttached && changing == null && changed == null) {
				eventsAreAttached = false;
				document.Changing -= document_Changing;
				document.Changed -= document_Changed;
			}
		}
		
		void document_Changing(object sender, DocumentChangeEventArgs e)
		{
			if (changing != null)
				changing(this, new TextChangeEventArgs(e.Offset, e.RemovedText, e.InsertedText));
		}
		
		void document_Changed(object sender, DocumentChangeEventArgs e)
		{
			if (changed != null)
				changed(this, new TextChangeEventArgs(e.Offset, e.RemovedText, e.InsertedText));
		}
		
		public event EventHandler<TextChangeEventArgs> Changing {
			add {
				changing += value;
				AttachEvents();
			}
			remove {
				changing -= value;
				DetachEvents();
			}
		}
		
		public event EventHandler<TextChangeEventArgs> Changed {
			add {
				changed += value;
				AttachEvents();
			}
			remove {
				changed -= value;
				DetachEvents();
			}
		}
		#endregion
	}
}
