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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	[Export(typeof(DbgObjectIdService))]
	sealed class DbgObjectIdServiceImpl : DbgObjectIdService {
		public override event EventHandler ObjectIdsChanged;

		readonly Lazy<DbgEngineObjectIdFactory, IDbgEngineObjectIdFactoryMetadata>[] dbgEngineObjectIdFactories;

		[ImportingConstructor]
		DbgObjectIdServiceImpl([ImportMany] IEnumerable<Lazy<DbgEngineObjectIdFactory, IDbgEngineObjectIdFactoryMetadata>> dbgEngineObjectIdFactories) =>
			this.dbgEngineObjectIdFactories = dbgEngineObjectIdFactories.OrderBy(a => a.Metadata.Order).ToArray();

		DbgEngineObjectIdFactory GetEngineObjectIdFactory(Guid runtimeGuid) {
			foreach (var lz in dbgEngineObjectIdFactories) {
				if (Guid.TryParse(lz.Metadata.RuntimeGuid, out var guid) && guid == runtimeGuid)
					return lz.Value;
			}
			Debug.Fail($"Missing exported {nameof(DbgEngineObjectIdFactory)}, runtime GUID = {runtimeGuid}");
			return NullDbgEngineObjectIdFactory.Instance;
		}

		DbgRuntimeObjectIdService GetRuntimeObjectIdService(DbgRuntime runtime) {
			return runtime.GetOrCreateData<DbgRuntimeObjectIdService>(() => {
				var service = new DbgRuntimeObjectIdServiceImpl(runtime, GetEngineObjectIdFactory(runtime.Guid));
				service.ObjectIdsChanged += DbgRuntimeObjectIdService_ObjectIdsChanged;
				return service;
			});
		}

		void DbgRuntimeObjectIdService_ObjectIdsChanged(object sender, EventArgs e) => ObjectIdsChanged?.Invoke(this, EventArgs.Empty);

		public override bool CanCreateObjectId(DbgValue value, CreateObjectIdOptions options) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return GetRuntimeObjectIdService(value.Runtime).CanCreateObjectId(value, options);
		}

		public override DbgObjectId[] CreateObjectIds(DbgValue[] values, CreateObjectIdOptions options) {
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (values.Length == 0)
				return Array.Empty<DbgObjectId>();
			// Common case
			if (values.Length == 1) {
				var value = values[0] ?? throw new ArgumentException();
				return GetRuntimeObjectIdService(value.Runtime).CreateObjectIds(values, options);
			}
			var dict = new Dictionary<DbgRuntime, List<(DbgValue value, int index)>>();
			for (int i = 0; i < values.Length; i++) {
				var value = values[i];
				if (value == null)
					throw new ArgumentException();
				if (!dict.TryGetValue(value.Runtime, out var list))
					dict.Add(value.Runtime, list = new List<(DbgValue, int)>());
				list.Add((value, i));
			}
			var res = new DbgObjectId[values.Length];
			foreach (var kv in dict) {
				var objectIds = GetRuntimeObjectIdService(kv.Key).CreateObjectIds(kv.Value.Select(a => a.value).ToArray(), options);
				if (objectIds.Length != kv.Value.Count)
					throw new InvalidOperationException();
				for (int i = 0; i < objectIds.Length; i++)
					res[kv.Value[i].index] = objectIds[i];
			}
			return res;
		}

		public override DbgObjectId GetObjectId(DbgValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return GetRuntimeObjectIdService(value.Runtime).GetObjectId(value);
		}

		public override DbgObjectId GetObjectId(DbgRuntime runtime, uint id) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			return GetRuntimeObjectIdService(runtime).GetObjectId(id);
		}

		public override DbgObjectId[] GetObjectIds(DbgRuntime runtime) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			return GetRuntimeObjectIdService(runtime).GetObjectIds();
		}

		public override void Remove(IEnumerable<DbgObjectId> objectIds) {
			if (objectIds == null)
				throw new ArgumentNullException(nameof(objectIds));
			var dict = new Dictionary<DbgRuntime, List<DbgObjectId>>();
			foreach (var objectId in objectIds) {
				if (objectId == null)
					throw new ArgumentException();
				if (!dict.TryGetValue(objectId.Runtime, out var list))
					dict.Add(objectId.Runtime, list = new List<DbgObjectId>());
				list.Add(objectId);
			}
			foreach (var kv in dict)
				GetRuntimeObjectIdService(kv.Key).Remove(kv.Value);
		}

		public override bool Equals(DbgObjectId objectId, DbgValue value) {
			if (objectId == null)
				throw new ArgumentNullException(nameof(objectId));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return GetRuntimeObjectIdService(objectId.Runtime).Equals(objectId, value);
		}

		public override int GetHashCode(DbgObjectId objectId) {
			if (objectId == null)
				throw new ArgumentNullException(nameof(objectId));
			return GetRuntimeObjectIdService(objectId.Runtime).GetHashCode(objectId);
		}
	}
}
