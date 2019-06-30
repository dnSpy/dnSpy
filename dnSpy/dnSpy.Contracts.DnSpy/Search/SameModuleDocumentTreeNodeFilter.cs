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
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Same module filter
	/// </summary>
	public sealed class SameModuleDocumentTreeNodeFilter : ChainDocumentTreeNodeFilter {
		readonly ModuleDef allowedModule;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="allowedModule">Module</param>
		/// <param name="filter">Filter</param>
		public SameModuleDocumentTreeNodeFilter(ModuleDef allowedModule, IDocumentTreeNodeFilter filter)
			: base(filter) => this.allowedModule = allowedModule;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public override DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) {
			if (!(allowedModule.Assembly is null) && allowedModule.Assembly != asm)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(asm);
		}

		public override DocumentTreeNodeFilterResult GetResult(ModuleDef mod) {
			if (mod != allowedModule)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(mod);
		}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
