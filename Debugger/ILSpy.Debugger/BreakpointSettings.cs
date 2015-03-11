using System.Collections.Generic;
using System.Linq;
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

		BreakpointSettings() {
			BookmarkManager.Added += delegate { Save(); };
			BookmarkManager.Removed += delegate { Save(); };
		}

		void Save()
		{
			ILSpySettings.Update(root => Save(root));
		}

		public void Load()
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

				if (token == null) continue;
				if (string.IsNullOrEmpty(moduleFullPath)) continue;
				if (string.IsNullOrEmpty(assemblyFullPath)) continue;
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

				var bpm = new BreakpointBookmark(method, location, endLocation, new ILRange(from.Value, to.Value));
				BookmarkManager.AddMark(bpm);
			}
		}

		public void Save(XElement root)
		{
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
				if (asm == null)
					continue;
				var mainModule = asm.ManifestModule;
				if (mainModule == null)
					continue;

				var bpx = new XElement("Breakpoint");
				bpx.SetAttributeValue("Token", bp.MethodKey.Token);
				bpx.SetAttributeValue("ModuleFullPath", bp.MethodKey.ModuleFullPath);
				bpx.SetAttributeValue("AssemblyFullPath", mainModule.Location);
				bpx.SetAttributeValue("From", bp.ILRange.From);
				bpx.SetAttributeValue("To", bp.ILRange.To);
				bpx.SetAttributeValue("IsEnabled", bp.IsEnabled);
				bpx.SetAttributeValue("LocationLine", bp.Location.Line);
				bpx.SetAttributeValue("LocationColumn", bp.Location.Column);
				bpx.SetAttributeValue("EndLocationLine", bp.EndLocation.Line);
				bpx.SetAttributeValue("EndLocationColumn", bp.EndLocation.Column);
				bps.Add(bpx);
			}
		}
	}
}
