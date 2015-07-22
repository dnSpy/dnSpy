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

using System.Diagnostics;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes.Filters
{
	sealed class EntryPointTreeViewNodeFilter : ShowNothingTreeViewNodeFilterBase
	{
		readonly AssemblyDef assembly;
		readonly ModuleDef module;

		public override string Text {
			get { return "Managed Entry Point"; }
		}

		public EntryPointTreeViewNodeFilter(ModuleDef module)
		{
			this.module = module;
			this.assembly = module.Assembly;
		}

		public override TreeViewNodeFilterResult GetFilterResult(LoadedAssembly asm, AssemblyFilterType type)
		{
			if (type == AssemblyFilterType.NonNetFile)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);

			if (type == AssemblyFilterType.Assembly) {
				if (assembly == null || asm.AssemblyDefinition != assembly)
					return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
				return new TreeViewNodeFilterResult(FilterResult.Match, false);
			}

			if (type == AssemblyFilterType.NetModule) {
				if (asm.AssemblyDefinition != assembly)
					return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
				if (assembly == null || assembly.ManifestModule != module) {
					if (asm.ModuleDefinition != module)
						return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
					return new TreeViewNodeFilterResult(FilterResult.Match, false);
				}
				else
					return new TreeViewNodeFilterResult(FilterResult.Match, asm.ModuleDefinition != assembly.ManifestModule);
			}

			Debug.Fail("Invalid AssemblyFilterType value");
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public override TreeViewNodeFilterResult GetFilterResult(string ns, LoadedAssembly owner)
		{
			if (owner.ModuleDefinition != module)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			return new TreeViewNodeFilterResult(FilterResult.Match, false);
		}

		public override TreeViewNodeFilterResult GetFilterResult(EventDef evt)
		{
			return new TreeViewNodeFilterResult(FilterResult.Match, false);
		}

		public override TreeViewNodeFilterResult GetFilterResult(MethodDef method)
		{
			return new TreeViewNodeFilterResult(FilterResult.Match, true);
		}

		public override TreeViewNodeFilterResult GetFilterResult(PropertyDef prop)
		{
			return new TreeViewNodeFilterResult(FilterResult.Match, false);
		}

		public override TreeViewNodeFilterResult GetFilterResult(TypeDef type)
		{
			return new TreeViewNodeFilterResult(FilterResult.Match, false);
		}
	}
}
