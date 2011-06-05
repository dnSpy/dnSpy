// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// A bookmark that can be attached to an AvalonEdit TextDocument.
	/// </summary>
	public class BookmarkBase : IBookmark
	{
		AstLocation location;
		
		protected virtual void RemoveMark()
		{
			
		}
		
		public AstLocation Location {
			get { return location; }
			set { location = value; }
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
		
		public MemberReference MemberReference { get; set; }
		
		public int LineNumber {
			get { return location.Line; }
		}
		
		public int ColumnNumber {
			get {  return location.Column; }
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
		
		public BookmarkBase(MemberReference member, AstLocation location)
		{
			this.MemberReference = member;
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
