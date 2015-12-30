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
	public abstract class ShowNothingFileTreeNodeFilterBase : IFileTreeNodeFilter {
		public virtual FileTreeNodeFilterResult GetResult(TypeDef type) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(EventDef evt) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(PropertyDef prop) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(FieldDef field) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(AssemblyRef asmRef) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypeNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IReferencesFolderNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourceNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourceElementNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IResourcesFolderNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(ModuleRef modRef) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(ModuleDef mod) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, Local local) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResultBody(MethodDef method) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResultLocals(MethodDef method) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IFileTreeNodeData node) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResult(IDnSpyFile file) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		public virtual FileTreeNodeFilterResult GetResultParamDefs(MethodDef method) {
			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}
	}
}
