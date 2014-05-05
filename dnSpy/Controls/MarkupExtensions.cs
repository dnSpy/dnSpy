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
using System.Windows.Markup;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Controls
{
	[MarkupExtensionReturnType(typeof(Color))]
	class ControlColor : MarkupExtension
	{
		readonly float val;
		
		/// <summary>
		/// Amount of highlight (0..1)
		/// </summary>
		public float Highlight { get; set; }
		
		/// <summary>
		/// val: Color value in the range 105..255.
		/// </summary>
		public ControlColor(float val)
		{
			if (!(val >= 105 && val <= 255))
				throw new ArgumentOutOfRangeException("val");
			this.val = val;
		}
		
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (val > 227) {
				return Interpolate(227, SystemColors.ControlLightColor, 255, SystemColors.ControlLightLightColor);
			} else if (val > 160) {
				return Interpolate(160, SystemColors.ControlDarkColor, 227, SystemColors.ControlLightColor);
			} else {
				return Interpolate(105, SystemColors.ControlDarkDarkColor, 160, SystemColors.ControlDarkColor);
			}
		}
		
		Color Interpolate(float v1, Color c1, float v2, Color c2)
		{
			float v = (val - v1) / (v2 - v1);
			Color c = c1 * (1 - v) + c2 * v;
			return c * (1 - Highlight) + SystemColors.HighlightColor * Highlight;
		}
	}
}