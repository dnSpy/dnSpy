/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings.HexGroups;

namespace dnSpy.Hex.Settings {
	abstract class CommonEditorOptions {
		protected HexViewOptionsGroup Group { get; }
		public string Tag { get; }

		protected CommonEditorOptions(HexViewOptionsGroup group, string tag) {
			if (group == null)
				throw new ArgumentNullException(nameof(group));
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			Group = group;
			Tag = tag;
		}

		public HexOffsetFormat HexOffsetFormat {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.HexOffsetFormatId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.HexOffsetFormatId, value); }
		}

		public bool ValuesLowerCaseHex {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ValuesLowerCaseHexId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ValuesLowerCaseHexId, value); }
		}

		public bool OffsetLowerCaseHex {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.OffsetLowerCaseHexId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.OffsetLowerCaseHexId, value); }
		}

		public int GroupSizeInBytes {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.GroupSizeInBytesId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.GroupSizeInBytesId, value); }
		}

		public bool EnableColorization {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.EnableColorizationId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.EnableColorizationId, value); }
		}

		public bool RemoveExtraTextLineVerticalPixels {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId, value); }
		}

		public bool ShowColumnLines {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ShowColumnLinesId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ShowColumnLinesId, value); }
		}

		public HexColumnLineKind ColumnLine0 {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ColumnLine0Id); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ColumnLine0Id, value); }
		}

		public HexColumnLineKind ColumnLine1 {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ColumnLine1Id); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ColumnLine1Id, value); }
		}

		public HexColumnLineKind ColumnGroupLine0 {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ColumnGroupLine0Id); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ColumnGroupLine0Id, value); }
		}

		public HexColumnLineKind ColumnGroupLine1 {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.ColumnGroupLine1Id); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.ColumnGroupLine1Id, value); }
		}

		public bool HighlightActiveColumn {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.HighlightActiveColumnId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.HighlightActiveColumnId, value); }
		}

		public bool HighlightCurrentValue {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.HighlightCurrentValueId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.HighlightCurrentValueId, value); }
		}

		public Encoding Encoding {
			get { return Group.GetOptionValue(Tag, DefaultHexViewOptions.EncodingId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewOptions.EncodingId, value); }
		}

		public bool EnableHighlightCurrentLine {
			get { return Group.GetOptionValue(Tag, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId); }
			set { Group.SetOptionValue(Tag, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId, value); }
		}

		public bool EnableMouseWheelZoom {
			get { return Group.GetOptionValue(Tag, DefaultWpfHexViewOptions.EnableMouseWheelZoomId); }
			set { Group.SetOptionValue(Tag, DefaultWpfHexViewOptions.EnableMouseWheelZoomId, value); }
		}

		public double ZoomLevel {
			get { return Group.GetOptionValue(Tag, DefaultWpfHexViewOptions.ZoomLevelId); }
			set { Group.SetOptionValue(Tag, DefaultWpfHexViewOptions.ZoomLevelId, value); }
		}

		public bool HorizontalScrollBar {
			get { return Group.GetOptionValue(Tag, DefaultHexViewHostOptions.HorizontalScrollBarId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewHostOptions.HorizontalScrollBarId, value); }
		}

		public bool VerticalScrollBar {
			get { return Group.GetOptionValue(Tag, DefaultHexViewHostOptions.VerticalScrollBarId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewHostOptions.VerticalScrollBarId, value); }
		}

		public bool SelectionMargin {
			get { return Group.GetOptionValue(Tag, DefaultHexViewHostOptions.SelectionMarginId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewHostOptions.SelectionMarginId, value); }
		}

		public bool ZoomControl {
			get { return Group.GetOptionValue(Tag, DefaultHexViewHostOptions.ZoomControlId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewHostOptions.ZoomControlId, value); }
		}

		public bool GlyphMargin {
			get { return Group.GetOptionValue(Tag, DefaultHexViewHostOptions.GlyphMarginId); }
			set { Group.SetOptionValue(Tag, DefaultHexViewHostOptions.GlyphMarginId, value); }
		}

		public bool ForceClearTypeIfNeeded {
			get { return Group.GetOptionValue(Tag, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId); }
			set { Group.SetOptionValue(Tag, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId, value); }
		}
	}
}
