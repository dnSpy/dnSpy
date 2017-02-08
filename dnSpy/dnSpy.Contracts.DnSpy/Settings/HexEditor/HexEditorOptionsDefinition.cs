/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Settings.HexEditor {
	/// <summary>
	/// Defines code editor options that will be shown in the UI. Use <see cref="ExportHexEditorOptionsDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public sealed class HexEditorOptionsDefinition {
	}

	/// <summary>Metadata</summary>
	public interface IHexEditorOptionsDefinitionMetadata {
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.Name"/></summary>
		string Name { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.SubGroup"/></summary>
		string SubGroup { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.Type"/></summary>
		Type Type { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HexOffsetFormat"/></summary>
		HexOffsetFormat HexOffsetFormat { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ValuesLowerCaseHex"/></summary>
		bool ValuesLowerCaseHex { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.OffsetLowerCaseHex"/></summary>
		bool OffsetLowerCaseHex { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.GroupSizeInBytes"/></summary>
		int GroupSizeInBytes { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.EnableColorization"/></summary>
		bool EnableColorization { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.RemoveExtraTextLineVerticalPixels"/></summary>
		bool RemoveExtraTextLineVerticalPixels { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ShowColumnLines"/></summary>
		bool ShowColumnLines { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ColumnLine0"/></summary>
		HexColumnLineKind ColumnLine0 { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ColumnLine1"/></summary>
		HexColumnLineKind ColumnLine1 { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ColumnGroupLine0"/></summary>
		HexColumnLineKind ColumnGroupLine0 { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ColumnGroupLine1"/></summary>
		HexColumnLineKind ColumnGroupLine1 { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HighlightActiveColumn"/></summary>
		bool HighlightActiveColumn { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HighlightCurrentValue"/></summary>
		bool HighlightCurrentValue { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HighlightCurrentValueDelayMilliSeconds"/></summary>
		int HighlightCurrentValueDelayMilliSeconds { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.EncodingCodePage"/></summary>
		int EncodingCodePage { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HighlightStructureUnderMouseCursor"/></summary>
		bool HighlightStructureUnderMouseCursor { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.EnableHighlightCurrentLine"/></summary>
		bool EnableHighlightCurrentLine { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.EnableMouseWheelZoom"/></summary>
		bool EnableMouseWheelZoom { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ZoomLevel"/></summary>
		double ZoomLevel { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.HorizontalScrollBar"/></summary>
		bool HorizontalScrollBar { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.VerticalScrollBar"/></summary>
		bool VerticalScrollBar { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.SelectionMargin"/></summary>
		bool SelectionMargin { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ZoomControl"/></summary>
		bool ZoomControl { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.GlyphMargin"/></summary>
		bool GlyphMargin { get; }
		/// <summary>See <see cref="ExportHexEditorOptionsDefinitionAttribute.ForceClearTypeIfNeeded"/></summary>
		bool ForceClearTypeIfNeeded { get; }
	}

	/// <summary>
	/// Exports a <see cref="HexEditorOptionsDefinition"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ExportHexEditorOptionsDefinitionAttribute : ExportAttribute, IHexEditorOptionsDefinitionMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name shown in the UI</param>
		/// <param name="subGroup">Sub group, eg. <see cref="PredefinedHexViewRoles.HexEditorGroupDefault"/></param>
		/// <param name="guid">Guid of settings</param>
		/// <param name="type">A type in your assembly so resource strings can be read from the resources</param>
		public ExportHexEditorOptionsDefinitionAttribute(string name, string subGroup, string guid, Type type)
			: base(typeof(HexEditorOptionsDefinition)) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (subGroup == null)
				throw new ArgumentNullException(nameof(subGroup));
			if (guid == null)
				throw new ArgumentNullException(nameof(guid));
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			Name = name;
			SubGroup = subGroup;
			Guid = guid;
			Type = type;
			HexOffsetFormat = DefaultHexEditorOptions.HexOffsetFormat;
			ValuesLowerCaseHex = DefaultHexEditorOptions.ValuesLowerCaseHex;
			OffsetLowerCaseHex = DefaultHexEditorOptions.OffsetLowerCaseHex;
			GroupSizeInBytes = DefaultHexEditorOptions.GroupSizeInBytes;
			EnableColorization = DefaultHexEditorOptions.EnableColorization;
			RemoveExtraTextLineVerticalPixels = DefaultHexEditorOptions.RemoveExtraTextLineVerticalPixels;
			ShowColumnLines = DefaultHexEditorOptions.ShowColumnLines;
			ColumnLine0 = DefaultHexEditorOptions.ColumnLine0;
			ColumnLine1 = DefaultHexEditorOptions.ColumnLine1;
			ColumnGroupLine0 = DefaultHexEditorOptions.ColumnGroupLine0;
			ColumnGroupLine1 = DefaultHexEditorOptions.ColumnGroupLine1;
			HighlightActiveColumn = DefaultHexEditorOptions.HighlightActiveColumn;
			HighlightCurrentValue = DefaultHexEditorOptions.HighlightCurrentValue;
			HighlightCurrentValueDelayMilliSeconds = DefaultHexEditorOptions.HighlightCurrentValueDelayMilliSeconds;
			EncodingCodePage = DefaultHexEditorOptions.EncodingCodePage;
			HighlightStructureUnderMouseCursor = DefaultHexEditorOptions.HighlightStructureUnderMouseCursor;
			EnableHighlightCurrentLine = DefaultHexEditorOptions.EnableHighlightCurrentLine;
			EnableMouseWheelZoom = DefaultHexEditorOptions.EnableMouseWheelZoom;
			ZoomLevel = DefaultHexEditorOptions.ZoomLevel;
			HorizontalScrollBar = DefaultHexEditorOptions.HorizontalScrollBar;
			VerticalScrollBar = DefaultHexEditorOptions.VerticalScrollBar;
			SelectionMargin = DefaultHexEditorOptions.SelectionMargin;
			ZoomControl = DefaultHexEditorOptions.ZoomControl;
			GlyphMargin = DefaultHexEditorOptions.GlyphMargin;
			ForceClearTypeIfNeeded = DefaultHexEditorOptions.ForceClearTypeIfNeeded;
		}

		/// <summary>
		/// Name shown in the UI
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Sub group, eg. <see cref="PredefinedHexViewRoles.HexEditorGroupDefault"/>
		/// </summary>
		public string SubGroup { get; }

		/// <summary>
		/// Guid of settings
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Type
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Offset format, default value is <see cref="DefaultHexEditorOptions.HexOffsetFormat"/>
		/// </summary>
		public HexOffsetFormat HexOffsetFormat { get; set; }

		/// <summary>
		/// Values lower case hex, default value is <see cref="DefaultHexEditorOptions.ValuesLowerCaseHex"/>
		/// </summary>
		public bool ValuesLowerCaseHex { get; set; }

		/// <summary>
		/// Offset lower case hex, default value is <see cref="DefaultHexEditorOptions.OffsetLowerCaseHex"/>
		/// </summary>
		public bool OffsetLowerCaseHex { get; set; }

		/// <summary>
		/// Group size in bytes, default value is <see cref="DefaultHexEditorOptions.GroupSizeInBytes"/>
		/// </summary>
		public int GroupSizeInBytes { get; set; }

		/// <summary>
		/// Colorize the hex view, default value is <see cref="DefaultHexEditorOptions.EnableColorization"/>
		/// </summary>
		public bool EnableColorization { get; set; }

		/// <summary>
		/// Remove extra text line vertical pixels, default value is <see cref="DefaultHexEditorOptions.RemoveExtraTextLineVerticalPixels"/>
		/// </summary>
		public bool RemoveExtraTextLineVerticalPixels { get; set; }

		/// <summary>
		/// Show column lines, default value is <see cref="DefaultHexEditorOptions.ShowColumnLines"/>
		/// </summary>
		public bool ShowColumnLines { get; set; }

		/// <summary>
		/// Line between the first two columns (eg. offset and values), default value is <see cref="DefaultHexEditorOptions.ColumnLine0"/>
		/// </summary>
		public HexColumnLineKind ColumnLine0 { get; set; }

		/// <summary>
		/// Line between second and third columns (eg. values and ASCII), default value is <see cref="DefaultHexEditorOptions.ColumnLine1"/>
		/// </summary>
		public HexColumnLineKind ColumnLine1 { get; set; }

		/// <summary>
		/// Values column line #0, default value is <see cref="DefaultHexEditorOptions.ColumnGroupLine0"/>
		/// </summary>
		public HexColumnLineKind ColumnGroupLine0 { get; set; }

		/// <summary>
		/// Values column line #1, default value is <see cref="DefaultHexEditorOptions.ColumnGroupLine1"/>
		/// </summary>
		public HexColumnLineKind ColumnGroupLine1 { get; set; }

		/// <summary>
		/// Highlight active column, default value is <see cref="DefaultHexEditorOptions.HighlightActiveColumn"/>
		/// </summary>
		public bool HighlightActiveColumn { get; set; }

		/// <summary>
		/// Highlight current value, default value is <see cref="DefaultHexEditorOptions.HighlightCurrentValue"/>
		/// </summary>
		public bool HighlightCurrentValue { get; set; }

		/// <summary>
		/// Highlight current value delay in milliseconds, default value is <see cref="DefaultHexEditorOptions.HighlightCurrentValueDelayMilliSeconds"/>
		/// </summary>
		public int HighlightCurrentValueDelayMilliSeconds { get; set; }

		/// <summary>
		/// Encoding code page, default value is <see cref="DefaultHexEditorOptions.EncodingCodePage"/>
		/// </summary>
		public int EncodingCodePage { get; set; }

		/// <summary>
		/// Highlight structure under mouse cursor, default value is <see cref="DefaultHexEditorOptions.HighlightStructureUnderMouseCursor"/>
		/// </summary>
		public bool HighlightStructureUnderMouseCursor { get; set; }

		/// <summary>
		/// Highlight current line, default value is <see cref="DefaultHexEditorOptions.EnableHighlightCurrentLine"/>
		/// </summary>
		public bool EnableHighlightCurrentLine { get; set; }

		/// <summary>
		/// Enable mouse wheel zoom, default value is <see cref="DefaultHexEditorOptions.EnableMouseWheelZoom"/>
		/// </summary>
		public bool EnableMouseWheelZoom { get; set; }

		/// <summary>
		/// Zoom level, default value is <see cref="DefaultHexEditorOptions.ZoomLevel"/>
		/// </summary>
		public double ZoomLevel { get; set; }

		/// <summary>
		/// Enable horizontal scrollbar, default value is <see cref="DefaultHexEditorOptions.HorizontalScrollBar"/>
		/// </summary>
		public bool HorizontalScrollBar { get; set; }

		/// <summary>
		/// Enable vertical scrollbar, default value is <see cref="DefaultHexEditorOptions.VerticalScrollBar"/>
		/// </summary>
		public bool VerticalScrollBar { get; set; }

		/// <summary>
		/// Enable selection margin, default value is <see cref="DefaultHexEditorOptions.SelectionMargin"/>
		/// </summary>
		public bool SelectionMargin { get; set; }

		/// <summary>
		/// Enable zoom control, default value is <see cref="DefaultHexEditorOptions.ZoomControl"/>
		/// </summary>
		public bool ZoomControl { get; set; }

		/// <summary>
		/// Enable glyph margin, default value is <see cref="DefaultHexEditorOptions.GlyphMargin"/>
		/// </summary>
		public bool GlyphMargin { get; set; }

		/// <summary>
		/// Force clear type, default value is <see cref="DefaultHexEditorOptions.ForceClearTypeIfNeeded"/>
		/// </summary>
		public bool ForceClearTypeIfNeeded { get; set; }
	}
}
