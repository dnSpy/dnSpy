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
using System.Collections.Generic;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgObjectId"/>s
	/// </summary>
	public abstract class DbgObjectIdService {
		/// <summary>
		/// Raised when one or more non-hidden <see cref="DbgObjectId"/>s are created or removed
		/// </summary>
		public abstract event EventHandler ObjectIdsChanged;

		/// <summary>
		/// Returns true if it's possible to create an object id
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract bool CanCreateObjectId(DbgValue value, CreateObjectIdOptions options = CreateObjectIdOptions.None);

		/// <summary>
		/// Creates an object id or returns null
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public DbgObjectId CreateObjectId(DbgValue value, CreateObjectIdOptions options = CreateObjectIdOptions.None) =>
			CreateObjectIds(new[] { value ?? throw new ArgumentNullException(nameof(value)) }, options)[0];

		/// <summary>
		/// Creates object ids. The returned array will contain null elements if it wasn't possible to create object ids
		/// </summary>
		/// <param name="values">Values</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgObjectId[] CreateObjectIds(DbgValue[] values, CreateObjectIdOptions options = CreateObjectIdOptions.None);

		/// <summary>
		/// Returns an non-hidden object id or null if there's none that references <paramref name="value"/>
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public abstract DbgObjectId GetObjectId(DbgValue value);

		/// <summary>
		/// Returns a non-hidden object id or null if there's none
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="id">Object id</param>
		/// <returns></returns>
		public abstract DbgObjectId GetObjectId(DbgRuntime runtime, uint id);

		/// <summary>
		/// Gets all non-hidden object ids
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <returns></returns>
		public abstract DbgObjectId[] GetObjectIds(DbgRuntime runtime);

		/// <summary>
		/// Removes and closes an object id
		/// </summary>
		/// <param name="objectId">Object id to remove and close</param>
		public void Remove(DbgObjectId objectId) =>
			Remove(new[] { objectId ?? throw new ArgumentNullException(nameof(objectId)) });

		/// <summary>
		/// Removes and closes object ids
		/// </summary>
		/// <param name="objectIds">Object ids to remove and close</param>
		public abstract void Remove(IEnumerable<DbgObjectId> objectIds);

		/// <summary>
		/// Checks if an object id and a value refer to the same data
		/// </summary>
		/// <param name="objectId">Object id</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public abstract bool Equals(DbgObjectId objectId, DbgValue value);

		/// <summary>
		/// Gets the hash code of an object id
		/// </summary>
		/// <param name="objectId">Object id</param>
		/// <returns></returns>
		public abstract int GetHashCode(DbgObjectId objectId);
	}

	/// <summary>
	/// Object ID options
	/// </summary>
	public enum CreateObjectIdOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None			= 0,

		/// <summary>
		/// Hidden object Id. It's not shown in any of the variables windows.
		/// </summary>
		Hidden			= 0x00000001,
	}
}
