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

using System;
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Created by a <see cref="DbgEngine"/>
	/// </summary>
	public sealed class DbgEngineRuntimeInfo {
		/// <summary>
		/// GUID returned by <see cref="DbgRuntime.Guid"/>
		/// </summary>
		public Guid Guid { get; }

		/// <summary>
		/// GUID returned by <see cref="DbgRuntime.RuntimeKindGuid"/>
		/// </summary>
		public Guid RuntimeKindGuid { get; }

		/// <summary>
		/// Name returned by <see cref="DbgRuntime.Name"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Id returned by <see cref="DbgRuntime.Id"/>
		/// </summary>
		public RuntimeId Id { get; }

		/// <summary>
		/// Tags returned by <see cref="DbgRuntime.Tags"/>
		/// </summary>
		public ReadOnlyCollection<string> Tags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">GUID returned by <see cref="DbgRuntime.Guid"/></param>
		/// <param name="runtimeKindGuid">GUID returned by <see cref="DbgRuntime.RuntimeKindGuid"/></param>
		/// <param name="name">Name returned by <see cref="DbgRuntime.Name"/></param>
		/// <param name="id">Id returned by <see cref="DbgRuntime.Id"/></param>
		/// <param name="tags">Tags returned by <see cref="DbgRuntime.Tags"/></param>
		public DbgEngineRuntimeInfo(Guid guid, Guid runtimeKindGuid, string name, RuntimeId id, ReadOnlyCollection<string> tags) {
			if (guid == Guid.Empty)
				throw new ArgumentOutOfRangeException(nameof(guid));
			if (runtimeKindGuid == Guid.Empty)
				throw new ArgumentOutOfRangeException(nameof(runtimeKindGuid));
			Guid = guid;
			RuntimeKindGuid = runtimeKindGuid;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Tags = tags ?? throw new ArgumentNullException(nameof(tags));
		}
	}
}
