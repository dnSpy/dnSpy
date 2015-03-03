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

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// An enum that specifies the possible Y positions that can be returned by VisualLine.GetVisualPosition.
	/// </summary>
	public enum VisualYPosition
	{
		/// <summary>
		/// Returns the top of the TextLine.
		/// </summary>
		LineTop,
		/// <summary>
		/// Returns the top of the text.
		/// If the line contains inline UI elements larger than the text, TextTop may be below LineTop.
		/// For a line containing regular text (all in the editor's main font), this will be equal to LineTop.
		/// </summary>
		TextTop,
		/// <summary>
		/// Returns the bottom of the TextLine.
		/// </summary>
		LineBottom,
		/// <summary>
		/// The middle between LineTop and LineBottom.
		/// </summary>
		LineMiddle,
		/// <summary>
		/// Returns the bottom of the text. 
		/// If the line contains inline UI elements larger than the text, TextBottom might be above LineBottom.
		/// For a line containing regular text (all in the editor's main font), this will be equal to LineBottom.
		/// </summary>
		TextBottom,
		/// <summary>
		/// The middle between TextTop and TextBottom.
		/// </summary>
		TextMiddle,
		/// <summary>
		/// Returns the baseline of the text.
		/// </summary>
		Baseline
	}
}
