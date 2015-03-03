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

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Provides information about the event that occured during use of snippets.
	/// </summary>
	public class SnippetEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the reason for deactivation.
		/// </summary>
		public DeactivateReason Reason { get; private set; }
		
		/// <summary>
		/// Creates a new SnippetEventArgs object, with a DeactivateReason.
		/// </summary>
		public SnippetEventArgs(DeactivateReason reason)
		{
			this.Reason = reason;
		}
	}
	
	/// <summary>
	/// Describes the reason for deactivation of a <see cref="SnippetElement" />.
	/// </summary>
	public enum DeactivateReason
	{
		/// <summary>
		/// Unknown reason.
		/// </summary>
		Unknown,
		/// <summary>
		/// Snippet was deleted.
		/// </summary>
		Deleted,
		/// <summary>
		/// There are no active elements in the snippet.
		/// </summary>
		NoActiveElements,
		/// <summary>
		/// The SnippetInputHandler was detached.
		/// </summary>
		InputHandlerDetached,
		/// <summary>
		/// Return was pressed by the user.
		/// </summary>
		ReturnPressed,
		/// <summary>
		/// Escape was pressed by the user.
		/// </summary>
		EscapePressed
	}
}
