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

using dnlib.DotNet;

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

		static TypeDef GetStateMachineTypeCore(MethodDef method) {
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
	}
}
