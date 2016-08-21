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

using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Default <see cref="ITextView"/> options
	/// </summary>
	public static class DefaultDnSpyTextViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly EditorOptionKey<bool> CanChangeOverwriteModeId = new EditorOptionKey<bool>("ITextView/CanChangeOverwriteMode");
		public static readonly EditorOptionKey<bool> CanChangeUseVisibleWhitespaceId = new EditorOptionKey<bool>("ITextView/CanChangeUseVisibleWhitespace");
		public static readonly EditorOptionKey<bool> CanChangeWordWrapStyleId = new EditorOptionKey<bool>("ITextView/CanChangeWordWrapStyle");
		public static readonly EditorOptionKey<bool> AllowBoxSelectionId = new EditorOptionKey<bool>("ITextView/AllowBoxSelection");
		public static readonly EditorOptionKey<bool> RefreshScreenOnChangeId = new EditorOptionKey<bool>("ITextView/RefreshScreenOnChange");
		public static readonly EditorOptionKey<int> RefreshScreenOnChangeWaitMilliSecondsId = new EditorOptionKey<int>("ITextView/RefreshScreenOnChangeWaitMilliSeconds");
		public const int DefaultRefreshScreenOnChangeWaitMilliSeconds = 150;
		public static readonly EditorOptionKey<bool> EnableColorizationId = new EditorOptionKey<bool>("ITextView/EnableColorization");
		public static readonly EditorOptionKey<bool> ReferenceHighlightingId = new EditorOptionKey<bool>("ITextView/ReferenceHighlighting");
		public static readonly EditorOptionKey<bool> BraceMatchingId = new EditorOptionKey<bool>("ITextView/BraceMatching");
		public static readonly EditorOptionKey<bool> LineSeparatorId = new EditorOptionKey<bool>("ITextView/LineSeparator");
		public static readonly EditorOptionKey<bool> HighlightRelatedKeywordsId = new EditorOptionKey<bool>("ITextView/HighlightRelatedKeywords");
		public static readonly EditorOptionKey<bool> CompressEmptyOrWhitespaceLinesId = new EditorOptionKey<bool>("ITextView/CompressEmptyOrWhitespaceLines");
		public static readonly EditorOptionKey<bool> CompressNonLetterLinesId = new EditorOptionKey<bool>("ITextView/CompressNonLetterLines");
		public static readonly EditorOptionKey<bool> ShowStructureLinesId = new EditorOptionKey<bool>("ITextView/ShowStructureLines");
		public static readonly EditorOptionKey<bool> RemoveExtraTextLineVerticalPixelsId = new EditorOptionKey<bool>("ITextView/RemoveExtraTextLineVerticalPixels");
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
