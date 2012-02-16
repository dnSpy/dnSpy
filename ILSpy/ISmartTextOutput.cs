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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Adds additional WPF-specific output features to <see cref="ITextOutput"/>.
	/// </summary>
	public interface ISmartTextOutput : ITextOutput
	{
		/// <summary>
		/// Inserts an interactive UI element at the current position in the text output.
		/// </summary>
		void AddUIElement(Func<UIElement> element);
	}
	
	public static class SmartTextOutputExtensions
	{
		/// <summary>
		/// Creates a button.
		/// </summary>
		public static void AddButton(this ISmartTextOutput output, ImageSource icon, string text, RoutedEventHandler click)
		{
			output.AddUIElement(
				delegate {
					Button button = new Button();
					button.Cursor = Cursors.Arrow;
					button.Margin = new Thickness(2);
					button.Padding = new Thickness(9, 1, 9, 1);
					button.MinWidth = 73;
					if (icon != null) {
						button.Content = new StackPanel {
							Orientation = Orientation.Horizontal,
							Children = {
								new Image { Width = 16, Height = 16, Source = icon, Margin = new Thickness(0, 0, 4, 0) },
								new TextBlock { Text = text }
							}
						};
					} else {
						button.Content = text;
					}
					button.Click += click;
					return button;
				});
		}
	}
}
