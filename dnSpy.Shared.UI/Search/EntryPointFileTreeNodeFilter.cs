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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Shared.UI.Search {
	public sealed class EntryPointFileTreeNodeFilter : ShowNothingFileTreeNodeFilterBase {
		readonly AssemblyDef assembly;
		readonly ModuleDef module;

		public EntryPointFileTreeNodeFilter(ModuleDef module) {
			this.module = module;
			this.assembly = module.Assembly;
		}

		public override FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
			if (assembly == null || asm != assembly)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return new FileTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override FileTreeNodeFilterResult GetResult(ModuleDef mod) {
			if (mod.Assembly != assembly)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			if (assembly == null || assembly.ManifestModule != module) {
				if (mod != module)
					return new FileTreeNodeFilterResult(FilterType.Hide, false);
				return new FileTreeNodeFilterResult(FilterType.Visible, false);
			}
			else
				return new FileTreeNodeFilterResult(FilterType.Visible, mod != assembly.ManifestModule);
		}

		public override FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) {
			if (owner.ModuleDef != module)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return new FileTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override FileTreeNodeFilterResult GetResult(EventDef evt) {
			return new FileTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override FileTreeNodeFilterResult GetResult(MethodDef method) {
			return new FileTreeNodeFilterResult(FilterType.Visible, true);
		}

		public override FileTreeNodeFilterResult GetResult(PropertyDef prop) {
			return new FileTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override FileTreeNodeFilterResult GetResult(TypeDef type) {
			return new FileTreeNodeFilterResult(FilterType.Visible, false);
		}
	}
}
