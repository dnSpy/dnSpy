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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dndbg.DotNet;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// Loads all new classes from dynamic modules
	/// </summary>
	sealed class ClassLoader {
		readonly IFileTreeView fileTreeView;
		readonly Window ownerWindow;
		readonly Dictionary<DnModule, HashSet<uint>> loadedClasses;

		public ClassLoader(IFileTreeView fileTreeView, Window ownerWindow) {
			this.fileTreeView = fileTreeView;
			this.ownerWindow = ownerWindow;
			this.loadedClasses = new Dictionary<DnModule, HashSet<uint>>();
		}

		public void LoadClass(DnModule dnModule, uint token) {
			Debug.Assert(dnModule.CorModuleDef != null);
			if (dnModule.CorModuleDef == null)
				return;

			HashSet<uint> hash;
			if (!loadedClasses.TryGetValue(dnModule, out hash))
				loadedClasses.Add(dnModule, hash = new HashSet<uint>());
			hash.Add(token);
		}

		public void UnloadClass(DnModule dnModule, uint token) {
			Debug.WriteLine(string.Format("Class unloaded: 0x{0:X8} {1}", token, dnModule.CorModuleDef));
			Debug.Assert(dnModule.CorModuleDef != null);
			if (dnModule.CorModuleDef == null)
				return;

			// Nothing to do
		}

		struct ModuleState {
			public CorModuleDefFile CorModuleDefFile;
			public IModuleFileNode ModuleNode;
			public HashSet<uint> ModifiedTypes;
			public HashSet<uint> LoadClassHash;

			public ModuleState(CorModuleDefFile corModuleDefFile, IModuleFileNode moduleNode, HashSet<uint> modifiedTypes, HashSet<uint> loadClassHash) {
				this.CorModuleDefFile = corModuleDefFile;
				this.ModuleNode = moduleNode;
				this.ModifiedTypes = modifiedTypes;
				this.LoadClassHash = loadClassHash;
			}
		}

		public void LoadNewClasses(Dictionary<CorModuleDefFile, IModuleFileNode> visibleModules) {
			var oldLoadedClasses = new Dictionary<DnModule, HashSet<uint>>(loadedClasses);
			loadedClasses.Clear();
			if (visibleModules.Count == 0)
				return;

			var states = new List<ModuleState>(visibleModules.Count);
			foreach (var kv in visibleModules) {
				Debug.Assert(kv.Key.DnModule.IsDynamic, "Only dynamic modules can add new types, members");
				HashSet<uint> hash;
				oldLoadedClasses.TryGetValue(kv.Key.DnModule, out hash);
				states.Add(new ModuleState(kv.Key, kv.Value, GetModifiedTypesList(kv.Key), hash));
			}

			foreach (var state in states) {
				var hash = new HashSet<uint>(state.ModifiedTypes);
				if (state.LoadClassHash != null) {
					foreach (var a in state.LoadClassHash)
						hash.Add(a);
				}
				var tokens = hash.ToList();
				tokens.Sort();
				foreach (uint token in tokens) {
					bool loaded = state.LoadClassHash != null && state.LoadClassHash.Contains(token);
					if (loaded)
						continue;   // It has already been initialized

					state.CorModuleDefFile.DnModule.CorModuleDef.ForceInitializeTypeDef(token & 0x00FFFFFF);
				}
			}

			// This must be called after ForceInitializeTypeDef()
			LoadEverything(states.Where(a => a.ModifiedTypes.Count != 0 || (a.LoadClassHash != null && a.LoadClassHash.Count != 0)).Select(a => a.CorModuleDefFile.DnModule.CorModuleDef));

			foreach (var state in states)
				new TreeViewUpdater(fileTreeView, state.CorModuleDefFile, state.ModuleNode, state.ModifiedTypes, state.LoadClassHash).Update();
		}

		HashSet<uint> GetModifiedTypesList(CorModuleDefFile cmdf) {
			var hash = new HashSet<uint>();

			var oldLastValid = cmdf.UpdateLastValidRids();
			var lastValid = cmdf.LastValidRids;
			if (oldLastValid.Equals(lastValid))
				return hash;

			const uint TYPEDEF_TOKEN = 0x02000000;

			// Optimization if we loaded a big file
			if (oldLastValid.TypeDefRid == 0) {
				for (uint rid = 1; rid <= lastValid.TypeDefRid; rid++)
					hash.Add(TYPEDEF_TOKEN + rid);
				return hash;
			}

			var cmd = cmdf.DnModule.CorModuleDef;
			Debug.Assert(cmd != null);

			var methodRids = new HashSet<uint>();
			var gpRids = new HashSet<uint>();
			for (uint rid = oldLastValid.TypeDefRid + 1; rid <= lastValid.TypeDefRid; rid++)
				hash.Add(TYPEDEF_TOKEN + rid);
			for (uint rid = oldLastValid.FieldRid + 1; rid <= lastValid.FieldRid; rid++) {
				var typeOwner = cmd.GetFieldOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.MethodRid + 1; rid <= lastValid.MethodRid; rid++) {
				methodRids.Add(rid);
				var typeOwner = cmd.GetMethodOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.ParamRid + 1; rid <= lastValid.ParamRid; rid++) {
				var methodOwner = cmd.GetParamOwnerToken(rid);
				if (methodRids.Contains(methodOwner.Rid))
					continue;
				var typeOwner = cmd.GetMethodOwnerToken(methodOwner.Rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.EventRid + 1; rid <= lastValid.EventRid; rid++) {
				var typeOwner = cmd.GetEventOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.PropertyRid + 1; rid <= lastValid.PropertyRid; rid++) {
				var typeOwner = cmd.GetPropertyOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.GenericParamRid + 1; rid <= lastValid.GenericParamRid; rid++) {
				gpRids.Add(rid);
				var ownerToken = cmd.GetGenericParamOwnerToken(rid);
				MDToken typeOwner;
				if (ownerToken.Table == Table.TypeDef)
					typeOwner = ownerToken;
				else if (ownerToken.Table == Table.Method) {
					if (methodRids.Contains(ownerToken.Rid))
						continue;
					typeOwner = cmd.GetMethodOwnerToken(ownerToken.Rid);
				}
				else
					continue;
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.GenericParamConstraintRid + 1; rid <= lastValid.GenericParamConstraintRid; rid++) {
				var gpOwner = cmd.GetGenericParamConstraintOwnerToken(rid);
				if (gpRids.Contains(gpOwner.Rid))
					continue;
				var ownerToken = cmd.GetGenericParamOwnerToken(gpOwner.Rid);
				MDToken typeOwner;
				if (ownerToken.Table == Table.TypeDef)
					typeOwner = ownerToken;
				else if (ownerToken.Table == Table.Method) {
					if (methodRids.Contains(ownerToken.Rid))
						continue;
					typeOwner = cmd.GetMethodOwnerToken(ownerToken.Rid);
				}
				else
					continue;
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}

			return hash;
		}

		public void LoadEverything(IEnumerable<CorModuleDef> modules) {
			var list = modules.ToArray();
			if (list.Length == 0)
				return;

			// We must cache everything in memory because the MD API COM instances can only be
			// called from the UI thread but the decompiler can run on any thread.

			foreach (var mod in list)
				mod.DisableMDAPICalls = false;
			try {
				var data = new LoadEverythingVM(list);
				var win = new LoadEverythingDlg();
				win.DataContext = data;
				win.Owner = ownerWindow;
				var res = win.ShowDialog();
				if (res != true) {
					Debug.Fail("User canceled but this is currently impossible...");
					//TODO: User canceled (can't currently happen though)
				}
			}
			finally {
				foreach (var mod in list)
					mod.DisableMDAPICalls = true;
			}
		}
	}
}
