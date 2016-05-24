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
	public interface ITextView {
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
	}
}
