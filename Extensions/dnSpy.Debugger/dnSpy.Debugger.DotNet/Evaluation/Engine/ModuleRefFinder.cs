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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	struct ModuleRefFinder {
		readonly HashSet<DmdModule> modules;
		int recursionCounter;

		ModuleRefFinder(bool dummy) {
			modules = new HashSet<DmdModule>();
			recursionCounter = 0;
		}

		public static ModuleRefFinder Create() => new ModuleRefFinder(true);

		public HashSet<DmdModule> GetModules() => modules;

		public void Add(DmdType type) {
			if (recursionCounter > 100)
				return;
			recursionCounter++;

			while (type.GetElementType() is DmdType etype)
				type = etype;
			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				modules.Add(type.Module);
				break;

			case DmdTypeSignatureKind.Pointer:
			case DmdTypeSignatureKind.ByRef:
			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
			case DmdTypeSignatureKind.SZArray:
			case DmdTypeSignatureKind.MDArray:
				break;

			case DmdTypeSignatureKind.GenericInstance:
				modules.Add(type.GetGenericTypeDefinition().Module);
				foreach (var ga in type.GetGenericArguments())
					Add(ga);
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				var sig = type.GetFunctionPointerMethodSignature();
				Add(sig.ReturnType);
				foreach (var t in sig.GetParameterTypes())
					Add(t);
				foreach (var t in sig.GetVarArgsParameterTypes())
					Add(t);
				break;

			default:
				Debug.Fail($"Unknown kind: {type.TypeSignatureKind}");
				break;
			}

			recursionCounter--;
		}
	}
}
