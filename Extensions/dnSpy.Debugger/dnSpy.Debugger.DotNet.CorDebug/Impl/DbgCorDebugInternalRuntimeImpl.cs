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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DbgCorDebugInternalRuntimeImpl : DbgCorDebugInternalRuntime, IDbgDotNetRuntime {
		public override DbgRuntime Runtime { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override CorDebugRuntimeVersion Version { get; }
		public override string ClrFilename { get; }
		public override string RuntimeDirectory { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public bool SupportsObjectIds => false;//TODO:

		public DbgCorDebugInternalRuntimeImpl(DbgEngineImpl owner, DbgRuntime runtime, DmdRuntime reflectionRuntime, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Version = new CorDebugRuntimeVersion(kind, version ?? throw new ArgumentNullException(nameof(version)));
			ClrFilename = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = runtimeDir ?? throw new ArgumentNullException(nameof(runtimeDir));
			Dispatcher = new DbgDotNetDispatcherImpl(owner);
			reflectionRuntime.GetOrCreateData(() => runtime);
		}

		sealed class GetFrameMethodState {
			public bool Initialized;
			public DmdMethodBase Method;
		}

		public DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			var state = frame.GetOrCreateData<GetFrameMethodState>();
			if (!state.Initialized) {
				if (ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame)) {
					ilFrame.GetFrameMethodInfo(out var module, out var methodMetadataToken, out var genericTypeArguments, out var genericMethodArguments);
					var method = module.ResolveMethod(methodMetadataToken, (IList<DmdType>)null, null, DmdResolveOptions.ThrowOnError);
					if (genericTypeArguments.Count != 0) {
						var type = method.ReflectedType.MakeGenericType(genericTypeArguments);
						method = type.GetMethod(method.Module, method.MetadataToken, throwOnError: true);
					}
					if (genericMethodArguments.Count != 0)
						method = ((DmdMethodInfo)method).MakeGenericMethod(genericMethodArguments);
					state.Method = method;
				}
				state.Initialized = true;
			}
			return state.Method;
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetAliasInfo>();//TODO:
		}

		public DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetExceptionInfo>();//TODO:
		}

		public DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetReturnValueInfo>();//TODO:
		}

		public DbgDotNetValue GetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return null;//TODO:
		}

		public DbgDotNetValue GetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return null;//TODO:
		}

		public bool CanCreateObjectId(DbgDotNetValue value) {
			return false;//TODO:
		}

		public DbgDotNetEngineObjectId CreateObjectId(DbgDotNetValue value, uint id) {
			throw new NotImplementedException();//TODO:
		}

		public bool Equals(DbgDotNetEngineObjectId objectId, DbgDotNetValue value) {
			throw new NotImplementedException();//TODO:
		}

		public int GetHashCode(DbgDotNetEngineObjectId objectId) {
			throw new NotImplementedException();//TODO:
		}

		public int GetHashCode(DbgDotNetValue value) {
			throw new NotImplementedException();//TODO:
		}

		public DbgDotNetValue GetValue(DbgEvaluationContext context, DbgDotNetEngineObjectId objectId, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		protected override void CloseCore() { }
	}
}
