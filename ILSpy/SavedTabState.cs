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
using System.Xml.Linq;
using dnSpy.HexEditor;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy {
	public class SavedTabGroupsState {
		public List<SavedTabGroupState> Groups = new List<SavedTabGroupState>();
		public int Index;
		public bool IsHorizontal;

		public XElement ToXml(XElement xml) {
			xml.SetAttributeValue("index", Index);
			xml.SetAttributeValue("is-horizontal", IsHorizontal);

			foreach (var group in Groups)
				xml.Add(group.ToXml(new XElement("TabGroup")));

			return xml;
		}

		public static SavedTabGroupsState FromXml(XElement child) {
			var savedState = new SavedTabGroupsState();

			savedState.Index = (int)child.Attribute("index");
			savedState.IsHorizontal = (bool)child.Attribute("is-horizontal");

			foreach (var group in child.Elements("TabGroup"))
				savedState.Groups.Add(SavedTabGroupState.FromXml(group));

			return savedState;
		}
	}

	public class SavedTabGroupState {
		public List<SavedTabState> Tabs = new List<SavedTabState>();
		public int Index;

		public XElement ToXml(XElement xml) {
			xml.SetAttributeValue("index", Index);

			foreach (var tab in Tabs)
				xml.Add(tab.ToXml(new XElement("Tab")));

			return xml;
		}

		public static SavedTabGroupState FromXml(XElement child) {
			var savedState = new SavedTabGroupState();

			savedState.Index = (int)child.Attribute("index");

			foreach (var tab in child.Elements("Tab")) {
				var tabState = SavedTabState.FromXml(tab);
				if (tabState != null)
					savedState.Tabs.Add(tabState);
			}

			return savedState;
		}
	}

	public abstract class SavedTabState {
		protected abstract string Type { get; }

		public XElement ToXml(XElement xml) {
			xml.SetAttributeValue("tab-type", Type);
			ToXmlOverride(xml);
			return xml;
		}

		protected abstract void ToXmlOverride(XElement xml);

		public static SavedTabState FromXml(XElement child) {
			var type = (string)child.Attribute("tab-type");
			//TODO: Remove the null check after some time after this commit. Only here so older files can be loaded.
			if (type == null || type == SavedDecompileTabState.TYPE)
				return SavedDecompileTabState.FromXmlInternal(child);
			if (type == SavedHexTabState.TYPE)
				return SavedHexTabState.FromXmlInternal(child);
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

		protected override void ToXmlOverride(XElement xml) {
			xml.SetAttributeValue("language", SessionSettings.Escape(Language));

			foreach (var path in Paths)
				xml.Add(path.ToXml(new XElement("Path")));

			var asms = new XElement("ActiveAutoLoadedAssemblies", ActiveAutoLoadedAssemblies.Select(p => new XElement("Node", SessionSettings.Escape(p))));
			xml.Add(asms);

			xml.Add(EditorPositionState.ToXml(new XElement("EditorPositionState")));
		}

		internal static SavedDecompileTabState FromXmlInternal(XElement child) {
			var savedState = new SavedDecompileTabState();

			savedState.Language = SessionSettings.Unescape((string)child.Attribute("language")) ?? "C#";

			foreach (var path in child.Elements("Path"))
				savedState.Paths.Add(FullNodePathName.FromXml(path));

			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			var autoAsms = child.Element("ActiveAutoLoadedAssemblies");
			if (autoAsms != null)
				savedState.ActiveAutoLoadedAssemblies.AddRange(autoAsms.Elements().Select(e => SessionSettings.Unescape((string)e)));

			savedState.EditorPositionState = EditorPositionState.FromXml(child.Element("EditorPositionState"));

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

		public int HexOffsetSize;
		public bool UseRelativeOffsets;
		public ulong BaseOffset;
		public string FileName;

		protected override string Type {
			get { return TYPE; }
		}

		protected override void ToXmlOverride(XElement xml) {
			xml.SetAttributeValue("BytesGroupCount", BytesGroupCount);
			xml.SetAttributeValue("BytesPerLine", BytesPerLine);
			xml.SetAttributeValue("UseHexPrefix", UseHexPrefix);
			xml.SetAttributeValue("ShowAscii", ShowAscii);
			xml.SetAttributeValue("LowerCaseHex", LowerCaseHex);

			xml.SetAttributeValue("HexOffsetSize", HexOffsetSize);
			xml.SetAttributeValue("UseRelativeOffsets", UseRelativeOffsets);
			xml.SetAttributeValue("BaseOffset", BaseOffset);
			xml.SetAttributeValue("FileName", SessionSettings.Escape(FileName));

			xml.SetAttributeValue("HexBoxState-TopOffset", HexBoxState.TopOffset);
			xml.SetAttributeValue("HexBoxState-Column", HexBoxState.Column);
			xml.SetAttributeValue("HexBoxState-StartOffset", HexBoxState.StartOffset);
			xml.SetAttributeValue("HexBoxState-EndOffset", HexBoxState.EndOffset);
			xml.SetAttributeValue("HexBoxState-HexBoxPosition-Offset", HexBoxState.CaretPosition.Offset);
			xml.SetAttributeValue("HexBoxState-HexBoxPosition-Kind", (int)HexBoxState.CaretPosition.Kind);
			xml.SetAttributeValue("HexBoxState-HexBoxPosition-KindPosition", (int)HexBoxState.CaretPosition.KindPosition);
			if (HexBoxState.Selection != null) {
				xml.SetAttributeValue("HexBoxState-Selection-From", HexBoxState.Selection.Value.From);
				xml.SetAttributeValue("HexBoxState-Selection-To", HexBoxState.Selection.Value.To);
			}
		}

		internal static SavedHexTabState FromXmlInternal(XElement child) {
			var savedState = new SavedHexTabState();

			savedState.BytesGroupCount = (int?)child.Attribute("BytesGroupCount");
			savedState.BytesPerLine = (int?)child.Attribute("BytesPerLine");
			savedState.UseHexPrefix = (bool?)child.Attribute("UseHexPrefix");
			savedState.ShowAscii = (bool?)child.Attribute("ShowAscii");
			savedState.LowerCaseHex = (bool?)child.Attribute("LowerCaseHex");

			savedState.HexOffsetSize = (int)child.Attribute("HexOffsetSize");
			savedState.UseRelativeOffsets = (bool)child.Attribute("UseRelativeOffsets");
			savedState.BaseOffset = (ulong)child.Attribute("BaseOffset");
			savedState.FileName = SessionSettings.Unescape((string)child.Attribute("FileName"));

			savedState.HexBoxState.TopOffset = (ulong)child.Attribute("HexBoxState-TopOffset");
			savedState.HexBoxState.Column = (int)child.Attribute("HexBoxState-Column");
			savedState.HexBoxState.StartOffset = (ulong)child.Attribute("HexBoxState-StartOffset");
			savedState.HexBoxState.EndOffset = (ulong)child.Attribute("HexBoxState-EndOffset");
			savedState.HexBoxState.CaretPosition.Offset = (ulong)child.Attribute("HexBoxState-HexBoxPosition-Offset");
			savedState.HexBoxState.CaretPosition.Kind = (HexBoxPositionKind)(int)child.Attribute("HexBoxState-HexBoxPosition-Kind");
			savedState.HexBoxState.CaretPosition.KindPosition = (byte)(int)child.Attribute("HexBoxState-HexBoxPosition-KindPosition");

			var from = child.Attribute("HexBoxState-Selection-From");
			var to = child.Attribute("HexBoxState-Selection-To");
			if (from != null && to != null)
				savedState.HexBoxState.Selection = new HexSelection((ulong)from, (ulong)to);

			return savedState;
		}
	}
}
