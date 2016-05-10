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

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Data passed to <see cref="IGuidObjectsCreator.GetGuidObjects(GuidObjectsCreatorArgs)"/>
	/// </summary>
	public struct GuidObjectsCreatorArgs {
		/// <summary>
		/// The owner object (<see cref="IMenuItemContext.CreatorObject"/>)
		/// </summary>
		public GuidObject CreatorObject { get; }

		/// <summary>
		/// true if it was opened from the keyboard
		/// </summary>
		public bool OpenedFromKeyboard { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="creatorObject">The owner object (<see cref="IMenuItemContext.CreatorObject"/>)</param>
		/// <param name="openedFromKeyboard">true if it was opened from the keyboard</param>
		public GuidObjectsCreatorArgs(GuidObject creatorObject, bool openedFromKeyboard) {
			CreatorObject = creatorObject;
			OpenedFromKeyboard = openedFromKeyboard;
		}
	}
}
