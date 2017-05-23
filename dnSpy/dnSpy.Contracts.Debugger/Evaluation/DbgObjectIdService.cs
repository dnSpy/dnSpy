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
using System.Collections.Generic;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgObjectId"/>s
	/// </summary>
	public abstract class DbgObjectIdService {
		/// <summary>
		/// Raised when one or more <see cref="DbgObjectId"/>s are created or removed
		/// </summary>
		public abstract event EventHandler ObjectIdsChanged;

		/// <summary>
		/// Returns true if it's possible to create an object id
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public abstract bool CanCreateObjectId(DbgValue value);

		/// <summary>
		/// Creates an object id or returns null
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public DbgObjectId CreateObjectId(DbgValue value) =>
			CreateObjectIds(new[] { value ?? throw new ArgumentNullException(nameof(value)) })[0];

		/// <summary>
		/// Creates object ids. The returned array will contain null elements if it wasn't possible to create object ids
		/// </summary>
		/// <param name="values">Values</param>
		/// <returns></returns>
		public abstract DbgObjectId[] CreateObjectIds(DbgValue[] values);

		/// <summary>
		/// Returns an object id or null if there's none that references <paramref name="value"/>
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public abstract DbgObjectId GetObjectId(DbgValue value);

		/// <summary>
		/// Gets all object ids
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
	}
}
