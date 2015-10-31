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
using dnSpy.Files;
using dnSpy.TreeNodes;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Search {
	public sealed class SameAssemblyTreeViewNodeFilter : ChainTreeViewNodeFilter {
		readonly AssemblyDef allowedAsm;
		readonly ModuleDef allowedMod;

		public SameAssemblyTreeViewNodeFilter(ModuleDef allowedMod, ITreeViewNodeFilter filter)
			: base(filter) {
			this.allowedAsm = allowedMod.Assembly;
			this.allowedMod = allowedMod;
		}

		public override TreeViewNodeFilterResult GetFilterResult(DnSpyFile file, AssemblyFilterType type) {
			if (file.AssemblyDef != allowedAsm)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			if (allowedAsm == null && file.ModuleDef != allowedMod)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return base.GetFilterResult(file, type);
		}
	}
}
