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

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Decompiler.Utils {
	public static class StateMachineHelpers {
		static readonly UTF8String System_Runtime_CompilerServices = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String IAsyncStateMachine = new UTF8String("IAsyncStateMachine");
		static readonly UTF8String AsyncStateMachineAttribute = new UTF8String("AsyncStateMachineAttribute");
		static readonly UTF8String IteratorStateMachineAttribute = new UTF8String("IteratorStateMachineAttribute");
		static readonly UTF8String stringSystem = new UTF8String("System");
		static readonly UTF8String stringType = new UTF8String("Type");
		static readonly UTF8String stringIDisposable = new UTF8String("IDisposable");
		static readonly UTF8String stringDispose = new UTF8String("Dispose");
		static readonly UTF8String System_Collections = new UTF8String("System.Collections");
		static readonly UTF8String System_Collections_Generic = new UTF8String("System.Collections.Generic");
		static readonly UTF8String IEnumerable = new UTF8String("IEnumerable");
		static readonly UTF8String IEnumerator = new UTF8String("IEnumerator");
		static readonly UTF8String IEnumerable_1 = new UTF8String("IEnumerable`1");
		static readonly UTF8String IEnumerator_1 = new UTF8String("IEnumerator`1");

		static bool EqualsName(ITypeDefOrRef tdr, UTF8String @namespace, UTF8String name) {
			if (tdr is TypeRef tr)
				return tr.Name == name && tr.Namespace == @namespace;
			if (tdr is TypeDef td)
				return td.Name == name && td.Namespace == @namespace;
			return false;
		}

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
				if (typeType == null || !EqualsName(typeType, stringSystem, stringType))
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

		static bool IsStateMachineTypeAttribute(ITypeDefOrRef tdr) =>
			EqualsName(tdr, System_Runtime_CompilerServices, AsyncStateMachineAttribute) ||
			EqualsName(tdr, System_Runtime_CompilerServices, IteratorStateMachineAttribute);

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
				if (!ImplementsInterface(nested, System_Runtime_CompilerServices, IAsyncStateMachine))
					continue;
				return nested;
			}
			return null;
		}

		static TypeDef GetIteratorStateMachineTypeFromInstructionsCore(MethodDef method) {
			if (!IsIteratorReturnType(method.MethodSig.GetRetType().RemovePinnedAndModifiers()))
				return null;
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
				if (!ImplementsInterface(ctor.DeclaringType, stringSystem, stringIDisposable))
					continue;
				var disposeMethod = FindDispose(ctor.DeclaringType);
				if (disposeMethod == null)
					continue;
				if (!disposeMethod.CustomAttributes.IsDefined("System.Diagnostics.DebuggerHiddenAttribute")) {
					// This attribute isn't always present. Make sure the type has a compiler generated name
					var name = ctor.DeclaringType.Name.String;
					if (!name.StartsWith("<") && !name.StartsWith("VB$StateMachine_"))
						continue;
				}

				return ctor.DeclaringType;
			}

			return null;
		}

		static bool IsIteratorReturnType(TypeSig typeSig) {
			var tdr = (typeSig as ClassSig)?.TypeDefOrRef;
			if (tdr == null)
				tdr = (typeSig as GenericInstSig)?.GenericType.TypeDefOrRef;
			if (tdr == null)
				return false;
			return EqualsName(tdr, System_Collections, IEnumerable) ||
				EqualsName(tdr, System_Collections, IEnumerator) ||
				EqualsName(tdr, System_Collections_Generic, IEnumerable_1) ||
				EqualsName(tdr, System_Collections_Generic, IEnumerator_1);
		}

		static bool ImplementsInterface(TypeDef type, UTF8String @namespace, UTF8String name) {
			var ifaces = type.Interfaces;
			for (int i = 0; i < ifaces.Count; i++) {
				var iface = ifaces[i].Interface;
				if (iface != null && EqualsName(iface, @namespace, name))
					return true;
			}
			return false;
		}

		static MethodDef FindDispose(TypeDef type) {
			foreach (var method in type.Methods) {
				foreach (var o in method.Overrides) {
					if (o.MethodDeclaration.Name != stringDispose)
						continue;
					if (!IsDisposeSig(o.MethodDeclaration.MethodSig))
						continue;
					return method;
				}
			}
			foreach (var method in type.Methods) {
				if (method.Name != stringDispose)
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

		/// <summary>
		/// Gets the state machine kickoff method. It's the original async/iterator method that the compiler moves to the MoveNext method
		/// </summary>
		/// <param name="method">A possible state machine MoveNext method</param>
		/// <param name="kickoffMethod">Updated with kickoff method on success</param>
		/// <returns></returns>
		public static bool TryGetKickoffMethod(MethodDef method, out MethodDef kickoffMethod) {
			kickoffMethod = null;
			var declType = method.DeclaringType;

			// Assume all state machine types are nested types
			if (!declType.IsNested)
				return false;

			if (ImplementsInterface(declType, System_Runtime_CompilerServices, IAsyncStateMachine)) {
				// async method

				if (TryGetKickoffMethodFromAttributes(declType, out kickoffMethod))
					return true;

				foreach (var possibleKickoffMethod in declType.DeclaringType.Methods) {
					if (GetAsyncStateMachineTypeFromInstructionsCore(possibleKickoffMethod) == declType) {
						kickoffMethod = possibleKickoffMethod;
						return true;
					}
				}
			}
			else if (ImplementsInterface(declType, System_Collections, IEnumerator)) {
				// IEnumerable, IEnumerable<T>, IEnumerator, IEnumerator<T>

				if (TryGetKickoffMethodFromAttributes(declType, out kickoffMethod))
					return true;

				foreach (var possibleKickoffMethod in declType.DeclaringType.Methods) {
					if (GetIteratorStateMachineTypeFromInstructionsCore(possibleKickoffMethod) == declType) {
						kickoffMethod = possibleKickoffMethod;
						return true;
					}
				}
			}

			return false;
		}

		static bool TryGetKickoffMethodFromAttributes(TypeDef smType, out MethodDef kickoffMethod) {
			foreach (var possibleKickoffMethod in smType.DeclaringType.Methods) {
				if (GetStateMachineTypeFromCustomAttributesCore(possibleKickoffMethod) == smType) {
					kickoffMethod = possibleKickoffMethod;
					return true;
				}
			}

			kickoffMethod = null;
			return false;
		}
	}
}
