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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Steppers.Engine {
	static class TaskEvalUtils {
		/// <summary>
		/// Gets the builder instance. It's assumed to be stored in a field in the current 'this' instance.
		/// 
		/// The decompiler should already know the field. If that info isn't available, we'll try to find
		/// the field by name, and if that fails, by field type.
		/// 
		/// null is returned if we couldn't find the field or if we failed to read the field.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="builderFieldModule">Module of builder field or null if unknown</param>
		/// <param name="builderFieldToken">Token of builder field or 0 if unknown</param>
		/// <returns></returns>
		public static DbgDotNetValue? TryGetBuilder(DbgEvaluationInfo evalInfo, DmdModule? builderFieldModule, uint builderFieldToken) {
			DbgDotNetValueResult thisArg = default;
			DbgDotNetValueResult tmpResult = default;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				thisArg = runtime.GetParameterValue(evalInfo, 0);
				if (!thisArg.IsNormalResult || thisArg.Value!.IsNull)
					return null;
				if (thisArg.Value.Type.IsByRef) {
					tmpResult = thisArg.Value.LoadIndirect();
					if (!tmpResult.IsNormalResult || tmpResult.Value!.IsNull)
						return null;
					thisArg.Value?.Dispose();
					thisArg = tmpResult;
					tmpResult = default;
				}

				DmdFieldInfo? builderField = null;
				if (builderFieldModule is not null && builderFieldToken != 0)
					builderField = thisArg.Value.Type.GetField(builderFieldModule, (int)builderFieldToken);
				if (builderField is null)
					builderField = TryGetBuilderField(thisArg.Value.Type);
				if (builderField is null)
					return null;
				Debug.Assert((object)builderField == TryGetBuilderFieldByType(thisArg.Value.Type));
				Debug2.Assert(TryGetBuilderFieldByname(thisArg.Value.Type) is null ||
					(object?)TryGetBuilderFieldByname(thisArg.Value.Type) == TryGetBuilderFieldByType(thisArg.Value.Type));
				tmpResult = runtime.LoadField(evalInfo, thisArg.Value, builderField);
				if (!tmpResult.IsNormalResult || tmpResult.Value!.IsNull)
					return null;
				var fieldValue = tmpResult.Value;
				tmpResult = default;
				return fieldValue;
			}
			finally {
				thisArg.Value?.Dispose();
				tmpResult.Value?.Dispose();
			}
		}

		static DmdFieldInfo? TryGetBuilderField(DmdType type) =>
			TryGetBuilderFieldByname(type) ?? TryGetBuilderFieldByType(type);

		static DmdFieldInfo? TryGetBuilderFieldByname(DmdType type) {
			foreach (var name in KnownMemberNames.builderFieldNames) {
				const DmdBindingFlags flags = DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic;
				if (type.GetField(name, flags) is DmdFieldInfo field)
					return field;
			}
			return null;
		}

		static DmdFieldInfo? TryGetBuilderFieldByType(DmdType type) {
			DmdFieldInfo? builderField = null;
			foreach (var field in type.Fields) {
				var fieldType = field.FieldType;
				if (fieldType.IsNested)
					continue;
				if (fieldType.IsConstructedGenericType)
					fieldType = fieldType.GetGenericTypeDefinition();
				foreach (var info in builderWellKnownTypeNames) {
					if (fieldType.MetadataNamespace == info.@namespace && fieldType.MetadataName == info.name)
						return field;
				}
				if (builderField is null && fieldType.MetadataName is not null &&
					(fieldType.MetadataName.EndsWith("MethodBuilder", StringComparison.Ordinal) ||
					fieldType.MetadataName.EndsWith("MethodBuilder`1", StringComparison.Ordinal))) {
					builderField = field;
				}
			}

			return builderField;
		}
		static readonly (string @namespace, string name)[] builderWellKnownTypeNames = new(string, string)[] {
			("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder"),
			("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder`1"),
			("System.Runtime.CompilerServices", "AsyncVoidMethodBuilder"),
			("System.Runtime.CompilerServices", "AsyncValueTaskMethodBuilder"),
			("System.Runtime.CompilerServices", "AsyncValueTaskMethodBuilder`1"),
		};

		/// <summary>
		/// Gets the task's object id or null on failure
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="builderValue">Builder value, see <see cref="TryGetBuilder(DbgEvaluationInfo, DmdModule, uint)"/></param>
		/// <returns></returns>
		public static DbgDotNetValue? TryGetTaskObjectId(DbgEvaluationInfo evalInfo, DbgDotNetValue builderValue) {
			var result =
				TryGetTaskObjectId_FrameworkBuilder(evalInfo, builderValue) ??
				TryGetTaskObjectId_ObjectIdForDebugger(evalInfo, builderValue) ??
				TryGetTaskObjectId_TaskProperty(evalInfo, builderValue);
			Debug2.Assert(result is null || !result.IsNull);
			return result;
		}

		static DbgDotNetValue? TryGetTaskObjectId_FrameworkBuilder(DbgEvaluationInfo evalInfo, DbgDotNetValue builderValue) {
			DbgDotNetValueResult fieldResult1 = default;
			DbgDotNetValueResult fieldResult2 = default;
			DbgDotNetValue? resultValue = null;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				DmdFieldInfo? field;
				var currInst = builderValue;

				field = currInst.Type.GetField(KnownMemberNames.AsyncTaskMethodBuilder_Builder_FieldName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				if (field is not null) {
					fieldResult1 = runtime.LoadField(evalInfo, currInst, field);
					if (fieldResult1.IsNormalResult)
						currInst = fieldResult1.Value!;
				}

				field = currInst.Type.GetField(KnownMemberNames.Builder_Task_FieldName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				if (field is not null) {
					fieldResult2 = runtime.LoadField(evalInfo, currInst, field);
					if (fieldResult2.IsNormalResult && !fieldResult2.Value!.IsNull)
						return resultValue = fieldResult2.Value;
				}

				return null;
			}
			finally {
				if (fieldResult1.Value != resultValue)
					fieldResult1.Value?.Dispose();
				if (fieldResult2.Value != resultValue)
					fieldResult2.Value?.Dispose();
			}
		}

		static DbgDotNetValue? TryGetTaskObjectId_ObjectIdForDebugger(DbgEvaluationInfo evalInfo, DbgDotNetValue builderValue) {
			DbgDotNetValueResult getObjectIdTaskResult = default;
			DbgDotNetValue? resultValue = null;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();

				var prop = builderValue.Type.GetProperty(KnownMemberNames.Builder_ObjectIdForDebugger_PropertyName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				var getMethod = prop?.GetGetMethod(DmdGetAccessorOptions.All);
				if (getMethod is not null && getMethod.GetMethodSignature().GetParameterTypes().Count == 0) {
					getObjectIdTaskResult = runtime.Call(evalInfo, builderValue, getMethod, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
					if (getObjectIdTaskResult.IsNormalResult && !getObjectIdTaskResult.Value!.IsNull)
						return resultValue = getObjectIdTaskResult.Value;
				}

				return null;
			}
			finally {
				if (getObjectIdTaskResult.Value != resultValue)
					getObjectIdTaskResult.Value?.Dispose();
			}
		}

		static DbgDotNetValue? TryGetTaskObjectId_TaskProperty(DbgEvaluationInfo evalInfo, DbgDotNetValue builderValue) {
			DbgDotNetValueResult getTaskResult = default;
			DbgDotNetValueResult taskFieldResult = default;
			DbgDotNetValue? resultValue = null;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();

				var prop = builderValue.Type.GetProperty(KnownMemberNames.Builder_Task_PropertyName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				var getMethod = prop?.GetGetMethod(DmdGetAccessorOptions.All);
				if (getMethod is null || getMethod.GetMethodSignature().GetParameterTypes().Count != 0)
					return null;

				getTaskResult = runtime.Call(evalInfo, builderValue, getMethod, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
				if (!getTaskResult.IsNormalResult || getTaskResult.Value!.IsNull)
					return null;
				if (!getTaskResult.Value.Type.IsValueType)
					return resultValue = getTaskResult.Value;

				var field = getTaskResult.Value.Type.GetField(KnownMemberNames.ValueTask_Task_FieldName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				if (field is null)
					field = getTaskResult.Value.Type.GetField(KnownMemberNames.ValueTask_Obj_FieldName, DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
				if (field is not null) {
					taskFieldResult = runtime.LoadField(evalInfo, getTaskResult.Value, field);
					if (taskFieldResult.IsNormalResult && !taskFieldResult.Value!.IsNull) {
						var taskType = taskFieldResult.Value.Type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Threading_Tasks_Task, isOptional: true);
						if (taskType is not null && taskFieldResult.Value.Type.IsSubclassOf(taskType))
							return resultValue = taskFieldResult.Value;
					}
				}

				return null;
			}
			finally {
				if (getTaskResult.Value != resultValue)
					getTaskResult.Value?.Dispose();
				if (taskFieldResult.Value != resultValue)
					taskFieldResult.Value?.Dispose();
			}
		}

		const string Task_NotifyDebuggerOfWaitCompletion_MethodName = "NotifyDebuggerOfWaitCompletion";
		sealed class AsyncStepOutState {
			public readonly DmdMethodInfo? NotifyDebuggerOfWaitCompletionMethod;
			public AsyncStepOutState(DmdMethodInfo? notifyDebuggerOfWaitCompletionMethod) => NotifyDebuggerOfWaitCompletionMethod = notifyDebuggerOfWaitCompletionMethod;
		}

		static AsyncStepOutState GetAsyncStepOutState(DmdAppDomain appDomain) {
			if (!appDomain.TryGetData(out AsyncStepOutState? state))
				state = SupportsAsyncStepOutCore(appDomain);
			return state;

			AsyncStepOutState SupportsAsyncStepOutCore(DmdAppDomain appDomain2) {
				var task = appDomain2.GetWellKnownType(DmdWellKnownType.System_Threading_Tasks_Task, isOptional: true);
				var method = task?.GetMethod(Task_NotifyDebuggerOfWaitCompletion_MethodName, DmdSignatureCallingConvention.HasThis,
					0, appDomain2.System_Void, Array.Empty<DmdType>(), throwOnError: false) as DmdMethodInfo;
				return appDomain2.GetOrCreateData(() => new AsyncStepOutState(method));
			}
		}

		public static bool SupportsAsyncStepOut(DmdAppDomain? appDomain) =>
			GetNotifyDebuggerOfWaitCompletionMethod(appDomain) is not null;

		public static DmdMethodInfo? GetNotifyDebuggerOfWaitCompletionMethod(DmdAppDomain? appDomain) =>
			appDomain is null ? null : GetAsyncStepOutState(appDomain).NotifyDebuggerOfWaitCompletionMethod;

		const string SetNotificationForWaitCompletion_Name = "SetNotificationForWaitCompletion";

		public static (bool success, DbgDotNetValue? taskValue) CallSetNotificationForWaitCompletion(DbgEvaluationInfo evalInfo, DbgModule builderFieldModule, uint builderFieldToken, bool value) {
			DbgDotNetValue? builderValue = null;
			DbgDotNetValue? taskValue = null;
			bool success = false;
			try {
				builderValue = TryGetBuilder(evalInfo, builderFieldModule.GetReflectionModule(), builderFieldToken);
				if (builderValue is null)
					return (false, null);
				bool calledMethod = TryCallSetNotificationForWaitCompletion(evalInfo, builderValue, value);
				taskValue = TryGetTaskValue(evalInfo, builderValue);
				if (!calledMethod && taskValue is not null)
					calledMethod = TryCallSetNotificationForWaitCompletion(evalInfo, taskValue, value);
				if (!calledMethod)
					return (false, null);
				success = true;
				return (true, taskValue);
			}
			finally {
				builderValue?.Dispose();
				if (!success)
					taskValue?.Dispose();
			}
		}

		static bool TryCallSetNotificationForWaitCompletion(DbgEvaluationInfo evalInfo, DbgDotNetValue builder, bool value) {
			var appDomain = builder.Type.AppDomain;
			var method = builder.Type.GetMethod(SetNotificationForWaitCompletion_Name, DmdSignatureCallingConvention.HasThis,
				0, appDomain.System_Void, new[] { appDomain.System_Boolean }, throwOnError: false);
			if (method is null)
				return false;

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var result = runtime.Call(evalInfo, builder, method, new object[] { value }, DbgDotNetInvokeOptions.None);
			result.Value?.Dispose();
			// Return true even if it failed due to an exception or if it timed out or some other error
			return true;
		}

		static DbgDotNetValue? TryGetTaskValue(DbgEvaluationInfo evalInfo, DbgDotNetValue value) {
			var result = TryGetTaskObjectId_TaskProperty(evalInfo, value);
			Debug2.Assert(result is null || !result.IsNull);
			return result;
		}
	}
}
