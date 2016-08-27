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
	sealed class CustomAttributesVM : ListVM<CustomAttributeVM, CustomAttribute> {
		public CustomAttributesVM(ModuleDef ownerModule, IDecompilerManager decompilerManager, TypeDef ownerType = null, MethodDef ownerMethod = null)
			: base(dnSpy_AsmEditor_Resources.EditCustomAttribute, dnSpy_AsmEditor_Resources.CreateCustomAttribute, ownerModule, decompilerManager, ownerType, ownerMethod) {
		}

		protected override CustomAttributeVM Create(CustomAttribute model) => new CustomAttributeVM(new CustomAttributeOptions(model), OwnerModule, decompilerManager, ownerType, ownerMethod);
		protected override CustomAttributeVM Clone(CustomAttributeVM obj) => new CustomAttributeVM(obj.CreateCustomAttributeOptions(), OwnerModule, decompilerManager, ownerType, ownerMethod);
		protected override CustomAttributeVM Create() => new CustomAttributeVM(new CustomAttributeOptions(), OwnerModule, decompilerManager, ownerType, ownerMethod);
	}
}
