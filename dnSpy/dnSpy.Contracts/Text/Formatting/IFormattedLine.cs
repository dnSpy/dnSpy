/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Represents a line of formatted text in the <see cref="ITextView"/>
	/// </summary>
	public interface IFormattedLine : IWpfTextViewLine, IDisposable {
		/// <summary>
		/// Gets the <see cref="Visual"/> that can be used to add this formatted text line to a <see cref="VisualCollection"/>
		/// </summary>
		/// <returns></returns>
		Visual GetOrCreateVisual();

		/// <summary>
		/// Remove the <see cref="Visual"/> that represents the rendered text of the line
		/// </summary>
		void RemoveVisual();

		/// <summary>
		/// Sets the <see cref="ITextViewLine.Change"/> property for this text line
		/// </summary>
		/// <param name="change">The <see cref="TextViewLineChange"/></param>
		void SetChange(TextViewLineChange change);

		/// <summary>
		/// Sets the change in position of the top of this formatted text line between the current view layout and the previous view layout
		/// </summary>
		/// <param name="deltaY">The change in value for the formatted text line</param>
		void SetDeltaY(double deltaY);

		/// <summary>
		/// Sets the line transform used to format the text in this formatted text line
		/// </summary>
		/// <param name="transform">The line transform for this formatted text line</param>
		void SetLineTransform(LineTransform transform);

		/// <summary>
		/// Sets the <see cref="ITextSnapshot"/> objects upon which this formatted text line is based
		/// </summary>
		/// <param name="visualSnapshot">The new snapshot for the line in the view model's visual buffer</param>
		/// <param name="editSnapshot">The new snapshot for the line in the view model's edit buffer</param>
		void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot);

		/// <summary>
		/// Sets the position used to format the text in this formatted text line
		/// </summary>
		/// <param name="top">The position for the top of the formatted text line</param>
		void SetTop(double top);

		/// <summary>
		/// Sets the visible area in which this text line will be formatted
		/// </summary>
		/// <param name="visibleArea">The bounds of the visible area on the drawing surface upon which this text line will be formatted</param>
		void SetVisibleArea(Rect visibleArea);
	}
}
