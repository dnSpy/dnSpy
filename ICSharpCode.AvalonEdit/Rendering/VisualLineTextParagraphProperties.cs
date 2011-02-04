// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Rendering
{
	sealed class VisualLineTextParagraphProperties : TextParagraphProperties
	{
		internal TextRunProperties defaultTextRunProperties;
		internal TextWrapping textWrapping;
		internal double tabSize;
		internal double indent;
		internal bool firstLineInParagraph;
		
		public override double DefaultIncrementalTab {
			get { return tabSize; }
		}
		
		public override FlowDirection FlowDirection { get { return FlowDirection.LeftToRight; } }
		public override TextAlignment TextAlignment { get { return TextAlignment.Left; } }
		public override double LineHeight { get { return double.NaN; } }
		public override bool FirstLineInParagraph { get { return firstLineInParagraph; } }
		public override TextRunProperties DefaultTextRunProperties { get { return defaultTextRunProperties; } }
		public override TextWrapping TextWrapping { get { return textWrapping; } }
		public override TextMarkerProperties TextMarkerProperties { get { return null; } }
		public override double Indent { get { return indent; } }
	}
}
