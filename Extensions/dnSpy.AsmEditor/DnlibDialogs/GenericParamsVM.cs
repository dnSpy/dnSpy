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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class GenericParamsVM : ListVM<GenericParamVM, GenericParam> {
		public GenericParamsVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod)
			: base(dnSpy_AsmEditor_Resources.EditGenericParameter, dnSpy_AsmEditor_Resources.CreateGenericParameter, ownerModule, decompilerService, ownerType, ownerMethod) {
		}

		protected override GenericParamVM Create(GenericParam model) => new GenericParamVM(new GenericParamOptions(model), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override GenericParamVM Clone(GenericParamVM obj) => new GenericParamVM(obj.CreateGenericParamOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override GenericParamVM Create() => new GenericParamVM(new GenericParamOptions(), OwnerModule, decompilerService, ownerType, ownerMethod);

		protected override int GetAddIndex(GenericParamVM obj) {
			ushort number = obj.Number.Value;
			for (int i = 0; i < Collection.Count; i++) {
				if (number < Collection[i].Number.Value)
					return i;
			}
			return Collection.Count;
		}
	}
}
