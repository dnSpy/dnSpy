// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
