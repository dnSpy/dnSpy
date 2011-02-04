// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Indentation.CSharp
{
	/// <summary>
	/// Interface used for the indentation class to access the document.
	/// </summary>
	public interface IDocumentAccessor
	{
		/// <summary>Gets if the current line is read only (because it is not in the
		/// selected text region)</summary>
		bool IsReadOnly { get; }
		/// <summary>Gets the number of the current line.</summary>
		int LineNumber { get; }
		/// <summary>Gets/Sets the text of the current line.</summary>
		string Text { get; set; }
		/// <summary>Advances to the next line.</summary>
		bool MoveNext();
	}
	
	#region TextDocumentAccessor
	/// <summary>
	/// Adapter IDocumentAccessor -> TextDocument
	/// </summary>
	public sealed class TextDocumentAccessor : IDocumentAccessor
	{
		readonly TextDocument doc;
		readonly int minLine;
		readonly int maxLine;
		
		/// <summary>
		/// Creates a new TextDocumentAccessor.
		/// </summary>
		public TextDocumentAccessor(TextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			doc = document;
			this.minLine = 1;
			this.maxLine = doc.LineCount;
		}
		
		/// <summary>
		/// Creates a new TextDocumentAccessor that indents only a part of the document.
		/// </summary>
		public TextDocumentAccessor(TextDocument document, int minLine, int maxLine)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			doc = document;
			this.minLine = minLine;
			this.maxLine = maxLine;
		}
		
		int num;
		string text;
		DocumentLine line;
		
		/// <inheritdoc/>
		public bool IsReadOnly {
			get {
				return num < minLine;
			}
		}
		
		/// <inheritdoc/>
		public int LineNumber {
			get {
				return num;
			}
		}
		
		bool lineDirty;
		
		/// <inheritdoc/>
		public string Text {
			get { return text; }
			set {
				if (num < minLine) return;
				text = value;
				lineDirty = true;
			}
		}
		
		/// <inheritdoc/>
		public bool MoveNext()
		{
			if (lineDirty) {
				doc.Replace(line, text);
				lineDirty = false;
			}
			++num;
			if (num > maxLine) return false;
			line = doc.GetLineByNumber(num);
			text = doc.GetText(line);
			return true;
		}
	}
	#endregion
}
