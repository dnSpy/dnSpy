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

using System.Collections.Generic;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Files;
using dnSpy.Shared.UI.Files;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// A class holding a <see cref="dndbg.DotNet.CorModuleDef"/> reference. Should only be used if
	/// it's a dynamic module since it uses <c>IMetaDataImport</c> to read the MD.
	/// </summary>
	sealed class CorModuleDefFile : DnSpyDotNetFileBase {
		sealed class MyKey : IDnSpyFilenameKey {
			readonly DnModule dnModule;

			public MyKey(DnModule dnModule) {
				this.dnModule = dnModule;
			}

			public override bool Equals(object obj) {
				var o = obj as MyKey;
				return o != null && dnModule == o.dnModule;
			}

			public override int GetHashCode() {
				return dnModule.GetHashCode();
			}
		}

		public override IDnSpyFilenameKey Key {
			get { return CreateKey(dnModule); }
		}

		public override SerializedDnSpyModule? SerializedDnSpyModule {
			get { return Contracts.Files.SerializedDnSpyModule.Create(ModuleDef, DnModule.IsDynamic, DnModule.IsInMemory); }
		}

		public override bool LoadedFromFile {
			get { return false; }
		}

		public DnModule DnModule {
			get { return dnModule; }
		}
		readonly DnModule dnModule;

		public static IDnSpyFilenameKey CreateKey(DnModule module) {
			return new MyKey(module);
		}

		public LastValidRids LastValidRids {
			get { return lastValidRids; }
		}
		LastValidRids lastValidRids;

		public CorModuleDefFile(DnModule dnModule, bool loadSyms)
			: base(dnModule.GetOrCreateCorModuleDef(), loadSyms) {
			this.dnModule = dnModule;
			this.lastValidRids = new LastValidRids();
		}

		public static CorModuleDefFile CreateAssembly(List<CorModuleDefFile> files) {
			var manifest = files[0];
			var file = new CorModuleDefFile(manifest.DnModule, false);
			file.files = new List<CorModuleDefFile>(files);
			return file;
		}

		protected override List<IDnSpyFile> CreateChildren() {
			var list = new List<IDnSpyFile>();
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
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.TypeDef, lastValidRids.TypeDefRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.FieldRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.Field, lastValidRids.FieldRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.MethodRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.Method, lastValidRids.MethodRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.ParamRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.Param, lastValidRids.ParamRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.EventRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.Event, lastValidRids.EventRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.PropertyRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.Property, lastValidRids.PropertyRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.GenericParamRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.GenericParam, lastValidRids.GenericParamRid + 1).Raw))
					break;
			}
			for (; ; lastValidRids.GenericParamConstraintRid++) {
				if (!dnModule.CorModuleDef.IsValidToken(new MDToken(Table.GenericParamConstraint, lastValidRids.GenericParamConstraintRid + 1).Raw))
					break;
			}

			return old;
		}
	}
}
