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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Settings.Repl;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Repl {
	[ExportContentTypeOptionDefinitionProvider(PredefinedTextViewGroupNames.REPL)]
	sealed class ContentTypeOptionDefinitionProvider : IContentTypeOptionDefinitionProvider {
		readonly Lazy<ReplOptionsDefinition, IReplOptionsDefinitionMetadata>[] replOptionsDefinitions;

		[ImportingConstructor]
		ContentTypeOptionDefinitionProvider([ImportMany] IEnumerable<Lazy<ReplOptionsDefinition, IReplOptionsDefinitionMetadata>> replOptionsDefinitions) => this.replOptionsDefinitions = replOptionsDefinitions.ToArray();

		IEnumerable<IReplOptionsDefinitionMetadata> GetOptionsDefinitions() {
			foreach (var lz in replOptionsDefinitions)
				yield return lz.Metadata;

			const string DEFAULT_NAME = "";
			const string GUID_REPL_DEFAULT = "6ECB7A27-C123-4592-A0F5-1AAA7153041A";
			yield return new ExportReplOptionsDefinitionAttribute(DEFAULT_NAME, ContentTypes.Any, GUID_REPL_DEFAULT);
		}

		public IEnumerable<ContentTypeOptionDefinition> GetOptions() {
			foreach (var md in GetOptionsDefinitions()) {
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewOptions.UseVirtualSpaceId, md.UseVirtualSpace);
				yield return new OptionDefinition<WordWrapStyles>(md.ContentType, DefaultTextViewOptions.WordWrapStyleId, md.WordWrapStyle);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.LineNumberMarginId, md.ShowLineNumbers);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultWpfViewOptions.EnableHighlightCurrentLineId, md.HighlightCurrentLine);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId, md.CutOrCopyBlankLineIfNoSelection);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewOptions.DisplayUrlsAsHyperlinksId, md.DisplayUrlsAsHyperlinks);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsWpfViewOptions.ForceClearTypeIfNeededId, md.ForceClearTypeIfNeeded);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.HorizontalScrollBarId, md.HorizontalScrollBar);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.VerticalScrollBarId, md.VerticalScrollBar);
				yield return new OptionDefinition<int>(md.ContentType, DefaultOptions.TabSizeOptionId, md.TabSize);
				yield return new OptionDefinition<int>(md.ContentType, DefaultOptions.IndentSizeOptionId, md.IndentSize);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultOptions.ConvertTabsToSpacesOptionId, md.ConvertTabsToSpaces);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.ReferenceHighlightingId, md.HighlightReferences);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.HighlightRelatedKeywordsId, md.HighlightRelatedKeywords);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.BraceMatchingId, md.HighlightMatchingBrace);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.LineSeparatorsId, md.LineSeparators);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewOptions.ShowBlockStructureId, md.ShowBlockStructure);
				yield return new OptionDefinition<BlockStructureLineKind>(md.ContentType, DefaultDsTextViewOptions.BlockStructureLineKindId, md.BlockStructureLineKind);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId, md.CompressEmptyOrWhitespaceLines);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.CompressNonLetterLinesId, md.CompressNonLetterLines);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId, md.RemoveExtraTextLineVerticalPixels);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.SelectionMarginId, md.SelectionMargin);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.GlyphMarginId, md.GlyphMargin);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultWpfViewOptions.EnableMouseWheelZoomId, md.MouseWheelZoom);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.ZoomControlId, md.ZoomControl);
				yield return new OptionDefinition<double>(md.ContentType, DefaultWpfViewOptions.ZoomLevelId, md.ZoomLevel);
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
