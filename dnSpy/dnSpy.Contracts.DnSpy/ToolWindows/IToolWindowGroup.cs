/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System.Collections.Generic;

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// Tool window tab group
	/// </summary>
	public interface IToolWindowGroup {
		/// <summary>
		/// Gets the owner <see cref="IToolWindowGroupService"/> instance
		/// </summary>
		IToolWindowGroupService ToolWindowGroupService { get; }

		/// <summary>
		/// Gets all <see cref="ToolWindowContent"/> instances
		/// </summary>
		IEnumerable<ToolWindowContent> TabContents { get; }

		/// <summary>
		/// Gets the active <see cref="ToolWindowContent"/> or null if <see cref="TabContents"/> is empty
		/// </summary>
		ToolWindowContent? ActiveTabContent { get; set; }

		/// <summary>
		/// Adds the content
		/// </summary>
		/// <param name="content">Content</param>
		void Add(ToolWindowContent content);

		/// <summary>
		/// Closes the content
		/// </summary>
		/// <param name="content">Content</param>
		void Close(ToolWindowContent content);

		/// <summary>
		/// Moves <paramref name="content"/> from this group to <paramref name="destGroup"/>
		/// </summary>
		/// <param name="destGroup">Destination group</param>
		/// <param name="content">Content in this group</param>
		void MoveTo(IToolWindowGroup destGroup, ToolWindowContent content);

		/// <summary>
		/// Sets keyboard focus
		/// </summary>
		/// <param name="content">Content</param>
		void SetFocus(ToolWindowContent content);

		/// <summary>
		/// true if <see cref="CloseActiveTab()"/> can execute
		/// </summary>
		/// <returns></returns>
		bool CloseActiveTabCanExecute { get; }

		/// <summary>
		/// Closes the active tab
		/// </summary>
		void CloseActiveTab();
	}
}
