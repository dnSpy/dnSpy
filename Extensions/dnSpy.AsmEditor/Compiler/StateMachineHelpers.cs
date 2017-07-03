/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.Compiler {
	static class StateMachineHelpers {
		public static TypeDef GetStateMachineType(MethodDef method) {
			var stateMachineType = GetStateMachineTypeCore(method);
			if (stateMachineType == null)
				return null;
			var body = method.Body;
			if (body == null)
				return null;

			foreach (var instr in body.Instructions) {
				var def = instr.Operand as IMemberDef;
				if (def?.DeclaringType == stateMachineType)
					return stateMachineType;
			}

			return null;
		}

		static TypeDef GetStateMachineTypeCore(MethodDef method) =>
			GetStateMachineTypeFromCustomAttributesCore(method) ??
			GetAsyncStateMachineTypeFromInstructionsCore(method) ??
			GetIteratorStateMachineTypeFromInstructionsCore(method);

		static TypeDef GetStateMachineTypeFromCustomAttributesCore(MethodDef method) {
			foreach (var ca in method.CustomAttributes) {
				if (ca.ConstructorArguments.Count != 1)
					continue;
				if (ca.Constructor?.MethodSig?.Params.Count != 1)
					continue;
				var typeType = (ca.Constructor.MethodSig.Params[0] as ClassOrValueTypeSig)?.TypeDefOrRef;
				if (typeType == null || typeType.FullName != "System.Type")
					continue;
				if (!IsStateMachineTypeAttribute(ca.AttributeType))
					continue;
				var caArg = ca.ConstructorArguments[0];
				var tdr = (caArg.Value as ClassOrValueTypeSig)?.TypeDefOrRef;
				if (tdr == null)
					continue;
				var td = tdr.Module.Find(tdr);
				if (td?.DeclaringType == method.DeclaringType)
					return td;
			}
			return null;
		}

		static bool IsStateMachineTypeAttribute(ITypeDefOrRef tdr) {
			var s = tdr.ReflectionFullName;
			return s == "System.Runtime.CompilerServices.AsyncStateMachineAttribute" ||
					s == "System.Runtime.CompilerServices.IteratorStateMachineAttribute";
		}

		static TypeDef GetAsyncStateMachineTypeFromInstructionsCore(MethodDef method) {
			var body = method.Body;
			if (body == null)
				return null;
			foreach (var local in body.Variables) {
				var type = local.Type.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
				if (type == null)
					continue;
				var nested = type.TypeDef;
				if (nested == null || nested.DeclaringType != method.DeclaringType)
					continue;
				if (!ImplementsInterface(nested, "System.Runtime.CompilerServices.IAsyncStateMachine"))
					continue;
				return nested;
			}
			return null;
		}

		static TypeDef GetIteratorStateMachineTypeFromInstructionsCore(MethodDef method) {
			var instrs = method.Body?.Instructions;
			if (instrs == null)
				return null;
			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i];
				if (instr.OpCode.Code != Code.Newobj)
					continue;
				var ctor = instr.Operand as MethodDef;
				if (ctor == null || ctor.DeclaringType.DeclaringType != method.DeclaringType)
					continue;
				if (!ImplementsInterface(ctor.DeclaringType, "System.IDisposable"))
					continue;
				var disposeMethod = FindDispose(ctor.DeclaringType);
				if (disposeMethod == null)
					continue;
				if (!disposeMethod.CustomAttributes.IsDefined("System.Diagnostics.DebuggerHiddenAttribute"))
					continue;

				return ctor.DeclaringType;
			}

			return null;
		}

		static bool ImplementsInterface(TypeDef type, string ifaceName) {
			foreach (var i in type.Interfaces) {
				var iface = i.Interface;
				if (iface != null && iface.ReflectionFullName == ifaceName)
					return true;
			}
			return false;
		}

		static MethodDef FindDispose(TypeDef type) {
			foreach (var method in type.Methods) {
				foreach (var o in method.Overrides) {
					if (o.MethodDeclaration.Name != "Dispose")
						continue;
					if (!IsDisposeSig(o.MethodDeclaration.MethodSig))
						continue;
					return method;
				}
			}
			foreach (var method in type.Methods) {
				if (method.Name != "Dispose")
					continue;
				if (!IsDisposeSig(method.MethodSig))
					continue;
				return method;
			}

			return null;
		}

		static bool IsDisposeSig(MethodSig sig) {
			if (sig.GenParamCount != 0)
				return false;
			if (sig.ParamsAfterSentinel != null)
				return false;
			if (sig.Params.Count != 0)
				return false;
			if (sig.RetType.GetElementType() != ElementType.Void)
				return false;
			if (sig.CallingConvention != CallingConvention.HasThis)
				return false;
			return true;
		}
	}
}
