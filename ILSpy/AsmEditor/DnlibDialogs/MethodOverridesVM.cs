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
	sealed class MethodOverridesVM : ListVM<MethodOverrideVM, MethodOverride> {
		public MethodOverridesVM(ModuleDef ownerModule, Language language, TypeDef ownerType, MethodDef ownerMethod)
			: base("Edit Method Override", "Create Method Override", ownerModule, language, ownerType, ownerMethod) {
		}

		protected override MethodOverrideVM Create(MethodOverride model) {
			return new MethodOverrideVM(new MethodOverrideOptions(model), ownerModule);
		}

		protected override MethodOverrideVM Clone(MethodOverrideVM obj) {
			return new MethodOverrideVM(obj.CreateMethodOverrideOptions(), ownerModule);
		}

		protected override MethodOverrideVM Create() {
			return new MethodOverrideVM(new MethodOverrideOptions(), ownerModule);
		}
	}
}
