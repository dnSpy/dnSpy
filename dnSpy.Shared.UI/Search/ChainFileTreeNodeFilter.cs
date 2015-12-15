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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Shared.UI.Search {
	public abstract class ChainFileTreeNodeFilter : IFileTreeNodeFilter {
		readonly IFileTreeNodeFilter filter;

		public ChainFileTreeNodeFilter(IFileTreeNodeFilter filter) {
			this.filter = filter;
		}

		public virtual string Description {
			get { return filter.Description; }
		}

		public virtual FileTreeNodeFilterResult GetResult(FieldDef field) {
			return this.filter.GetResult(field);
		}

		public virtual FileTreeNodeFilterResult GetResult(PropertyDef prop) {
			return this.filter.GetResult(prop);
		}

		public virtual FileTreeNodeFilterResult GetResult(EventDef evt) {
			return this.filter.GetResult(evt);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method) {
			return this.filter.GetResult(method);
		}

		public virtual FileTreeNodeFilterResult GetResult(TypeDef type) {
			return this.filter.GetResult(type);
		}

		public virtual FileTreeNodeFilterResult GetResult(ModuleRef modRef) {
			return this.filter.GetResult(modRef);
		}

		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourcesFolderNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourceElementNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourceNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IReferencesFolderNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypeNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeNode node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(AssemblyRef asmRef) {
			return this.filter.GetResult(asmRef);
		}

		public virtual FileTreeNodeFilterResult GetResult(ModuleDef mod) {
			return this.filter.GetResult(mod);
		}

		public virtual FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
			return this.filter.GetResult(asm);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) {
			return this.filter.GetResult(method, param);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, Local local) {
			return this.filter.GetResult(method, local);
		}

		public virtual FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) {
			return this.filter.GetResult(ns, owner);
		}

		public virtual FileTreeNodeFilterResult GetResultBody(MethodDef method) {
			return this.filter.GetResultBody(method);
		}

		public virtual FileTreeNodeFilterResult GetResultLocals(MethodDef method) {
			return this.filter.GetResultLocals(method);
		}

		public virtual FileTreeNodeFilterResult GetResult(IFileTreeNodeData node) {
			return this.filter.GetResult(node);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDnSpyFile file) {
			return this.filter.GetResult(file);
		}

		public virtual FileTreeNodeFilterResult GetResultParamDefs(MethodDef method) {
			return this.filter.GetResultParamDefs(method);
		}
	}
}
