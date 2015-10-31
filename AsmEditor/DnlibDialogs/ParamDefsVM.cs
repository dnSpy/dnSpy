/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ParamDefsVM : ListVM<ParamDefVM, ParamDef> {
		public ParamDefsVM(ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod)
			: base("Edit Parameter", "Create Parameter", ownerModule, language, ownerType, ownerMethod) {
		}

		protected override ParamDefVM Create(ParamDef model) {
			return new ParamDefVM(new ParamDefOptions(model), ownerModule, language, ownerType, ownerMethod);
		}

		protected override ParamDefVM Clone(ParamDefVM obj) {
			return new ParamDefVM(obj.CreateParamDefOptions(), ownerModule, language, ownerType, ownerMethod);
		}

		protected override ParamDefVM Create() {
			return new ParamDefVM(new ParamDefOptions(), ownerModule, language, ownerType, ownerMethod);
		}

		protected override int GetAddIndex(ParamDefVM obj) {
			ushort sequence = obj.Sequence.Value;
			for (int i = 0; i < Collection.Count; i++) {
				if (sequence < Collection[i].Sequence.Value)
					return i;
			}
			return Collection.Count;
		}
	}
}
