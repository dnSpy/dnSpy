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
using ICSharpCode.ILSpy.Xaml;

namespace ICSharpCode.ILSpy.TreeNodes
{
	enum AssemblyFilterType
	{
		/// <summary>
		/// non-.NET file node
		/// </summary>
		NonNetFile,

		/// <summary>
		/// NetModule node or a module node in an assembly
		/// </summary>
		NetModule,

		/// <summary>
		/// Assembly node
		/// </summary>
		Assembly,
	}

	struct TreeViewNodeFilterResult
	{
		public FilterResult? FilterResult;
		public bool IsMatch;

		public TreeViewNodeFilterResult(FilterResult? filterResult, bool isMatch)
		{
			this.FilterResult = filterResult;
			this.IsMatch = isMatch;
		}
	}

	interface ITreeViewNodeFilter
	{
		string Text { get; }
		// NOTE: Any node arguments (not dnlib types) can be null when called.
		TreeViewNodeFilterResult GetFilterResult(AssemblyRef asmRef);
		TreeViewNodeFilterResult GetFilterResult(LoadedAssembly asm, AssemblyFilterType type);
		TreeViewNodeFilterResult GetFilterResult(BaseTypesEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(BaseTypesTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(DerivedTypesEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(DerivedTypesTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(EventDef evt);
		TreeViewNodeFilterResult GetFilterResult(FieldDef field);
		TreeViewNodeFilterResult GetFilterResult(MethodDef method);
		TreeViewNodeFilterResult GetFilterResult(ModuleRef modRef);
		TreeViewNodeFilterResult GetFilterResult(string ns);
		TreeViewNodeFilterResult GetFilterResult(PropertyDef prop);
		TreeViewNodeFilterResult GetFilterResult(ReferenceFolderTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceListTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(TypeDef type);
		TreeViewNodeFilterResult GetFilterResultBody(MethodDef method);
		TreeViewNodeFilterResult GetFilterResultParamDef(MethodDef method);
		TreeViewNodeFilterResult GetFilterResult(CursorResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(IconResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(ImageListResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(ImageResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourcesFileTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(XamlResourceEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(XmlResourceEntryNode node);
	}
}
