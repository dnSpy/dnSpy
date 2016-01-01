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
using System.Windows;
using dnSpy.Contracts.Menus;

namespace dnSpy.Contracts.Tabs {
	/// <summary>
	/// <see cref="ITabGroupManager"/> options
	/// </summary>
	public sealed class TabGroupManagerOptions {
		/// <summary>
		/// A style or a resource key or null to use the default style
		/// </summary>
		public object TabControlStyle;

		/// <summary>
		/// A style or a resource key or null to use the default style
		/// </summary>
		public object TabItemStyle;

		/// <summary>
		/// Guid to use to initialize the context menu if <see cref="InitializeContextMenu"/> is null
		/// </summary>
		public Guid TabGroupGuid;

		/// <summary>
		/// Called in the <see cref="ITabGroup"/> constructor to initialize the context menu. If
		/// null, the instance itself initializes it using <see cref="TabGroupGuid"/>
		/// </summary>
		public Func<IMenuManager, ITabGroup, FrameworkElement, IContextMenuCreator> InitializeContextMenu;

		/// <summary>
		/// Default constructor
		/// </summary>
		public TabGroupManagerOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tabGroupGuid">See <see cref="TabGroupGuid"/></param>
		public TabGroupManagerOptions(string tabGroupGuid)
			: this(new Guid(tabGroupGuid)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tabGroupGuid">See <see cref="TabGroupGuid"/></param>
		public TabGroupManagerOptions(Guid tabGroupGuid) {
			this.TabGroupGuid = tabGroupGuid;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public TabGroupManagerOptions Clone() {
			return new TabGroupManagerOptions {
				TabControlStyle = TabControlStyle,
				TabItemStyle = TabItemStyle,
				TabGroupGuid = TabGroupGuid,
				InitializeContextMenu = InitializeContextMenu,
			};
		}
	}
}
