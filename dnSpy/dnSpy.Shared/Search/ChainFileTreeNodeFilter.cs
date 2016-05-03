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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Shared.Search {
	public abstract class ChainFileTreeNodeFilter : IFileTreeNodeFilter {
		readonly IFileTreeNodeFilter filter;

		public ChainFileTreeNodeFilter(IFileTreeNodeFilter filter) {
			this.filter = filter;
		}

		public virtual FileTreeNodeFilterResult GetResult(FieldDef field) => this.filter.GetResult(field);
		public virtual FileTreeNodeFilterResult GetResult(PropertyDef prop) => this.filter.GetResult(prop);
		public virtual FileTreeNodeFilterResult GetResult(EventDef evt) => this.filter.GetResult(evt);
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method) => this.filter.GetResult(method);
		public virtual FileTreeNodeFilterResult GetResult(TypeDef type) => this.filter.GetResult(type);
		public virtual FileTreeNodeFilterResult GetResult(ModuleRef modRef) => this.filter.GetResult(modRef);
		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IResourcesFolderNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IResourceElementNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IResourceNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IReferencesFolderNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypeNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeNode node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(AssemblyRef asmRef) => this.filter.GetResult(asmRef);
		public virtual FileTreeNodeFilterResult GetResult(ModuleDef mod) => this.filter.GetResult(mod);
		public virtual FileTreeNodeFilterResult GetResult(AssemblyDef asm) => this.filter.GetResult(asm);
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => this.filter.GetResult(method, param);
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, Local local) => this.filter.GetResult(method, local);
		public virtual FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) => this.filter.GetResult(ns, owner);
		public virtual FileTreeNodeFilterResult GetResultBody(MethodDef method) => this.filter.GetResultBody(method);
		public virtual FileTreeNodeFilterResult GetResultLocals(MethodDef method) => this.filter.GetResultLocals(method);
		public virtual FileTreeNodeFilterResult GetResult(IFileTreeNodeData node) => this.filter.GetResult(node);
		public virtual FileTreeNodeFilterResult GetResult(IDnSpyFile file) => this.filter.GetResult(file);
		public virtual FileTreeNodeFilterResult GetResultParamDefs(MethodDef method) => this.filter.GetResultParamDefs(method);
		public virtual FileTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => this.filter.GetResultAttributes(hca);
	}
}
