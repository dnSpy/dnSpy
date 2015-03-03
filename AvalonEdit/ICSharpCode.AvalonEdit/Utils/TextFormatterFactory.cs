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
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Creates TextFormatter instances that with the correct TextFormattingMode, if running on .NET 4.0.
	/// </summary>
	static class TextFormatterFactory
	{
		#if !DOTNET4
		readonly static DependencyProperty TextFormattingModeProperty;
		
		static TextFormatterFactory()
		{
			Assembly presentationFramework = typeof(FrameworkElement).Assembly;
			Type textOptionsType = presentationFramework.GetType("System.Windows.Media.TextOptions", false);
			if (textOptionsType != null) {
				TextFormattingModeProperty = textOptionsType.GetField("TextFormattingModeProperty").GetValue(null) as DependencyProperty;
			}
		}
		#endif
		
		/// <summary>
		/// Creates a <see cref="TextFormatter"/> using the formatting mode used by the specified owner object.
		/// </summary>
		public static TextFormatter Create(DependencyObject owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			#if DOTNET4
			return TextFormatter.Create(TextOptions.GetTextFormattingMode(owner));
			#else
			if (TextFormattingModeProperty != null) {
				object formattingMode = owner.GetValue(TextFormattingModeProperty);
				return (TextFormatter)typeof(TextFormatter).InvokeMember(
					"Create",
					BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
					null, null,
					new object[] { formattingMode },
					CultureInfo.InvariantCulture);
			} else {
				return TextFormatter.Create();
			}
			#endif
		}
		
		/// <summary>
		/// Returns whether the specified dependency property affects the text formatter creation.
		/// Controls should re-create their text formatter for such property changes.
		/// </summary>
		public static bool PropertyChangeAffectsTextFormatter(DependencyProperty dp)
		{
			#if DOTNET4
			return dp == TextOptions.TextFormattingModeProperty;
			#else
			return dp == TextFormattingModeProperty && TextFormattingModeProperty != null;
			#endif
		}
		
		/// <summary>
		/// Creates formatted text.
		/// </summary>
		/// <param name="element">The owner element. The text formatter setting are read from this element.</param>
		/// <param name="text">The text.</param>
		/// <param name="typeface">The typeface to use. If this parameter is null, the typeface of the <paramref name="element"/> will be used.</param>
		/// <param name="emSize">The font size. If this parameter is null, the font size of the <paramref name="element"/> will be used.</param>
		/// <param name="foreground">The foreground color. If this parameter is null, the foreground of the <paramref name="element"/> will be used.</param>
		/// <returns>A FormattedText object using the specified settings.</returns>
		public static FormattedText CreateFormattedText(FrameworkElement element, string text, Typeface typeface, double? emSize, Brush foreground)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			if (text == null)
				throw new ArgumentNullException("text");
			if (typeface == null)
				typeface = element.CreateTypeface();
			if (emSize == null)
				emSize = TextBlock.GetFontSize(element);
			if (foreground == null)
				foreground = TextBlock.GetForeground(element);
			#if DOTNET4
			return new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				typeface,
				emSize.Value,
				foreground,
				null,
				TextOptions.GetTextFormattingMode(element)
			);
			#else
			if (TextFormattingModeProperty != null) {
				object formattingMode = element.GetValue(TextFormattingModeProperty);
				return (FormattedText)Activator.CreateInstance(
					typeof(FormattedText),
					text,
					CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight,
					typeface,
					emSize,
					foreground,
					null,
					formattingMode
				);
			} else {
				return new FormattedText(
					text,
					CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight,
					typeface,
					emSize.Value,
					foreground
				);
			}
			#endif
		}
	}
}
