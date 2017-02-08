/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Entry point filter
	/// </summary>
	public sealed class EntryPointDocumentTreeNodeFilter : ShowNothingDocumentTreeNodeFilterBase {
		readonly AssemblyDef assembly;
		readonly ModuleDef module;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public EntryPointDocumentTreeNodeFilter(ModuleDef module) {
			this.module = module;
			assembly = module.Assembly;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public override DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) {
			if (assembly == null || asm != assembly)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override DocumentTreeNodeFilterResult GetResult(ModuleDef mod) {
			if (mod.Assembly != assembly)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			if (assembly == null || assembly.ManifestModule != module) {
				if (mod != module)
					return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
				return new DocumentTreeNodeFilterResult(FilterType.Visible, false);
			}
			else
				return new DocumentTreeNodeFilterResult(FilterType.Visible, mod != assembly.ManifestModule);
		}

		public override DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) {
			if (owner.ModuleDef != module)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, false);
		}

		public override DocumentTreeNodeFilterResult GetResult(EventDef evt) => new DocumentTreeNodeFilterResult(FilterType.Visible, false);
		public override DocumentTreeNodeFilterResult GetResult(MethodDef method) => new DocumentTreeNodeFilterResult(FilterType.Visible, true);
		public override DocumentTreeNodeFilterResult GetResult(PropertyDef prop) => new DocumentTreeNodeFilterResult(FilterType.Visible, false);
		public override DocumentTreeNodeFilterResult GetResult(TypeDef type) => new DocumentTreeNodeFilterResult(FilterType.Visible, false);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
