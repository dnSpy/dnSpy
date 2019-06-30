/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer.Settings {
	[ExportContentTypeOptionDefinitionProvider(PredefinedTextViewGroupNames.DocumentViewer)]
	sealed class ContentTypeOptionDefinitionProvider : IContentTypeOptionDefinitionProvider {
		public IEnumerable<ContentTypeOptionDefinition> GetOptions() {
			var contentTypes = new string[] { ContentTypes.Any };
			foreach (var contentType in contentTypes) {
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewOptions.UseVirtualSpaceId, false);
				yield return new OptionDefinition<WordWrapStyles>(contentType, DefaultTextViewOptions.WordWrapStyleId, WordWrapStylesConstants.DefaultValue);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.LineNumberMarginId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultWpfViewOptions.EnableHighlightCurrentLineId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewOptions.DisplayUrlsAsHyperlinksId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsWpfViewOptions.ForceClearTypeIfNeededId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.HorizontalScrollBarId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.VerticalScrollBarId, true);
				yield return new OptionDefinition<int>(contentType, DefaultOptions.TabSizeOptionId, 4);
				yield return new OptionDefinition<int>(contentType, DefaultOptions.IndentSizeOptionId, 4);
				yield return new OptionDefinition<bool>(contentType, DefaultOptions.ConvertTabsToSpacesOptionId, false);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.ReferenceHighlightingId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.HighlightRelatedKeywordsId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.BraceMatchingId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.LineSeparatorsId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewOptions.ShowBlockStructureId, true);
				yield return new OptionDefinition<BlockStructureLineKind>(contentType, DefaultDsTextViewOptions.BlockStructureLineKindId, BlockStructureLineKind.Dashed_3_3);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.CompressNonLetterLinesId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId, false);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.SelectionMarginId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.GlyphMarginId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultWpfViewOptions.EnableMouseWheelZoomId, true);
				yield return new OptionDefinition<bool>(contentType, DefaultTextViewHostOptions.ZoomControlId, true);
				yield return new OptionDefinition<double>(contentType, DefaultWpfViewOptions.ZoomLevelId, 100);
			}
		}

		sealed class OptionDefinition<T> : ContentTypeOptionDefinition<T> {
			public OptionDefinition(string contentType, EditorOptionKey<T> option, T defaultValue)
				: base(option) {
				ContentType = contentType;
				DefaultValue = defaultValue;
			}
		}
	}
}
