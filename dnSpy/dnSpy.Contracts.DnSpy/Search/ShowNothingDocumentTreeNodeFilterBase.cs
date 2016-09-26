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
	/// Show nothing filter base class
	/// </summary>
	public abstract class ShowNothingDocumentTreeNodeFilterBase : IDocumentTreeNodeFilter {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public virtual DocumentTreeNodeFilterResult GetResult(TypeDef type) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(EventDef evt) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(PropertyDef prop) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(FieldDef field) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IBaseTypeNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IDerivedTypeNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IReferencesFolderNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourceNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourceElementNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IResourcesFolderNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleRef modRef) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(ModuleDef mod) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResultBody(MethodDef method) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResultLocals(MethodDef method) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IDocumentTreeNodeData node) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResult(IDsDocument document) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		public virtual DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => new DocumentTreeNodeFilterResult(FilterType.Hide, false);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
