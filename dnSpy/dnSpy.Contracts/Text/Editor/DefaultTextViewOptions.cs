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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Default <see cref="ITextView"/> options
	/// </summary>
	public static class DefaultTextViewOptions {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static readonly EditorOptionKey<bool> CutOrCopyBlankLineIfNoSelectionId = new EditorOptionKey<bool>("ITextView/CutOrCopyBlankLineIfNoSelection");
		public static readonly EditorOptionKey<bool> DisplayUrlsAsHyperlinksId = new EditorOptionKey<bool>("ITextView/DisplayUrlsAsHyperlinks");
		public static readonly EditorOptionKey<bool> DragDropEditingId = new EditorOptionKey<bool>("ITextView/DragDrop");
		public static readonly EditorOptionKey<bool> CanChangeOverwriteModeId = new EditorOptionKey<bool>("ITextView/CanChangeOverwriteMode");
		public static readonly EditorOptionKey<bool> OverwriteModeId = new EditorOptionKey<bool>("ITextView/OverwriteMode");
		public static readonly EditorOptionKey<bool> UseVirtualSpaceId = new EditorOptionKey<bool>("ITextView/UseVirtualSpace");
		public static readonly EditorOptionKey<bool> CanChangeUseVisibleWhitespaceId = new EditorOptionKey<bool>("ITextView/CanChangeUseVisibleWhitespace");
		public static readonly EditorOptionKey<bool> UseVisibleWhitespaceId = new EditorOptionKey<bool>("ITextView/UseVisibleWhitespace");
		public static readonly EditorOptionKey<bool> ViewProhibitUserInputId = new EditorOptionKey<bool>("ITextView/ProhibitUserInput");
		public static readonly EditorOptionKey<bool> CanChangeWordWrapStyleId = new EditorOptionKey<bool>("ITextView/CanChangeWordWrapStyle");
		public static readonly EditorOptionKey<WordWrapStyles> WordWrapStyleId = new EditorOptionKey<WordWrapStyles>("ITextView/WordWrapStyle");
		public static readonly EditorOptionKey<bool> ScrollBelowDocumentId = new EditorOptionKey<bool>("ITextView/ScrollBelowDocument");
		public static readonly EditorOptionKey<bool> RectangularSelectionId = new EditorOptionKey<bool>("ITextView/RectangularSelection");
		public static readonly EditorOptionKey<bool> HideCaretWhileTypingId = new EditorOptionKey<bool>("ITextView/HideCaretWhileTyping");
		public static readonly EditorOptionKey<bool> ShowColumnRulerId = new EditorOptionKey<bool>("ITextView/ShowColumnRuler");
		public static readonly EditorOptionKey<int> ColumnRulerPositionId = new EditorOptionKey<int>("ITextView/ColumnRulerPosition");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
