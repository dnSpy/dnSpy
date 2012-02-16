// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
