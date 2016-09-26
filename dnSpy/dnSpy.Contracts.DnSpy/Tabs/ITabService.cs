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

using System.Collections.Generic;

namespace dnSpy.Contracts.Tabs {
	/// <summary>
	/// Creates <see cref="ITabGroupService"/> instances
	/// </summary>
	public interface ITabService {
		/// <summary>
		/// Gets all <see cref="ITabGroupService"/> instances
		/// </summary>
		IEnumerable<ITabGroupService> TabGroupServices { get; }

		/// <summary>
		/// Gets the active <see cref="ITabGroupService"/> instance
		/// </summary>
		ITabGroupService ActiveTabGroupService { get; }

		/// <summary>
		/// Creates a new <see cref="ITabGroupService"/> instance
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		ITabGroupService Create(TabGroupServiceOptions options);

		/// <summary>
		/// Removes a <see cref="ITabGroupService"/> instance
		/// </summary>
		/// <param name="mgr">Instance to remove</param>
		void Remove(ITabGroupService mgr);
	}
}
