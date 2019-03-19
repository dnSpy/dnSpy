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
using System.Threading;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DbgDotNetObjectIdImpl : DbgDotNetObjectId {
		public override uint Id { get; }

		internal ObjectMirror Value { get; }
		internal DmdAppDomain ReflectionAppDomain { get; }
		internal DbgDotNetValue GCHandleValue { get; }

		readonly DbgMonoDebugInternalRuntimeImpl owner;
		int disposed;

		public DbgDotNetObjectIdImpl(DbgMonoDebugInternalRuntimeImpl owner, uint id, DbgDotNetValue gcHandleValue, ObjectMirror value, DmdAppDomain reflectionAppDomain) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Id = id;
			GCHandleValue = gcHandleValue ?? throw new ArgumentNullException(nameof(gcHandleValue));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			ReflectionAppDomain = reflectionAppDomain ?? throw new ArgumentNullException(nameof(reflectionAppDomain));
		}

		public override void Dispose() {
			if (Interlocked.Exchange(ref disposed, 1) == 0)
				owner.FreeObjectId(this);
		}
	}
}
