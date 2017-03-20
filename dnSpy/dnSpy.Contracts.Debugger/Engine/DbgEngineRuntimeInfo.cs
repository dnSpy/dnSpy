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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Created by a <see cref="DbgEngine"/>
	/// </summary>
	public sealed class DbgEngineRuntimeInfo {
		/// <summary>
		/// Name returned by <see cref="DbgRuntime.Name"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Id returned by <see cref="DbgRuntime.Id"/>
		/// </summary>
		public RuntimeId Id { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name returned by <see cref="DbgRuntime.Name"/></param>
		/// <param name="id">Id returned by <see cref="DbgRuntime.Id"/></param>
		public DbgEngineRuntimeInfo(string name, RuntimeId id) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Id = id ?? throw new ArgumentNullException(nameof(id));
		}
	}
}
