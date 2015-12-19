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

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.Files;

namespace dnSpy.Debugger.Breakpoints {
	[ExportAutoLoaded]
	sealed class BreakpointListLoader : IAutoLoaded {
		[ImportingConstructor]
		BreakpointListLoader(IBreakpointListSettings breakpointListSettings) {
			// breakpointListSettings loads the breakpoints
		}
	}

	interface IBreakpointListSettings {
	}

	[Export, Export(typeof(IBreakpointListSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class BreakpointListSettings : IBreakpointListSettings {
		static readonly Guid SETTINGS_GUID = new Guid("FBC6039C-8A7A-49DC-9C32-52C1B73DE0A3");

		readonly ISettingsManager settingsManager;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IBreakpointManager breakpointManager;

		[ImportingConstructor]
		BreakpointListSettings(ISettingsManager settingsManager, Lazy<IModuleLoader> moduleLoader, IBreakpointManager breakpointManager) {
			this.settingsManager = settingsManager;
			this.moduleLoader = moduleLoader;
			this.breakpointManager = breakpointManager;
			breakpointManager.OnListModified += BreakpointManager_OnListModified;
			Load();
		}

		void BreakpointManager_OnListModified(object sender, BreakpointListModifiedEventArgs e) {
			if (e.Added)
				e.Breakpoint.PropertyChanged += Breakpoint_PropertyChanged;
			else
				e.Breakpoint.PropertyChanged -= Breakpoint_PropertyChanged;

			Save();
		}

		void Breakpoint_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled")
				Save();
		}

		void Load() {
			disableSaveCounter++;
			try {
				LoadInternal();
			}
			finally {
				disableSaveCounter--;
			}
		}
		int disableSaveCounter;

		void LoadInternal() {
			var section = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			breakpointManager.Clear();
			foreach (var bpx in section.SectionsWithName("Breakpoint")) {
				uint? token = bpx.Attribute<uint?>("Token");
				string asmFullName = bpx.Attribute<string>("AssemblyFullName");
				string moduleName = bpx.Attribute<string>("ModuleName");
				bool? isDynamic = bpx.Attribute<bool?>("IsDynamic");
				bool? isInMemory = bpx.Attribute<bool?>("IsInMemory");
				uint? ilOffset = bpx.Attribute<uint?>("ILOffset");
				bool? isEnabled = bpx.Attribute<bool?>("IsEnabled");

				if (token == null)
					continue;
				if (isDynamic == null || isInMemory == null)
					continue;
				if (string.IsNullOrEmpty(asmFullName))
					continue;
				if (string.IsNullOrEmpty(moduleName))
					continue;
				if (ilOffset == null)
					continue;
				if (isEnabled == null)
					continue;

				var snModule = SerializedDnSpyModule.Create(asmFullName, moduleName, isDynamic.Value, isInMemory.Value);
				var key = new SerializedDnSpyToken(snModule, token.Value);

				if (!isInMemory.Value && !isDynamic.Value) {
					var s = bpx.Attribute<string>("Method");
					if (s == null || s != GetMethodAsString(key))
						continue;
				}

				var bp = new ILCodeBreakpoint(key, ilOffset.Value, isEnabled.Value);
				breakpointManager.Add(bp);
			}
		}

		void Save() {
			// Prevent Load() from saving the settings every time a new BP is added
			if (disableSaveCounter != 0)
				return;

			var section = settingsManager.RecreateSection(SETTINGS_GUID);

			foreach (var bp in breakpointManager.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null) {
					if (string.IsNullOrEmpty(ilbp.SerializedDnSpyToken.Module.ModuleName))
						continue;
					if (string.IsNullOrEmpty(ilbp.SerializedDnSpyToken.Module.AssemblyFullName))
						continue;

					var bpx = section.CreateSection("Breakpoint");
					bpx.Attribute("Token", ilbp.SerializedDnSpyToken.Token);
					bpx.Attribute("AssemblyFullName", ilbp.SerializedDnSpyToken.Module.AssemblyFullName);
					bpx.Attribute("ModuleName", ilbp.SerializedDnSpyToken.Module.ModuleName);
					bpx.Attribute("IsDynamic", ilbp.SerializedDnSpyToken.Module.IsDynamic);
					bpx.Attribute("IsInMemory", ilbp.SerializedDnSpyToken.Module.IsInMemory);
					bpx.Attribute("ILOffset", ilbp.ILOffset);
					bpx.Attribute("IsEnabled", ilbp.IsEnabled);
					if (!ilbp.SerializedDnSpyToken.Module.IsInMemory && !ilbp.SerializedDnSpyToken.Module.IsDynamic) {
						var s = GetMethodAsString(ilbp.SerializedDnSpyToken);
						if (s == null)
							continue;
						bpx.Attribute("Method", s);
					}
					continue;
				}

				var debp = bp as DebugEventBreakpoint;
				if (debp != null) {
					//TODO:
					continue;
				}
			}
		}

		string GetMethodAsString(SerializedDnSpyToken key) {
			var file = moduleLoader.Value.LoadModule(key.Module, true, true);
			var method = file == null ? null : file.ModuleDef.ResolveToken(key.Token) as MethodDef;
			return method == null ? null : method.ToString();
		}
	}
}
