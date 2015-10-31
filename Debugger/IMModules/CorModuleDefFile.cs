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
using dnSpy.Files;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// A class holding a <see cref="dndbg.DotNet.CorModuleDef"/> reference. Should only be used if
	/// it's a dynamic module since it uses <c>IMetaDataImport</c> to read the MD.
	/// </summary>
	sealed class CorModuleDefFile : DotNetFileBase {
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

		public override bool CanBeSavedToSettingsFile {
			get { return false; }
		}

		public override IDnSpyFilenameKey Key {
			get { return CreateKey(dnModule); }
		}

		public override SerializedDnSpyModule? SerializedDnSpyModule {
			get { return Files.SerializedDnSpyModule.Create(ModuleDef, DnModule.IsDynamic, DnModule.IsInMemory); }
		}

		public override bool LoadedFromFile {
			get { return false; }
		}

		public override bool IsReadOnly {
			get { return !dnModule.Process.HasExited; }
		}

		public DnModule DnModule {
			get { return dnModule; }
		}
		readonly DnModule dnModule;

		public static IDnSpyFilenameKey CreateKey(DnModule module) {
			return new MyKey(module);
		}

		internal Dictionary<ModuleDef, CorModuleDefFile> Dictionary {
			get { return dict; }
		}
		readonly Dictionary<ModuleDef, CorModuleDefFile> dict;

		public LastValidRids LastValidRids {
			get { return lastValidRids; }
		}
		LastValidRids lastValidRids;

		public CorModuleDefFile(Dictionary<ModuleDef, CorModuleDefFile> dict, DnModule dnModule, bool loadSyms)
			: base(dnModule.GetOrCreateCorModuleDef(), loadSyms) {
			this.dict = dict;
			this.dnModule = dnModule;
			this.lastValidRids = new LastValidRids();
		}

		public override DnSpyFile CreateDnSpyFile(ModuleDef module) {
			if (module == null)
				return null;
			CorModuleDefFile file;
			dict.TryGetValue(module, out file);
			return file;
		}

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
