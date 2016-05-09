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

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// <see cref="ITextBuffer"/> edit
	/// </summary>
	public interface ITextBufferEdit : IDisposable {
		/// <summary>
		/// true if the edit operation has been canceled
		/// </summary>
		bool Canceled { get; }

		/// <summary>
		/// Gets the original snapshot
		/// </summary>
		ITextSnapshot Snapshot { get; }

		/// <summary>
		/// Applies all changes to the text buffer. The new snapshot is returned, or the same
		/// one if no changes were applied.
		/// </summary>
		/// <returns></returns>
		ITextSnapshot Apply();

		/// <summary>
		/// Cancels all changes
		/// </summary>
		void Cancel();
	}
}
