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
using dndbg.Engine;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointSettings {
		public static readonly BreakpointSettings Instance = new BreakpointSettings();
		int disableSaveCounter;

		BreakpointSettings() {
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
			ILSpySettings.Update(root => Save(root));
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

		public void LoadInternal() {
			ILSpySettings settings = ILSpySettings.Load();
			var bpsx = settings["Breakpoints"];
			BreakpointManager.Instance.Clear();
			foreach (var bpx in bpsx.Elements("Breakpoint")) {
				uint? token = (uint?)bpx.Attribute("Token");
				string assemblyFullPath = SessionSettings.Unescape((string)bpx.Attribute("AssemblyFullPath"));
				string moduleFullPath = SessionSettings.Unescape((string)bpx.Attribute("ModuleFullPath"));
				bool isDynamic = (bool?)bpx.Attribute("IsDynamic") ?? false;
				bool isInMemory = (bool?)bpx.Attribute("IsInMemory") ?? false;
				uint? ilOffset = (uint?)bpx.Attribute("ILOffset") ?? (uint?)bpx.Attribute("From");//TODO: Remove "From" some time after this commit
				bool? isEnabled = (bool?)bpx.Attribute("IsEnabled");

				if (token == null)
					continue;
				if (string.IsNullOrEmpty(moduleFullPath))
					continue;
				if (ilOffset == null)
					continue;
				if (isEnabled == null)
					continue;

				var snModule = new SerializedDnModule(moduleFullPath, isDynamic, isInMemory);
				var key = MethodKey.Create(token.Value, snModule);
				var bp = new ILCodeBreakpoint(assemblyFullPath, key, ilOffset.Value, isEnabled.Value);
				BreakpointManager.Instance.Add(bp);
			}
		}

		public void Save(XElement root) {
			// Prevent Load() from saving the settings every time a new BP is added
			if (disableSaveCounter != 0)
				return;

			var bps = new XElement("Breakpoints");
			var existingElement = root.Element("Breakpoints");
			if (existingElement != null)
				existingElement.ReplaceWith(bps);
			else
				root.Add(bps);

			foreach (var bp in BreakpointManager.Instance.Breakpoints) {
				var ilbp = bp as ILCodeBreakpoint;
				if (ilbp != null) {
					var bpx = new XElement("Breakpoint");
					bpx.SetAttributeValue("Token", ilbp.MethodKey.Token);
					bpx.SetAttributeValue("AssemblyFullPath", SessionSettings.Escape(ilbp.Assembly));
					bpx.SetAttributeValue("ModuleFullPath", SessionSettings.Escape(ilbp.MethodKey.Module.Name));
					bpx.SetAttributeValue("IsDynamic", ilbp.MethodKey.Module.IsDynamic);
					bpx.SetAttributeValue("IsInMemory", ilbp.MethodKey.Module.IsInMemory);
					bpx.SetAttributeValue("ILOffset", ilbp.ILOffset);
					bpx.SetAttributeValue("IsEnabled", ilbp.IsEnabled);
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
	}
}
