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

using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Repl {
	/// <summary>
	/// Default REPL options
	/// </summary>
	static class DefaultReplOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const bool UseVirtualSpace = false;
		public const WordWrapStyles WordWrapStyle = WordWrapStylesConstants.DefaultValue;
		public const bool ShowLineNumbers = true;
		public const bool HighlightCurrentLine = true;
		public const bool CutOrCopyBlankLineIfNoSelection = true;
		public const bool DisplayUrlsAsHyperlinks = true;
		public const bool ForceClearTypeIfNeeded = true;
		public const bool HorizontalScrollBar = true;
		public const bool VerticalScrollBar = true;
		public const int TabSize = 4;
		public const int IndentSize = 4;
		public const bool ConvertTabsToSpaces = false;
		public const bool HighlightReferences = true;
		public const bool HighlightRelatedKeywords = true;
		public const bool HighlightMatchingBrace = true;
		public const bool LineSeparators = true;
		public const bool ShowBlockStructure = true;
		public const BlockStructureLineKind BlockStructureLineKind = Text.Editor.BlockStructureLineKind.Dashed_3_3;
		public const bool CompressEmptyOrWhitespaceLines = true;
		public const bool CompressNonLetterLines = true;
		public const bool RemoveExtraTextLineVerticalPixels = false;
		public const bool SelectionMargin = true;
		public const bool GlyphMargin = false;
		public const bool MouseWheelZoom = true;
		public const bool ZoomControl = true;
		public const double ZoomLevel = 100;
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
