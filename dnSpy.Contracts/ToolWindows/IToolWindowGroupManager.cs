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
using System.Collections.Generic;

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// Tool window group manager
	/// </summary>
	public interface IToolWindowGroupManager {
		/// <summary>
		/// Gets all <see cref="IToolWindowGroup"/> instances
		/// </summary>
		IEnumerable<IToolWindowGroup> TabGroups { get; }

		/// <summary>
		/// Gets the active <see cref="IToolWindowGroup"/> or null if <see cref="TabGroups"/> is empty
		/// </summary>
		IToolWindowGroup ActiveTabGroup { get; set; }

		/// <summary>
		/// true if the <see cref="IToolWindowGroup"/>s are lined up horizontally, else vertically
		/// </summary>
		bool IsHorizontal { get; set; }

		/// <summary>
		/// Creates a new <see cref="IToolWindowGroup"/> instance
		/// </summary>
		/// <returns></returns>
		IToolWindowGroup Create();

		/// <summary>
		/// Gets the UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Raised when a new tab has been selected
		/// </summary>
		event EventHandler<ToolWindowSelectedEventArgs> TabSelectionChanged;

		/// <summary>
		/// Raised when a new tab group has been selected
		/// </summary>
		event EventHandler<ToolWindowGroupSelectedEventArgs> TabGroupSelectionChanged;

		/// <summary>
		/// Raised when a tab group has been added or removed
		/// </summary>
		event EventHandler<ToolWindowGroupCollectionChangedEventArgs> TabGroupCollectionChanged;

		/// <summary>
		/// Closes the group
		/// </summary>
		/// <param name="group">Group</param>
		void Close(IToolWindowGroup group);

		/// <summary>
		/// true if <see cref="CloseAllTabs()"/> can execute
		/// </summary>
		bool CloseAllTabsCanExecute { get; }

		/// <summary>
		/// Closes all tabs
		/// </summary>
		void CloseAllTabs();

		/// <summary>
		/// true if <see cref="NewHorizontalTabGroup()"/> can execute
		/// </summary>
		bool NewHorizontalTabGroupCanExecute { get; }

		/// <summary>
		/// Moves the active tab to a new horizontal tab group
		/// </summary>
		void NewHorizontalTabGroup();

		/// <summary>
		/// true if <see cref="NewVerticalTabGroup()"/> can execute
		/// </summary>
		bool NewVerticalTabGroupCanExecute { get; }

		/// <summary>
		/// Moves the active tab to a new vertical tab group
		/// </summary>
		void NewVerticalTabGroup();

		/// <summary>
		/// true if <see cref="MoveToNextTabGroup()"/> can execute
		/// </summary>
		bool MoveToNextTabGroupCanExecute { get; }

		/// <summary>
		/// Moves active tab to the next tab group
		/// </summary>
		void MoveToNextTabGroup();

		/// <summary>
		/// true if <see cref="MoveToPreviousTabGroup()"/> can execute
		/// </summary>
		bool MoveToPreviousTabGroupCanExecute { get; }

		/// <summary>
		/// Moves the active tab to the previous tab group
		/// </summary>
		void MoveToPreviousTabGroup();

		/// <summary>
		/// true if <see cref="MoveAllToNextTabGroup()"/> can execute
		/// </summary>
		bool MoveAllToNextTabGroupCanExecute { get; }

		/// <summary>
		/// Moves all tabs in the current tab group to the next tab group
		/// </summary>
		void MoveAllToNextTabGroup();

		/// <summary>
		/// true if <see cref="MoveAllToPreviousTabGroup()"/> can execute
		/// </summary>
		bool MoveAllToPreviousTabGroupCanExecute { get; }

		/// <summary>
		/// Moves all tabs in the current tab group to the previous tab group
		/// </summary>
		void MoveAllToPreviousTabGroup();

		/// <summary>
		/// true if <see cref="CloseTabGroup()"/> can execute
		/// </summary>
		bool CloseTabGroupCanExecute { get; }

		/// <summary>
		/// Closes the tab group and all its tabs
		/// </summary>
		void CloseTabGroup();

		/// <summary>
		/// true if <see cref="CloseAllTabGroupsButThis()"/> can execute
		/// </summary>
		bool CloseAllTabGroupsButThisCanExecute { get; }

		/// <summary>
		/// Closes all tab groups except the active one
		/// </summary>
		void CloseAllTabGroupsButThis();

		/// <summary>
		/// true if <see cref="MoveTabGroupAfterNextTabGroup()"/> can execute
		/// </summary>
		bool MoveTabGroupAfterNextTabGroupCanExecute { get; }

		/// <summary>
		/// Moves the active tab group after the next one
		/// </summary>
		void MoveTabGroupAfterNextTabGroup();

		/// <summary>
		/// true if <see cref="MoveTabGroupBeforePreviousTabGroup()"/> can execute
		/// </summary>
		bool MoveTabGroupBeforePreviousTabGroupCanExecute { get; }

		/// <summary>
		/// Moves the active tab group before the previous one
		/// </summary>
		void MoveTabGroupBeforePreviousTabGroup();

		/// <summary>
		/// true if <see cref="MergeAllTabGroups()"/> can execute
		/// </summary>
		bool MergeAllTabGroupsCanExecute { get; }

		/// <summary>
		/// Moves all tabs to one tab group and closes the remaining (empty) tab groups
		/// </summary>
		void MergeAllTabGroups();

		/// <summary>
		/// true if <see cref="UseVerticalTabGroups()"/> can execute
		/// </summary>
		bool UseVerticalTabGroupsCanExecute { get; }

		/// <summary>
		/// Stacks all tab groups vertically
		/// </summary>
		void UseVerticalTabGroups();

		/// <summary>
		/// true if <see cref="UseHorizontalTabGroups()"/> can execute
		/// </summary>
		bool UseHorizontalTabGroupsCanExecute { get; }

		/// <summary>
		/// Stacks all tab groups horizontally
		/// </summary>
		void UseHorizontalTabGroups();
	}
}
