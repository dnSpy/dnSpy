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

namespace dnSpy.Contracts.ToolWindows.App {
	/// <summary>
	/// Allows adding tool windows
	/// </summary>
	public interface IMainToolWindowManager {
		/// <summary>
		/// Adds <paramref name="content"/> to a tool window and gives it keyboard focus. If it's
		/// already been added, it becomes active and gets keyboard focus.
		/// </summary>
		/// <param name="content">Content</param>
		/// <param name="location">Location or null to use the default location
		/// (<see cref="ToolWindowContentInfo.Location"/>). It's ignored if the content is already
		/// present in the UI.</param>
		void Show(IToolWindowContent content, AppToolWindowLocation? location = null);

		/// <summary>
		/// Adds content to a tool window and gives it keyboard focus. If it's already been added,
		/// it becomes active and gets keyboard focus.
		/// </summary>
		/// <param name="guid">Guid of content, see <see cref="IToolWindowContent.Guid"/></param>
		/// <param name="location">Location or null to use the default location
		/// (<see cref="ToolWindowContentInfo.Location"/>). It's ignored if the content is already
		/// present in the UI.</param>
		void Show(Guid guid, AppToolWindowLocation? location = null);

		/// <summary>
		/// Removes <paramref name="content"/> from the UI
		/// </summary>
		/// <param name="content">Content</param>
		void Close(IToolWindowContent content);

		/// <summary>
		/// Returns true if <paramref name="content"/> is shown in the UI
		/// </summary>
		/// <param name="content">Content</param>
		/// <returns></returns>
		bool IsShown(IToolWindowContent content);

		/// <summary>
		/// Returns true if the content is shown in the UI
		/// </summary>
		/// <param name="guid">Guid of content, see <see cref="IToolWindowContent.Guid"/></param>
		/// <returns></returns>
		bool IsShown(Guid guid);

		/// <summary>
		/// Returns true if it owns <paramref name="toolWindowGroup"/>
		/// </summary>
		/// <param name="toolWindowGroup">Group</param>
		/// <returns></returns>
		bool Owns(IToolWindowGroup toolWindowGroup);

		/// <summary>
		/// Returns true if <see cref="Move(IToolWindowContent, AppToolWindowLocation)"/> can execute
		/// </summary>
		/// <param name="content">Content</param>
		/// <param name="location">Location</param>
		/// <returns></returns>
		bool CanMove(IToolWindowContent content, AppToolWindowLocation location);

		/// <summary>
		/// Moves <paramref name="content"/> to a new location
		/// </summary>
		/// <param name="content">Content</param>
		/// <param name="location">Location</param>
		void Move(IToolWindowContent content, AppToolWindowLocation location);

		/// <summary>
		/// Returns true if <see cref="Move(IToolWindowGroup, AppToolWindowLocation)"/> can execute
		/// </summary>
		/// <param name="group">Group</param>
		/// <param name="location">Location</param>
		/// <returns></returns>
		bool CanMove(IToolWindowGroup group, AppToolWindowLocation location);

		/// <summary>
		/// Moves <paramref name="group"/> to a new location
		/// </summary>
		/// <param name="group">Group</param>
		/// <param name="location">Location</param>
		void Move(IToolWindowGroup group, AppToolWindowLocation location);
	}
}
