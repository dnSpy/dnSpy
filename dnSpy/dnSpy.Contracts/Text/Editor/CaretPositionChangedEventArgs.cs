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
	/// Caret position changed event args
	/// </summary>
	public sealed class CaretPositionChangedEventArgs : EventArgs {
		/// <summary>
		/// <see cref="ITextView"/> owner
		/// </summary>
		public ITextView TextView { get; }

		/// <summary>
		/// Old position
		/// </summary>
		public CaretPosition OldPosition { get; }

		/// <summary>
		/// New position
		/// </summary>
		public CaretPosition NewPosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textView"><see cref="ITextView"/> owner</param>
		/// <param name="oldPosition">Old position</param>
		/// <param name="newPosition">New position</param>
		public CaretPositionChangedEventArgs(ITextView textView, CaretPosition oldPosition, CaretPosition newPosition) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			TextView = textView;
			OldPosition = oldPosition;
			NewPosition = newPosition;
		}
	}
}
