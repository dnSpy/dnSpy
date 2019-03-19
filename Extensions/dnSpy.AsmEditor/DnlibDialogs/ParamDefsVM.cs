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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ParamDefsVM : ListVM<ParamDefVM, ParamDef> {
		public ParamDefsVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod)
			: base(dnSpy_AsmEditor_Resources.EditParameter, dnSpy_AsmEditor_Resources.CreateParameter, ownerModule, decompilerService, ownerType, ownerMethod) {
		}

		protected override ParamDefVM Create(ParamDef model) => new ParamDefVM(new ParamDefOptions(model), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override ParamDefVM Clone(ParamDefVM obj) => new ParamDefVM(obj.CreateParamDefOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override ParamDefVM Create() => new ParamDefVM(new ParamDefOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);

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
