/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
	/// Collection changed event args
	/// </summary>
	public sealed class TabGroupCollectionChangedEventArgs : EventArgs {
		/// <summary>
		/// true if <see cref="TabGroup"/> was added, false if it was removed
		/// </summary>
		public bool Added { get; private set; }

		/// <summary>
		/// The tab group
		/// </summary>
		public ITabGroup TabGroup { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="added">true if it was added</param>
		/// <param name="tabGroup">Tab group</param>
		public TabGroupCollectionChangedEventArgs(bool added, ITabGroup tabGroup) {
			this.Added = added;
			this.TabGroup = tabGroup;
		}
	}
}
