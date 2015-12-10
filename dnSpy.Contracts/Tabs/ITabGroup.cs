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
using System.Collections.Generic;

namespace dnSpy.Contracts.Tabs {
	/// <summary>
	/// Contains 0 or more tabs
	/// </summary>
	public interface ITabGroup {
		/// <summary>
		/// Any value can be written here. It's ignored by this instance.
		/// </summary>
		object Tag { get; set; }

		/// <summary>
		/// Gets the owner <see cref="ITabGroupManager"/> instance
		/// </summary>
		ITabGroupManager TabGroupManager { get; }

		/// <summary>
		/// Gets all <see cref="ITabContent"/> instances
		/// </summary>
		IEnumerable<ITabContent> TabContents { get; }

		/// <summary>
		/// Gets the active <see cref="ITabContent"/> or null if <see cref="TabContents"/> is empty
		/// </summary>
		ITabContent ActiveTabContent { get; set; }

		/// <summary>
		/// Raised when a <see cref="ITabContent"/> is attached/detached
		/// </summary>
		event EventHandler<TabContentAttachedEventArgs> TabContentAttached;

		/// <summary>
		/// true if keyboard focus is within the tab
		/// </summary>
		bool IsKeyboardFocusWithin { get; }

		/// <summary>
		/// Sets keyboard focus
		/// </summary>
		/// <param name="content">Content</param>
		void SetFocus(ITabContent content);

		/// <summary>
		/// Closes the tab
		/// </summary>
		/// <param name="content">Content</param>
		void Close(ITabContent content);

		/// <summary>
		/// Adds tab content
		/// </summary>
		/// <param name="content">Content</param>
		void Add(ITabContent content);

		/// <summary>
		/// true if <see cref="CloseActiveTab()"/> can execute
		/// </summary>
		/// <returns></returns>
		bool CloseActiveTabCanExecute { get; }

		/// <summary>
		/// Closes the active tab
		/// </summary>
		void CloseActiveTab();

		/// <summary>
		/// true if <see cref="CloseAllTabs()"/> can execute
		/// </summary>
		bool CloseAllTabsCanExecute { get; }

		/// <summary>
		/// Closes all tabs
		/// </summary>
		void CloseAllTabs();

		/// <summary>
		/// true if <see cref="CloseAllButActiveTab()"/> can execute
		/// </summary>
		bool CloseAllButActiveTabCanExecute { get; }

		/// <summary>
		/// Closes all tabs except the active tab
		/// </summary>
		void CloseAllButActiveTab();

		/// <summary>
		/// true if <see cref="SelectNextTab()"/> can execute
		/// </summary>
		bool SelectNextTabCanExecute { get; }

		/// <summary>
		/// Selects the next tab
		/// </summary>
		void SelectNextTab();

		/// <summary>
		/// true if <see cref="SelectPreviousTab()"/> can execute
		/// </summary>
		bool SelectPreviousTabCanExecute { get; }

		/// <summary>
		/// Selects the previous tab
		/// </summary>
		void SelectPreviousTab();
	}
}
