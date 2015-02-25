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
