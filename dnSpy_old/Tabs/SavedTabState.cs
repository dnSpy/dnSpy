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
using dnSpy.AvalonEdit;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.TreeNodes;

namespace dnSpy.Tabs {
	public class SavedTabGroupsState {
		public List<SavedTabGroupState> Groups = new List<SavedTabGroupState>();
		public int Index;
		public bool IsHorizontal;

		public void Write(ISettingsSection section) {
			section.Attribute("index", Index);
			section.Attribute("is-horizontal", IsHorizontal);

			foreach (var group in Groups)
				group.Write(section.CreateSection("TabGroup"));
		}

		public static SavedTabGroupsState Read(ISettingsSection section) {
			var savedState = new SavedTabGroupsState();

			savedState.Index = section.Attribute<int?>("index") ?? 0;
			savedState.IsHorizontal = section.Attribute<bool?>("is-horizontal") ?? true;

			foreach (var group in section.SectionsWithName("TabGroup"))
				savedState.Groups.Add(SavedTabGroupState.Read(group));

			return savedState;
		}
	}

	public class SavedTabGroupState {
		public List<SavedTabState> Tabs = new List<SavedTabState>();
		public int Index;

		public void Write(ISettingsSection section) {
			section.Attribute("index", Index);

			foreach (var tab in Tabs)
				tab.Write(section.CreateSection("Tab"));
		}

		public static SavedTabGroupState Read(ISettingsSection section) {
			var savedState = new SavedTabGroupState();

			savedState.Index = section.Attribute<int?>("index") ?? 0;

			foreach (var tab in section.SectionsWithName("Tab")) {
				var tabState = SavedTabState.Read(tab);
				if (tabState != null)
					savedState.Tabs.Add(tabState);
			}

			return savedState;
		}
	}

	public abstract class SavedTabState {
		protected abstract string Type { get; }

		public void Write(ISettingsSection xml) {
			xml.Attribute("tab-type", Type);
			WriteOverride(xml);
		}

		protected abstract void WriteOverride(ISettingsSection xml);

		public static SavedTabState Read(ISettingsSection child) {
			var type = child.Attribute<string>("tab-type");
			if (type == SavedDecompileTabState.TYPE)
				return SavedDecompileTabState.ReadInternal(child);
			if (type == SavedHexTabState.TYPE)
				return SavedHexTabState.ReadInternal(child);
			Debug.Fail(string.Format("Unknown type: {0}", type));
			return null;
		}
	}

	public class SavedDecompileTabState : SavedTabState {
		public static readonly string TYPE = "code";

		public List<FullNodePathName> Paths = new List<FullNodePathName>();
		public List<string> ActiveAutoLoadedAssemblies;
		public EditorPositionState EditorPositionState;
		public string Language;

		protected override string Type {
			get { return TYPE; }
		}

		protected override void WriteOverride(ISettingsSection section) {
			section.Attribute("language", Language);

			foreach (var path in Paths)
				path.Write(section.CreateSection("Path"));

			if (ActiveAutoLoadedAssemblies.Count > 0) {
				var autoLoadedSection = section.CreateSection("ActiveAutoLoadedAssemblies");
				foreach (var a in ActiveAutoLoadedAssemblies) {
					var nodeSection = autoLoadedSection.CreateSection("Node");
					nodeSection.Attribute("Value", a);
				}
			}

			EditorPositionState.Write(section.CreateSection("EditorPositionState"));
		}

		internal static SavedDecompileTabState ReadInternal(ISettingsSection section) {
			var savedState = new SavedDecompileTabState();

			savedState.Language = section.Attribute<string>("language") ?? "C#";

			foreach (var path in section.SectionsWithName("Path"))
				savedState.Paths.Add(FullNodePathName.Read(path));

			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			var autoAsms = section.TryGetSection("ActiveAutoLoadedAssemblies");
			if (autoAsms != null)
				savedState.ActiveAutoLoadedAssemblies.AddRange(autoAsms.SectionsWithName("Node").Select(e => e.Attribute<string>("Value")));

			savedState.EditorPositionState = EditorPositionState.Read(section.GetOrCreateSection("EditorPositionState"));

			return savedState;
		}
	}

	public class SavedHexTabState : SavedTabState {
		public static readonly string TYPE = "hex";

