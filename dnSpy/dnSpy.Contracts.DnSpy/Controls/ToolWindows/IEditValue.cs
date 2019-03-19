/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Contracts.Controls.ToolWindows {
	/// <summary>
	/// Creates <see cref="IEditValue"/>s
	/// </summary>
	public interface IEditValueProvider {
		/// <summary>
		/// Creates a <see cref="IEditValue"/>. This is called by the control when the user has
		/// started the edit operation.
		/// </summary>
		/// <param name="text">Text shown in the control</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		IEditValue Create(string text, EditValueFlags flags);
	}

	/// <summary>
	/// Edits a value
	/// </summary>
	public interface IEditValue : IDisposable {
		/// <summary>
		/// Gets the UI object (text control)
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// true if the control has keyboard focus
		/// </summary>
		bool IsKeyboardFocused { get; }

		/// <summary>
		/// Raised when the edit is completed (there's new text or the user canceled the edit operation)
		/// </summary>
		event EventHandler<EditCompletedEventArgs> EditCompleted;
	}

	/// <summary>
	/// <see cref="IEditValue.EditCompleted"/> event args
	/// </summary>
	public sealed class EditCompletedEventArgs : EventArgs {
		/// <summary>
		/// Gets the new text or null if it was canceled
		/// </summary>
		public string NewText { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="newText">New text or null if it was canceled</param>
		public EditCompletedEventArgs(string newText) => NewText = newText;
	}
}
