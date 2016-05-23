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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Text;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Text.Editor {
	sealed class ReplEditorUI : IReplEditorUI {
		public object UIObject => textEditor;
		public IInputElement FocusedElement => textEditor.FocusedElement;
		public FrameworkElement ScaleElement => textEditor.ScaleElement;
		public object Tag { get; set; }

		internal string PrimaryPrompt { get; }
		internal string SecondaryPrompt { get; }

		readonly DnSpyTextEditor textEditor;
		readonly Dispatcher dispatcher;
		readonly CachedColorsList cachedColorsList;
		readonly IWpfTextView wpfTextView;

		const int LEFT_MARGIN = 15;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ReplEditorUI replEditorUI;

			public GuidObjectsCreator(ReplEditorUI replEditorUI) {
				this.replEditorUI = replEditorUI;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_REPL_EDITOR_GUID, replEditorUI);
			}
		}

		public ReplEditorUI(ReplEditorOptions options, ITextEditorFactoryService2 textEditorFactoryService2) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			options = options ?? new ReplEditorOptions();
			this.PrimaryPrompt = options.PrimaryPrompt;
			this.SecondaryPrompt = options.SecondaryPrompt;
			this.subBuffers = new List<SubBuffer>();

			var wpfTextView = textEditorFactoryService2.CreateTextView(null, options, (object)options.ContentType ?? options.ContentTypeGuid, false, () => new GuidObjectsCreator(this));
			this.wpfTextView = wpfTextView;
			this.textEditor = wpfTextView.DnSpyTextEditor;
			this.cachedColorsList = new CachedColorsList();
			textEditor.AddColorizer(new CachedColorsListColorizer(this.cachedColorsList, ColorPriority.Default));
			this.textEditor.TextArea.AllowDrop = false;
			AddNewDocument();
			this.textEditor.TextArea.TextView.Document.UndoStack.SizeLimit = 100;
			this.textEditor.TextArea.LeftMargins.Insert(0, new FrameworkElement { Margin = new Thickness(LEFT_MARGIN, 0, 0, 0) });
			this.textEditor.TextArea.TextEntering += TextArea_TextEntering;
			this.textEditor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
			AddBinding(ApplicationCommands.Paste, (s, e) => Paste(), (s, e) => e.CanExecute = CanPaste && IsAtEditingPosition);
			AddBinding(ApplicationCommands.Cut, (s, e) => CutSelection(), (s, e) => e.CanExecute = CanCutSelection && IsAtEditingPosition);
			WriteOffsetOfPrompt(null, true);
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

		void WriteOffsetOfPrompt(int? newValue, bool force = false) {
			if (force || offsetOfPrompt.HasValue != newValue.HasValue) {
				if (newValue == null) {
					Debug.Assert(scriptOutputCachedTextTokenColors == null);
					scriptOutputCachedTextTokenColors = new CachedTextTokenColors();
					Debug.Assert(LastLine.Length == 0);
					cachedColorsList.AddOrUpdate(LastLine.EndOffset, scriptOutputCachedTextTokenColors);
					textEditor.TextArea.TextView.Redraw(LastLine.EndOffset, scriptOutputCachedTextTokenColors.Length);
				}
				else {
					Debug.Assert(scriptOutputCachedTextTokenColors != null);
					scriptOutputCachedTextTokenColors?.Flush();
					scriptOutputCachedTextTokenColors = null;
				}
			}
			offsetOfPrompt = newValue;
		}
		int? offsetOfPrompt;
		CachedTextTokenColors scriptOutputCachedTextTokenColors;

		public void Reset() {
			ClearPendingOutput();
			ClearUndoRedoHistory();
			CreateEmptyLastLineIfNeededAndMoveCaret();
			if (offsetOfPrompt != null)
				AddCodeSubBuffer();
			scriptOutputCachedTextTokenColors = null;
			WriteOffsetOfPrompt(null, true);
		}

		bool CanCutSelection => !this.textEditor.TextArea.Selection.IsEmpty;

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
				var doc = this.textEditor.TextArea.TextView.Document;
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
			var line = this.textEditor.TextArea.TextView.Document.GetLineByOffset(offset);
			var prefixString = line.Offset == offsetOfPrompt.Value ? PrimaryPrompt : SecondaryPrompt;
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

		void HandleEscape() => ClearCurrentInput();

		void ClearCurrentInput(bool removePrompt = false) {
			Debug.Assert(IsCommandMode);
			if (!IsCommandMode)
				return;
			int offs = removePrompt ? offsetOfPrompt.Value : FilterOffset(offsetOfPrompt.Value);
			this.textEditor.TextArea.Caret.Offset = offs;
			var sel = Selection.Create(this.textEditor.TextArea, offs, LastLine.EndOffset);
			this.textEditor.TextArea.Selection = sel;

			if (removePrompt) {
				var oldValue = offsetOfPrompt;
				offsetOfPrompt = null;

				this.textEditor.TextArea.Selection.ReplaceSelectionWithText(string.Empty);
				this.textEditor.TextArea.Caret.BringCaretToView();
				SearchText = null;

				offsetOfPrompt = oldValue;
				WriteOffsetOfPrompt(null);
			}
			else {
				this.textEditor.TextArea.Selection.ReplaceSelectionWithText(string.Empty);
				this.textEditor.TextArea.Caret.BringCaretToView();
				SearchText = null;
			}
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
			RawAppend(Environment.NewLine);
			AddCodeSubBuffer();
			WriteOffsetOfPrompt(null);
			ClearUndoRedoHistory();
			this.textEditor.TextArea.Caret.BringCaretToView();
			commandVersion++;
			this.CommandHandler.ExecuteCommand(input);
			return false;
		}
		int commandVersion = 0;

		string CurrentInput {
			get {
				Debug.Assert(IsCommandMode);
				if (!IsCommandMode)
					return string.Empty;

				string s = this.textEditor.TextArea.TextView.Document.GetText(offsetOfPrompt.Value, LastLine.EndOffset - offsetOfPrompt.Value);
				return ToInputString(s, PrimaryPrompt);
			}
		}

		string ToInputString(string text, string prefixString) {
			var sb = new StringBuilder(text.Length);
			int so = 0;
			while (so < text.Length) {
				int nlOffs = text.IndexOfAny(newLineChars, so);
				if (nlOffs >= 0) {
					int soNext = nlOffs;
					int nlLen = text[soNext] == '\r' && soNext + 1 < text.Length && text[soNext + 1] == '\n' ? 2 : 1;
					soNext += nlLen;

					int remaining = nlOffs - so;
					Debug.Assert(remaining >= prefixString.Length);
					if (remaining >= prefixString.Length) {
						Debug.Assert(text.Substring(so, prefixString.Length).Equals(prefixString));
						so += prefixString.Length;
					}
					sb.Append(text, so, soNext - so);

					so = soNext;
				}
				else {
					int remaining = text.Length - so;
					Debug.Assert(remaining >= prefixString.Length);
					if (remaining >= prefixString.Length) {
						Debug.Assert(text.Substring(so, prefixString.Length).Equals(prefixString));
						so += prefixString.Length;
					}

					sb.Append(text, so, text.Length - so);
					break;
				}

				prefixString = SecondaryPrompt;
			}
			return sb.ToString();
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
			var buf = FindBuffer(textEditor.TextArea.Caret.Offset);
			var newSel = Selection.Create(this.textEditor.TextArea, buf.Kind == BufferKind.Code ? buf.StartOffset + PrimaryPrompt.Length : buf.StartOffset, buf.EndOffset);
			if (newSel.IsEmpty || this.textEditor.TextArea.Selection.Equals(newSel))
				newSel = Selection.Create(this.textEditor.TextArea, 0, LastLine.EndOffset);
			this.textEditor.TextArea.Selection = newSel;
			// We don't move the caret to the end of the selection because then we can't press
			// Ctrl+A again to toggle between selecting all or just the current buffer.
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
				var startLine = this.textEditor.TextArea.TextView.Document.GetLineByOffset(start);
				var endLine = this.textEditor.TextArea.TextView.Document.GetLineByOffset(end);
				if (startLine != endLine || end > endLine.EndOffset) {
					endLine = this.textEditor.TextArea.TextView.Document.GetLineByNumber(startLine.LineNumber + 1);
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
				var line = this.textEditor.TextArea.TextView.Document.GetLineByOffset(end);
				if (line.Offset == end || FilterOffset(start) != start) {
					var prevLine = this.textEditor.TextArea.TextView.Document.GetLineByNumber(line.LineNumber - 1);
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
			if (ta.OverstrikeMode && ta.Selection.IsEmpty && ta.TextView.Document.GetLineByNumber(ta.Caret.Line).EndOffset > ta.Caret.Offset)
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
					sb.Append(SecondaryPrompt);
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

		public bool CanClear => true;

		public void Clear() {
			if (!CanClear)
				return;
			ClearPendingOutput();
			AddNewDocument();
			ClearUndoRedoHistory();
			bool hasPrompt = offsetOfPrompt != null;
			WriteOffsetOfPrompt(null, true);
			if (hasPrompt)
				PrintPrompt();
		}

		void AddNewDocument() {
			docVersion++;
			prevCommandTextChangedState?.Cancel();
			subBuffers.Clear();
			scriptOutputCachedTextTokenColors = null;
			cachedColorsList.Clear();
			var doc = new TextDocument();
			doc.Changed += TextDocument_Changed;
			this.textEditor.Document = doc;
		}
		int docVersion;

		async void TextDocument_Changed(object sender, DocumentChangeEventArgs e) {
			if (!IsCommandMode)
				return;
			var buf = CreateReplCommandInput(e);
			if (buf == null)
				return;

			int baseOffset = this.offsetOfPrompt.Value;
			int totalLength = LastLine.EndOffset - baseOffset;
			int currentDocVersion = docVersion;

			prevCommandTextChangedState?.CancelIfSameVersion(commandVersion);
			var changedState = new CommandTextChangedState(commandVersion);
			prevCommandTextChangedState = changedState;

			try {
				cachedColorsList.SetAsyncUpdatingAfterChanges(baseOffset);
				await this.CommandHandler.OnCommandUpdatedAsync(buf, changedState.CancellationToken);

				if (changedState.CancellationToken.IsCancellationRequested)
					return;
				var cachedColors = new CachedTextTokenColorsCreator(this, totalLength).Create(buf.Input, buf.ColorInfos);
				Debug.Assert(cachedColors.Length == totalLength);
				if (currentDocVersion == docVersion)
					cachedColorsList.AddOrUpdate(baseOffset, cachedColors);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken.Equals(changedState.CancellationToken)) {
			}
			catch (Exception ex) {
				Debug.Fail("Exception: " + ex.Message);
				if (currentDocVersion == docVersion)
					cachedColorsList.AddOrUpdate(baseOffset, new CachedTextTokenColors());
			}
			finally {
				if (prevCommandTextChangedState == changedState)
					prevCommandTextChangedState = null;
				changedState.Dispose();
			}
		}
		CommandTextChangedState prevCommandTextChangedState;

		ReplCommandInput CreateReplCommandInput(DocumentChangeEventArgs e) {
			Debug.Assert(IsCommandMode);
			if (!IsCommandMode)
				return null;
			Debug.Assert(e.Offset >= offsetOfPrompt.Value);

			var input = CurrentInput;

			var line = this.textEditor.TextArea.TextView.Document.GetLineByOffset(e.Offset);
			var promptText = line.Offset == offsetOfPrompt.Value ? PrimaryPrompt : SecondaryPrompt;
			int offset = DocumentOffsetToInputOffset(e.Offset, line);

			var removed = DocumentTextToInputText(e.RemovedText.Text, line, e.Offset, promptText);
			var added = DocumentTextToInputText(e.InsertedText.Text, line, e.Offset, promptText);

			return new ReplCommandInput(input, offset, added, removed);
		}

		int DocumentOffsetToInputOffset(int docOffset, DocumentLine line) {
			var promptLine = this.textEditor.TextArea.TextView.Document.GetLineByOffset(offsetOfPrompt.Value);

			int offset = docOffset;
			if (line.LineNumber == promptLine.LineNumber) {
				offset -= offsetOfPrompt.Value;
				Debug.Assert(line.Length == 0 || line.Length >= PrimaryPrompt.Length);
				if (line.Length != 0)
					offset -= PrimaryPrompt.Length;
				if (offset < 0)
					offset = 0;
			}
			else {
				offset -= offsetOfPrompt.Value + PrimaryPrompt.Length;
				offset -= (line.LineNumber - promptLine.LineNumber - 1) * SecondaryPrompt.Length;
				Debug.Assert(line.Length == 0 || line.Length >= SecondaryPrompt.Length);
				if (line.Length != 0)
					offset -= SecondaryPrompt.Length;
			}
			Debug.Assert(offset >= 0);

			return offset;
		}

		string DocumentTextToInputText(string text, DocumentLine line, int offset, string promptText) {
			if (text.Length == 0)
				return text;

			int lineOffset = offset - line.Offset;
			Debug.Assert(lineOffset >= 0);
			if (lineOffset >= promptText.Length)
				promptText = string.Empty;
			else if (lineOffset != 0)
				promptText = promptText.Substring(lineOffset);

			return ToInputString(text, promptText);
		}

		public bool CanSelectPreviousCommand => IsCommandMode && replCommands.CanSelectPrevious;
		readonly ReplCommands replCommands = new ReplCommands();

		public void SelectPreviousCommand() {
			if (!CanSelectPreviousCommand)
				return;
			replCommands.SelectPrevious();
			UpdateCommand(true);
		}

		public bool CanSelectNextCommand => IsCommandMode && replCommands.CanSelectNext;

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

		void RawAppend(string text) =>
			this.textEditor.TextArea.TextView.Document.Insert(LastLine.EndOffset, text);

		void FlushScriptOutputUIThread() {
			dispatcher.VerifyAccess();

			ColorAndText[] newPendingOutput = null;
			var sb = new StringBuilder();
			lock (pendingScriptOutputLock) {
				pendingScriptOutput_dispatching = false;
				newPendingOutput = pendingScriptOutput.ToArray();
				pendingScriptOutput.Clear();
			}

			string currentCommand = null;
			bool isCommandMode = IsCommandMode;
			if (isCommandMode) {
				currentCommand = CurrentInput;
				cachedColorsList.RemoveLastCachedTextTokenColors();
				ClearCurrentInput(true);
			}
			if (newPendingOutput != null) {
				Debug.Assert(scriptOutputCachedTextTokenColors != null);
				foreach (var info in newPendingOutput) {
					sb.Append(info.Text);
					scriptOutputCachedTextTokenColors?.Append(info.Color, info.Text);
				}
				scriptOutputCachedTextTokenColors?.Flush();
			}
			RawAppend(sb.ToString());
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
		List<ColorAndText> pendingScriptOutput = new List<ColorAndText>();
		bool pendingScriptOutput_dispatching;

		void ClearPendingOutput() {
			lock (pendingScriptOutputLock) {
				pendingScriptOutput = new List<ColorAndText>();
				pendingScriptOutput_dispatching = false;
			}
		}

		void IReplEditor.OutputPrint(string text, OutputColor color, bool startOnNewLine) =>
			((IReplEditor)this).OutputPrint(text, color.Box(), startOnNewLine);

		void IReplEditor.OutputPrint(string text, object color, bool startOnNewLine) {
			if (string.IsNullOrEmpty(text))
				return;

			lock (pendingScriptOutputLock) {
				if (startOnNewLine) {
					if (pendingScriptOutput.Count > 0) {
						var last = pendingScriptOutput[pendingScriptOutput.Count - 1];
						if (last.Text.Length > 0 && last.Text[last.Text.Length - 1] != '\n')
							pendingScriptOutput.Add(new ColorAndText(BoxedOutputColor.Text, Environment.NewLine));
					}
					else if (LastLine.Length != 0)
						pendingScriptOutput.Add(new ColorAndText(BoxedOutputColor.Text, Environment.NewLine));
				}
				pendingScriptOutput.Add(new ColorAndText(color, text));
			}

			FlushScriptOutput();
		}

		void IReplEditor.OutputPrintLine(string text, OutputColor color, bool startOnNewLine) =>
			((IReplEditor)this).OutputPrint(text + Environment.NewLine, color.Box(), startOnNewLine);

		void IReplEditor.OutputPrintLine(string text, object color, bool startOnNewLine) =>
			((IReplEditor)this).OutputPrint(text + Environment.NewLine, color, startOnNewLine);

		void IReplEditor.OutputPrint(IEnumerable<ColorAndText> text) {
			lock (pendingScriptOutputLock)
				pendingScriptOutput.AddRange(text);
			FlushScriptOutput();
		}

		sealed class ReplCommandHandler : IReplCommandHandler {
			public static readonly IReplCommandHandler Null = new ReplCommandHandler();
			public void ExecuteCommand(string input) { }
			public bool IsCommand(string text) => false;
			public void OnNewCommand() { }
			public Task OnCommandUpdatedAsync(IReplCommandInput command, CancellationToken cancellationToken) => Task.CompletedTask;
		}

		public IReplCommandHandler CommandHandler {
			get { return replCommandHandler ?? ReplCommandHandler.Null; }
			set { replCommandHandler = value; }
		}
		IReplCommandHandler replCommandHandler;

		void ClearUndoRedoHistory() => this.textEditor.TextArea.TextView.Document.UndoStack.ClearAll();

		DocumentLine LastLine {
			get {
				var doc = this.textEditor.TextArea.TextView.Document;
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

		public void OnCommandExecuted() => PrintPrompt();

		void PrintPrompt() {
			// Can happen if we reset the script and it throws an OperationCanceledException
			if (offsetOfPrompt != null)
				return;

			CreateEmptyLastLineIfNeededAndMoveCaret();
			AddOrUpdateOutputSubBuffer();
			WriteOffsetOfPrompt(LastLine.EndOffset);
			CommandHandler.OnNewCommand();
			RawAppend(PrimaryPrompt);
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

		/// <summary>
		/// true if we're reading user input and new user commands get executed when enter is pressed
		/// </summary>
		bool IsCommandMode => offsetOfPrompt != null;

		/// <summary>
		/// true if the script is executing and we don't accept any user input
		/// </summary>
		bool IsExecMode => offsetOfPrompt == null;

		public bool CanCopyCode => !textEditor.TextArea.Selection.IsEmpty;

		public void CopyCode() {
			if (!CanCopyCode)
				return;

			int startOffset = textEditor.TextArea.TextView.Document.GetOffset(textEditor.TextArea.Selection.StartPosition.Location);
			int endOffset = textEditor.TextArea.TextView.Document.GetOffset(textEditor.TextArea.Selection.EndPosition.Location);
			if (startOffset > endOffset) {
				var tmp = startOffset;
				startOffset = endOffset;
				endOffset = tmp;
			}
			Debug.Assert(endOffset > startOffset);
			if (endOffset <= startOffset)
				return;

			var sb = new StringBuilder();
			foreach (var buf in AllSubBuffers)
				AddCode(sb, buf, startOffset, endOffset);
			if (sb.Length > 0) {
				try {
					Clipboard.SetText(sb.ToString());
				}
				catch (ExternalException) { }
			}
		}

		void AddCode(StringBuilder sb, SubBuffer buf, int startOffset, int endOffset) {
			if (buf.Kind != BufferKind.Code)
				return;
			startOffset = Math.Max(startOffset, buf.StartOffset);
			endOffset = Math.Min(endOffset, buf.EndOffset);
			if (startOffset >= endOffset)
				return;

			var firstLine = textEditor.TextArea.TextView.Document.GetLineByOffset(buf.StartOffset);
			var startLine = textEditor.TextArea.TextView.Document.GetLineByOffset(startOffset);
			var prompt = firstLine == startLine ? PrimaryPrompt : SecondaryPrompt;

			int offs = startOffset;
			while (offs < endOffset) {
				var line = textEditor.TextArea.TextView.Document.GetLineByOffset(offs);
				int skipChars = offs - line.Offset;
				if (skipChars < prompt.Length)
					offs += prompt.Length - skipChars;
				int eol = line.EndOffset + line.DelimiterLength;
				int end = eol;
				if (end >= endOffset)
					end = endOffset;
				if (offs >= end)
					break;
				var s = textEditor.TextArea.TextView.Document.GetText(offs, end - offs);
				sb.Append(s);

				offs = eol;
				prompt = SecondaryPrompt;
			}
		}

		IEnumerable<SubBuffer> AllSubBuffers {
			get {
				foreach (var b in subBuffers)
					yield return b;
				yield return ActiveSubBuffer;
			}
		}

		SubBuffer ActiveSubBuffer {
			get {
				int startOffset = subBuffers.Count == 0 ? 0 : subBuffers[subBuffers.Count - 1].EndOffset;
				int endOffset = LastLine.EndOffset;
				return new SubBuffer(IsCommandMode ? BufferKind.Code : BufferKind.Output, startOffset, endOffset);
			}
		}

		public void Dispose() {
			if (!wpfTextView.IsClosed)
				wpfTextView.Close();
			textEditor.Dispose();
		}

		enum BufferKind {
			Output,
			Code,
		}

		sealed class SubBuffer {
			public BufferKind Kind { get; }
			public int StartOffset { get; }
			/// <summary>
			/// End offset, not inclusive
			/// </summary>
			public int EndOffset { get; set; }
			public int Length => EndOffset - StartOffset;

			public SubBuffer(BufferKind kind, int startOffset, int endOffset) {
				Kind = kind;
				StartOffset = startOffset;
				EndOffset = endOffset;
			}
		}

		/// <summary>
		/// Doesn't include the active buffer, use <see cref="AllSubBuffers"/> instead
		/// </summary>
		readonly List<SubBuffer> subBuffers;

		void AddSubBuffer(SubBuffer buffer) {
			Debug.Assert(subBuffers.Count == 0 || subBuffers[subBuffers.Count - 1].EndOffset == buffer.StartOffset);
			// AddOrUpdateOutputSubBuffer() should be called to merge output sub buffers
			Debug.Assert(buffer.Kind == BufferKind.Code || subBuffers.Count == 0 || subBuffers[subBuffers.Count - 1].Kind != BufferKind.Output);
			Debug.Assert(textEditor.TextArea.TextView.Document.GetLineByOffset(buffer.StartOffset).Offset == buffer.StartOffset);
			Debug.Assert(textEditor.TextArea.TextView.Document.GetLineByOffset(buffer.EndOffset).Offset == buffer.EndOffset);
			if (buffer.Kind == BufferKind.Output && buffer.Length == 0)
				return;
			subBuffers.Add(buffer);
		}

		void AddCodeSubBuffer() {
			Debug.Assert(offsetOfPrompt != null);
			Debug.Assert(LastLine.Length == 0);
			AddSubBuffer(new SubBuffer(BufferKind.Code, offsetOfPrompt.Value, LastLine.Offset));
		}

		/// <summary>
		/// If the previous completed sub buffer is an output sub buffer, update it to include the
		/// new output, else create a new one.
		/// </summary>
		void AddOrUpdateOutputSubBuffer() {
			Debug.Assert(offsetOfPrompt == null);
			Debug.Assert(LastLine.Length == 0);

			int startOffset = subBuffers.Count == 0 ? 0 : subBuffers[subBuffers.Count - 1].EndOffset;
			int endOffset = LastLine.Offset;

			if (subBuffers.Count > 0 && subBuffers[subBuffers.Count - 1].Kind == BufferKind.Output) {
				var buf = subBuffers[subBuffers.Count - 1];
				Debug.Assert(buf.EndOffset == startOffset);
				buf.EndOffset = endOffset;
			}
			else
				AddSubBuffer(new SubBuffer(BufferKind.Output, startOffset, LastLine.Offset));
		}

		SubBuffer FindBuffer(int offset) {
			foreach (var buf in AllSubBuffers) {
				if (buf.StartOffset <= offset && offset < buf.EndOffset)
					return buf;
			}
			var active = ActiveSubBuffer;
			if (active.StartOffset <= offset && offset <= active.EndOffset)
				return active;
			Debug.Fail("Couldn't find a buffer");
			return active;
		}
	}

	struct CachedTextTokenColorsCreator {
		static readonly char[] newLineChars = new char[] { '\r', '\n' };
		readonly ReplEditorUI owner;
		readonly CachedTextTokenColors cachedColors;
		readonly int totalLength;

		public CachedTextTokenColorsCreator(ReplEditorUI owner, int totalLength) {
			this.owner = owner;
			this.cachedColors = new CachedTextTokenColors();
			this.totalLength = totalLength;
		}

		public CachedTextTokenColors Create(string command, List<ColorOffsetInfo> colorInfos) {
			cachedColors.Append(BoxedOutputColor.ReplPrompt1, owner.PrimaryPrompt);
			int cmdOffs = 0;
			foreach (var cinfo in colorInfos) {
				Debug.Assert(cmdOffs <= cinfo.Offset);
				if (cmdOffs < cinfo.Offset)
					Append(BoxedOutputColor.Text, command, cmdOffs, cinfo.Offset - cmdOffs);
				Append(cinfo.Color, command, cinfo.Offset, cinfo.Length);
				cmdOffs = cinfo.Offset + cinfo.Length;
			}
			if (cmdOffs < command.Length)
				Append(BoxedOutputColor.Text, command, cmdOffs, command.Length - cmdOffs);

			cachedColors.Finish();
			return cachedColors;
		}

		void Append(object color, string s, int offset, int length) {
			int so = offset;
			int end = offset + length;
			while (so < end) {
				int nlOffs = s.IndexOfAny(newLineChars, so, end - so);
				if (nlOffs >= 0) {
					int nlLen = s[nlOffs] == '\r' && nlOffs + 1 < end && s[nlOffs + 1] == '\n' ? 2 : 1;
					cachedColors.Append(color, s, so, nlOffs - so + nlLen);
					so = nlOffs + nlLen;
					if (cachedColors.Length < totalLength)
						cachedColors.Append(BoxedOutputColor.ReplPrompt2, owner.SecondaryPrompt);
				}
				else {
					cachedColors.Append(color, s, so, end - so);
					break;
				}
			}
		}
	}

	sealed class CommandTextChangedState : IDisposable {
		public CancellationToken CancellationToken => cancellationTokenSource.Token;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly int version;
		bool hasDisposed;

		public CommandTextChangedState(int version) {
			this.cancellationTokenSource = new CancellationTokenSource();
			this.version = version;
		}

		public void Cancel() {
			Debug.Assert(!hasDisposed);
			cancellationTokenSource.Cancel();
		}

		public void CancelIfSameVersion(int version) {
			Debug.Assert(!hasDisposed);
			if (this.version == version)
				cancellationTokenSource.Cancel();
		}

		public void Dispose() {
			cancellationTokenSource.Dispose();
			hasDisposed = true;
		}
	}

	sealed class ReplCommandInput : IReplCommandInput {
		public string Input { get; }
		public ITextChange[] Changes { get; }

		public List<ColorOffsetInfo> ColorInfos { get; } = new List<ColorOffsetInfo>();

		public ReplCommandInput(string input, int offset, string added, string removed) {
			Input = input;
			Changes = new ITextChange[] { new TextChange(offset, removed, added) };
		}

		public void AddColor(int offset, int length, object color) =>
			AddColor(new ColorOffsetInfo(offset, length, color));

		public void AddColor(int offset, int length, OutputColor color) =>
			AddColor(new ColorOffsetInfo(offset, length, color));

		public void AddColor(ColorOffsetInfo info) {
#if DEBUG
			Debug.Assert(info.Offset >= nextMinOffset);
			if (info.Offset < nextMinOffset)
				throw new InvalidOperationException();
			nextMinOffset = info.Offset + info.Length;
#endif
			ColorInfos.Add(info);
		}
#if DEBUG
		int nextMinOffset;
#endif

		public void AddColors(IEnumerable<ColorOffsetInfo> infos) {
			foreach (var info in infos)
				AddColor(info);
		}
	}
}
