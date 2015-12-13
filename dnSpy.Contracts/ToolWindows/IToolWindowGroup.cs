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

using System.Collections.Generic;

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// Tool window tab group
	/// </summary>
	public interface IToolWindowGroup {
		/// <summary>
		/// Gets the owner <see cref="IToolWindowGroupManager"/> instance
		/// </summary>
		IToolWindowGroupManager ToolWindowGroupManager { get; }

		/// <summary>
		/// Gets all <see cref="IToolWindowContent"/> instances
		/// </summary>
		IEnumerable<IToolWindowContent> TabContents { get; }

		/// <summary>
		/// Gets the active <see cref="IToolWindowContent"/> or null if <see cref="TabContents"/> is empty
		/// </summary>
		IToolWindowContent ActiveTabContent { get; set; }

		/// <summary>
		/// Adds the content
		/// </summary>
		/// <param name="content">Content</param>
		void Add(IToolWindowContent content);

		/// <summary>
		/// Closes the content
		/// </summary>
		/// <param name="content">Content</param>
		void Close(IToolWindowContent content);

		/// <summary>
		/// Moves <paramref name="content"/> from this group to <paramref name="destGroup"/>
		/// </summary>
		/// <param name="destGroup">Destination group</param>
		/// <param name="content">Content in this group</param>
		void MoveTo(IToolWindowGroup destGroup, IToolWindowContent content);

		/// <summary>
		/// Sets keyboard focus
		/// </summary>
		/// <param name="content">Content</param>
		void SetFocus(IToolWindowContent content);

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
