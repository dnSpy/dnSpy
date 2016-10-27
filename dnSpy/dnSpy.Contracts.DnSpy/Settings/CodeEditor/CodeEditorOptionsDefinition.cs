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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.CodeEditor {
	/// <summary>
	/// Defines code editor options that will be shown in the UI. Use <see cref="ExportCodeEditorOptionsDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public sealed class CodeEditorOptionsDefinition {
	}

	/// <summary>Metadata</summary>
	public interface ICodeEditorOptionsDefinitionMetadata {
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ContentType"/></summary>
		string ContentType { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.LanguageName"/></summary>
		string LanguageName { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.UseVirtualSpace"/></summary>
		bool UseVirtualSpace { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.WordWrapStyle"/></summary>
		WordWrapStyles WordWrapStyle { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ShowLineNumbers"/></summary>
		bool ShowLineNumbers { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.HighlightCurrentLine"/></summary>
		bool HighlightCurrentLine { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.CutOrCopyBlankLineIfNoSelection"/></summary>
		bool CutOrCopyBlankLineIfNoSelection { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.DisplayUrlsAsHyperlinks"/></summary>
		bool DisplayUrlsAsHyperlinks { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ForceClearTypeIfNeeded"/></summary>
		bool ForceClearTypeIfNeeded { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.HorizontalScrollBar"/></summary>
		bool HorizontalScrollBar { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.VerticalScrollBar"/></summary>
		bool VerticalScrollBar { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.TabSize"/></summary>
		int TabSize { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.IndentSize"/></summary>
		int IndentSize { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ConvertTabsToSpaces"/></summary>
		bool ConvertTabsToSpaces { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.HighlightReferences"/></summary>
		bool HighlightReferences { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.HighlightRelatedKeywords"/></summary>
		bool HighlightRelatedKeywords { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.HighlightMatchingBrace"/></summary>
		bool HighlightMatchingBrace { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.LineSeparators"/></summary>
		bool LineSeparators { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ShowBlockStructure"/></summary>
		bool ShowBlockStructure { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.CompressEmptyOrWhitespaceLines"/></summary>
		bool CompressEmptyOrWhitespaceLines { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.CompressNonLetterLines"/></summary>
		bool CompressNonLetterLines { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.RemoveExtraTextLineVerticalPixels"/></summary>
		bool RemoveExtraTextLineVerticalPixels { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.SelectionMargin"/></summary>
		bool SelectionMargin { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.GlyphMargin"/></summary>
		bool GlyphMargin { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.MouseWheelZoom"/></summary>
		bool MouseWheelZoom { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ZoomControl"/></summary>
		bool ZoomControl { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ZoomLevel"/></summary>
		double ZoomLevel { get; }
	}

	/// <summary>
	/// Exports a <see cref="CodeEditorOptionsDefinition"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ExportCodeEditorOptionsDefinitionAttribute : ExportAttribute, ICodeEditorOptionsDefinitionMetadata {
		/// <summary>Constructor</summary>
		/// <param name="languageName">Language name shown in the UI</param>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="guid">Guid of settings, eg. <see cref="AppSettingsConstants.GUID_CODE_EDITOR_CSHARP_ROSLYN"/></param>
		public ExportCodeEditorOptionsDefinitionAttribute(string languageName, string contentType, string guid)
			: base(typeof(CodeEditorOptionsDefinition)) {
			if (languageName == null)
				throw new ArgumentNullException(nameof(languageName));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (guid == null)
				throw new ArgumentNullException(nameof(guid));
			ContentType = contentType;
			Guid = guid;
			LanguageName = languageName;
			UseVirtualSpace = DefaultCodeEditorOptions.UseVirtualSpace;
			WordWrapStyle = DefaultCodeEditorOptions.WordWrapStyle;
			ShowLineNumbers = DefaultCodeEditorOptions.ShowLineNumbers;
			HighlightCurrentLine = DefaultCodeEditorOptions.HighlightCurrentLine;
			CutOrCopyBlankLineIfNoSelection = DefaultCodeEditorOptions.CutOrCopyBlankLineIfNoSelection;
			DisplayUrlsAsHyperlinks = DefaultCodeEditorOptions.DisplayUrlsAsHyperlinks;
			ForceClearTypeIfNeeded = DefaultCodeEditorOptions.ForceClearTypeIfNeeded;
			HorizontalScrollBar = DefaultCodeEditorOptions.HorizontalScrollBar;
			VerticalScrollBar = DefaultCodeEditorOptions.VerticalScrollBar;
			TabSize = DefaultCodeEditorOptions.TabSize;
			IndentSize = DefaultCodeEditorOptions.IndentSize;
			ConvertTabsToSpaces = DefaultCodeEditorOptions.ConvertTabsToSpaces;
			HighlightReferences = DefaultCodeEditorOptions.HighlightReferences;
			HighlightRelatedKeywords = DefaultCodeEditorOptions.HighlightRelatedKeywords;
			HighlightMatchingBrace = DefaultCodeEditorOptions.HighlightMatchingBrace;
			LineSeparators = DefaultCodeEditorOptions.LineSeparators;
			ShowBlockStructure = DefaultCodeEditorOptions.ShowBlockStructure;
			CompressEmptyOrWhitespaceLines = DefaultCodeEditorOptions.CompressEmptyOrWhitespaceLines;
			CompressNonLetterLines = DefaultCodeEditorOptions.CompressNonLetterLines;
			RemoveExtraTextLineVerticalPixels = DefaultCodeEditorOptions.RemoveExtraTextLineVerticalPixels;
			SelectionMargin = DefaultCodeEditorOptions.SelectionMargin;
			GlyphMargin = DefaultCodeEditorOptions.GlyphMargin;
			MouseWheelZoom = DefaultCodeEditorOptions.MouseWheelZoom;
			ZoomControl = DefaultCodeEditorOptions.ZoomControl;
			ZoomLevel = DefaultCodeEditorOptions.ZoomLevel;
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
		/// Use virtual space, default value is <see cref="DefaultCodeEditorOptions.UseVirtualSpace"/>
		/// </summary>
		public bool UseVirtualSpace { get; set; }

		/// <summary>
		/// Word wrap style, default value is <see cref="DefaultCodeEditorOptions.WordWrapStyle"/>
		/// </summary>
		public WordWrapStyles WordWrapStyle { get; set; }

		/// <summary>
		/// Show line numbers, default value is <see cref="DefaultCodeEditorOptions.ShowLineNumbers"/>
		/// </summary>
		public bool ShowLineNumbers { get; set; }

		/// <summary>
		/// Highlight current line, default value is <see cref="DefaultCodeEditorOptions.HighlightCurrentLine"/>
		/// </summary>
		public bool HighlightCurrentLine { get; set; }

		/// <summary>
		/// Cut or copy blank link if no selection, default value is <see cref="DefaultCodeEditorOptions.CutOrCopyBlankLineIfNoSelection"/>
		/// </summary>
		public bool CutOrCopyBlankLineIfNoSelection { get; set; }

		/// <summary>
		/// Display URLs as hyperlinks, default value is <see cref="DefaultCodeEditorOptions.DisplayUrlsAsHyperlinks"/>
		/// </summary>
		public bool DisplayUrlsAsHyperlinks { get; set; }

		/// <summary>
		/// Force ClearType, default value is <see cref="DefaultCodeEditorOptions.ForceClearTypeIfNeeded"/>
		/// </summary>
		public bool ForceClearTypeIfNeeded { get; set; }

		/// <summary>
		/// Show horizontal scroll bar, default value is <see cref="DefaultCodeEditorOptions.HorizontalScrollBar"/>
		/// </summary>
		public bool HorizontalScrollBar { get; set; }

		/// <summary>
		/// Show vertical scroll bar, default value is <see cref="DefaultCodeEditorOptions.VerticalScrollBar"/>
		/// </summary>
		public bool VerticalScrollBar { get; set; }

		/// <summary>
		/// Tab size, default value is <see cref="DefaultCodeEditorOptions.TabSize"/>
		/// </summary>
		public int TabSize { get; set; }

		/// <summary>
		/// Indent size, default value is <see cref="DefaultCodeEditorOptions.IndentSize"/>
		/// </summary>
		public int IndentSize { get; set; }

		/// <summary>
		/// true to convert tabs to spaces, default value is <see cref="DefaultCodeEditorOptions.ConvertTabsToSpaces"/>
		/// </summary>
		public bool ConvertTabsToSpaces { get; set; }

		/// <summary>
		/// Highlight references, default value is <see cref="DefaultCodeEditorOptions.HighlightReferences"/>
		/// </summary>
		public bool HighlightReferences { get; set; }

		/// <summary>
		/// Highlight related keywords, default value is <see cref="DefaultCodeEditorOptions.HighlightRelatedKeywords"/>
		/// </summary>
		public bool HighlightRelatedKeywords { get; set; }

		/// <summary>
		/// Highlight matching brace, default value is <see cref="DefaultCodeEditorOptions.HighlightMatchingBrace"/>
		/// </summary>
		public bool HighlightMatchingBrace { get; set; }

		/// <summary>
		/// Line separators, default value is <see cref="DefaultCodeEditorOptions.LineSeparators"/>
		/// </summary>
		public bool LineSeparators { get; set; }

		/// <summary>
		/// Show indent guides, default value is <see cref="DefaultCodeEditorOptions.ShowBlockStructure"/>
		/// </summary>
		public bool ShowBlockStructure { get; set; }

		/// <summary>
		/// Compress empty/whitespace lines, default value is <see cref="DefaultCodeEditorOptions.CompressEmptyOrWhitespaceLines"/>
		/// </summary>
		public bool CompressEmptyOrWhitespaceLines { get; set; }

		/// <summary>
		/// Compress non-letter lines, default value is <see cref="DefaultCodeEditorOptions.CompressNonLetterLines"/>
		/// </summary>
		public bool CompressNonLetterLines { get; set; }

		/// <summary>
		/// Don't use extra line spacing, default value is <see cref="DefaultCodeEditorOptions.RemoveExtraTextLineVerticalPixels"/>
		/// </summary>
		public bool RemoveExtraTextLineVerticalPixels { get; set; }

		/// <summary>
		/// Show selection margin, default value is <see cref="DefaultCodeEditorOptions.SelectionMargin"/>
		/// </summary>
		public bool SelectionMargin { get; set; }

		/// <summary>
		/// Show glyph margin, default value is <see cref="DefaultCodeEditorOptions.GlyphMargin"/>
		/// </summary>
		public bool GlyphMargin { get; set; }

		/// <summary>
		/// Enable mouse wheel zoom, default value is <see cref="DefaultCodeEditorOptions.MouseWheelZoom"/>
		/// </summary>
		public bool MouseWheelZoom { get; set; }

		/// <summary>
		/// Show zoom control, default value is <see cref="DefaultCodeEditorOptions.ZoomControl"/>
		/// </summary>
		public bool ZoomControl { get; set; }

		/// <summary>
		/// Zoom level, default value is <see cref="DefaultCodeEditorOptions.ZoomLevel"/>
		/// </summary>
		public double ZoomLevel { get; set; }
	}
}
