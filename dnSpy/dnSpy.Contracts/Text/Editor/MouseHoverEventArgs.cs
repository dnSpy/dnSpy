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
	/// Mouse hover event args
	/// </summary>
	public sealed class MouseHoverEventArgs : EventArgs {
		/// <summary>
		/// Text view
		/// </summary>
		public ITextView View { get; }

		/// <summary>
		/// Position in the text buffer
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Text position
		/// </summary>
		public IMappingPoint TextPosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">Text view</param>
		/// <param name="position">Position in the text buffer</param>
		/// <param name="textPosition">Text position</param>
		public MouseHoverEventArgs(ITextView view, int position, IMappingPoint textPosition) {
			if (view == null)
				throw new ArgumentNullException(nameof(view));
			if ((uint)position > (uint)view.TextSnapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (textPosition == null)
				throw new ArgumentNullException(nameof(textPosition));
			View = view;
			Position = position;
			TextPosition = textPosition;
		}
	}
}
