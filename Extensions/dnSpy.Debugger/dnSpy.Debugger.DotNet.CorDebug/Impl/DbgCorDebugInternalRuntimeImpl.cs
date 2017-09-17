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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation;
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

		readonly DbgEngineImpl engine;

		public DbgCorDebugInternalRuntimeImpl(DbgEngineImpl engine, DbgRuntime runtime, DmdRuntime reflectionRuntime, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Version = new CorDebugRuntimeVersion(kind, version ?? throw new ArgumentNullException(nameof(version)));
			ClrFilename = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = runtimeDir ?? throw new ArgumentNullException(nameof(runtimeDir));
			Dispatcher = new DbgDotNetDispatcherImpl(engine);
			reflectionRuntime.GetOrCreateData(() => runtime);
		}

		sealed class GetFrameMethodState {
			public bool Initialized;
			public DmdMethodBase Method;
		}

		public DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetFrameMethodCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetFrameMethodCore(context, frame, cancellationToken));
		}

		DmdMethodBase GetFrameMethodCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
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

		CorType GetType(CorAppDomain appDomain, DmdType type) => CorDebugTypeCreator.GetType(engine, appDomain, type);

		static CorValue GetObjectOrPrimitiveValue(CorValue value) {
			if (value.IsReference) {
				if (value.IsNull)
					throw new InvalidOperationException();
				value = value.DereferencedValue ?? throw new InvalidOperationException();
			}
			if (value.IsBox)
				value = value.BoxedValue ?? throw new InvalidOperationException();
			return value;
		}

		public DbgDotNetValueResult LoadField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return LoadFieldCore(context, frame, obj, field, cancellationToken);
			return Dispatcher.Invoke(() => LoadFieldCore(context, frame, obj, field, cancellationToken));
		}

		DbgDotNetValueResult LoadFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			var appDomain = ilFrame.GetCorAppDomain();

			int hr;
			CorType corFieldDeclType;
			if (obj == null) {
				if (!field.IsStatic)
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

				if (field.IsLiteral) {
					//TODO:
				}
				else {
					corFieldDeclType = GetType(appDomain, field.DeclaringType);
					//TODO: We don't get an error when reading 'System.Reflection.ConstructorInfo.ConstructorName' even when
					//		the cctor hasn't executed yet. It returns a null value instead of an error.
					var fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out hr);
					if (fieldValue == null) {
						if (hr == CordbgErrors.CORDBG_E_CLASS_NOT_LOADED) {
							//TODO:
						}
						else if (hr == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE) {
							//TODO: Func-eval the cctor (if it exists), but only do it once per type
						}
						else
							Debug.Fail($"Couldn't read static field {field.DeclaringType}.{field.Name}, error: 0x{hr.ToString("X8")}");
					}
					if (fieldValue == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					return new DbgDotNetValueResult(engine.CreateDotNetValue_CorDebug(fieldValue, frame.Thread.AppDomain.GetReflectionAppDomain(), tryCreateStrongHandle: true), valueIsException: false);
				}
			}
			else {
				if (field.IsStatic)
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

				var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
				corFieldDeclType = GetType(appDomain, field.DeclaringType);
				var objValue = GetObjectOrPrimitiveValue(objImp.Value);
				if (objValue.IsObject) {
					var fieldValue = objValue.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr);
					if (fieldValue == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					return new DbgDotNetValueResult(engine.CreateDotNetValue_CorDebug(fieldValue, frame.Thread.AppDomain.GetReflectionAppDomain(), tryCreateStrongHandle: true), valueIsException: false);
				}
				else {
					if (IsPrimitiveValueType(objValue.ElementType)) {
						//TODO:
					}
				}
			}

			return new DbgDotNetValueResult("NYI");//TODO:
		}

		static bool IsPrimitiveValueType(CorElementType etype) {
			switch (etype) {
			case CorElementType.Boolean:
			case CorElementType.Char:
			case CorElementType.I1:
			case CorElementType.U1:
			case CorElementType.I2:
			case CorElementType.U2:
			case CorElementType.I4:
			case CorElementType.U4:
			case CorElementType.I8:
			case CorElementType.U8:
			case CorElementType.R4:
			case CorElementType.R8:
			case CorElementType.I:
			case CorElementType.U:
				return true;

			default:
				return false;
			}
		}

		public string StoreField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, DbgDotNetValue value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return StoreFieldCore(context, frame, obj, field, value, cancellationToken);
			return Dispatcher.Invoke(() => StoreFieldCore(context, frame, obj, field, value, cancellationToken));
		}

		string StoreFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, DbgDotNetValue value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return "NYI";//TODO:
		}

		public DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CallCore(context, frame, obj, method, arguments, cancellationToken);
			return Dispatcher.Invoke(() => CallCore(context, frame, obj, method, arguments, cancellationToken));
		}

		DbgDotNetValueResult CallCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return engine.Eval_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), method, obj, arguments, cancellationToken);
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetAliasesCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetAliasesCore(context, frame, cancellationToken));
		}

		DbgDotNetAliasInfo[] GetAliasesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetAliasInfo>();//TODO:
		}

		public DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetExceptionsCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetExceptionsCore(context, frame, cancellationToken));
		}

		DbgDotNetExceptionInfo[] GetExceptionsCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetExceptionInfo>();//TODO:
		}

		public DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetReturnValuesCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetReturnValuesCore(context, frame, cancellationToken));
		}

		DbgDotNetReturnValueInfo[] GetReturnValuesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetReturnValueInfo>();//TODO:
		}

		DbgDotNetValueResult CreateValue(CorValue value, ILDbgEngineStackFrame ilFrame) {
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			var dnValue = engine.CreateDotNetValue_CorDebug(value, reflectionAppDomain, tryCreateStrongHandle: true);
			return new DbgDotNetValueResult(dnValue, valueIsException: false);
		}

		public DbgDotNetValueResult GetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetLocalValueCore(context, frame, index, cancellationToken);
			return Dispatcher.Invoke(() => GetLocalValueCore(context, frame, index, cancellationToken));
		}

		DbgDotNetValueResult GetLocalValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			var value = ilFrame.CorFrame.GetILLocal(index, out int hr);
			if (value == null)
				return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
			return CreateValue(value, ilFrame);
		}

		public DbgDotNetValueResult GetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueCore(context, frame, index, cancellationToken);
			return Dispatcher.Invoke(() => GetParameterValueCore(context, frame, index, cancellationToken));
		}

		DbgDotNetValueResult GetParameterValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			var value = ilFrame.CorFrame.GetILArgument(index, out int hr);
			if (value == null)
				return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
			return CreateValue(value, ilFrame);
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

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
