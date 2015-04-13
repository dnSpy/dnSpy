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
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.NRefactory;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger
{
	sealed class BreakpointSettings
	{
		public static readonly BreakpointSettings Instance = new BreakpointSettings();
		int disableSaveCounter;

		BreakpointSettings() {
			BookmarkManager.Added += BookmarkManager_Added;
			BookmarkManager.Removed += BookmarkManager_Removed;
		}

		void BookmarkManager_Added(object sender, BookmarkEventArgs e)
		{
			var bpm = e.Bookmark as BreakpointBookmark;
			if (bpm == null)
				return;
			bpm.IsEnabledChanged += BreakpointBookmark_OnIsEnabledChanged;
			Save();
		}

		void BookmarkManager_Removed(object sender, BookmarkEventArgs e)
		{
			var bpm = e.Bookmark as BreakpointBookmark;
			if (bpm == null)
				return;
			bpm.IsEnabledChanged -= BreakpointBookmark_OnIsEnabledChanged;
			Save();
		}

		void BreakpointBookmark_OnIsEnabledChanged(object sender, System.EventArgs e)
		{
			Save();
		}

		void Save()
		{
			ILSpySettings.Update(root => Save(root));
		}

		public void Load()
		{
			Interlocked.Increment(ref disableSaveCounter);
			try {
				LoadInternal();
			}
			finally {
				Interlocked.Decrement(ref disableSaveCounter);
			}
		}

		public void LoadInternal()
		{
			ILSpySettings settings = ILSpySettings.Load();
			var bpsx = settings["Breakpoints"];
			BookmarkManager.RemoveMarks<BreakpointBookmark>();
			foreach (var bpx in bpsx.Elements("Breakpoint")) {
				int? token = (int?)bpx.Attribute("Token");
				string moduleFullPath = (string)bpx.Attribute("ModuleFullPath");
				string assemblyFullPath = (string)bpx.Attribute("AssemblyFullPath");
				uint? from = (uint?)bpx.Attribute("From");
				uint? to = (uint?)bpx.Attribute("To");
				bool? isEnabled = (bool?)bpx.Attribute("IsEnabled");
				int? locationLine = (int?)bpx.Attribute("LocationLine");
				int? locationColumn = (int?)bpx.Attribute("LocationColumn");
				int? endLocationLine = (int?)bpx.Attribute("EndLocationLine");
				int? endLocationColumn = (int?)bpx.Attribute("EndLocationColumn");
				string methodFullName = (string)bpx.Attribute("MethodFullName");

				if (token == null) continue;
				if (string.IsNullOrEmpty(moduleFullPath)) continue;
				if (assemblyFullPath == null) continue;
				if (from == null || to == null || from.Value >= to.Value) continue;
				if (isEnabled == null) continue;
				if (locationLine == null || locationLine.Value < 1) continue;
				if (locationColumn == null || locationColumn.Value < 1) continue;
				if (endLocationLine == null || endLocationLine.Value < 1) continue;
				if (endLocationColumn == null || endLocationColumn.Value < 1) continue;
				var location = new TextLocation(locationLine.Value, locationColumn.Value);
				var endLocation = new TextLocation(endLocationLine.Value, endLocationColumn.Value);
				if (location >= endLocation) continue;

				ModuleDefMD loadedMod;
				try {
					loadedMod = MainWindow.Instance.LoadAssembly(assemblyFullPath, moduleFullPath).ModuleDefinition as ModuleDefMD;
				}
				catch {
					continue;
				}
				if (loadedMod == null)
					continue;

				var method = loadedMod.ResolveToken(token.Value) as MethodDef;
				if (method == null)
					continue;

				// Add an extra check to make sure that the file hasn't been re-created. This check
				// isn't perfect but should work most of the time unless the file was re-compiled
				// with the same tools and no methods were added or removed.
				if (method.FullName != methodFullName) continue;

				var bpm = new BreakpointBookmark(method, location, endLocation, new ILRange(from.Value, to.Value), isEnabled.Value);
				BookmarkManager.AddMark(bpm);
			}
		}

		public void Save(XElement root)
		{
			// Prevent Load() from saving the settings every time a new BP is added
			if (disableSaveCounter != 0)
				return;

			var bps = new XElement("Breakpoints");
			var existingElement = root.Element("Breakpoints");
			if (existingElement != null)
				existingElement.ReplaceWith(bps);
			else
				root.Add(bps);

			foreach (var bm in BookmarkManager.Bookmarks) {
				var bp = bm as BreakpointBookmark;
				if (bp == null)
					continue;
				var method = bp.MemberReference as MethodDef;
				if (method == null)
					continue;

				var asm = method.Module.Assembly;
				var mainModule = asm == null ? null : asm.ManifestModule;

				var bpx = new XElement("Breakpoint");
				bpx.SetAttributeValue("Token", bp.MethodKey.Token);
				bpx.SetAttributeValue("ModuleFullPath", bp.MethodKey.ModuleFullPath);
				bpx.SetAttributeValue("AssemblyFullPath", mainModule == null ? bp.MethodKey.ModuleFullPath : mainModule.Location);
				bpx.SetAttributeValue("From", bp.ILRange.From);
				bpx.SetAttributeValue("To", bp.ILRange.To);
				bpx.SetAttributeValue("IsEnabled", bp.IsEnabled);
				bpx.SetAttributeValue("LocationLine", bp.Location.Line);
				bpx.SetAttributeValue("LocationColumn", bp.Location.Column);
				bpx.SetAttributeValue("EndLocationLine", bp.EndLocation.Line);
				bpx.SetAttributeValue("EndLocationColumn", bp.EndLocation.Column);
				bpx.SetAttributeValue("MethodFullName", method.FullName);
				bps.Add(bpx);
			}
		}
	}
}
