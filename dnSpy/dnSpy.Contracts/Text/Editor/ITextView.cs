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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text view
	/// </summary>
	public interface ITextView : IPropertyOwner {
		/// <summary>
		/// Closes the text view
		/// </summary>
		void Close();

		/// <summary>
		/// true if it's been closed
		/// </summary>
		bool IsClosed { get; }

		/// <summary>
		/// Raised when the text view has been closed
		/// </summary>
		event EventHandler Closed;

		/// <summary>
		/// Raised when it or any of its adornments got the keyboard focus
		/// </summary>
		event EventHandler GotAggregateFocus;

		/// <summary>
		/// Raised when it and all its adornments lost the keyboard focus
		/// </summary>
		event EventHandler LostAggregateFocus;

		/// <summary>
		/// true if it or any of its adornments has focus
		/// </summary>
		bool HasAggregateFocus { get; }

		/// <summary>
		/// true if the mouse is over it or any of its adornments
		/// </summary>
		bool IsMouseOverViewOrAdornments { get; }

		/// <summary>
		/// Gets the caret
		/// </summary>
		ITextCaret Caret { get; }

		/// <summary>
		/// Gets the selection
		/// </summary>
		ITextSelection Selection { get; }

		/// <summary>
		/// Text buffer shown in this text view
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// <see cref="ITextSnapshot"/> of <see cref="TextBuffer"/> except when handling a <see cref="ITextBuffer.Changed"/>
		/// event on that buffer
		/// </summary>
		ITextSnapshot TextSnapshot { get; }

		/// <summary>
		/// <see cref="ITextSnapshot"/> of the visual buffer
		/// </summary>
		ITextSnapshot VisualSnapshot { get; }

		/// <summary>
		/// Gets the text data model
		/// </summary>
		ITextDataModel TextDataModel { get; }

		/// <summary>
		/// Gets the text view model
		/// </summary>
		ITextViewModel TextViewModel { get; }

		/// <summary>
		/// Gets the roles
		/// </summary>
		ITextViewRoleSet Roles { get; }

		/// <summary>
		/// Gets the options
		/// </summary>
		IEditorOptions Options { get; }
	}
}