		public HexBoxState HexBoxState = new HexBoxState();
		public int? BytesGroupCount;
		public int? BytesPerLine;
		public bool? UseHexPrefix;
		public bool? ShowAscii;
		public bool? LowerCaseHex;
		public AsciiEncoding? AsciiEncoding;

		public int HexOffsetSize;
		public bool UseRelativeOffsets;
		public ulong BaseOffset;
		public string FileName;

		protected override string Type {
			get { return TYPE; }
		}

		protected override void WriteOverride(ISettingsSection section) {
			section.Attribute("BytesGroupCount", BytesGroupCount);
			section.Attribute("BytesPerLine", BytesPerLine);
			section.Attribute("UseHexPrefix", UseHexPrefix);
			section.Attribute("ShowAscii", ShowAscii);
			section.Attribute("LowerCaseHex", LowerCaseHex);
			section.Attribute("AsciiEncoding", AsciiEncoding);

			section.Attribute("HexOffsetSize", HexOffsetSize);
			section.Attribute("UseRelativeOffsets", UseRelativeOffsets);
			section.Attribute("BaseOffset", BaseOffset);
			section.Attribute("FileName", FileName);

			section.Attribute("HexBoxState-TopOffset", HexBoxState.TopOffset);
			section.Attribute("HexBoxState-Column", HexBoxState.Column);
			section.Attribute("HexBoxState-StartOffset", HexBoxState.StartOffset);
			section.Attribute("HexBoxState-EndOffset", HexBoxState.EndOffset);
			section.Attribute("HexBoxState-HexBoxPosition-Offset", HexBoxState.CaretPosition.Offset);
			section.Attribute("HexBoxState-HexBoxPosition-Kind", HexBoxState.CaretPosition.Kind);
			section.Attribute("HexBoxState-HexBoxPosition-KindPosition", HexBoxState.CaretPosition.KindPosition);
			if (HexBoxState.Selection != null) {
				section.Attribute("HexBoxState-Selection-From", HexBoxState.Selection.Value.From);
				section.Attribute("HexBoxState-Selection-To", HexBoxState.Selection.Value.To);
			}
		}

		internal static SavedHexTabState ReadInternal(ISettingsSection section) {
			var savedState = new SavedHexTabState();

			savedState.BytesGroupCount = section.Attribute<int?>("BytesGroupCount");
			savedState.BytesPerLine = section.Attribute<int?>("BytesPerLine");
			savedState.UseHexPrefix = section.Attribute<bool?>("UseHexPrefix");
			savedState.ShowAscii = section.Attribute<bool?>("ShowAscii");
			savedState.LowerCaseHex = section.Attribute<bool?>("LowerCaseHex");
			savedState.AsciiEncoding = section.Attribute<AsciiEncoding?>("AsciiEncoding");

			savedState.HexOffsetSize = section.Attribute<int?>("HexOffsetSize") ?? 0;
			savedState.UseRelativeOffsets = section.Attribute<bool?>("UseRelativeOffsets") ?? false;
			savedState.BaseOffset = section.Attribute<ulong?>("BaseOffset") ?? 0;
			savedState.FileName = section.Attribute<string>("FileName");

			savedState.HexBoxState.TopOffset = section.Attribute<ulong?>("HexBoxState-TopOffset") ?? 0;
			savedState.HexBoxState.Column = section.Attribute<int?>("HexBoxState-Column") ?? 0;
			savedState.HexBoxState.StartOffset = section.Attribute<ulong?>("HexBoxState-StartOffset") ?? 0;
			savedState.HexBoxState.EndOffset = section.Attribute<ulong?>("HexBoxState-EndOffset") ?? 0;
			savedState.HexBoxState.CaretPosition.Offset = section.Attribute<ulong?>("HexBoxState-HexBoxPosition-Offset") ?? 0;
			savedState.HexBoxState.CaretPosition.Kind = section.Attribute<HexBoxPositionKind?>("HexBoxState-HexBoxPosition-Kind") ?? 0;
			savedState.HexBoxState.CaretPosition.KindPosition = section.Attribute<byte?>("HexBoxState-HexBoxPosition-KindPosition") ?? 0;

			var from = section.Attribute<ulong?>("HexBoxState-Selection-From");
			var to = section.Attribute<ulong?>("HexBoxState-Selection-To");
			if (from != null && to != null)
				savedState.HexBoxState.Selection = new HexSelection((ulong)from, (ulong)to);

			return savedState;
		}
	}
}
