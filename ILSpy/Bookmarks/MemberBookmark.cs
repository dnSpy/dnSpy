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
using ICSharpCode.AvalonEdit.Document;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// Bookmark used to give additional operations for class members.
	/// Does not derive from SDBookmark because it is not stored in the central BookmarkManager,
	/// but only in the document's BookmarkManager.
	/// </summary>
	public class MemberBookmark : IBookmark
	{
		IMemberRef member;
		
		public IMemberRef Member {
			get {
				return member;
			}
		}
		
		public MemberBookmark(IMemberRef member, TextLocation location)
		{
			this.member = member;
			LineNumber = location.Line;
		}
		
		public virtual ImageSource Image {
			get {
				if (member is FieldDef)
					return TreeNodes.FieldTreeNode.GetIcon((FieldDef)member);
				
				if (member is PropertyDef)
					return TreeNodes.PropertyTreeNode.GetIcon((PropertyDef)member);
				
				if (member is EventDef)
					return TreeNodes.EventTreeNode.GetIcon((EventDef)member);
				
				if (member is MethodDef)
					return TreeNodes.MethodTreeNode.GetIcon((MethodDef)member);
				
				if (member is TypeDef)
					return TreeNodes.TypeTreeNode.GetIcon((TypeDef)member);
				
				return null;
			}
		}
		
		public int LineNumber {
			get; private set;
		}
		
		public virtual void MouseDown(MouseButtonEventArgs e)
		{
		}
		
		public virtual void MouseUp(MouseButtonEventArgs e)
		{
		}
		
		int IBookmark.ZOrder {
			get { return -10; }
		}
		
		bool IBookmark.CanDragDrop {
			get { return false; }
		}
		
		void IBookmark.Drop(int lineNumber)
		{
			throw new NotSupportedException();
		}
	}
}
