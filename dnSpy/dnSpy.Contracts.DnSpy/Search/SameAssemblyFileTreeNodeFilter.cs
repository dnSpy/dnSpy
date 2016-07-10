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
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Same assembly filter
	/// </summary>
	public sealed class SameAssemblyFileTreeNodeFilter : ChainFileTreeNodeFilter {
		readonly AssemblyDef allowedAsm;
		readonly ModuleDef allowedMod;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="allowedMod">Module</param>
		/// <param name="filter">Filter</param>
		public SameAssemblyFileTreeNodeFilter(ModuleDef allowedMod, IFileTreeNodeFilter filter)
			: base(filter) {
			this.allowedAsm = allowedMod.Assembly;
			this.allowedMod = allowedMod;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public override FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
			if (asm != allowedAsm)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(asm);
		}

		public override FileTreeNodeFilterResult GetResult(ModuleDef mod) {
			if (allowedAsm == null && mod != allowedMod)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			return base.GetResult(mod);
		}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
