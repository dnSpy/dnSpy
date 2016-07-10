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

using System.Windows;
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class EditMethodDef : IEdit<MethodDefVM> {
		readonly ModuleDef ownerModule;
		readonly DnlibTypePicker dnlibTypePicker;

		public EditMethodDef(ModuleDef ownerModule)
			: this(ownerModule, null) {
		}

		public EditMethodDef(ModuleDef ownerModule, Window ownerWindow) {
			this.ownerModule = ownerModule;
			this.dnlibTypePicker = new DnlibTypePicker(ownerWindow);
		}

		public MethodDefVM Edit(string title, MethodDefVM vm) {
			var method = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Method, new SameModuleFileTreeNodeFilter(ownerModule, new FlagsFileTreeNodeFilter(VisibleMembersFlags.MethodDef)), vm.Method, ownerModule);
			if (method == null)
				return null;

			vm.Method = method;
			return vm;
		}
	}
}
