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
	sealed class SecurityAttributesVM : ListVM<SecurityAttributeVM, SecurityAttribute> {
		public SecurityAttributesVM(ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef ownerType, MethodDef ownerMethod)
			: base(dnSpy_AsmEditor_Resources.EditSecurityAttribute, dnSpy_AsmEditor_Resources.CreateSecurityAttribute, ownerModule, decompilerService, ownerType, ownerMethod) {
		}

		protected override SecurityAttributeVM Create(SecurityAttribute model) => new SecurityAttributeVM(model, OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override SecurityAttributeVM Clone(SecurityAttributeVM obj) => new SecurityAttributeVM(obj.CreateSecurityAttribute(), OwnerModule, decompilerService, ownerType, ownerMethod);
		protected override SecurityAttributeVM Create() => new SecurityAttributeVM(new SecurityAttribute(), OwnerModule, decompilerService, ownerType, ownerMethod);
	}
}
