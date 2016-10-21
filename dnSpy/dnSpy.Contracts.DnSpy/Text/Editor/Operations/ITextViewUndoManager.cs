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

using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Text view undo manager
	/// </summary>
	public interface ITextViewUndoManager {
		/// <summary>
		/// Gets the text view
		/// </summary>
		IDsWpfTextView TextView { get; }

		/// <summary>
		/// Gets the undo history
		/// </summary>
		ITextUndoHistory TextViewUndoHistory { get; }

		/// <summary>
		/// Clears the undo/redo history. <see cref="TextViewUndoHistory"/> also gets
		/// updated with a new instance.
		/// </summary>
		void ClearUndoHistory();
	}
}
