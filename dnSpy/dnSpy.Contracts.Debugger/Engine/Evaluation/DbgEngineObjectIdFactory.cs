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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgEngineObjectId"/>s.
	/// Use <see cref="ExportDbgEngineObjectIdFactoryAttribute"/> to export an instance.
	/// </summary>
	public abstract class DbgEngineObjectIdFactory {
		/// <summary>
		/// Returns true if it's possible to create an object id
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public abstract bool CanCreateObjectId(DbgEngineValue value);

		/// <summary>
		/// Creates an object id or returns null
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <param name="id">Unique id</param>
		/// <returns></returns>
		public abstract DbgEngineObjectId CreateObjectId(DbgEngineValue value, uint id);

		/// <summary>
		/// Checks if an object id and a value refer to the same data
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public abstract bool Equals(DbgEngineObjectId objectId, DbgEngineValue value);

		/// <summary>
		/// Gets the hash code of an object id
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <returns></returns>
		public abstract int GetHashCode(DbgEngineObjectId objectId);

		/// <summary>
		/// Gets the hash code of a value created by this runtime
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		public abstract int GetHashCode(DbgEngineValue value);
	}

	/// <summary>Metadata</summary>
	public interface IDbgEngineObjectIdFactoryMetadata {
		/// <summary>See <see cref="ExportDbgEngineObjectIdFactoryAttribute.RuntimeGuid"/></summary>
		string RuntimeGuid { get; }
		/// <summary>See <see cref="ExportDbgEngineObjectIdFactoryAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgEngineObjectIdFactory"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgEngineObjectIdFactoryAttribute : ExportAttribute, IDbgEngineObjectIdFactoryMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtimeGuid">Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <param name="order">Order</param>
		public ExportDbgEngineObjectIdFactoryAttribute(string runtimeGuid, double order = double.MaxValue)
			: base(typeof(DbgEngineObjectIdFactory)) {
			RuntimeGuid = runtimeGuid ?? throw new ArgumentNullException(nameof(runtimeGuid));
			Order = order;
		}

		/// <summary>
		/// Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public string RuntimeGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
