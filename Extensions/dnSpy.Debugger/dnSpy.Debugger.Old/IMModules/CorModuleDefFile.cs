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

using System.Collections.Generic;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// A class holding a <see cref="dndbg.DotNet.CorModuleDef"/> reference. Should only be used if
	/// it's a dynamic module since it uses <c>IMetaDataImport</c> to read the MD.
	/// </summary>
	sealed class CorModuleDefFile : DsDotNetDocumentBase, IModuleIdHolder {
		sealed class MyKey : IDsDocumentNameKey {
			readonly DnModule dnModule;

			public MyKey(DnModule dnModule) {
				this.dnModule = dnModule;
			}

			public override bool Equals(object obj) {
				var o = obj as MyKey;
				return o != null && dnModule == o.dnModule;
			}

			public override int GetHashCode() => dnModule.GetHashCode();
		}

		public override IDsDocumentNameKey Key => CreateKey(DnModule);
		public ModuleId ModuleId => ModuleId.Create(ModuleDef, DnModule.IsDynamic, DnModule.IsInMemory);
		public override DsDocumentInfo? SerializedDocument => null;
		public DnModule DnModule { get; }
		public static IDsDocumentNameKey CreateKey(DnModule module) => new MyKey(module);
		public override bool IsActive => !DnModule.Process.HasExited;

		public LastValidRids LastValidRids => lastValidRids;
		LastValidRids lastValidRids;

		public CorModuleDefFile(DnModule dnModule, bool loadSyms)
			: base(dnModule.GetOrCreateCorModuleDef(), loadSyms) {
			DnModule = dnModule;
			lastValidRids = new LastValidRids();
		}

		public static CorModuleDefFile CreateAssembly(List<CorModuleDefFile> files) {
			var manifest = files[0];
			var file = new CorModuleDefFile(manifest.DnModule, false);
			file.files = new List<CorModuleDefFile>(files);
			return file;
		}

		protected override List<IDsDocument> CreateChildren() {
			var list = new List<IDsDocument>();
			if (files != null) {
				list.AddRange(files);
				files = null;
			}
			return list;
		}
		List<CorModuleDefFile> files;

		public LastValidRids UpdateLastValidRids() {
			var old = lastValidRids;

			// Linear search but shouldn't be a problem except the first time if we load a big file

			for (; ; lastValidRids.TypeDefRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.TypeDef, lastValidRids.TypeDefRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.FieldRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.Field, lastValidRids.FieldRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.MethodRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.Method, lastValidRids.MethodRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.ParamRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.Param, lastValidRids.ParamRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.EventRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.Event, lastValidRids.EventRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.PropertyRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.Property, lastValidRids.PropertyRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.GenericParamRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.GenericParam, lastValidRids.GenericParamRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.GenericParamConstraintRid++) {
				if (!DnModule.CorModuleDef.IsValidToken(new MDToken(Table.GenericParamConstraint, lastValidRids.GenericParamConstraintRid + 1).Raw))
					break;
			}

			return old;
		}
	}
}
