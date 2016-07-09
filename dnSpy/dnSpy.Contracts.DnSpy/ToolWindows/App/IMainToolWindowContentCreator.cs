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

namespace dnSpy.Contracts.ToolWindows.App {
	/// <summary>
	/// Creates <see cref="IToolWindowContent"/> instances.
	/// </summary>
	public interface IMainToolWindowContentCreator {
		/// <summary>
		/// Creates a <see cref="IToolWindowContent"/> instance or returns a cached instance if it's
		/// already been created. Returns null if someone else should create it.
		/// </summary>
		/// <param name="guid">Guid, see <see cref="IToolWindowContent.Guid"/></param>
		/// <returns></returns>
		IToolWindowContent GetOrCreate(Guid guid);

		/// <summary>
		/// Gets the tool windows it can create
		/// </summary>
		IEnumerable<ToolWindowContentInfo> ContentInfos { get; }
	}
}
