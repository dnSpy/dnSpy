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
using dnSpy.Contracts.Languages;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class DeclSecuritiesVM : ListVM<DeclSecurityVM, DeclSecurity> {
		public DeclSecuritiesVM(ModuleDef ownerModule, ILanguageManager languageManager, TypeDef ownerType, MethodDef ownerMethod)
			: base(dnSpy_AsmEditor_Resources.EditSecurityDeclaration, dnSpy_AsmEditor_Resources.CreateSecurityDeclaration, ownerModule, languageManager, ownerType, ownerMethod) {
		}

		protected override DeclSecurityVM Create(DeclSecurity model) {
			return new DeclSecurityVM(new DeclSecurityOptions(model), ownerModule, languageManager, ownerType, ownerMethod);
		}

		protected override DeclSecurityVM Clone(DeclSecurityVM obj) {
			return new DeclSecurityVM(obj.CreateDeclSecurityOptions(), ownerModule, languageManager, ownerType, ownerMethod);
		}

		protected override DeclSecurityVM Create() {
			return new DeclSecurityVM(new DeclSecurityOptions(), ownerModule, languageManager, ownerType, ownerMethod);
		}
	}
}
