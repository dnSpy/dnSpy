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
using System.Runtime.CompilerServices;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
#if NREFACTORY
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Editor;
#endif

namespace ICSharpCode.AvalonEdit.Editing
{
	sealed class EmptySelection : Selection
	{
		public EmptySelection(TextArea textArea) : base(textArea)
		{
		}
		
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			return this;
		}
		
		public override TextViewPosition StartPosition {
			get { return new TextViewPosition(TextLocation.Empty); }
		}
		
		public override TextViewPosition EndPosition {
			get { return new TextViewPosition(TextLocation.Empty); }
		}
		
		public override ISegment SurroundingSegment {
			get { return null; }
		}
		
		public override Selection SetEndpoint(TextViewPosition endPosition)
		{
			throw new NotSupportedException();
		}
		
		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
		{
			var document = textArea.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			return Create(textArea, startPosition, endPosition);
		}
		
		public override IEnumerable<SelectionSegment> Segments {
			get { return Empty<SelectionSegment>.Array; }
		}
		
		public override string GetText()
		{
			return string.Empty;
		}
		
		public override void ReplaceSelectionWithText(string newText)
		{
			if (newText == null)
				throw new ArgumentNullException("newText");
			newText = AddSpacesIfRequired(newText, textArea.Caret.Position, textArea.Caret.Position);
			if (newText.Length > 0) {
				if (textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset)) {
					textArea.Document.Insert(textArea.Caret.Offset, newText);
				}
			}
			textArea.Caret.VisualColumn = -1;
		}
		
		public override int Length {
			get { return 0; }
		}
		
		// Use reference equality because there's only one EmptySelection per text area.
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}
		
		public override bool Equals(object obj)
		{
			return this == obj;
		}
	}
}
