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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineObjectIdFactoryImpl : DbgEngineObjectIdFactory {
		readonly Guid runtimeGuid;

		public DbgEngineObjectIdFactoryImpl(Guid runtimeGuid) => this.runtimeGuid = runtimeGuid;

		public override bool CanCreateObjectId(DbgEngineValue value) {
			var dnValue = ((DbgEngineValueImpl)value).DotNetValue;
			var runtime = dnValue.TryGetDotNetRuntime();
			if (runtime == null)
				return false;
			if ((runtime.Features & DbgDotNetRuntimeFeatures.ObjectIds) == 0)
				return false;
			return runtime.CanCreateObjectId(dnValue);
		}

		public override DbgEngineObjectId CreateObjectId(DbgEngineValue value, uint id) {
			var dnValue = ((DbgEngineValueImpl)value).DotNetValue;
			var runtime = dnValue.TryGetDotNetRuntime();
			if (runtime == null)
				return null;
			if ((runtime.Features & DbgDotNetRuntimeFeatures.ObjectIds) == 0)
				return null;
			var objectId = runtime.CreateObjectId(dnValue, id);
			if (objectId == null)
				return null;
			try {
				return new DbgEngineObjectIdImpl(runtime, objectId);
			}
			catch {
				objectId.Dispose();
				throw;
			}
		}

		public override bool Equals(DbgEngineObjectId objectId, DbgEngineValue value) {
			var dnObjectId = ((DbgEngineObjectIdImpl)objectId).DotNetObjectId;
			var dnValue = ((DbgEngineValueImpl)value).DotNetValue;
			var runtime = dnValue.TryGetDotNetRuntime();
			if (runtime == null)
				return false;
			if ((runtime.Features & DbgDotNetRuntimeFeatures.ObjectIds) == 0)
				return false;
			return runtime.Equals(dnObjectId, dnValue);
		}

		public override int GetHashCode(DbgEngineObjectId objectId) {
			var impl = (DbgEngineObjectIdImpl)objectId;
			var dnObjectId = impl.DotNetObjectId;
			if ((impl.Runtime.Features & DbgDotNetRuntimeFeatures.ObjectIds) == 0)
				return 0;
			return impl.Runtime.GetHashCode(dnObjectId);
		}

		public override int GetHashCode(DbgEngineValue value) {
			var dnValue = ((DbgEngineValueImpl)value).DotNetValue;
			var runtime = dnValue.TryGetDotNetRuntime();
			if (runtime == null)
				return 0;
			if ((runtime.Features & DbgDotNetRuntimeFeatures.ObjectIds) == 0)
				return 0;
			return runtime.GetHashCode(dnValue);
		}
	}
}
