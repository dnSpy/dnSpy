/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	sealed class DynamicModuleDefDocument : DsDotNetDocumentBase, IModuleIdHolder {
		sealed class DocKey : IDsDocumentNameKey {
			readonly DbgModule module;
			public DocKey(DbgModule module) => this.module = module ?? throw new ArgumentNullException(nameof(module));
			public override bool Equals(object? obj) => obj is DocKey o && module == o.module;
			public override int GetHashCode() => module.GetHashCode();
		}

		public override IDsDocumentNameKey Key => CreateKey(DbgModule);
		public ModuleId ModuleId { get; }
		public override DsDocumentInfo? SerializedDocument => null;
		public DbgModule DbgModule { get; }
		public static IDsDocumentNameKey CreateKey(DbgModule module) => new DocKey(module);
		public override bool IsActive => !DbgModule.IsClosed;

		public DynamicModuleDefDocument(ModuleId moduleId, DbgModule module, ModuleDef moduleDef, bool loadSyms)
			: base(moduleDef, loadSyms) {
			ModuleId = moduleId;
			DbgModule = module;
		}

		public static DynamicModuleDefDocument CreateAssembly(List<DynamicModuleDefDocument> files) {
			var manifest = files[0];
			var file = new DynamicModuleDefDocument(manifest.ModuleId, manifest.DbgModule, manifest.ModuleDef!, false);
			file.files = new List<DynamicModuleDefDocument>(files);
			return file;
		}

		protected override TList<IDsDocument> CreateChildren() {
			var list = new TList<IDsDocument>();
			if (files is not null) {
				list.AddRange(files);
				files = null;
			}
			return list;
		}
		List<DynamicModuleDefDocument>? files;
	}
}
