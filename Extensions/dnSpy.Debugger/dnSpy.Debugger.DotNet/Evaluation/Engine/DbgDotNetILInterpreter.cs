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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetILInterpreter {
		public abstract DbgDotNetILInterpreterState CreateState(DbgEvaluationContext context, byte[] assembly);
		public abstract DbgDotNetValue Execute(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetILInterpreterState state, string typeName, string methodName, DbgValueNodeEvaluationOptions options, out DmdType expectedType, CancellationToken cancellationToken);
	}

	abstract class DbgDotNetILInterpreterState {
	}

	sealed class DbgDotNetILInterpreterImpl : DbgDotNetILInterpreter {
		sealed class DbgDotNetILInterpreterStateImpl : DbgDotNetILInterpreterState {
			public ILVM ILVM { get; }

			readonly List<(int appDomainId, string typeName, string methodName, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, MethodInfoState methodState)> methodStates;
			readonly List<(int appDomainId, DmdAssembly assembly)> assemblies;
			readonly byte[] assemblyBytes;

			public DbgDotNetILInterpreterStateImpl(byte[] assemblyBytes) {
				ILVM = ILVMFactory.Create();
				methodStates = new List<(int, string, string, IList<DmdType>, IList<DmdType>, MethodInfoState)>();
				assemblies = new List<(int, DmdAssembly)>();
				this.assemblyBytes = assemblyBytes ?? throw new ArgumentNullException(nameof(assemblyBytes));
			}

			public MethodInfoState GetOrCreateMethodInfoState(DbgAppDomain appDomain, string typeName, string methodName, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
				int appDomainId = appDomain.Id;
				foreach (var info in methodStates) {
					if (methodName == info.methodName && typeName == info.typeName && info.appDomainId == appDomainId && Equals(info.genericTypeArguments, genericTypeArguments) && Equals(info.genericMethodArguments, genericMethodArguments))
						return info.methodState;
				}

				var methodState = new MethodInfoState();
				methodStates.Add((appDomain.Id, typeName, methodName, genericTypeArguments, genericMethodArguments, methodState));
				return methodState;
			}

			bool Equals(IList<DmdType> a, IList<DmdType> b) {
				if (a == b)
					return true;
				if (a.Count != b.Count)
					return false;
				for (int i = 0; i < a.Count; i++) {
					if (a[i] != b[i])
						return false;
				}
				return true;
			}

			public DmdAssembly GetOrCreateReflectionAssembly(DbgAppDomain appDomain) {
				foreach (var info in assemblies) {
					if (info.appDomainId == appDomain.Id)
						return info.assembly;
				}
				var reflectionAppDomain = appDomain.GetReflectionAppDomain();
				if (reflectionAppDomain == null)
					throw new InvalidOperationException();

				const bool isInMemory = true;
				const bool isDynamic = false;
				var fullyQualifiedName = DmdModule.GetFullyQualifiedName(isInMemory, isDynamic, fullyQualifiedName: null);
				Func<DmdLazyMetadataBytes> getMetadata = () => new DmdLazyMetadataBytesArray(assemblyBytes, isFileLayout: true);
				var assembly = reflectionAppDomain.CreateSyntheticAssembly(getMetadata, isInMemory, isDynamic, fullyQualifiedName, string.Empty);

				assemblies.Add((appDomain.Id, assembly));
				return assembly;
			}
		}

		sealed class MethodInfoState {
			public ILVMExecuteState ILVMExecuteState { get; set; }
			public DmdType ExpectedType { get; set; }
		}

		public override DbgDotNetILInterpreterState CreateState(DbgEvaluationContext context, byte[] assembly) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			return new DbgDotNetILInterpreterStateImpl(assembly);
		}

		public override DbgDotNetValue Execute(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetILInterpreterState state, string typeName, string methodName, DbgValueNodeEvaluationOptions options, out DmdType expectedType, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (state == null)
				throw new ArgumentNullException(nameof(state));
			if (typeName == null)
				throw new ArgumentNullException(nameof(typeName));
			if (methodName == null)
				throw new ArgumentNullException(nameof(methodName));
			var stateImpl = state as DbgDotNetILInterpreterStateImpl;
			if (stateImpl == null)
				throw new ArgumentException();

			var debuggerRuntime = GetDebuggerRuntime(context.Runtime);
			debuggerRuntime.Runtime.Dispatcher.VerifyAccess();

			var appDomain = frame.Thread.AppDomain;
			if (appDomain == null)
				throw new ArgumentException("No AppDomain available");

			var reflectionAssembly = stateImpl.GetOrCreateReflectionAssembly(appDomain);
			var frameMethod = frame.Runtime.GetDotNetRuntime().GetFrameMethod(context, frame, cancellationToken) ?? throw new InvalidOperationException();
			var genericTypeArguments = frameMethod.ReflectedType.GetGenericArguments();
			var genericMethodArguments = frameMethod.GetGenericArguments();
			var methodState = stateImpl.GetOrCreateMethodInfoState(appDomain, typeName, methodName, genericTypeArguments, genericMethodArguments);

			DbgDotNetValue resultValue;
			using (reflectionAssembly.AppDomain.AddTemporaryAssembly(reflectionAssembly)) {
				var ilvmState = methodState.ILVMExecuteState;
				if (ilvmState == null) {
					var type = reflectionAssembly.GetTypeThrow(typeName);
					Debug.Assert(type.GetGenericArguments().Count == genericTypeArguments.Count);
					if (genericTypeArguments.Count != 0)
						type = type.MakeGenericType(genericTypeArguments);
					const DmdBindingFlags bindingFlags = DmdBindingFlags.Static | DmdBindingFlags.Public | DmdBindingFlags.NonPublic;
					var method = type.GetMethod(methodName, bindingFlags) ?? throw new InvalidOperationException();
					Debug.Assert(method.GetGenericArguments().Count == genericMethodArguments.Count);
					if (genericMethodArguments.Count != 0)
						method = method.MakeGenericMethod(genericMethodArguments);

					methodState.ExpectedType = method.ReturnType;
					ilvmState = stateImpl.ILVM.CreateExecuteState(method);
					methodState.ILVMExecuteState = ilvmState;
				}

				expectedType = methodState.ExpectedType;
				debuggerRuntime.Initialize(context, frame, cancellationToken);
				try {
					var execResult = stateImpl.ILVM.Execute(debuggerRuntime, ilvmState);
					resultValue = debuggerRuntime.GetDotNetValue(execResult);
				}
				finally {
					debuggerRuntime.Clear();
				}
			}

			return resultValue;
		}

		sealed class State {
			public DebuggerRuntimeImpl DebuggerRuntime;
		}

		DebuggerRuntimeImpl GetDebuggerRuntime(DbgRuntime runtime) {
			var state = StateWithKey<State>.GetOrCreate(runtime, this);
			if (state.DebuggerRuntime == null)
				state.DebuggerRuntime = new DebuggerRuntimeImpl(runtime.GetDotNetRuntime(), runtime.Process.PointerSize);
			return state.DebuggerRuntime;
		}
	}
}
