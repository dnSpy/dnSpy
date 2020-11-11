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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	abstract class MonoTypeLoader {
		public abstract TypeMirror? Load(AssemblyMirror assembly, string typeFullName);
	}

	sealed class MonoTypeLoaderImpl : MonoTypeLoader {
		readonly DbgEngineImpl engine;
		readonly DbgEvaluationInfo evalInfo;

		public MonoTypeLoaderImpl(DbgEngineImpl engine, DbgEvaluationInfo evalInfo) {
			this.engine = engine;
			this.evalInfo = evalInfo;
		}

		sealed class LoaderState {
			public readonly HashSet<string> LoadedTypes = new HashSet<string>(StringComparer.Ordinal);
			public DmdMethodBase? Method_System_Reflection_Assembly_GetType_String;
			public DmdMethodBase? Method_System_Array_CreateInstance_Type_Int32;
		}

		public override TypeMirror? Load(AssemblyMirror assembly, string typeFullName) {
			var res = engine.CheckFuncEval(evalInfo.Context);
			if (res is not null)
				return null;
			var appDomain = evalInfo.Frame.AppDomain;
			if (appDomain is null)
				return null;
			var state = appDomain.GetOrCreateData<LoaderState>();
			if (!state.LoadedTypes.Add(typeFullName))
				return null;

			var reflectionAppDomain = appDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
			var runtime = engine.DotNetRuntime;

			DbgDotNetValue? asmTypeValue = null;
			DbgDotNetValueResult result1 = default;
			DbgDotNetValueResult result2 = default;
			try {
				if (state.Method_System_Reflection_Assembly_GetType_String is null) {
					var assemblyType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly);
					state.Method_System_Reflection_Assembly_GetType_String =
						assemblyType.GetMethod(nameof(System.Reflection.Assembly.GetType), DmdSignatureCallingConvention.HasThis,
						0, reflectionAppDomain.System_Type, new[] { reflectionAppDomain.System_String }, throwOnError: false);
					Debug2.Assert(state.Method_System_Reflection_Assembly_GetType_String is not null);
					if (state.Method_System_Reflection_Assembly_GetType_String is null)
						return null;

					state.Method_System_Array_CreateInstance_Type_Int32 =
						reflectionAppDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default,
						0, reflectionAppDomain.System_Array, new[] { reflectionAppDomain.System_Type, reflectionAppDomain.System_Int32 }, throwOnError: false);
					Debug2.Assert(state.Method_System_Array_CreateInstance_Type_Int32 is not null);
					if (state.Method_System_Array_CreateInstance_Type_Int32 is null)
						return null;
				}

				if (state.Method_System_Reflection_Assembly_GetType_String is null || state.Method_System_Array_CreateInstance_Type_Int32 is null)
					return null;

				asmTypeValue = engine.CreateDotNetValue_MonoDebug(reflectionAppDomain, assembly.GetAssemblyObject(), null);
				result1 = runtime.Call(evalInfo, asmTypeValue, state.Method_System_Reflection_Assembly_GetType_String,
					new[] { typeFullName }, DbgDotNetInvokeOptions.None);
				if (result1.IsNormalResult) {
					result2 = runtime.Call(evalInfo, null, state.Method_System_Array_CreateInstance_Type_Int32,
						new object[2] { result1.Value!, 0 }, DbgDotNetInvokeOptions.None);
					if (result2.IsNormalResult) {
						var arrayType = result2.Value!.Type;
						Debug.Assert(arrayType.IsSZArray);
						if (arrayType.IsSZArray)
							return MonoDebugTypeCreator.TryGetType(arrayType.GetElementType()!);
					}
				}

				return null;
			}
			finally {
				asmTypeValue?.Dispose();
				result1.Value?.Dispose();
				result2.Value?.Dispose();
			}
		}
	}
}
