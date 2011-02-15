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
using ICSharpCode.AvalonEdit;
using ICSharpCode.NRefactory.CSharp;

namespace ILSpy.Debugger.ToolTips
{
	public class ToolTipRequestEventArgs : EventArgs
	{
		/// <summary>
		/// Gets whether the tool tip request was handled.
		/// </summary>
		public bool Handled { get; set; }
		
		/// <summary>
		/// Gets the editor causing the request.
		/// </summary>
		public TextEditor Editor { get; private set; }
		
		/// <summary>
		/// Gets whether the mouse was inside the document bounds.
		/// </summary>
		public bool InDocument { get; set; }
		
		/// <summary>
		/// The mouse position, in document coordinates.
		/// </summary>
		public AstLocation LogicalPosition { get; set; }
		
		/// <summary>
		/// Gets/Sets the content to show as a tooltip.
		/// </summary>
		public object ContentToShow { get; set; }
		
		/// <summary>
		/// Sets the tooltip to be shown.
		/// </summary>
		public void SetToolTip(object content)
		{
			if (content == null)
				throw new ArgumentNullException("content");
			this.Handled = true;
			this.ContentToShow = content;
		}
		
		public ToolTipRequestEventArgs(TextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");
			this.Editor = editor;
			this.InDocument = true;
		}
	}
}
