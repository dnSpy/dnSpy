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
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Chain filter
	/// </summary>
	public abstract class ChainDocumentTreeNodeFilter : IDocumentTreeNodeFilter {
		readonly IDocumentTreeNodeFilter filter;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filter"></param>
		public ChainDocumentTreeNodeFilter(IDocumentTreeNodeFilter filter) {
			this.filter = filter;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public virtual DocumentTreeNodeFilterResult GetResult(FieldDef field) => this.filter.GetResult(field);
		public virtual DocumentTreeNodeFilterResult GetResult(PropertyDef prop) => this.filter.GetResult(prop);
		public virtual DocumentTreeNodeFilterResult GetResult(EventDef evt) => this.filter.GetResult(evt);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method) => this.filter.GetResult(method);
		public virtual DocumentTreeNodeFilterResult GetResult(TypeDef type) => this.filter.GetResult(type);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleRef modRef) => this.filter.GetResult(modRef);
		public virtual DocumentTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourcesFolderNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourceElementNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourceNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IReferencesFolderNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IDerivedTypeNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IBaseTypeNode node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef) => this.filter.GetResult(asmRef);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleDef mod) => this.filter.GetResult(mod);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) => this.filter.GetResult(asm);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => this.filter.GetResult(method, param);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local) => this.filter.GetResult(method, local);
		public virtual DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) => this.filter.GetResult(ns, owner);
		public virtual DocumentTreeNodeFilterResult GetResultBody(MethodDef method) => this.filter.GetResultBody(method);
		public virtual DocumentTreeNodeFilterResult GetResultLocals(MethodDef method) => this.filter.GetResultLocals(method);
		public virtual DocumentTreeNodeFilterResult GetResult(IDocumentTreeNodeData node) => this.filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IDsDocument document) => this.filter.GetResult(document);
		public virtual DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method) => this.filter.GetResultParamDefs(method);
		public virtual DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => this.filter.GetResultAttributes(hca);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
