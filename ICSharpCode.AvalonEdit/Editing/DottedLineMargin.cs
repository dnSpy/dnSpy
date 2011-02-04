// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Margin for use with the text area.
	/// A vertical dotted line to separate the line numbers from the text view.
	/// </summary>
	public static class DottedLineMargin
	{
		static readonly object tag = new object();
		
		/// <summary>
		/// Creates a vertical dotted line to separate the line numbers from the text view.
		/// </summary>
		public static UIElement Create()
		{
			Line line = new Line {
				X1 = 0, Y1 = 0, X2 = 0, Y2 = 1,
				StrokeDashArray = { 0, 2 },
				Stretch = Stretch.Fill,
				StrokeThickness = 1,
				StrokeDashCap = PenLineCap.Round,
				Margin = new Thickness(2, 0, 2, 0),
				Tag = tag
			};
			
			return line;
		}
		
		/// <summary>
		/// Creates a vertical dotted line to separate the line numbers from the text view.
		/// </summary>
		[Obsolete("This method got published accidentally; and will be removed again in a future version. Use the parameterless overload instead.")]
		public static UIElement Create(TextEditor editor)
		{
			Line line = (Line)Create();
			
			line.SetBinding(
				Line.StrokeProperty,
				new Binding("LineNumbersForeground") { Source = editor }
			);
			
			return line;
		}
		
		/// <summary>
		/// Gets whether the specified UIElement is the result of a DottedLineMargin.Create call.
		/// </summary>
		public static bool IsDottedLineMargin(UIElement element)
		{
			Line l = element as Line;
			return l != null && l.Tag == tag;
		}
	}
}
