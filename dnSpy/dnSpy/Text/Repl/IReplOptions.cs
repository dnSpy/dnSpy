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
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Repl {
	interface IReplOptions {
		IContentType ContentType { get; }
		Guid Guid { get; }
		string LanguageName { get; }
		bool UseVirtualSpace { get; set; }
		WordWrapStyles WordWrapStyle { get; set; }
		bool LineNumberMargin { get; set; }
		bool EnableHighlightCurrentLine { get; set; }
		bool CutOrCopyBlankLineIfNoSelection { get; set; }
		bool DisplayUrlsAsHyperlinks { get; set; }
		bool ForceClearTypeIfNeeded { get; set; }
		bool HorizontalScrollBar { get; set; }
		bool VerticalScrollBar { get; set; }
		int TabSize { get; set; }
		int IndentSize { get; set; }
		bool ConvertTabsToSpaces { get; set; }
		bool ReferenceHighlighting { get; set; }
		bool HighlightRelatedKeywords { get; set; }
		bool BraceMatching { get; set; }
		bool LineSeparators { get; set; }
		bool ShowBlockStructure { get; set; }
		BlockStructureLineKind BlockStructureLineKind { get; set; }
		bool CompressEmptyOrWhitespaceLines { get; set; }
		bool CompressNonLetterLines { get; set; }
		bool RemoveExtraTextLineVerticalPixels { get; set; }
		bool SelectionMargin { get; set; }
		bool GlyphMargin { get; set; }
		bool EnableMouseWheelZoom { get; set; }
		bool ZoomControl { get; set; }
		double ZoomLevel { get; set; }
	}
}
