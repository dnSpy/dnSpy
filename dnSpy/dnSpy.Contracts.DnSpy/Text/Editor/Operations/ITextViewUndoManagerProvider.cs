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

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Enables undo/redo in text views
	/// </summary>
	public interface ITextViewUndoManagerProvider {
		/// <summary>
		/// Creates or returns a cached <see cref="ITextViewUndoManager"/> instance
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		ITextViewUndoManager GetTextViewUndoManager(IDsWpfTextView textView);

		/// <summary>
		/// Tries to return an existing <see cref="ITextViewUndoManager"/> instance
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="manager">Updated with the existing instance or null if none exists</param>
		/// <returns></returns>
		bool TryGetTextViewUndoManager(IDsWpfTextView textView, out ITextViewUndoManager manager);

		/// <summary>
		/// Removes the cached <see cref="ITextViewUndoManager"/> instance, if any.
		/// </summary>
		/// <param name="textView">Text view</param>
		void RemoveTextViewUndoManager(IDsWpfTextView textView);
	}
}
