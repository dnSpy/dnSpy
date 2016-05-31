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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class WpfTextView : IWpfTextView {
		public PropertyCollection Properties { get; }
		public FrameworkElement VisualElement => DnSpyTextEditor.FocusedElement;
		public object UIObject => DnSpyTextEditor;
		public IInputElement FocusedElement => DnSpyTextEditor.FocusedElement;
		public FrameworkElement ScaleElement => DnSpyTextEditor.ScaleElement;
		public object Tag { get; set; }
		public ITextViewRoleSet Roles { get; }
		public IEditorOptions Options { get; }
		public ICommandTarget CommandTarget => RegisteredCommandElement.CommandTarget;
		IRegisteredCommandElement RegisteredCommandElement { get; }
		public ITextCaret Caret { get; }
		public ITextSelection Selection { get; }
		public bool HasAggregateFocus => DnSpyTextEditor.IsKeyboardFocusWithin;
		public bool IsMouseOverViewOrAdornments => DnSpyTextEditor.IsMouseOver;
		//TODO: Remove public from this property once all refs to it from REPL and LOG editors have been removed
		public DnSpyTextEditor DnSpyTextEditor { get; }

		const int LEFT_MARGIN = 15;
		readonly FrameworkElement paddingElement;

		public WpfTextView(DnSpyTextEditor dnSpyTextEditor, ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandManager commandManager) {
			if (dnSpyTextEditor == null)
				throw new ArgumentNullException(nameof(dnSpyTextEditor));
			if (textViewModel == null)
				throw new ArgumentNullException(nameof(textViewModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			this.paddingElement = new FrameworkElement { Margin = new Thickness(LEFT_MARGIN, 0, 0, 0) };
			Properties = new PropertyCollection();
			DnSpyTextEditor = dnSpyTextEditor;
			RegisteredCommandElement = commandManager.Register(dnSpyTextEditor.TextArea, this);
			TextViewModel = textViewModel;
			Roles = roles;
			Options = editorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;
			Options.OptionChanged += EditorOptions_OptionChanged;
			Selection = new TextSelection(this, dnSpyTextEditor);
			Caret = new TextCaret(this, dnSpyTextEditor);
			InitializeFrom(Options);
			DnSpyTextEditor.Loaded += DnSpyTextEditor_Loaded;
			DnSpyTextEditor.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(DnSpyTextEditor_GotKeyboardFocus), true);
			DnSpyTextEditor.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(DnSpyTextEditor_LostKeyboardFocus), true);
			hasKeyboardFocus = DnSpyTextEditor.IsKeyboardFocusWithin;
		}

		bool hasKeyboardFocus;
		void DnSpyTextEditor_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => UpdateKeyboardFocus();
		void DnSpyTextEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => UpdateKeyboardFocus();
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;
		void UpdateKeyboardFocus() {
			bool newValue = DnSpyTextEditor.IsKeyboardFocusWithin;
			if (hasKeyboardFocus != newValue) {
				hasKeyboardFocus = newValue;
				if (hasKeyboardFocus)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		void DnSpyTextEditor_Loaded(object sender, RoutedEventArgs e) {
			DnSpyTextEditor.Loaded -= DnSpyTextEditor_Loaded;
			// Fix the highlighted line, it won't be shown correctly if this control is in
			// a hidden tab that now got activated for the first time.
			DnSpyTextEditor.UpdateCurrentLineColors();
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) => UpdateOption(e.OptionId);

		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }

		public bool IsClosed { get; set; }
		public event EventHandler Closed;
		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			RegisteredCommandElement.Unregister();
			TextViewModel.Dispose();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
		}

		void InitializeFrom(IEditorOptions options) {
			UpdateOption(DefaultOptions.TabSizeOptionId.Name);
			UpdateOption(DefaultOptions.NewLineCharacterOptionId.Name);
			UpdateOption(DefaultOptions.ReplicateNewLineCharacterOptionId.Name);
			UpdateOption(DefaultOptions.ConvertTabsToSpacesOptionId.Name);
			UpdateOption(DefaultTextViewHostOptions.HorizontalScrollBarId.Name);
			UpdateOption(DefaultTextViewHostOptions.VerticalScrollBarId.Name);
			UpdateOption(DefaultTextViewHostOptions.LineNumberMarginId.Name);
			UpdateOption(DefaultTextViewHostOptions.SelectionMarginId.Name);
			UpdateOption(DefaultTextViewHostOptions.GlyphMarginId.Name);
			UpdateOption(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId.Name);
			UpdateOption(DefaultTextViewOptions.DisplayUrlsAsHyperlinksId.Name);
			UpdateOption(DefaultTextViewOptions.DragDropEditingId.Name);
			UpdateOption(DefaultTextViewOptions.OverwriteModeId.Name);
			UpdateOption(DefaultTextViewOptions.UseVirtualSpaceId.Name);
			UpdateOption(DefaultTextViewOptions.UseVisibleWhitespaceId.Name);
			UpdateOption(DefaultTextViewOptions.ViewProhibitUserInputId.Name);
			UpdateOption(DefaultTextViewOptions.WordWrapStyleId.Name);
			UpdateOption(DefaultTextViewOptions.ScrollBelowDocumentId.Name);
			UpdateOption(DefaultTextViewOptions.RectangularSelectionId.Name);
			UpdateOption(DefaultTextViewOptions.HideCaretWhileTypingId.Name);
			UpdateOption(DefaultTextViewOptions.ShowColumnRulerId.Name);
			UpdateOption(DefaultTextViewOptions.ColumnRulerPositionId.Name);
			UpdateOption(DefaultWpfViewOptions.EnableHighlightCurrentLineId.Name);
		}

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			if (optionId == DefaultOptions.TabSizeOptionId.Name)
				DnSpyTextEditor.Options.IndentationSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			else if (optionId == DefaultOptions.NewLineCharacterOptionId.Name)
				DnSpyTextEditor.Options.NewLineCharacter = Options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId);
			else if (optionId == DefaultOptions.ReplicateNewLineCharacterOptionId.Name)
				DnSpyTextEditor.Options.ReplicateNewLineCharacter = Options.GetOptionValue(DefaultOptions.ReplicateNewLineCharacterOptionId);
			else if (optionId == DefaultOptions.ConvertTabsToSpacesOptionId.Name)
				DnSpyTextEditor.Options.ConvertTabsToSpaces = Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
			else if (optionId == DefaultTextViewHostOptions.HorizontalScrollBarId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId);
				DnSpyTextEditor.HorizontalScrollBarVisibility = newValue ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}
			else if (optionId == DefaultTextViewHostOptions.VerticalScrollBarId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId);
				DnSpyTextEditor.VerticalScrollBarVisibility = newValue ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}
			else if (optionId == DefaultTextViewHostOptions.LineNumberMarginId.Name)
				DnSpyTextEditor.ShowLineNumbers = Options.GetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId);
			else if (optionId == DefaultTextViewHostOptions.SelectionMarginId.Name) {
				bool enable = Options.GetOptionValue(DefaultTextViewHostOptions.SelectionMarginId);
				bool b = DnSpyTextEditor.TextArea.LeftMargins.Remove(paddingElement);
				Debug.Assert(b == !enable);
				if (enable)
					DnSpyTextEditor.TextArea.LeftMargins.Add(paddingElement);
			}
			else if (optionId == DefaultTextViewHostOptions.GlyphMarginId.Name) {
				//TODO:
			}
			else if (optionId == DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId.Name)
				DnSpyTextEditor.Options.CutCopyWholeLine = Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
			else if (optionId == DefaultTextViewOptions.DisplayUrlsAsHyperlinksId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.DisplayUrlsAsHyperlinksId);
				DnSpyTextEditor.Options.EnableHyperlinks = newValue;
				DnSpyTextEditor.Options.EnableEmailHyperlinks = newValue;
				DnSpyTextEditor.Options.RequireControlModifierForHyperlinkClick = false;
			}
			else if (optionId == DefaultTextViewOptions.DragDropEditingId.Name) {
				// DnSpyTextEditor.Options.EnableTextDragDrop also exists, but it's better to disable drag and drop
				// to the text area. If EnableTextDragDrop is false, the user can't drag selected text to another
				// window / application that supports drag and drop.
				DnSpyTextEditor.TextArea.AllowDrop = Options.GetOptionValue(DefaultTextViewOptions.DragDropEditingId);
			}
			else if (optionId == DefaultTextViewOptions.OverwriteModeId.Name)
				DnSpyTextEditor.Options.AllowToggleOverstrikeMode = Options.GetOptionValue(DefaultTextViewOptions.OverwriteModeId);
			else if (optionId == DefaultTextViewOptions.UseVirtualSpaceId.Name)
				DnSpyTextEditor.Options.EnableVirtualSpace = Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId);
			else if (optionId == DefaultTextViewOptions.UseVisibleWhitespaceId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.UseVisibleWhitespaceId);
				DnSpyTextEditor.Options.ShowSpaces = newValue;
				DnSpyTextEditor.Options.ShowTabs = newValue;
			}
			else if (optionId == DefaultTextViewOptions.ViewProhibitUserInputId.Name)
				DnSpyTextEditor.IsReadOnly = Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId);
			else if (optionId == DefaultTextViewOptions.WordWrapStyleId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId);
				DnSpyTextEditor.WordWrap = (newValue & WordWrapStyles.WordWrap) != 0;
				DnSpyTextEditor.Options.InheritWordWrapIndentation = (newValue & WordWrapStyles.AutoIndent) != 0;
			}
			else if (optionId == DefaultTextViewOptions.ScrollBelowDocumentId.Name)
				DnSpyTextEditor.Options.AllowScrollBelowDocument = Options.GetOptionValue(DefaultTextViewOptions.ScrollBelowDocumentId);
			else if (optionId == DefaultTextViewOptions.RectangularSelectionId.Name)
				DnSpyTextEditor.Options.EnableRectangularSelection = Options.GetOptionValue(DefaultTextViewOptions.RectangularSelectionId);
			else if (optionId == DefaultTextViewOptions.HideCaretWhileTypingId.Name)
				DnSpyTextEditor.Options.HideCursorWhileTyping = Options.GetOptionValue(DefaultTextViewOptions.HideCaretWhileTypingId);
			else if (optionId == DefaultTextViewOptions.ShowColumnRulerId.Name)
				DnSpyTextEditor.Options.ShowColumnRuler = Options.GetOptionValue(DefaultTextViewOptions.ShowColumnRulerId);
			else if (optionId == DefaultTextViewOptions.ColumnRulerPositionId.Name)
				DnSpyTextEditor.Options.ColumnRulerPosition = Options.GetOptionValue(DefaultTextViewOptions.ColumnRulerPositionId);
			else if (optionId == DefaultWpfViewOptions.EnableHighlightCurrentLineId.Name)
				DnSpyTextEditor.HighlightCurrentLine = Options.GetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId);
		}
	}
}
