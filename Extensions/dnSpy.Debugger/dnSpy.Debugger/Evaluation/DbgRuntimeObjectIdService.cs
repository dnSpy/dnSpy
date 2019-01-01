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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	abstract class DbgRuntimeObjectIdService {
		public abstract event EventHandler ObjectIdsChanged;
		public abstract bool CanCreateObjectId(DbgValue value, CreateObjectIdOptions options);
		public abstract DbgObjectId[] CreateObjectIds(DbgValue[] values, CreateObjectIdOptions options);
		public abstract DbgObjectId GetObjectId(DbgValue value);
		public abstract DbgObjectId GetObjectId(uint id);
		public abstract DbgObjectId[] GetObjectIds();
		public abstract void Remove(IList<DbgObjectId> objectIds);
		public abstract bool Equals(DbgObjectId objectId, DbgValue value);
		public abstract int GetHashCode(DbgObjectId objectId);
	}

	sealed class DbgRuntimeObjectIdServiceImpl : DbgRuntimeObjectIdService, IDisposable {
		const uint HiddenObjectId = 0;

		public override event EventHandler ObjectIdsChanged;
		internal DbgRuntime Runtime { get; }

		readonly object lockObj;
		uint objectIdCounter;
		readonly DbgEngineObjectIdFactory dbgEngineObjectIdFactory;
		readonly HashSet<DbgObjectId> hiddenObjectIds;
		// The key is a DbgObjectIdImpl, but a DbgValueImpl can also be used when looking up values.
		// Note that key == value in this dictionary.
		readonly Dictionary<object, DbgObjectId> objectIds;
		readonly Dictionary<uint, DbgObjectId> idToObjectId;

		sealed class ObjectIdComparer : IEqualityComparer<object> {
			readonly DbgEngineObjectIdFactory dbgEngineObjectIdFactory;

			public ObjectIdComparer(DbgEngineObjectIdFactory dbgEngineObjectIdFactory) => this.dbgEngineObjectIdFactory = dbgEngineObjectIdFactory;

			public new bool Equals(object x, object y) {
				if (x == y)
					return true;
				if (x is DbgObjectIdImpl objectId1 && y is DbgValueImpl value1)
					return dbgEngineObjectIdFactory.Equals(objectId1.EngineObjectId, value1.EngineValue);
				if (y is DbgObjectIdImpl objectId2 && x is DbgValueImpl value2)
					return dbgEngineObjectIdFactory.Equals(objectId2.EngineObjectId, value2.EngineValue);
				return false;
			}

			public int GetHashCode(object obj) {
				switch (obj) {
				case DbgObjectIdImpl objectId:	return dbgEngineObjectIdFactory.GetHashCode(objectId.EngineObjectId);
				case DbgValueImpl value:		return dbgEngineObjectIdFactory.GetHashCode(value.EngineValue);
				default:						throw new ArgumentException();
				}
			}
		}

		// The caller adds 'this' to the runtime so it's closed when the runtime is closed
		public DbgRuntimeObjectIdServiceImpl(DbgRuntime runtime, DbgEngineObjectIdFactory dbgEngineObjectIdFactory) {
			lockObj = new object();
			objectIdCounter = 1;
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.dbgEngineObjectIdFactory = dbgEngineObjectIdFactory ?? throw new ArgumentNullException(nameof(dbgEngineObjectIdFactory));
			hiddenObjectIds = new HashSet<DbgObjectId>();
			objectIds = new Dictionary<object, DbgObjectId>(new ObjectIdComparer(dbgEngineObjectIdFactory));
			idToObjectId = new Dictionary<uint, DbgObjectId>();
		}

		public override bool CanCreateObjectId(DbgValue value, CreateObjectIdOptions options) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (value.Runtime != Runtime)
				throw new ArgumentException();
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (Runtime.IsClosed || value.IsClosed)
				return false;
			bool isHidden = (options & CreateObjectIdOptions.Hidden) != 0;
			lock (lockObj)
				return (isHidden || !objectIds.ContainsKey(valueImpl)) && dbgEngineObjectIdFactory.CanCreateObjectId(valueImpl.EngineValue);
		}

		public override DbgObjectId[] CreateObjectIds(DbgValue[] values, CreateObjectIdOptions options) {
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (values.Length == 0)
				return Array.Empty<DbgObjectId>();
			var res = new DbgObjectId[values.Length];
			bool isHidden = (options & CreateObjectIdOptions.Hidden) != 0;
			lock (lockObj) {
				for (int i = 0; i < values.Length; i++) {
					var value = values[i] as DbgValueImpl;
					if (value?.Runtime != Runtime)
						throw new ArgumentException();
					DbgObjectId objectId;
					if (Runtime.IsClosed || value.IsClosed || (!isHidden && objectIds.ContainsKey(value)))
						objectId = null;
					else {
						uint id = isHidden ? HiddenObjectId : objectIdCounter;
						var engineObjectId = dbgEngineObjectIdFactory.CreateObjectId(value.EngineValue, id);
						if (engineObjectId == null)
							objectId = null;
						else {
							if (engineObjectId.Id != id)
								throw new InvalidOperationException();
							Debug.Assert(dbgEngineObjectIdFactory.Equals(engineObjectId, value.EngineValue));
							Debug.Assert(dbgEngineObjectIdFactory.GetHashCode(engineObjectId) == dbgEngineObjectIdFactory.GetHashCode(value.EngineValue));
							if (!isHidden)
								objectIdCounter++;
							objectId = new DbgObjectIdImpl(this, engineObjectId);
							if (isHidden)
								hiddenObjectIds.Add(objectId);
							else {
								objectIds.Add(objectId, objectId);
								idToObjectId.Add(objectId.Id, objectId);
							}
							Debug.Assert(objectIds.Count == idToObjectId.Count);
						}
					}
					res[i] = objectId;
				}
			}
			if (!isHidden)
				ObjectIdsChanged?.Invoke(this, EventArgs.Empty);
			return res;
		}

		public override DbgObjectId GetObjectId(DbgValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (value.Runtime != Runtime)
				throw new ArgumentException();
			if (Runtime.IsClosed || value.IsClosed)
				return null;
			lock (lockObj) {
				if (objectIds.Count == 0)
					return null;
				if (objectIds.TryGetValue(value, out var objectId))
					return objectId;
				return null;
			}
		}

		public override DbgObjectId GetObjectId(uint id) {
			if (Runtime.IsClosed)
				return null;
			lock (lockObj) {
				if (idToObjectId.TryGetValue(id, out var objectId))
					return objectId;
				return null;
			}
		}

		public override DbgObjectId[] GetObjectIds() {
			lock (lockObj) {
				int count = idToObjectId.Count;
				if (count == 0)
					return Array.Empty<DbgObjectId>();
				var objectIds = new DbgObjectId[count];
				int i = 0;
				foreach (var kv in idToObjectId)
					objectIds[i++] = kv.Value;
				Debug.Assert(i == objectIds.Length);
				return objectIds;
			}
		}

		public override void Remove(IList<DbgObjectId> objectIds) {
			if (objectIds == null)
				throw new ArgumentNullException(nameof(objectIds));
			lock (lockObj) {
				Debug.Assert(this.objectIds.Count == idToObjectId.Count);
				foreach (var objectId in objectIds) {
					if (objectId == null || objectId.Runtime != Runtime)
						throw new ArgumentException();
					if (this.objectIds.Remove(objectId))
						idToObjectId.Remove(objectId.Id);
					else {
						bool b = hiddenObjectIds.Remove(objectId);
						Debug.Assert(b);
					}
				}
				Debug.Assert(this.objectIds.Count == idToObjectId.Count);
			}
			if (objectIds.Count > 0) {
				Runtime.Process.DbgManager.Close(objectIds);
				if (!HasHidden(objectIds))
					ObjectIdsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		static bool HasHidden(IList<DbgObjectId> objectIds) {
			for (int i = 0; i < objectIds.Count; i++) {
				if (objectIds[i].Id == HiddenObjectId)
					return true;
			}
			return false;
		}

		public override bool Equals(DbgObjectId objectId, DbgValue value) {
			if (objectId == null)
				throw new ArgumentNullException(nameof(objectId));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			var objectIdImpl = objectId as DbgObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			var valueImpl = value as DbgValueImpl;
			if (valueImpl == null)
				throw new ArgumentException();
			if (objectId.Runtime != value.Runtime)
				return false;
			return dbgEngineObjectIdFactory.Equals(objectIdImpl.EngineObjectId, valueImpl.EngineValue);
		}

		public override int GetHashCode(DbgObjectId objectId) {
			if (objectId == null)
				throw new ArgumentNullException(nameof(objectId));
			var objectIdImpl = objectId as DbgObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			return dbgEngineObjectIdFactory.GetHashCode(objectIdImpl.EngineObjectId);
		}

		void IDisposable.Dispose() {
			var dispatcher = Runtime.Process.DbgManager.Dispatcher;
			dispatcher.VerifyAccess();
			DbgObjectId[] objsToClose1, objsToClose2;
			lock (lockObj) {
				Debug.Assert(objectIds.Count == idToObjectId.Count);
				objsToClose1 = objectIds.Values.ToArray();
				objsToClose2 = hiddenObjectIds.ToArray();
				objectIds.Clear();
				idToObjectId.Clear();
			}
			foreach (var obj in objsToClose1)
				obj.Close(dispatcher);
			foreach (var obj in objsToClose2)
				obj.Close(dispatcher);
		}
	}
}
