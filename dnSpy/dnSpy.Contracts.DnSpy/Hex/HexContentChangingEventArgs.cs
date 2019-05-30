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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex content changing event args
	/// </summary>
	public sealed class HexContentChangingEventArgs : EventArgs {
		/// <summary>
		/// true if <see cref="Cancel"/> has been called
		/// </summary>
		public bool Canceled { get; private set; }

		/// <summary>
		/// Version before the change
		/// </summary>
		public HexVersion BeforeVersion { get; }

		/// <summary>
		/// Edit tag
		/// </summary>
		public object? EditTag { get; }

		readonly Action<HexContentChangingEventArgs>? cancelAction;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="beforeVersion">Version before the change</param>
		/// <param name="editTag">Edit tag</param>
		/// <param name="cancelAction">Called when <see cref="Cancel"/> gets called</param>
		public HexContentChangingEventArgs(HexVersion beforeVersion, object? editTag, Action<HexContentChangingEventArgs>? cancelAction) {
			BeforeVersion = beforeVersion;
			EditTag = editTag;
			this.cancelAction = cancelAction;
		}

		/// <summary>
		/// Cancels the edit
		/// </summary>
		public void Cancel() {
			if (!Canceled) {
				Canceled = true;
				cancelAction?.Invoke(this);
			}
		}
	}
}
