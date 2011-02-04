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
		/// Returns the top of the text. If the line contains inline UI elements larger than the text, TextTop
		/// will be below LineTop.
		/// </summary>
		TextTop,
		/// <summary>
		/// Returns the bottom of the TextLine. This is the same as the bottom of the text (the text is always
		/// aligned at the bottom border).
		/// </summary>
		LineBottom,
		/// <summary>
		/// The middle between LineTop and LineBottom.
		/// </summary>
		LineMiddle
	}
}
