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
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Shared.UI.Search {
	public sealed class SameModuleFileTreeNodeFilter : ChainFileTreeNodeFilter {
		readonly ModuleDef allowedModule;

		public SameModuleFileTreeNodeFilter(ModuleDef allowedModule, IFileTreeNodeFilter filter)
			: base(filter) {
			this.allowedModule = allowedModule;
		}

		public override FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
			if (allowedModule.Assembly != null && allowedModule.Assembly != asm)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(asm);
		}

		public override FileTreeNodeFilterResult GetResult(ModuleDef mod) {
			if (mod != allowedModule)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(mod);
		}
	}
}
