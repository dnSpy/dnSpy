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
using System.Text;
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
		}

		public override IEnumerable<TagOptionDefinition> GetOptions() {
			foreach (var md in GetOptionsDefinitions()) {
				yield return new OptionDefinition<HexOffsetFormat>(md.Tag, DefaultHexViewOptions.HexOffsetFormatId, md.HexOffsetFormat);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.ValuesLowerCaseHexId, md.ValuesLowerCaseHex);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.OffsetLowerCaseHexId, md.OffsetLowerCaseHex);
				yield return new OptionDefinition<int>(md.Tag, DefaultHexViewOptions.GroupSizeInBytesId, md.GroupSizeInBytes);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.EnableColorizationId, md.EnableColorization);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId, md.RemoveExtraTextLineVerticalPixels);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.ShowColumnLinesId, md.ShowColumnLines);
				yield return new OptionDefinition<HexColumnLineKind>(md.Tag, DefaultHexViewOptions.ColumnLine0Id, md.ColumnLine0);
				yield return new OptionDefinition<HexColumnLineKind>(md.Tag, DefaultHexViewOptions.ColumnLine1Id, md.ColumnLine1);
				yield return new OptionDefinition<HexColumnLineKind>(md.Tag, DefaultHexViewOptions.ColumnGroupLine0Id, md.ColumnGroupLine0);
				yield return new OptionDefinition<HexColumnLineKind>(md.Tag, DefaultHexViewOptions.ColumnGroupLine1Id, md.ColumnGroupLine1);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.HighlightActiveColumnId, md.HighlightActiveColumn);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewOptions.HighlightCurrentValueId, md.HighlightCurrentValue);
				yield return new OptionDefinition<Encoding>(md.Tag, DefaultHexViewOptions.EncodingId, Encoding.GetEncoding(md.Encoding));
				yield return new OptionDefinition<bool>(md.Tag, DefaultWpfHexViewOptions.EnableHighlightCurrentLineId, md.EnableHighlightCurrentLine);
				yield return new OptionDefinition<bool>(md.Tag, DefaultWpfHexViewOptions.EnableMouseWheelZoomId, md.EnableMouseWheelZoom);
				yield return new OptionDefinition<double>(md.Tag, DefaultWpfHexViewOptions.ZoomLevelId, md.ZoomLevel);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewHostOptions.HorizontalScrollBarId, md.HorizontalScrollBar);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewHostOptions.VerticalScrollBarId, md.VerticalScrollBar);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewHostOptions.SelectionMarginId, md.SelectionMargin);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewHostOptions.ZoomControlId, md.ZoomControl);
				yield return new OptionDefinition<bool>(md.Tag, DefaultHexViewHostOptions.GlyphMarginId, md.GlyphMargin);
				yield return new OptionDefinition<bool>(md.Tag, DefaultWpfHexViewOptions.ForceClearTypeIfNeededId, md.ForceClearTypeIfNeeded);
			}
		}

		public override string GetTag(WpfHexView hexView) {
			foreach (var lz in hexEditorOptionsDefinitions) {
				if (hexView.Buffer.Tags.Contains(lz.Metadata.Tag))
					return lz.Metadata.Tag;
			}
			return null;
		}

		sealed class OptionDefinition<T> : TagOptionDefinition<T> {
			public OptionDefinition(string tag, VSTE.EditorOptionKey<T> option, T defaultValue)
				: base(option) {
				Tag = tag;
				DefaultValue = defaultValue;
			}
		}
	}
}
