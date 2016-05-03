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
	public abstract class FileTreeNodeFilterBase : IFileTreeNodeFilter {
		public virtual FileTreeNodeFilterResult GetResult(ModuleDef mod) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(FieldDef field) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(PropertyDef prop) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(EventDef evt) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(TypeDef type) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(ModuleRef modRef) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IResourcesFolderNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IResourceElementNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IResourceNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IReferencesFolderNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypeNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeNode node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(AssemblyRef asmRef) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(AssemblyDef asm) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, Local local) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResultBody(MethodDef method) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResultLocals(MethodDef method) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IFileTreeNodeData node) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResult(IDnSpyFile file) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResultParamDefs(MethodDef method) => new FileTreeNodeFilterResult();
		public virtual FileTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) => new FileTreeNodeFilterResult();
	}
}
