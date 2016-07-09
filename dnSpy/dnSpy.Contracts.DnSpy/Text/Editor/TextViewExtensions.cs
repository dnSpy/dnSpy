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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ITextView"/> extensions
	/// </summary>
	public static class TextViewExtensions {
		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="line">Line number, 0-based</param>
		/// <returns></returns>
		public static CaretPosition MoveCaretTo(this ITextView textView, int line) => MoveCaretTo(textView, line, 0);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <returns></returns>
		public static CaretPosition MoveCaretTo(this ITextView textView, int line, int column) =>
			MoveCaretTo(textView, line, column, PositionAffinity.Successor);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <returns></returns>
		public static CaretPosition MoveCaretTo(this ITextView textView, int line, int column, PositionAffinity caretAffinity) =>
			MoveCaretTo(textView, line, column, caretAffinity, true);

		/// <summary>
		/// Moves the caret
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <param name="caretAffinity">Caret affinity</param>
		/// <param name="captureHorizontalPosition">true to capture the caret's horizontal position for subsequent
		/// moves up or down, false to retain the previously captured position</param>
		/// <returns></returns>
		public static CaretPosition MoveCaretTo(this ITextView textView, int line, int column, PositionAffinity caretAffinity, bool captureHorizontalPosition) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (line < 0)
				throw new ArgumentOutOfRangeException(nameof(line));
			if (column < 0)
				throw new ArgumentOutOfRangeException(nameof(column));
			if (line >= textView.TextSnapshot.LineCount)
				line = textView.TextSnapshot.LineCount - 1;
			var snapshotLine = textView.TextSnapshot.GetLineFromLineNumber(line);
			if (column >= snapshotLine.Length)
				column = snapshotLine.Length;
			return textView.Caret.MoveTo(snapshotLine.Start + column, caretAffinity, captureHorizontalPosition);
		}
	}
}
