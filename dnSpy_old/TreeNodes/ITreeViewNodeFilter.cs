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
using dnSpy.Files;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes {
	public enum AssemblyFilterType {
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

	public struct TreeViewNodeFilterResult {
		/// <summary>
		/// null if the <see cref="ILSpyTreeNode"/> should decide what result to return in its
		/// Filter() method. <see cref="FilterResult.Hidden"/> is returned if the node should be
		/// hidden and it's not necessary to recurse into this node to find other matches.
		/// </summary>
		public FilterResult? FilterResult;

		/// <summary>
		/// true if this is a node that can be returned as a result to the user
		/// </summary>
		public bool IsMatch;

		public TreeViewNodeFilterResult(FilterResult? filterResult, bool isMatch) {
			this.FilterResult = filterResult;
			this.IsMatch = isMatch;
		}
	}

	public interface ITreeViewNodeFilter {
		string Text { get; }
		// NOTE: Any node arguments (not dnlib types) can be null when called.
		TreeViewNodeFilterResult GetFilterResult(DnSpyFile file, AssemblyFilterType type);
		TreeViewNodeFilterResult GetFilterResult(string ns, DnSpyFile owner);
		TreeViewNodeFilterResult GetFilterResult(TypeDef type);
		TreeViewNodeFilterResult GetFilterResult(FieldDef field);
		TreeViewNodeFilterResult GetFilterResult(MethodDef method);
		TreeViewNodeFilterResult GetFilterResult(PropertyDef prop);
		TreeViewNodeFilterResult GetFilterResult(EventDef evt);
		TreeViewNodeFilterResult GetFilterResultBody(MethodDef method);
		TreeViewNodeFilterResult GetFilterResultParamDefs(MethodDef method);
		TreeViewNodeFilterResult GetFilterResult(MethodDef method, ParamDef param);
		TreeViewNodeFilterResult GetFilterResultLocals(MethodDef method);
		TreeViewNodeFilterResult GetFilterResult(MethodDef method, Local local);
		TreeViewNodeFilterResult GetFilterResult(AssemblyRef asmRef);
		TreeViewNodeFilterResult GetFilterResult(ModuleRef modRef);
		TreeViewNodeFilterResult GetFilterResult(BaseTypesEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(BaseTypesTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(DerivedTypesEntryNode node);
		TreeViewNodeFilterResult GetFilterResult(DerivedTypesTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ReferenceFolderTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceListTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(ResourceElementTreeNode node);
		TreeViewNodeFilterResult GetFilterResult(PETreeNode node);
		TreeViewNodeFilterResult GetFilterResult(HexTreeNode node);
	}
}
