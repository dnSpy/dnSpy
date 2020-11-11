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

using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter.Hooks {
	sealed class dnSpy_Roslyn_ExpressionEvaluator_IntrinsicMethods : DotNetClassHook {
		readonly IDebuggerRuntime runtime;

		public dnSpy_Roslyn_ExpressionEvaluator_IntrinsicMethods(IDebuggerRuntime runtime) => this.runtime = runtime;

		public override DbgDotNetValue? Call(DotNetClassHookCallOptions options, DbgDotNetValue? objValue, DmdMethodBase method, ILValue[] arguments) {
			if (!method.Module.IsSynthetic)
				return null;
			Debug.Assert(method.IsStatic);
			if (!method.IsStatic)
				return null;

			var sig = method.GetMethodSignature();
			var ps = sig.GetParameterTypes();
			var appDomain = method.AppDomain;
			DmdType type;
			string name;
			switch (method.Name) {
			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This function returns the object at a given address and is used by the Debug Managed Memory feature. In the
			// C# Expression Compiler, this function is called by evaluating expressions such as @0x12341234. This means
			// evaluate the object at address 0x12341234."
			case ExpressionCompilerConstants.GetObjectAtAddressMethodName:
				// public static object GetObjectAtAddress(ulong address)
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.System_Object && ps.Count == 1 && ps[0] == appDomain.System_UInt64) {
					ulong address = runtime.ToUInt64(arguments[0]);
					return runtime.GetObjectAtAddress(address);
				}
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This function returns the exception on the current thread"
			case ExpressionCompilerConstants.GetExceptionMethodName:
				// public static Exception GetException()
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.GetWellKnownType(DmdWellKnownType.System_Exception, isOptional: true) && ps.Count == 0)
					return runtime.GetException();
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This function returns the stowed exception"
			case ExpressionCompilerConstants.GetStowedExceptionMethodName:
				// public static Exception GetStowedException()
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.GetWellKnownType(DmdWellKnownType.System_Exception, isOptional: true) && ps.Count == 0)
					return runtime.GetStowedException();
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This function returns the return value of the given index. The indexes are 1-based. If 0 is passed, this
			// function returns the last return value."
			case ExpressionCompilerConstants.GetReturnValueMethodName:
				// public static object GetReturnValue(int index)
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.System_Object && ps.Count == 1 && ps[0] == appDomain.System_Int32) {
					int index = runtime.ToInt32(arguments[0]);
					return runtime.GetReturnValue(index);
				}
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This method is used to define new variables in the debugger. It takes a type and name. If there is additional
			// type information needed by the compiler that can't be represented in the metadata, it can pass the information
			// in the custom type info payload. The custom type id is a GUID unique to the language defining the variable.
			// These values are passed back for future calls to GetAliases."
			case ExpressionCompilerConstants.CreateVariableMethodName:
				// public static void CreateVariable(Type type, string name, Guid customTypeInfoPayloadTypeId, byte[] customTypeInfoPayload)
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.System_Void && ps.Count == 4 && ps[0] == appDomain.System_Type &&
					ps[1] == appDomain.System_String && ps[2] == appDomain.GetWellKnownType(DmdWellKnownType.System_Guid, isOptional: true) &&
					ps[3].IsSZArray && ps[3].GetElementType() == appDomain.System_Byte) {
					type = runtime.ToType(arguments[0]);
					name = runtime.ToString(arguments[1]);
					var customTypeInfoPayloadTypeId = runtime.ToGuid(arguments[2]);
					var customTypeInfoPayload = runtime.ToByteArray(arguments[3]);
					runtime.CreateVariable(type, name, customTypeInfoPayloadTypeId, customTypeInfoPayload);
					return new SyntheticNullValue(type.AppDomain.System_Object);
				}
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This function is used to get the value of an alias. It is used only for pseudo-variables created using
			// CreateVariable and for objects IDs that are created using the Make Object ID feature."
			case ExpressionCompilerConstants.GetVariableValueMethodName:
				// public static object GetObjectByAlias(string name)
				if (sig.GenericParameterCount == 0 && sig.ReturnType == appDomain.System_Object && ps.Count == 1 && ps[0] == appDomain.System_String) {
					name = runtime.ToString(arguments[0]);
					return runtime.GetObjectByAlias(name);
				}
				break;

			// https://github.com/Microsoft/ConcordExtensibilitySamples/wiki/CLR-Expression-Evaluator-Intrinsics
			// "This returns a reference (or L-value) to a pseudo-variable. It is used to assign values to pseudo-variables
			// created using CreateVariable. It cannot be used for object IDs because they are immutable."
			case ExpressionCompilerConstants.GetVariableAddressMethodName:
				// public unsafe static T* GetVariableAddress<T>(string name)
				if (!sig.ReturnType.IsPointer)
					break;
				type = sig.ReturnType.GetElementType()!;
				var origMethod = method.ReflectedType!.GetMethod(method.Module, method.MetadataToken);
				Debug2.Assert(origMethod is not null);
				sig = origMethod.GetMethodSignature();
				ps = sig.GetParameterTypes();
				if (sig.GenericParameterCount == 1 && sig.ReturnType.IsPointer &&
					sig.ReturnType.GetElementType()!.TypeSignatureKind == DmdTypeSignatureKind.MethodGenericParameter &&
					sig.ReturnType.GetElementType()!.GenericParameterPosition == 0 && ps.Count == 1 && ps[0] == appDomain.System_String) {
					name = runtime.ToString(arguments[0]);
					return runtime.GetVariableAddress(type, name);
				}
				break;
			}

			Debug.Fail($"Unknown intrinsics method: {method}");
			return null;
		}
	}
}
