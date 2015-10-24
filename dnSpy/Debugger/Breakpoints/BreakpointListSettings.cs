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

using System.ComponentModel;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.Files;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointListSettings {
		public static readonly BreakpointListSettings Instance = new BreakpointListSettings();
		const string SETTINGS_NAME = "Breakpoints";
		int disableSaveCounter;

		BreakpointListSettings() {
			BreakpointManager.Instance.OnListModified += BreakpointManager_OnListModified;
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

		void Save() {
			DNSpySettings.Update(root => Save(root));
		}

		internal void OnLoaded() {
			disableSaveCounter++;
			try {
				LoadInternal();
			}
			finally {
				disableSaveCounter--;
			}
		}

		void LoadInternal() {
			DNSpySettings settings = DNSpySettings.Load();
			var bpsx = settings[SETTINGS_NAME];
			BreakpointManager.Instance.Clear();
			foreach (var bpx in bpsx.Elements("Breakpoint")) {
				uint? token = (uint?)bpx.Attribute("Token");
				string asmFullName = SessionSettings.Unescape((string)bpx.Attribute("AssemblyFullName"));
				string moduleName = SessionSettings.Unescape((string)bpx.Attribute("ModuleName"));
				bool? isDynamic = (bool?)bpx.Attribute("IsDynamic");
				bool? isInMemory = (bool?)bpx.Attribute("IsInMemory");
				uint? ilOffset = (uint?)bpx.Attribute("ILOffset");
				bool? isEnabled = (bool?)bpx.Attribute("IsEnabled");

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
					var s = SessionSettings.Unescape((string)bpx.Attribute("Method"));
					if (s == null || s != GetMethodAsString(key))
						continue;
				}

				var bp = new ILCodeBreakpoint(key, ilOffset.Value, isEnabled.Value);
				BreakpointManager.Instance.Add(bp);
			}
		}

		void Save(XElement root) {
			// Prevent Load() from saving the settings every time a new BP is added
			if (disableSaveCounter != 0)
				return;

			var bps = new XElement(SETTINGS_NAME);
			var existingElement = root.Element(SETTINGS_NAME);
			if (existingElement != null)
				existingElement.ReplaceWith(bps);
			else
				root.Add(bps);

			foreach (var bp in BreakpointManager.Instance.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null) {
					if (string.IsNullOrEmpty(ilbp.SerializedDnSpyToken.Module.ModuleName))
						continue;
					if (string.IsNullOrEmpty(ilbp.SerializedDnSpyToken.Module.AssemblyFullName))
						continue;

					var bpx = new XElement("Breakpoint");
					bpx.SetAttributeValue("Token", ilbp.SerializedDnSpyToken.Token);
					bpx.SetAttributeValue("AssemblyFullName", SessionSettings.Escape(ilbp.SerializedDnSpyToken.Module.AssemblyFullName));
					bpx.SetAttributeValue("ModuleName", SessionSettings.Escape(ilbp.SerializedDnSpyToken.Module.ModuleName));
					bpx.SetAttributeValue("IsDynamic", ilbp.SerializedDnSpyToken.Module.IsDynamic);
					bpx.SetAttributeValue("IsInMemory", ilbp.SerializedDnSpyToken.Module.IsInMemory);
					bpx.SetAttributeValue("ILOffset", ilbp.ILOffset);
					bpx.SetAttributeValue("IsEnabled", ilbp.IsEnabled);
					if (!ilbp.SerializedDnSpyToken.Module.IsInMemory && !ilbp.SerializedDnSpyToken.Module.IsDynamic) {
						var s = GetMethodAsString(ilbp.SerializedDnSpyToken);
						if (s == null)
							continue;
						bpx.SetAttributeValue("Method", SessionSettings.Escape(s));
					}
					bps.Add(bpx);
					continue;
				}

				var debp = bp as DebugEventBreakpoint;
				if (debp != null) {
					//TODO:
					continue;
				}
			}
		}

		static string GetMethodAsString(SerializedDnSpyToken key) {
			var file = ModuleLoader.Instance.LoadModule(key.Module, true);
			var method = file == null ? null : file.ModuleDef.ResolveToken(key.Token) as MethodDef;
			return method == null ? null : method.ToString();
		}
	}
}
