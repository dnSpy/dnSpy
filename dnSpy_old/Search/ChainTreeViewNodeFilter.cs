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
using dnSpy.TreeNodes;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Search {
	public abstract class ChainTreeViewNodeFilter : ITreeViewNodeFilter {
		readonly ITreeViewNodeFilter filter;

		public ChainTreeViewNodeFilter(ITreeViewNodeFilter filter) {
			this.filter = filter;
		}

		public virtual string Text {
			get { return filter.Text; }
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(AssemblyRef asmRef) {
			return filter.GetFilterResult(asmRef);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(DnSpyFile file, AssemblyFilterType type) {
			return filter.GetFilterResult(file, type);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(BaseTypesEntryNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(BaseTypesTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(DerivedTypesEntryNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(DerivedTypesTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(EventDef evt) {
			return filter.GetFilterResult(evt);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(FieldDef field) {
			return filter.GetFilterResult(field);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method) {
			return filter.GetFilterResult(method);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ModuleRef modRef) {
			return filter.GetFilterResult(modRef);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(string ns, DnSpyFile file) {
			return filter.GetFilterResult(ns, file);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(PropertyDef prop) {
			return filter.GetFilterResult(prop);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ReferenceFolderTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceListTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(ResourceElementTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(PETreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(HexTreeNode node) {
			return filter.GetFilterResult(node);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(TypeDef type) {
			return filter.GetFilterResult(type);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultBody(MethodDef method) {
			return filter.GetFilterResultBody(method);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultParamDefs(MethodDef method) {
			return filter.GetFilterResultParamDefs(method);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method, ParamDef param) {
			return filter.GetFilterResult(method, param);
		}

		public virtual TreeViewNodeFilterResult GetFilterResultLocals(MethodDef method) {
			return filter.GetFilterResultLocals(method);
		}

		public virtual TreeViewNodeFilterResult GetFilterResult(MethodDef method, Local local) {
			return filter.GetFilterResult(method, local);
		}
	}
}
