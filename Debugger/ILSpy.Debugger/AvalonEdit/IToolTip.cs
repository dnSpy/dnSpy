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
using System.Windows;

namespace ILSpy.Debugger.AvalonEdit
{
	/// <summary>
	/// Content of text editor tooltip (used as <see cref="ToolTipRequestEventArgs.ContentToShow"/>), 
	/// specifying whether it should be displayed in a WPF Popup.
	/// </summary>
	public interface ITooltip
	{
		/// <summary>
		/// If true, this ITooltip will be displayed in a WPF Popup.
		/// Otherwise it will be displayed in a WPF Tooltip.
		/// WPF Popups are (unlike WPF Tooltips) focusable.
		/// </summary>
		bool ShowAsPopup { get; }
		
		/// <summary>
		/// Closes this tooltip.
		/// </summary>
		/// <param name="mouseClick">True if close request is raised 
		/// because of mouse click on some SharpDevelop GUI element.</param>
		/// <returns>True if Close succeeded (that is, can close). False otherwise.</returns>
		bool Close(bool mouseClick);
		
		/// <summary>
		/// Occurs when this tooltip decides to close.
		/// </summary>
		event RoutedEventHandler Closed;
	}
}
