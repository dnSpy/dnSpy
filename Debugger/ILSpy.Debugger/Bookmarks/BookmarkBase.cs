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
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.NRefactory.CSharp;
using ILSpy.Debugger.AvalonEdit.Editor;

namespace ILSpy.Debugger.Bookmarks
{
	/// <summary>
	/// A bookmark that can be attached to an AvalonEdit TextDocument.
	/// </summary>
	public class BookmarkBase : IBookmark
	{
		AstLocation location;
		
		IDocument document;
		ITextAnchor anchor;
		
		public IDocument Document {
			get {
				return document;
			}
			set {
				if (document != value) {
					if (anchor != null) {
						location = anchor.Location;
						anchor = null;
					}
					document = value;
					CreateAnchor();
					OnDocumentChanged(EventArgs.Empty);
				}
			}
		}
		
		void CreateAnchor()
		{
			if (document != null) {
				int lineNumber = Math.Max(1, Math.Min(location.Line, document.TotalNumberOfLines));
				int lineLength = document.GetLine(lineNumber).Length;
				int offset = document.PositionToOffset(
					lineNumber,
					Math.Max(1, Math.Min(location.Column, lineLength + 1))
				);
				anchor = document.CreateAnchor(offset);
				// after insertion: keep bookmarks after the initial whitespace (see DefaultFormattingStrategy.SmartReplaceLine)
				anchor.MovementType = AnchorMovementType.AfterInsertion;
				anchor.Deleted += AnchorDeleted;
			} else {
				anchor = null;
			}
		}
		
		void AnchorDeleted(object sender, EventArgs e)
		{
			// the anchor just became invalid, so don't try to use it again
			location = AstLocation.Empty;
			anchor = null;
			RemoveMark();
		}
		
		protected virtual void RemoveMark()
		{
			
		}
		
		/// <summary>
		/// Gets the TextAnchor used for this bookmark.
		/// Is null if the bookmark is not connected to a document.
		/// </summary>
		public ITextAnchor Anchor {
			get { return anchor; }
		}
		
		public AstLocation Location {
			get {
				if (anchor != null)
					return anchor.Location;
				else
					return location;
			}
			set {
				location = value;
				CreateAnchor();
			}
		}
		
		public event EventHandler DocumentChanged;
		
		protected virtual void OnDocumentChanged(EventArgs e)
		{
			if (DocumentChanged != null) {
				DocumentChanged(this, e);
			}
		}
		
		protected virtual void Redraw()
		{
			
		}
		
		public string TypeName { get; set; }
		
		public int LineNumber {
			get {
				if (anchor != null)
					return anchor.Line;
				else
					return location.Line;
			}
		}
		
		public int ColumnNumber {
			get {
				if (anchor != null)
					return anchor.Column;
				else
					return location.Column;
			}
		}
		
		public virtual int ZOrder {
			get { return 0; }
		}
		
		/// <summary>
		/// Gets if the bookmark can be toggled off using the 'set/unset bookmark' command.
		/// </summary>
		public virtual bool CanToggle {
			get {
				return true;
			}
		}
		
		public BookmarkBase(string typeName, AstLocation location)
		{
			this.TypeName = typeName;
			this.Location = location;
		}
		
		public virtual ImageSource Image {
			get { return null; }
		}
		
		public virtual void MouseDown(MouseButtonEventArgs e)
		{
		}
		
		public virtual void MouseUp(MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && CanToggle) {
				RemoveMark();
				e.Handled = true;
			}
		}
		
		public virtual bool CanDragDrop {
			get { return false; }
		}
		
		public virtual void Drop(int lineNumber)
		{
		}
	}
}
