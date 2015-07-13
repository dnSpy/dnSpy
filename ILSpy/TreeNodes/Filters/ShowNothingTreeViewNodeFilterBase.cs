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
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy.TreeNodes.Filters
{
	abstract class ShowNothingTreeViewNodeFilterBase : ITreeViewNodeFilter
	{
		public virtual string Text {
			get { return null; }
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(AssemblyRef asmRef)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(LoadedAssembly asm, AssemblyFilterType type)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(BaseTypesEntryNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(BaseTypesTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(DerivedTypesEntryNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(DerivedTypesTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(EventDef evt)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(FieldDef field)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ModuleRef modRef)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(string ns, LoadedAssembly owner)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(PropertyDef prop)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ReferenceFolderTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceListTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceElementTreeNode node)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(TypeDef type)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultBody(MethodDef method)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultParamDefs(MethodDef method)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method, ParamDef param)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultLocals(MethodDef method)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method, Local local)
		{
			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}
	}
}
