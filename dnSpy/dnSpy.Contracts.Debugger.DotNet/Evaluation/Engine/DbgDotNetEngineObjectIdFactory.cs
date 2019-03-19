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
using dnSpy.Contracts.Debugger.Engine.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine {
	/// <summary>
	/// A .NET <see cref="DbgEngineObjectIdFactory"/>.
	/// Use <see cref="ExportDbgEngineObjectIdFactoryAttribute"/> to export an instance.
	/// </summary>
	public abstract class DbgDotNetEngineObjectIdFactory : DbgEngineObjectIdFactory {
		readonly DbgEngineObjectIdFactory dbgEngineObjectIdFactory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtimeGuid">Runtime guid, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <param name="dbgDotNetLanguageService">.NET language service instance</param>
		protected DbgDotNetEngineObjectIdFactory(Guid runtimeGuid, DbgDotNetLanguageService dbgDotNetLanguageService) {
			if (dbgDotNetLanguageService == null)
				throw new ArgumentNullException(nameof(dbgDotNetLanguageService));
			dbgEngineObjectIdFactory = dbgDotNetLanguageService.GetEngineObjectIdFactory(runtimeGuid);
		}

		/// <summary>
		/// Returns true if it's possible to create an object id
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public sealed override bool CanCreateObjectId(DbgEngineValue value) => dbgEngineObjectIdFactory.CanCreateObjectId(value);

		/// <summary>
		/// Creates an object id or returns null
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <param name="id">Unique id</param>
		/// <returns></returns>
		public sealed override DbgEngineObjectId CreateObjectId(DbgEngineValue value, uint id) => dbgEngineObjectIdFactory.CreateObjectId(value, id);

		/// <summary>
		/// Checks if an object id and a value refer to the same data
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public sealed override bool Equals(DbgEngineObjectId objectId, DbgEngineValue value) => dbgEngineObjectIdFactory.Equals(objectId, value);

		/// <summary>
		/// Gets the hash code of an object id
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <returns></returns>
		public sealed override int GetHashCode(DbgEngineObjectId objectId) => dbgEngineObjectIdFactory.GetHashCode(objectId);

		/// <summary>
		/// Gets the hash code of a value created by this runtime
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public sealed override int GetHashCode(DbgEngineValue value) => dbgEngineObjectIdFactory.GetHashCode(value);
	}
}
