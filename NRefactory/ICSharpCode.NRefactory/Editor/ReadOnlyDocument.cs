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

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// Read-only implementation of <see cref="IDocument"/>.
	/// </summary>
	[Serializable]
	public sealed class ReadOnlyDocument : IDocument
	{
		readonly ITextSource textSource;
		readonly string fileName;
		int[] lines;
		
		static readonly char[] newline = { '\r', '\n' };
		
		/// <summary>
		/// Creates a new ReadOnlyDocument from the given text source.
		/// </summary>
		public ReadOnlyDocument(ITextSource textSource)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			// ensure that underlying buffer is immutable
			this.textSource = textSource.CreateSnapshot();
			List<int> lines = new List<int>();
			lines.Add(0);
			int offset = 0;
			int textLength = textSource.TextLength;
			while ((offset = textSource.IndexOfAny(newline, offset, textLength - offset)) >= 0) {
				offset++;
				if (textSource.GetCharAt(offset - 1) == '\r' && offset < textLength && textSource.GetCharAt(offset) == '\n') {
					offset++;
				}
				lines.Add(offset);
			}
			this.lines = lines.ToArray();
		}
		
		/// <summary>
		/// Creates a new ReadOnlyDocument from the given string.
		/// </summary>
		public ReadOnlyDocument(string text)
			: this(new StringTextSource(text))
		{
		}
		
		/// <summary>
		/// Creates a new ReadOnlyDocument from the given text source;
		/// and sets IDocument.FileName to the specified file name.
		/// </summary>
		public ReadOnlyDocument(ITextSource textSource, string fileName)
			: this(textSource)
		{
			this.fileName = fileName;
		}
		
		/// <inheritdoc/>
		public IDocumentLine GetLineByNumber(int lineNumber)
		{
			if (lineNumber < 1 || lineNumber > lines.Length)
				throw new ArgumentOutOfRangeException("lineNumber", lineNumber, "Value must be between 1 and " + lines.Length);
			return new ReadOnlyDocumentLine(this, lineNumber);
		}
		
		sealed class ReadOnlyDocumentLine : IDocumentLine
		{
			readonly ReadOnlyDocument doc;
			readonly int lineNumber;
			readonly int offset, endOffset;
			
			public ReadOnlyDocumentLine(ReadOnlyDocument doc, int lineNumber)
			{
				this.doc = doc;
				this.lineNumber = lineNumber;
				this.offset = doc.GetStartOffset(lineNumber);
				this.endOffset = doc.GetEndOffset(lineNumber);
			}
			
			public override int GetHashCode()
			{
				return doc.GetHashCode() ^ lineNumber;
			}
			
			public override bool Equals(object obj)
			{
				ReadOnlyDocumentLine other = obj as ReadOnlyDocumentLine;
				return other != null && doc == other.doc && lineNumber == other.lineNumber;
			}
			
			public int Offset {
				get { return offset; }
			}
			
			public int Length {
				get { return endOffset - offset; }
			}
			
			public int EndOffset {
				get { return endOffset; }
			}
			
			public int TotalLength {
				get {
					return doc.GetTotalEndOffset(lineNumber) - offset;
				}
			}
			
			public int DelimiterLength {
				get {
					return doc.GetTotalEndOffset(lineNumber) - endOffset;
				}
			}
			
			public int LineNumber {
				get { return lineNumber; }
			}
			
			public IDocumentLine PreviousLine {
				get {
					if (lineNumber == 1)
						return null;
					else
						return new ReadOnlyDocumentLine(doc, lineNumber - 1);
				}
			}
			
			public IDocumentLine NextLine {
				get {
					if (lineNumber == doc.LineCount)
						return null;
					else
						return new ReadOnlyDocumentLine(doc, lineNumber + 1);
				}
			}
			
			public bool IsDeleted {
				get { return false; }
			}
		}
		
		int GetStartOffset(int lineNumber)
		{
			return lines[lineNumber-1];
		}
		
		int GetTotalEndOffset(int lineNumber)
		{
			return lineNumber < lines.Length ? lines[lineNumber] : textSource.TextLength;
		}
		
		int GetEndOffset(int lineNumber)
		{
			if (lineNumber == lines.Length)
				return textSource.TextLength;
			int off = lines[lineNumber] - 1;
			if (off > 0 && textSource.GetCharAt(off - 1) == '\r' && textSource.GetCharAt(off) == '\n')
				off--;
			return off;
		}
		
		/// <inheritdoc/>
		public IDocumentLine GetLineByOffset(int offset)
		{
			return GetLineByNumber(GetLineNumberForOffset(offset));
		}
		
		int GetLineNumberForOffset(int offset)
		{
			int r = Array.BinarySearch(lines, offset);
			return r < 0 ? ~r : r + 1;
		}
		
		/// <inheritdoc/>
		public int GetOffset(int line, int column)
		{
			if (line < 1 || line > lines.Length)
				throw new ArgumentOutOfRangeException("line", line, "Value must be between 1 and " + lines.Length);
			int lineStart = GetStartOffset(line);
			if (column <= 1)
				return lineStart;
			int lineEnd = GetEndOffset(line);
			if (column - 1 >= lineEnd - lineStart)
				return lineEnd;
			return lineStart + column - 1;
		}
		
		/// <inheritdoc/>
		public int GetOffset(TextLocation location)
		{
			return GetOffset(location.Line, location.Column);
		}
		
		/// <inheritdoc/>
		public TextLocation GetLocation(int offset)
		{
			if (offset < 0 || offset > textSource.TextLength)
				throw new ArgumentOutOfRangeException("offset", offset, "Value must be between 0 and " + textSource.TextLength);
			int line = GetLineNumberForOffset(offset);
			return new TextLocation(line, offset-GetStartOffset(line)+1);
		}
		
		/// <inheritdoc/>
		public string Text {
			get { return textSource.Text; }
			set {
				throw new NotSupportedException();
			}
		}
		
		/// <inheritdoc/>
		public int LineCount {
			get { return lines.Length; }
		}
		
		/// <inheritdoc/>
		public ITextSourceVersion Version {
			get { return textSource.Version; }
		}
		
		/// <inheritdoc/>
		public int TextLength {
			get { return textSource.TextLength; }
		}
		
		event EventHandler<TextChangeEventArgs> IDocument.TextChanging { add {} remove {} }
		
		event EventHandler<TextChangeEventArgs> IDocument.TextChanged { add {} remove {} }
		
		event EventHandler IDocument.ChangeCompleted { add {} remove {} }
		
		void IDocument.Insert(int offset, string text)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Remove(int offset, int length)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Replace(int offset, int length, string newText)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Insert(int offset, ITextSource text)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.Replace(int offset, int length, ITextSource newText)
		{
			throw new NotSupportedException();
		}
		
		void IDocument.StartUndoableAction()
		{
		}
		
		void IDocument.EndUndoableAction()
		{
		}
		
		IDisposable IDocument.OpenUndoGroup()
		{
			return null;
		}
		
		/// <inheritdoc/>
		public ITextAnchor CreateAnchor(int offset)
		{
			return new ReadOnlyDocumentTextAnchor(GetLocation(offset), offset);
		}
		
		sealed class ReadOnlyDocumentTextAnchor : ITextAnchor
		{
			readonly TextLocation location;
			readonly int offset;
			
			public ReadOnlyDocumentTextAnchor(TextLocation location, int offset)
			{
				this.location = location;
				this.offset = offset;
			}
			
			public event EventHandler Deleted { add {} remove {} }
			
			public TextLocation Location {
				get { return location; }
			}
			
			public int Offset {
				get { return offset; }
			}
			
			public AnchorMovementType MovementType { get; set; }
			
			public bool SurviveDeletion { get; set; }
			
			public bool IsDeleted {
				get { return false; }
			}
			
			public int Line {
				get { return location.Line; }
			}
			
			public int Column {
				get { return location.Column; }
			}
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot()
		{
			return textSource; // textBuffer is immutable
		}
		
		/// <inheritdoc/>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			return textSource.CreateSnapshot(offset, length);
		}
		
		/// <inheritdoc/>
		public IDocument CreateDocumentSnapshot()
		{
			return this; // ReadOnlyDocument is immutable
		}
		
		/// <inheritdoc/>
		public System.IO.TextReader CreateReader()
		{
			return textSource.CreateReader();
		}
		
		/// <inheritdoc/>
		public System.IO.TextReader CreateReader(int offset, int length)
		{
			return textSource.CreateReader(offset, length);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(System.IO.TextWriter writer)
		{
			textSource.WriteTextTo(writer);
		}
		
		/// <inheritdoc/>
		public void WriteTextTo(System.IO.TextWriter writer, int offset, int length)
		{
			textSource.WriteTextTo(writer, offset, length);
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			return textSource.GetCharAt(offset);
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			return textSource.GetText(offset, length);
		}
		
		/// <inheritdoc/>
		public string GetText(ISegment segment)
		{
			return textSource.GetText(segment);
		}
		
		/// <inheritdoc/>
		public int IndexOf(char c, int startIndex, int count)
		{
			return textSource.IndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return textSource.IndexOfAny(anyOf, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return textSource.IndexOf(searchText, startIndex, count, comparisonType);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(char c, int startIndex, int count)
		{
			return textSource.LastIndexOf(c, startIndex, count);
		}
		
		/// <inheritdoc/>
		public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return textSource.LastIndexOf(searchText, startIndex, count, comparisonType);
		}
		
		object IServiceProvider.GetService(Type serviceType)
		{
			return null;
		}
		
		/// <inheritdoc/>
		/// <remarks>Will never be raised on <see cref="ReadOnlyDocument" />.</remarks>
		public event EventHandler FileNameChanged { add {} remove {} }
		
		/// <inheritdoc/>
		public string FileName {
			get { return fileName; }
		}
	}
}
