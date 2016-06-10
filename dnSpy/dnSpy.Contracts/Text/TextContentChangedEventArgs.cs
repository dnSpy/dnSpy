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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text content changed event args
	/// </summary>
	public sealed class TextContentChangedEventArgs : TextSnapshotChangedEventArgs {
		/// <summary>
		/// Gets all the changes
		/// </summary>
		public INormalizedTextChangeCollection Changes => Before.Version.Changes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="beforeSnapshot">Old snapshot</param>
		/// <param name="afterSnapshot">New snapshot</param>
		/// <param name="editTag">Edit tag or null</param>
		public TextContentChangedEventArgs(ITextSnapshot beforeSnapshot, ITextSnapshot afterSnapshot, object editTag)
			: base(beforeSnapshot, afterSnapshot, editTag) {
		}
	}
}
