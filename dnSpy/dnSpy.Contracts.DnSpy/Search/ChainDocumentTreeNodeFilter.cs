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
		public virtual DocumentTreeNodeFilterResult GetResult(FieldDef field) => filter.GetResult(field);
		public virtual DocumentTreeNodeFilterResult GetResult(PropertyDef prop) => filter.GetResult(prop);
		public virtual DocumentTreeNodeFilterResult GetResult(EventDef evt) => filter.GetResult(evt);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method) => filter.GetResult(method);
		public virtual DocumentTreeNodeFilterResult GetResult(TypeDef type) => filter.GetResult(type);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleRef modRef) => filter.GetResult(modRef);
		public virtual DocumentTreeNodeFilterResult GetResult(BaseTypeFolderNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(DerivedTypesFolderNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(ResourcesFolderNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(ResourceElementNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(ResourceNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(ReferencesFolderNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(DerivedTypeNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(BaseTypeNode node) => filter.GetResult(node);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef) => filter.GetResult(asmRef);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleDef mod) => filter.GetResult(mod);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) => filter.GetResult(asm);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => filter.GetResult(method, param);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local) => filter.GetResult(method, local);
		public virtual DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) => filter.GetResult(ns, owner);
		public virtual DocumentTreeNodeFilterResult GetResultBody(MethodDef method) => filter.GetResultBody(method);
		public virtual DocumentTreeNodeFilterResult GetResultLocals(MethodDef method) => filter.GetResultLocals(method);
		public virtual DocumentTreeNodeFilterResult GetResultOther(DocumentTreeNodeData node) => filter.GetResultOther(node);
		public virtual DocumentTreeNodeFilterResult GetResult(IDsDocument document) => filter.GetResult(document);
		public virtual DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method) => filter.GetResultParamDefs(method);
		public virtual DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => filter.GetResultAttributes(hca);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
