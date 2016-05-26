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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text content changing event args
	/// </summary>
	public sealed class TextContentChangingEventArgs : EventArgs {
		/// <summary>
		/// Snapshot before the change
		/// </summary>
		public ITextSnapshot Before { get; }

		/// <summary>
		/// true if it was canceled. Set by calling <see cref="Cancel"/>
		/// </summary>
		public bool Canceled { get; private set; }

		/// <summary>
		/// Gets the edit tag
		/// </summary>
		public object EditTag { get; }

		readonly Action<TextContentChangingEventArgs> cancelAction;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="beforeSnapshot">Snapshot before the change</param>
		/// <param name="editTag">Edit tag or null</param>
		/// <param name="cancelAction">Cancel action or null. It's called when <see cref="Cancel"/> is called</param>
		public TextContentChangingEventArgs(ITextSnapshot beforeSnapshot, object editTag, Action<TextContentChangingEventArgs> cancelAction) {
			if (beforeSnapshot == null)
				throw new ArgumentNullException(nameof(beforeSnapshot));
			Before = beforeSnapshot;
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
