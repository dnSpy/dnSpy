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
using System.Windows.Input;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Custom commands for AvalonEdit.
	/// </summary>
	public static class AvalonEditCommands
	{
		/// <summary>
		/// Deletes the current line.
		/// The default shortcut is Ctrl+D.
		/// </summary>
		public static readonly RoutedCommand DeleteLine = new RoutedCommand(
			"DeleteLine", typeof(TextEditor),
			new InputGestureCollection {
				new KeyGesture(Key.D, ModifierKeys.Control)
			});
		
		/// <summary>
		/// Removes leading whitespace from the selected lines (or the whole document if the selection is empty).
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		public static readonly RoutedCommand RemoveLeadingWhitespace = new RoutedCommand("RemoveLeadingWhitespace", typeof(TextEditor));
				
		/// <summary>
		/// Removes trailing whitespace from the selected lines (or the whole document if the selection is empty).
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "WPF uses 'Whitespace'")]
		public static readonly RoutedCommand RemoveTrailingWhitespace = new RoutedCommand("RemoveTrailingWhitespace", typeof(TextEditor));
		
		/// <summary>
		/// Converts the selected text to upper case.
		/// </summary>
		public static readonly RoutedCommand ConvertToUppercase = new RoutedCommand("ConvertToUppercase", typeof(TextEditor));
		
		/// <summary>
		/// Converts the selected text to lower case.
		/// </summary>
		public static readonly RoutedCommand ConvertToLowercase = new RoutedCommand("ConvertToLowercase", typeof(TextEditor));
		
		/// <summary>
		/// Converts the selected text to title case.
		/// </summary>
		public static readonly RoutedCommand ConvertToTitleCase = new RoutedCommand("ConvertToTitleCase", typeof(TextEditor));
		
		/// <summary>
		/// Inverts the case of the selected text.
		/// </summary>
		public static readonly RoutedCommand InvertCase = new RoutedCommand("InvertCase", typeof(TextEditor));
		
		/// <summary>
		/// Converts tabs to spaces in the selected text.
		/// </summary>
		public static readonly RoutedCommand ConvertTabsToSpaces = new RoutedCommand("ConvertTabsToSpaces", typeof(TextEditor));
		
		/// <summary>
		/// Converts spaces to tabs in the selected text.
		/// </summary>
		public static readonly RoutedCommand ConvertSpacesToTabs = new RoutedCommand("ConvertSpacesToTabs", typeof(TextEditor));
		
		/// <summary>
		/// Converts leading tabs to spaces in the selected lines (or the whole document if the selection is empty).
		/// </summary>
		public static readonly RoutedCommand ConvertLeadingTabsToSpaces = new RoutedCommand("ConvertLeadingTabsToSpaces", typeof(TextEditor));
		
		/// <summary>
		/// Converts leading spaces to tabs in the selected lines (or the whole document if the selection is empty).
		/// </summary>
		public static readonly RoutedCommand ConvertLeadingSpacesToTabs = new RoutedCommand("ConvertLeadingSpacesToTabs", typeof(TextEditor));
		
		/// <summary>
		/// Runs the IIndentationStrategy on the selected lines (or the whole document if the selection is empty).
		/// </summary>
		public static readonly RoutedCommand IndentSelection = new RoutedCommand(
			"IndentSelection", typeof(TextEditor),
			new InputGestureCollection {
				new KeyGesture(Key.I, ModifierKeys.Control)
			});
	}
}
