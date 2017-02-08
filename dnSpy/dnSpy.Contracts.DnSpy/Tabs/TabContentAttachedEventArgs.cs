/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Tabs {
	/// <summary>
	/// <see cref="ITabContent"/> attached/detached event args
	/// </summary>
	public sealed class TabContentAttachedEventArgs : EventArgs {
		/// <summary>
		/// true if attached, false if detached
		/// </summary>
		public bool Attached { get; }

		/// <summary>
		/// The attached/detached tab content instance
		/// </summary>
		public ITabContent TabContent { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="attached">true if attached, false if detached</param>
		/// <param name="tabContent">Tab content</param>
		public TabContentAttachedEventArgs(bool attached, ITabContent tabContent) {
			Attached = attached;
			TabContent = tabContent;
		}
	}
}
