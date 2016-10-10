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
	public static class DefaultDsTextViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string CanChangeOverwriteModeName = "ITextView/CanChangeOverwriteMode";
		public static readonly EditorOptionKey<bool> CanChangeOverwriteModeId = new EditorOptionKey<bool>(CanChangeOverwriteModeName);
		public const string CanChangeUseVisibleWhitespaceName = "ITextView/CanChangeUseVisibleWhitespace";
		public static readonly EditorOptionKey<bool> CanChangeUseVisibleWhitespaceId = new EditorOptionKey<bool>(CanChangeUseVisibleWhitespaceName);
		public const string CanChangeWordWrapStyleName = "ITextView/CanChangeWordWrapStyle";
		public static readonly EditorOptionKey<bool> CanChangeWordWrapStyleId = new EditorOptionKey<bool>(CanChangeWordWrapStyleName);
		public const string AllowBoxSelectionName = "ITextView/AllowBoxSelection";
		public static readonly EditorOptionKey<bool> AllowBoxSelectionId = new EditorOptionKey<bool>(AllowBoxSelectionName);
		public const string RefreshScreenOnChangeName = "ITextView/RefreshScreenOnChange";
		public static readonly EditorOptionKey<bool> RefreshScreenOnChangeId = new EditorOptionKey<bool>(RefreshScreenOnChangeName);
		public const string RefreshScreenOnChangeWaitMilliSecondsName = "ITextView/RefreshScreenOnChangeWaitMilliSeconds";
		public static readonly EditorOptionKey<int> RefreshScreenOnChangeWaitMilliSecondsId = new EditorOptionKey<int>(RefreshScreenOnChangeWaitMilliSecondsName);
		public const int DefaultRefreshScreenOnChangeWaitMilliSeconds = 150;
		public const string EnableColorizationName = "ITextView/EnableColorization";
		public static readonly EditorOptionKey<bool> EnableColorizationId = new EditorOptionKey<bool>(EnableColorizationName);
		public const string ReferenceHighlightingName = "ITextView/ReferenceHighlighting";
		public static readonly EditorOptionKey<bool> ReferenceHighlightingId = new EditorOptionKey<bool>(ReferenceHighlightingName);
		public const string BraceMatchingName = "ITextView/BraceMatching";
		public static readonly EditorOptionKey<bool> BraceMatchingId = new EditorOptionKey<bool>(BraceMatchingName);
		public const string LineSeparatorsName = "ITextView/LineSeparators";
		public static readonly EditorOptionKey<bool> LineSeparatorsId = new EditorOptionKey<bool>(LineSeparatorsName);
		public const string HighlightRelatedKeywordsName = "ITextView/HighlightRelatedKeywords";
		public static readonly EditorOptionKey<bool> HighlightRelatedKeywordsId = new EditorOptionKey<bool>(HighlightRelatedKeywordsName);
		public const string CompressEmptyOrWhitespaceLinesName = "ITextView/CompressEmptyOrWhitespaceLines";
		public static readonly EditorOptionKey<bool> CompressEmptyOrWhitespaceLinesId = new EditorOptionKey<bool>(CompressEmptyOrWhitespaceLinesName);
		public const string CompressNonLetterLinesName = "ITextView/CompressNonLetterLines";
		public static readonly EditorOptionKey<bool> CompressNonLetterLinesId = new EditorOptionKey<bool>(CompressNonLetterLinesName);
		public const string ShowStructureLinesName = "ITextView/ShowStructureLines";
		public static readonly EditorOptionKey<bool> ShowStructureLinesId = new EditorOptionKey<bool>(ShowStructureLinesName);
		public const string RemoveExtraTextLineVerticalPixelsName = "ITextView/RemoveExtraTextLineVerticalPixels";
		public static readonly EditorOptionKey<bool> RemoveExtraTextLineVerticalPixelsId = new EditorOptionKey<bool>(RemoveExtraTextLineVerticalPixelsName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
