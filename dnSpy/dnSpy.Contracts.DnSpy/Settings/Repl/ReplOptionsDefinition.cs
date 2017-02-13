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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Repl {
	/// <summary>
	/// Defines REPL options that will be shown in the UI. Use <see cref="ExportReplOptionsDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	sealed class ReplOptionsDefinition {
	}

	/// <summary>Metadata</summary>
	public interface IReplOptionsDefinitionMetadata {
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ContentType"/></summary>
		string ContentType { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.LanguageName"/></summary>
		string LanguageName { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.UseVirtualSpace"/></summary>
		bool UseVirtualSpace { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.WordWrapStyle"/></summary>
		WordWrapStyles WordWrapStyle { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ShowLineNumbers"/></summary>
		bool ShowLineNumbers { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.HighlightCurrentLine"/></summary>
		bool HighlightCurrentLine { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.CutOrCopyBlankLineIfNoSelection"/></summary>
		bool CutOrCopyBlankLineIfNoSelection { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.DisplayUrlsAsHyperlinks"/></summary>
		bool DisplayUrlsAsHyperlinks { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ForceClearTypeIfNeeded"/></summary>
		bool ForceClearTypeIfNeeded { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.HorizontalScrollBar"/></summary>
		bool HorizontalScrollBar { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.VerticalScrollBar"/></summary>
		bool VerticalScrollBar { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.TabSize"/></summary>
		int TabSize { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.IndentSize"/></summary>
		int IndentSize { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ConvertTabsToSpaces"/></summary>
		bool ConvertTabsToSpaces { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.HighlightReferences"/></summary>
		bool HighlightReferences { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.HighlightRelatedKeywords"/></summary>
		bool HighlightRelatedKeywords { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.HighlightMatchingBrace"/></summary>
		bool HighlightMatchingBrace { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.LineSeparators"/></summary>
		bool LineSeparators { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ShowBlockStructure"/></summary>
		bool ShowBlockStructure { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.BlockStructureLineKind"/></summary>
		BlockStructureLineKind BlockStructureLineKind { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.CompressEmptyOrWhitespaceLines"/></summary>
		bool CompressEmptyOrWhitespaceLines { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.CompressNonLetterLines"/></summary>
		bool CompressNonLetterLines { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.RemoveExtraTextLineVerticalPixels"/></summary>
		bool RemoveExtraTextLineVerticalPixels { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.SelectionMargin"/></summary>
		bool SelectionMargin { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.GlyphMargin"/></summary>
		bool GlyphMargin { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.MouseWheelZoom"/></summary>
		bool MouseWheelZoom { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ZoomControl"/></summary>
		bool ZoomControl { get; }
		/// <summary>See <see cref="ExportReplOptionsDefinitionAttribute.ZoomLevel"/></summary>
		double ZoomLevel { get; }
	}

	/// <summary>
	/// Exports a <see cref="ReplOptionsDefinition"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	sealed class ExportReplOptionsDefinitionAttribute : ExportAttribute, IReplOptionsDefinitionMetadata {
		/// <summary>Constructor</summary>
		/// <param name="languageName">Language name shown in the UI</param>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.ReplCSharpRoslyn"/></param>
		/// <param name="guid">Guid of settings, eg. <see cref="AppSettingsConstants.GUID_REPL_CSHARP_ROSLYN"/></param>
		public ExportReplOptionsDefinitionAttribute(string languageName, string contentType, string guid)
			: base(typeof(ReplOptionsDefinition)) {
			ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			Guid = guid ?? throw new ArgumentNullException(nameof(guid));
			LanguageName = languageName ?? throw new ArgumentNullException(nameof(languageName));
			UseVirtualSpace = DefaultReplOptions.UseVirtualSpace;
			WordWrapStyle = DefaultReplOptions.WordWrapStyle;
			ShowLineNumbers = DefaultReplOptions.ShowLineNumbers;
			HighlightCurrentLine = DefaultReplOptions.HighlightCurrentLine;
			CutOrCopyBlankLineIfNoSelection = DefaultReplOptions.CutOrCopyBlankLineIfNoSelection;
			DisplayUrlsAsHyperlinks = DefaultReplOptions.DisplayUrlsAsHyperlinks;
			ForceClearTypeIfNeeded = DefaultReplOptions.ForceClearTypeIfNeeded;
			HorizontalScrollBar = DefaultReplOptions.HorizontalScrollBar;
			VerticalScrollBar = DefaultReplOptions.VerticalScrollBar;
			TabSize = DefaultReplOptions.TabSize;
			IndentSize = DefaultReplOptions.IndentSize;
			ConvertTabsToSpaces = DefaultReplOptions.ConvertTabsToSpaces;
			HighlightReferences = DefaultReplOptions.HighlightReferences;
			HighlightRelatedKeywords = DefaultReplOptions.HighlightRelatedKeywords;
			HighlightMatchingBrace = DefaultReplOptions.HighlightMatchingBrace;
			LineSeparators = DefaultReplOptions.LineSeparators;
			ShowBlockStructure = DefaultReplOptions.ShowBlockStructure;
			BlockStructureLineKind = DefaultReplOptions.BlockStructureLineKind;
			CompressEmptyOrWhitespaceLines = DefaultReplOptions.CompressEmptyOrWhitespaceLines;
			CompressNonLetterLines = DefaultReplOptions.CompressNonLetterLines;
			RemoveExtraTextLineVerticalPixels = DefaultReplOptions.RemoveExtraTextLineVerticalPixels;
			SelectionMargin = DefaultReplOptions.SelectionMargin;
			GlyphMargin = DefaultReplOptions.GlyphMargin;
			MouseWheelZoom = DefaultReplOptions.MouseWheelZoom;
			ZoomControl = DefaultReplOptions.ZoomControl;
			ZoomLevel = DefaultReplOptions.ZoomLevel;
		}

		/// <summary>
		/// Content type
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Guid of settings
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Language name
		/// </summary>
		public string LanguageName { get; }

		/// <summary>
		/// Use virtual space, default value is <see cref="DefaultReplOptions.UseVirtualSpace"/>
		/// </summary>
		public bool UseVirtualSpace { get; set; }

		/// <summary>
		/// Word wrap style, default value is <see cref="DefaultReplOptions.WordWrapStyle"/>
		/// </summary>
		public WordWrapStyles WordWrapStyle { get; set; }

		/// <summary>
		/// Show line numbers, default value is <see cref="DefaultReplOptions.ShowLineNumbers"/>
		/// </summary>
		public bool ShowLineNumbers { get; set; }

		/// <summary>
		/// Highlight current line, default value is <see cref="DefaultReplOptions.HighlightCurrentLine"/>
		/// </summary>
		public bool HighlightCurrentLine { get; set; }

		/// <summary>
		/// Cut or copy blank link if no selection, default value is <see cref="DefaultReplOptions.CutOrCopyBlankLineIfNoSelection"/>
		/// </summary>
		public bool CutOrCopyBlankLineIfNoSelection { get; set; }

		/// <summary>
		/// Display URLs as hyperlinks, default value is <see cref="DefaultReplOptions.DisplayUrlsAsHyperlinks"/>
		/// </summary>
		public bool DisplayUrlsAsHyperlinks { get; set; }

		/// <summary>
		/// Force ClearType, default value is <see cref="DefaultReplOptions.ForceClearTypeIfNeeded"/>
		/// </summary>
		public bool ForceClearTypeIfNeeded { get; set; }

		/// <summary>
		/// Show horizontal scroll bar, default value is <see cref="DefaultReplOptions.HorizontalScrollBar"/>
		/// </summary>
		public bool HorizontalScrollBar { get; set; }

		/// <summary>
		/// Show vertical scroll bar, default value is <see cref="DefaultReplOptions.VerticalScrollBar"/>
		/// </summary>
		public bool VerticalScrollBar { get; set; }

		/// <summary>
		/// Tab size, default value is <see cref="DefaultReplOptions.TabSize"/>
		/// </summary>
		public int TabSize { get; set; }

		/// <summary>
		/// Indent size, default value is <see cref="DefaultReplOptions.IndentSize"/>
		/// </summary>
		public int IndentSize { get; set; }

		/// <summary>
		/// true to convert tabs to spaces, default value is <see cref="DefaultReplOptions.ConvertTabsToSpaces"/>
		/// </summary>
		public bool ConvertTabsToSpaces { get; set; }

		/// <summary>
		/// Highlight references, default value is <see cref="DefaultReplOptions.HighlightReferences"/>
		/// </summary>
		public bool HighlightReferences { get; set; }

		/// <summary>
		/// Highlight related keywords, default value is <see cref="DefaultReplOptions.HighlightRelatedKeywords"/>
		/// </summary>
		public bool HighlightRelatedKeywords { get; set; }

		/// <summary>
		/// Highlight matching brace, default value is <see cref="DefaultReplOptions.HighlightMatchingBrace"/>
		/// </summary>
		public bool HighlightMatchingBrace { get; set; }

		/// <summary>
		/// Line separators, default value is <see cref="DefaultReplOptions.LineSeparators"/>
		/// </summary>
		public bool LineSeparators { get; set; }

		/// <summary>
		/// Show indent guides, default value is <see cref="DefaultReplOptions.ShowBlockStructure"/>
		/// </summary>
		public bool ShowBlockStructure { get; set; }

		/// <summary>
		/// Block structure line kind, default value is <see cref="DefaultReplOptions.BlockStructureLineKind"/>
		/// </summary>
		public BlockStructureLineKind BlockStructureLineKind { get; set; }

		/// <summary>
		/// Compress empty/whitespace lines, default value is <see cref="DefaultReplOptions.CompressEmptyOrWhitespaceLines"/>
		/// </summary>
		public bool CompressEmptyOrWhitespaceLines { get; set; }

		/// <summary>
		/// Compress non-letter lines, default value is <see cref="DefaultReplOptions.CompressNonLetterLines"/>
		/// </summary>
		public bool CompressNonLetterLines { get; set; }

		/// <summary>
		/// Don't use extra line spacing, default value is <see cref="DefaultReplOptions.RemoveExtraTextLineVerticalPixels"/>
		/// </summary>
		public bool RemoveExtraTextLineVerticalPixels { get; set; }

		/// <summary>
		/// Show selection margin, default value is <see cref="DefaultReplOptions.SelectionMargin"/>
		/// </summary>
		public bool SelectionMargin { get; set; }

		/// <summary>
		/// Show glyph margin, default value is <see cref="DefaultReplOptions.GlyphMargin"/>
		/// </summary>
		public bool GlyphMargin { get; set; }

		/// <summary>
		/// Enable mouse wheel zoom, default value is <see cref="DefaultReplOptions.MouseWheelZoom"/>
		/// </summary>
		public bool MouseWheelZoom { get; set; }

		/// <summary>
		/// Show zoom control, default value is <see cref="DefaultReplOptions.ZoomControl"/>
		/// </summary>
		public bool ZoomControl { get; set; }

		/// <summary>
		/// Zoom level, default value is <see cref="DefaultReplOptions.ZoomLevel"/>
		/// </summary>
		public double ZoomLevel { get; set; }
	}
}
