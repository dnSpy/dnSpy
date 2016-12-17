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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings.HexEditor;
using dnSpy.Contracts.Settings.HexGroups;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.HexEditor {
	[ExportTagOptionDefinitionProvider(PredefinedHexViewGroupNames.HexEditor)]
	sealed class TagOptionDefinitionProviderImpl : TagOptionDefinitionProvider {
		readonly Lazy<HexEditorOptionsDefinition, IHexEditorOptionsDefinitionMetadata>[] hexEditorOptionsDefinitions;

		[ImportingConstructor]
		TagOptionDefinitionProviderImpl([ImportMany] IEnumerable<Lazy<HexEditorOptionsDefinition, IHexEditorOptionsDefinitionMetadata>> hexEditorOptionsDefinitions) {
			this.hexEditorOptionsDefinitions = hexEditorOptionsDefinitions.ToArray();
		}

		IEnumerable<IHexEditorOptionsDefinitionMetadata> GetOptionsDefinitions() {
			foreach (var lz in hexEditorOptionsDefinitions)
				yield return lz.Metadata;

			const string GUID_CODE_EDITOR_DEFAULT = "7ACED6FB-E26B-42F7-8B7B-90C3A83776ED";
			yield return new ExportHexEditorOptionsDefinitionAttribute(string.Empty, string.Empty, GUID_CODE_EDITOR_DEFAULT, GetType());
		}

		public override IEnumerable<TagOptionDefinition> GetOptions() {
			foreach (var md in GetOptionsDefinitions()) {
				yield return new OptionDefinition<HexOffsetFormat>(md.SubGroup, DefaultHexViewOptions.HexOffsetFormatId, md.HexOffsetFormat);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.ValuesLowerCaseHexId, md.ValuesLowerCaseHex);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.OffsetLowerCaseHexId, md.OffsetLowerCaseHex);
				yield return new OptionDefinition<int>(md.SubGroup, DefaultHexViewOptions.GroupSizeInBytesId, md.GroupSizeInBytes);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.EnableColorizationId, md.EnableColorization);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId, md.RemoveExtraTextLineVerticalPixels);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.ShowColumnLinesId, md.ShowColumnLines);
				yield return new OptionDefinition<HexColumnLineKind>(md.SubGroup, DefaultHexViewOptions.ColumnLine0Id, md.ColumnLine0);
				yield return new OptionDefinition<HexColumnLineKind>(md.SubGroup, DefaultHexViewOptions.ColumnLine1Id, md.ColumnLine1);
				yield return new OptionDefinition<HexColumnLineKind>(md.SubGroup, DefaultHexViewOptions.ColumnGroupLine0Id, md.ColumnGroupLine0);
				yield return new OptionDefinition<HexColumnLineKind>(md.SubGroup, DefaultHexViewOptions.ColumnGroupLine1Id, md.ColumnGroupLine1);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.HighlightActiveColumnId, md.HighlightActiveColumn);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.HighlightCurrentValueId, md.HighlightCurrentValue);
				yield return new OptionDefinition<int>(md.SubGroup, DefaultHexViewOptions.EncodingCodePageId, md.EncodingCodePage);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewOptions.HighlightStructureUnderMouseCursorId, md.HighlightStructureUnderMouseCursor);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId, md.EnableHighlightCurrentLine);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultWpfHexViewOptions.EnableMouseWheelZoomId, md.EnableMouseWheelZoom);
				yield return new OptionDefinition<double>(md.SubGroup, DefaultWpfHexViewOptions.ZoomLevelId, md.ZoomLevel);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewHostOptions.HorizontalScrollBarId, md.HorizontalScrollBar);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewHostOptions.VerticalScrollBarId, md.VerticalScrollBar);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewHostOptions.SelectionMarginId, md.SelectionMargin);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewHostOptions.ZoomControlId, md.ZoomControl);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultHexViewHostOptions.GlyphMarginId, md.GlyphMargin);
				yield return new OptionDefinition<bool>(md.SubGroup, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId, md.ForceClearTypeIfNeeded);
			}
		}

		public override string GetSubGroup(WpfHexView hexView) {
			foreach (var lz in hexEditorOptionsDefinitions) {
				if (hexView.Roles.Contains(lz.Metadata.SubGroup))
					return lz.Metadata.SubGroup;
			}
			return null;
		}

		sealed class OptionDefinition<T> : TagOptionDefinition<T> {
			public OptionDefinition(string subGroup, VSTE.EditorOptionKey<T> option, T defaultValue)
				: base(option) {
				SubGroup = subGroup;
				DefaultValue = defaultValue;
			}
		}
	}
}
