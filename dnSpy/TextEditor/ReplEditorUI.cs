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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.TextEditor {
	sealed class ReplEditorUI : IReplEditorUI {
		public object UIObject {
			get { return textEditor; }
		}

		public IInputElement FocusedElement {
			get { return textEditor.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return textEditor.TextArea; }
		}

		public object Tag { get; set; }

		readonly NewTextEditor textEditor;
		readonly ReplEditorOptions options;
		readonly Dispatcher dispatcher;

		const int LEFT_MARGIN = 15;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ReplEditorUI replEditorUI;

			public GuidObjectsCreator(ReplEditorUI uiContext) {
				this.replEditorUI = uiContext;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_REPL_EDITOR_GUID, replEditorUI);

				var teCtrl = (NewTextEditor)creatorObject.Object;
				var position = openedFromKeyboard ? teCtrl.TextArea.Caret.Position : teCtrl.GetPositionFromMousePosition();
				if (position != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, new TextEditorLocation(position.Value.Line, position.Value.Column));
			}
		}

		public ReplEditorUI(ReplEditorOptions options, IThemeManager themeManager, IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorSettings textEditorSettings) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.options = (options ?? new ReplEditorOptions()).Clone();
			this.textEditor = new NewTextEditor(themeManager, textEditorSettings);
			this.textEditor.TextArea.AllowDrop = false;
			this.textEditor.TextArea.Document = new TextDocument();
			this.textEditor.TextArea.Document.UndoStack.SizeLimit = 100;
			this.textEditor.TextArea.LeftMargins.Insert(0, new FrameworkElement { Margin = new Thickness(LEFT_MARGIN, 0, 0, 0) });
			this.textEditor.TextArea.TextEntering += TextArea_TextEntering;
			this.textEditor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
			AddBinding(ApplicationCommands.Paste, (s, e) => Paste(), (s, e) => e.CanExecute = CanPaste && IsAtEditingPosition);
			AddBinding(ApplicationCommands.Cut, (s, e) => CutSelection(), (s, e) => e.CanExecute = CanCutSelection && IsAtEditingPosition);

			if (this.options.TextEditorCommandGuid != null)
				wpfCommandManager.Add(this.options.TextEditorCommandGuid.Value, textEditor);
			if (this.options.TextAreaCommandGuid != null)
				wpfCommandManager.Add(this.options.TextAreaCommandGuid.Value, textEditor.TextArea);
			if (this.options.MenuGuid != null)
				menuManager.InitializeContextMenu(this.textEditor, this.options.MenuGuid.Value, new GuidObjectsCreator(this), new ContextMenuInitializer(textEditor, textEditor));
		}

		void AddBinding(RoutedUICommand routedCmd, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute) {
			Remove(this.textEditor.TextArea.CommandBindings, routedCmd);
			this.textEditor.TextArea.CommandBindings.Add(new CommandBinding(routedCmd, executed, canExecute));
		}

		static void Remove(CommandBindingCollection bindings, ICommand cmd) {
			for (int i = bindings.Count - 1; i >= 0; i--) {
				var b = bindings[i];
				if (b.Command == cmd)
					bindings.RemoveAt(i);
			}
		}

		public void Reset() {
			lock (pendingScriptOutputLock) {
				pendingScriptOutput = new StringBuilder();
				pendingScriptOutput_dispatching = false;
			}

			offsetOfPrompt = null;
			ClearUndoRedoHistory();
			CreateEmptyLastLineIfNeededAndMoveCaret();
			this.textEditor.TextArea.Caret.BringCaretToView();
		}

		bool CanCutSelection {
			get { return !this.textEditor.TextArea.Selection.IsEmpty; }
		}

		void CutSelection() {
			if (!UpdateCaretForEdit())
				return;
			var text = this.textEditor.TextArea.Selection.GetText();
			try {
				Clipboard.SetText(text);
			}
			catch (ExternalException) { return; }
			AddUserInput(string.Empty);
		}

		bool CanPaste {
			get {
				if (!this.textEditor.TextArea.ReadOnlySectionProvider.CanInsert(this.textEditor.TextArea.Caret.Offset))
					return false;
				try {
					return Clipboard.ContainsText();
				}
				catch (ExternalException) { return false; }
			}
		}

		void Paste() {
			if (!UpdateCaretForEdit())
				return;
			string text;
			try {
				if (!Clipboard.ContainsText())
					return;
				text = Clipboard.GetText();
			}
			catch (ExternalException) { return; }
			if (string.IsNullOrEmpty(text))
				return;
			AddUserInput(text);
		}

		/// <summary>
		/// true if the caret is within the command line(s). It can also return true if the caret
		/// is within the prompt/continue text.
		/// </summary>
		bool IsAtEditingPosition {
			get {
				if (!IsCommandMode)
					return false;
				return this.textEditor.TextArea.Caret.Offset >= offsetOfPrompt.Value;
			}
		}

		/// <summary>
		/// Returns false if the caret isn't within the editing area. If the caret is within the
		/// prompt or continue text (eg. first two chars of the line), then the caret is moved to
		/// the first character after that text.
		/// </summary>
		/// <returns></returns>
		bool UpdateCaretForEdit() {
			if (!IsAtEditingPosition)
				return false;
			var caret = this.textEditor.TextArea.Caret;
			caret.Offset = FilterOffset(caret.Offset);

			var sel = this.textEditor.TextArea.Selection;
			if (!sel.IsEmpty) {
				var doc = this.textEditor.TextArea.Document;
				int start = FilterOffset(doc.GetOffset(sel.StartPosition.Location));
				int end = FilterOffset(doc.GetOffset(sel.EndPosition.Location));
				this.textEditor.TextArea.Selection = Selection.Create(this.textEditor.TextArea, start, end);
			}

			return true;
		}

		int FilterOffset(int offset) {
			Debug.Assert(offsetOfPrompt != null);
			if (offset < offsetOfPrompt.Value)
				offset = offsetOfPrompt.Value;
			var line = this.textEditor.TextArea.Document.GetLineByOffset(offset);
			var prefixString = line.Offset == offsetOfPrompt.Value ? options.PromptText : options.ContinueText;
			int col = offset - line.Offset;
			if (col < prefixString.Length)
				offset = line.Offset + prefixString.Length;
			if (offset > LastLine.EndOffset)
				offset = LastLine.EndOffset;
			return offset;
		}

		void TextArea_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (!PreviewKeyDown(e.KeyboardDevice.Modifiers, e.Key == Key.System ? e.SystemKey : e.Key)) {
				e.Handled = true;
				return;
			}
		}

		// Makes sure that the text editor doesn't try to modify the document
		bool PreviewKeyDown(ModifierKeys mod, Key key) {
			if (mod == ModifierKeys.Control && key == Key.Z) {
				SearchText = null;
				return UpdateCaretForEdit();
			}
			if ((mod == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.Z) ||
				(mod == ModifierKeys.Control && key == Key.Y)) {
				SearchText = null;
				return UpdateCaretForEdit();
			}
			if ((mod == ModifierKeys.None && key == Key.Delete) || (mod == ModifierKeys.Control && key == Key.Delete)) {
				HandleDelete();
				return false;
			}
			if ((mod == ModifierKeys.None && key == Key.Back) || (mod == ModifierKeys.Shift && key == Key.Back) || (mod == ModifierKeys.Control && key == Key.Back)) {
				HandleBackspace();
				return false;
			}
			if (mod == ModifierKeys.Control && key == Key.A) {
				HandleSelectAll();
				return false;
			}

			if (mod == ModifierKeys.None && key == Key.Enter)
				return HandleEnter(false);
			if (mod == ModifierKeys.Control && key == Key.Enter) {
				this.textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
				this.textEditor.TextArea.ClearSelection();
				return HandleEnter(true);
			}
			if (mod == ModifierKeys.Shift && key == Key.Enter)
				return UpdateCaretForEdit();
			if (mod == ModifierKeys.None && key == Key.Tab)
				return HandleTab();
			if (mod == ModifierKeys.Shift && key == Key.Tab)
				return false;
			if (mod == ModifierKeys.None && key == Key.Escape) {
				HandleEscape();
				return false;
			}
			if (mod == ModifierKeys.Control && key == Key.L) {
				Clear();
				return false;
			}
			if (mod == ModifierKeys.Alt && key == Key.Up) {
				SelectPreviousCommand();
				return false;
			}
			if (mod == ModifierKeys.Alt && key == Key.Down) {
				SelectNextCommand();
				return false;
			}
			if (mod == (ModifierKeys.Control | ModifierKeys.Alt) && key == Key.Up) {
				SelectSameTextPreviousCommand();
				return false;
			}
			if (mod == (ModifierKeys.Control | ModifierKeys.Alt) && key == Key.Down) {
				SelectSameTextNextCommand();
				return false;
			}

			// AvalonEditCommands.DeleteLine, AvalonEditCommands.IndentSelection
			if ((mod == ModifierKeys.Control && key == Key.D) || (mod == ModifierKeys.Control && key == Key.I))
				return false;

			return true;
		}

		void HandleEscape() {
			ClearCurrentInput();
		}

		void ClearCurrentInput(bool removePrompt = false) {
			Debug.Assert(IsCommandMode);
			if (!IsCommandMode)
				return;
			int offs = removePrompt ? offsetOfPrompt.Value : FilterOffset(offsetOfPrompt.Value);
			this.textEditor.TextArea.Caret.Offset = offs;
			var sel = Selection.Create(this.textEditor.TextArea, offs, LastLine.EndOffset);
			this.textEditor.TextArea.Selection = sel;

			this.textEditor.TextArea.Selection.ReplaceSelectionWithText(string.Empty);
			this.textEditor.TextArea.Caret.BringCaretToView();
			SearchText = null;

			if (removePrompt)
				offsetOfPrompt = null;
		}

		bool HandleEnter(bool force) {
			if (!UpdateCaretForEdit())
				return false;
			if (this.textEditor.TextArea.Caret.Offset != LastLine.EndOffset)
				return true;
			if (!this.textEditor.TextArea.Selection.IsEmpty)
				return true;

			var input = CurrentInput;
			bool isCmd = force || this.CommandHandler.IsCommand(input);
			if (!isCmd) {
				AddUserInput(Environment.NewLine);
				return false;
			}

			SearchText = null;
			if (!string.IsNullOrEmpty(input))
				replCommands.Add(input);
			offsetOfPrompt = null;
			RawAppend(Environment.NewLine);
			ClearUndoRedoHistory();
			this.textEditor.TextArea.Caret.BringCaretToView();
			this.CommandHandler.ExecuteCommand(input);
			return false;
		}

		string CurrentInput {
			get {
				Debug.Assert(IsCommandMode);
				if (!IsCommandMode)
					return string.Empty;

				string s = this.textEditor.TextArea.Document.GetText(offsetOfPrompt.Value, LastLine.EndOffset - offsetOfPrompt.Value);
				var sb = new StringBuilder(s.Length);
				int so = 0;
				string prefixString = options.PromptText;
				while (so < s.Length) {
					int nlOffs = s.IndexOfAny(newLineChars, so);
					if (nlOffs >= 0) {
						int soNext = nlOffs;
						int nlLen = s[soNext] == '\r' && soNext + 1 < s.Length && s[soNext + 1] == '\n' ? 2 : 1;
						soNext += nlLen;

						int remaining = nlOffs - so;
						Debug.Assert(remaining >= prefixString.Length);
						if (remaining >= prefixString.Length) {
							Debug.Assert(s.Substring(so, prefixString.Length).Equals(prefixString));
							so += prefixString.Length;
						}
						sb.Append(s, so, soNext - so);

						so = soNext;
					}
					else {
						int remaining = s.Length - so;
						Debug.Assert(remaining >= prefixString.Length);
						if (remaining >= prefixString.Length) {
							Debug.Assert(s.Substring(so, prefixString.Length).Equals(prefixString));
							so += prefixString.Length;
						}

						sb.Append(s, so, s.Length - so);
						break;
					}

					prefixString = options.ContinueText;
				}
				return sb.ToString();
			}
		}

		bool HandleTab() {
			if (!UpdateCaretForEdit())
				return false;
			if (!this.textEditor.TextArea.Selection.IsEmpty) {
				AddUserInput(string.Empty);
				return false;
			}

			return true;
		}

		void HandleSelectAll() {
			var newSel = Selection.Create(this.textEditor.TextArea, offsetOfPrompt == null ? 0 : FilterOffset(offsetOfPrompt.Value), LastLine.EndOffset);
			if (newSel.IsEmpty || this.textEditor.TextArea.Selection.Equals(newSel))
				newSel = Selection.Create(this.textEditor.TextArea, 0, LastLine.EndOffset);
			this.textEditor.TextArea.Selection = newSel;
			this.textEditor.TextArea.Caret.Offset = this.textEditor.TextArea.Document.GetOffset(newSel.EndPosition.Location);
			this.textEditor.TextArea.Caret.BringCaretToView();
		}

		void HandleDelete() {
			if (!UpdateCaretForEdit())
				return;
			if (!this.textEditor.TextArea.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				if (this.textEditor.TextArea.Caret.Offset >= LastLine.EndOffset)
					return;
				int start = FilterOffset(this.textEditor.TextArea.Caret.Offset);
				int end = start + 1;
				var startLine = this.textEditor.TextArea.Document.GetLineByOffset(start);
				var endLine = this.textEditor.TextArea.Document.GetLineByOffset(end);
				if (startLine != endLine || end > endLine.EndOffset) {
					endLine = this.textEditor.TextArea.Document.GetLineByNumber(startLine.LineNumber + 1);
					end = FilterOffset(endLine.Offset);
				}
				this.textEditor.TextArea.Selection = Selection.Create(this.textEditor.TextArea, start, end);
				AddUserInput(string.Empty);
			}
		}

		void HandleBackspace() {
			if (!UpdateCaretForEdit())
				return;
			if (!this.textEditor.TextArea.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				int start = FilterOffset(offsetOfPrompt.Value);
				int offs = this.textEditor.TextArea.Caret.Offset;
				if (offs <= start)
					return;
				int end = this.textEditor.TextArea.Caret.Offset;
				start = end - 1;
				var line = this.textEditor.TextArea.Document.GetLineByOffset(end);
				if (line.Offset == end || FilterOffset(start) != start) {
					var prevLine = this.textEditor.TextArea.Document.GetLineByNumber(line.LineNumber - 1);
					start = prevLine.EndOffset;
				}
				this.textEditor.TextArea.Selection = Selection.Create(this.textEditor.TextArea, start, end);
				AddUserInput(string.Empty);
			}
		}

		static TextViewPosition GetPrevCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace) {
			int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode, enableVirtualSpace);
			if (pos >= 0)
				return visualLine.GetTextViewPosition(pos);
			var previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
			if (previousDocumentLine != null) {
				var previousLine = textView.GetOrConstructVisualLine(previousDocumentLine);
				pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode, enableVirtualSpace);
				return previousLine.GetTextViewPosition(pos);
			}
			return new TextViewPosition(0, 0);
		}

		void TextArea_TextEntering(object sender, TextCompositionEventArgs e) {
			e.Handled = true;
			if (!UpdateCaretForEdit())
				return;

			var ta = textEditor.TextArea;
			if (ta.OverstrikeMode && ta.Selection.IsEmpty && ta.Document.GetLineByNumber(ta.Caret.Line).EndOffset > ta.Caret.Offset)
				EditingCommands.SelectRightByCharacter.Execute(null, ta);
			AddUserInput(e.Text);
		}

		string GetNewString(string s) {
			var sb = new StringBuilder(s.Length);
			int so = 0;
			while (so < s.Length) {
				int nlOffs = s.IndexOfAny(newLineChars, so);
				if (nlOffs >= 0) {
					sb.Append(s, so, nlOffs - so);
					sb.Append(Environment.NewLine);
					sb.Append(options.ContinueText);
					so = nlOffs;
					int nlLen = s[so] == '\r' && so + 1 < s.Length && s[so + 1] == '\n' ? 2 : 1;
					so += nlLen;
				}
				else {
					sb.Append(s, so, s.Length - so);
					break;
				}
			}
			return sb.ToString();
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		void AddUserInput(string text, bool clearSearchText = true) {
			if (!UpdateCaretForEdit())
				return;
			var s = GetNewString(text);
			this.textEditor.TextArea.Selection.ReplaceSelectionWithText(s);
			this.textEditor.TextArea.Caret.BringCaretToView();
			if (clearSearchText)
				SearchText = null;
		}

		public bool CanClear {
			get { return true; }
		}

		public void Clear() {
			if (!CanClear)
				return;
			this.textEditor.TextArea.Document = new TextDocument();
			ClearUndoRedoHistory();
			bool hasPrompt = offsetOfPrompt != null;
			offsetOfPrompt = null;
			if (hasPrompt)
				PrintPrompt();
		}

		public bool CanSelectPreviousCommand {
			get { return IsCommandMode && replCommands.CanSelectPrevious; }
		}
		readonly ReplCommands replCommands = new ReplCommands();

		public void SelectPreviousCommand() {
			if (!CanSelectPreviousCommand)
				return;
			replCommands.SelectPrevious();
			UpdateCommand(true);
		}

		public bool CanSelectNextCommand {
			get { return IsCommandMode && replCommands.CanSelectNext; }
		}

		public void SelectNextCommand() {
			if (!CanSelectNextCommand)
				return;
			replCommands.SelectNext();
			UpdateCommand(true);
		}

		string SearchText {
			get { return searchText ?? (searchText = CurrentInput); }
			set { searchText = value; }
		}
		string searchText = string.Empty;

		void SelectSameTextPreviousCommand() {
			if (!IsCommandMode)
				return;

			replCommands.SelectPrevious(SearchText);
			UpdateCommand(false);
		}

		void SelectSameTextNextCommand() {
			if (!IsCommandMode)
				return;

			replCommands.SelectNext(SearchText);
			UpdateCommand(false);
		}

		void UpdateCommand(bool clearSearchText) {
			Debug.Assert(IsCommandMode);
			if (!IsCommandMode)
				return;

			var command = replCommands.SelectedCommand;
			if (command == null)
				return;

			this.textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
			var currentInput = CurrentInput;
			if (currentInput.Equals(command))
				return;

			this.textEditor.TextArea.Selection = Selection.Create(this.textEditor.TextArea, FilterOffset(offsetOfPrompt.Value), LastLine.EndOffset);
			this.textEditor.TextArea.Caret.Offset = FilterOffset(offsetOfPrompt.Value);
			AddUserInput(command, clearSearchText);
		}

		void RawAppend(string text) {
			this.textEditor.TextArea.Document.Insert(LastLine.EndOffset, text);
		}

		void FlushScriptOutputUIThread() {
			dispatcher.VerifyAccess();

			string output;
			lock (pendingScriptOutputLock) {
				pendingScriptOutput_dispatching = false;
				output = pendingScriptOutput.ToString();
				pendingScriptOutput.Clear();
			}

			string currentCommand = null;
			bool isCommandMode = IsCommandMode;
			if (isCommandMode) {
				currentCommand = CurrentInput;
				ClearCurrentInput(true);
			}
			RawAppend(output);
			this.textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
			if (isCommandMode) {
				PrintPrompt();
				AddUserInput(currentCommand);
			}

			this.textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
			this.textEditor.TextArea.Caret.BringCaretToView();
		}

		void FlushScriptOutput() {
			if (!dispatcher.CheckAccess()) {
				lock (pendingScriptOutputLock) {
					if (pendingScriptOutput_dispatching)
						return;
					pendingScriptOutput_dispatching = true;
					try {
						dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(FlushScriptOutputUIThread));
					}
					catch {
						pendingScriptOutput_dispatching = false;
						throw;
					}
				}
			}
			else
				FlushScriptOutputUIThread();
		}
		readonly object pendingScriptOutputLock = new object();
		StringBuilder pendingScriptOutput = new StringBuilder();
		bool pendingScriptOutput_dispatching;

		void IReplEditor.OutputPrint(string text) {
			if (string.IsNullOrEmpty(text))
				return;

			lock (pendingScriptOutputLock)
				pendingScriptOutput.Append(text);

			FlushScriptOutput();
		}

		void IReplEditor.OutputPrintLine(string text) {
			((IReplEditor)this).OutputPrint(text + Environment.NewLine);
		}

		sealed class ReplCommandHandler : IReplCommandHandler {
			public static readonly IReplCommandHandler Null = new ReplCommandHandler();

			public void ExecuteCommand(string input) {
			}

			public bool IsCommand(string text) {
				return false;
			}
		}

		public IReplCommandHandler CommandHandler {
			get { return replCommandHandler ?? ReplCommandHandler.Null; }
			set { replCommandHandler = value; }
		}
		IReplCommandHandler replCommandHandler;

		void ClearUndoRedoHistory() {
			this.textEditor.TextArea.Document.UndoStack.ClearAll();
		}

		DocumentLine LastLine {
			get {
				var doc = this.textEditor.TextArea.Document;
				return doc.GetLineByNumber(doc.LineCount);
			}
		}

		void CreateEmptyLastLineIfNeededAndMoveCaret() {
			var lastLine = LastLine;
			if (lastLine.Length != 0)
				RawAppend(Environment.NewLine);
			this.textEditor.TextArea.Caret.Offset = lastLine.EndOffset;
			this.textEditor.TextArea.Caret.BringCaretToView();
		}

		public void OnCommandExecuted() {
			PrintPrompt();
		}

		void PrintPrompt() {
			// Can happen if we reset the script and it throws an OperationCanceledException
			if (offsetOfPrompt != null)
				return;

			CreateEmptyLastLineIfNeededAndMoveCaret();
			offsetOfPrompt = LastLine.EndOffset;
			RawAppend(options.PromptText);
			ClearUndoRedoHistory();
			this.textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
			this.textEditor.TextArea.Caret.BringCaretToView();

			// Somehow the caret isn't shown if we have word-wrap enabled and lots of text is shown
			// so the window gets scrolled, eg. try: typeof(IntPtr).GetMethods()
			var tempOffs = this.textEditor.TextArea.Caret.Offset;
			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (this.textEditor.TextArea.Caret.Offset == tempOffs)
					this.textEditor.TextArea.Caret.BringCaretToView();
			}));
		}
		int? offsetOfPrompt = null;

		/// <summary>
		/// true if we're reading user input and new user commands get executed when enter is pressed
		/// </summary>
		bool IsCommandMode {
			get { return offsetOfPrompt != null; }
		}

		/// <summary>
		/// true if the script is executing and we don't accept any user input
		/// </summary>
		bool IsExecMode {
			get { return offsetOfPrompt == null; }
		}

		public void Dispose() {
			this.textEditor.Dispose();
		}
	}
}
