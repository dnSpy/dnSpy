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
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Text.Editor {
	sealed class ReplEditor : IReplEditor {
		public object UIObject => wpfTextView.UIObject;
		public IInputElement FocusedElement => wpfTextView.FocusedElement;
		public FrameworkElement ScaleElement => wpfTextView.ScaleElement;
		public object Tag { get; set; }

		internal string PrimaryPrompt { get; }
		internal string SecondaryPrompt { get; }

		readonly DnSpyTextEditor textEditor;
		readonly Dispatcher dispatcher;
		readonly CachedColorsList cachedColorsList;
		readonly IWpfTextView wpfTextView;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly ReplEditor replEditorUI;

			public GuidObjectsCreator(ReplEditor replEditorUI) {
				this.replEditorUI = replEditorUI;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_REPL_EDITOR_GUID, replEditorUI);
			}
		}

		public ReplEditor(ReplEditorOptions options, ITextEditorFactoryService2 textEditorFactoryService2, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			options = options ?? new ReplEditorOptions();
			this.PrimaryPrompt = options.PrimaryPrompt;
			this.SecondaryPrompt = options.SecondaryPrompt;
			this.subBuffers = new List<SubBuffer>();
			this.cachedColorsList = new CachedColorsList();

			var contentType = contentTypeRegistryService.GetContentType((object)options.ContentType ?? options.ContentTypeGuid) ?? textBufferFactoryService.TextContentType;
			var textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			CachedColorsListColorizerProvider.AddColorizer(textBuffer, cachedColorsList, ColorPriority.Default);
			var wpfTextView = textEditorFactoryService2.CreateTextView(textBuffer, options, () => new GuidObjectsCreator(this));
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.DragDropEditingId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.OverwriteModeId, true);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
			//TODO: Support box selection
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.RectangularSelectionId, false);
			this.wpfTextView = wpfTextView;
			this.wpfTextView.TextBuffer.Changed += TextBuffer_Changed;
			this.textEditor = wpfTextView.DnSpyTextEditor;
			AddNewDocument();
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

		void Select(int start, int end) {
			bool isReversed = start > end;
			var pos = !isReversed ?
				new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, start), new SnapshotPoint(wpfTextView.TextSnapshot, end)) :
				new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, end), new SnapshotPoint(wpfTextView.TextSnapshot, start));
			wpfTextView.Selection.Select(pos, isReversed);
		}

		void MoveToEnd() => MoveTo(wpfTextView.TextSnapshot.Length);
		void MoveTo(int offset) =>
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, offset));
		int CaretOffset => GetOffset(wpfTextView.Caret.Position.VirtualBufferPosition);
		static int GetOffset(VirtualSnapshotPoint point) => point.Position.Position;

		void WriteOffsetOfPrompt(int? newValue, bool force = false) {
			if (force || offsetOfPrompt.HasValue != newValue.HasValue) {
				if (newValue == null) {
					Debug.Assert(scriptOutputCachedTextTokenColors == null);
					scriptOutputCachedTextTokenColors = new CachedTextTokenColors();
					Debug.Assert(LastLine.Length == 0);
					cachedColorsList.AddOrUpdate(wpfTextView.TextSnapshot.Length, scriptOutputCachedTextTokenColors);
					textEditor.TextArea.TextView.Redraw(wpfTextView.TextSnapshot.Length, scriptOutputCachedTextTokenColors.Length);
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

		bool CanCutSelection => !wpfTextView.Selection.IsEmpty;

		void CutSelection() {
			if (!UpdateCaretForEdit())
				return;
			var text = wpfTextView.Selection.GetText();
			try {
				Clipboard.SetText(text);
			}
			catch (ExternalException) { return; }
			AddUserInput(string.Empty);
		}

		bool CanPaste {
			get {
				if (!this.textEditor.TextArea.ReadOnlySectionProvider.CanInsert(CaretOffset))
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
				return CaretOffset >= offsetOfPrompt.Value;
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
			MoveTo(FilterOffset(CaretOffset));

			if (!wpfTextView.Selection.IsEmpty) {
				int start = FilterOffset(GetOffset(wpfTextView.Selection.AnchorPoint));
				int end = FilterOffset(GetOffset(wpfTextView.Selection.ActivePoint));
				Select(start, end);
			}

			return true;
		}

		int FilterOffset(int offset) {
			Debug.Assert(offsetOfPrompt != null);
			if (offset < offsetOfPrompt.Value)
				offset = offsetOfPrompt.Value;
			var line = wpfTextView.TextSnapshot.GetLineFromPosition(offset);
			var prefixString = line.Start.Position == offsetOfPrompt.Value ? PrimaryPrompt : SecondaryPrompt;
			int col = offset - line.Start.Position;
			if (col < prefixString.Length)
				offset = line.Start.Position + prefixString.Length;
			if (offset > wpfTextView.TextSnapshot.Length)
				offset = wpfTextView.TextSnapshot.Length;
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
				MoveToEnd();
				wpfTextView.Selection.Clear();
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
			MoveTo(offs);
			var span = Contracts.Text.Span.FromBounds(offs, wpfTextView.TextSnapshot.Length);

			if (removePrompt) {
				var oldValue = offsetOfPrompt;
				offsetOfPrompt = null;

				wpfTextView.TextBuffer.Delete(span);
				wpfTextView.Caret.EnsureVisible();
				SearchText = null;

				offsetOfPrompt = oldValue;
				WriteOffsetOfPrompt(null);
			}
			else {
				wpfTextView.TextBuffer.Delete(span);
				wpfTextView.Caret.EnsureVisible();
				SearchText = null;
			}
		}

		bool HandleEnter(bool force) {
			if (!UpdateCaretForEdit())
				return false;
			if (CaretOffset != wpfTextView.TextSnapshot.Length)
				return true;
			if (!wpfTextView.Selection.IsEmpty)
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
			wpfTextView.Caret.EnsureVisible();
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

				string s = wpfTextView.TextBuffer.CurrentSnapshot.GetText(offsetOfPrompt.Value, wpfTextView.TextSnapshot.Length - offsetOfPrompt.Value);
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
			if (!wpfTextView.Selection.IsEmpty) {
				AddUserInput("\t");
				return false;
			}

			return true;
		}

		void HandleSelectAll() {
			var buf = FindBuffer(CaretOffset);
			var newSel = new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, buf.Kind == BufferKind.Code ? buf.StartOffset + PrimaryPrompt.Length : buf.StartOffset), new SnapshotPoint(wpfTextView.TextSnapshot, buf.EndOffset));
			if (newSel.IsEmpty || (wpfTextView.Selection.Mode == TextSelectionMode.Stream && wpfTextView.Selection.StreamSelectionSpan == new VirtualSnapshotSpan(newSel)))
				newSel = new SnapshotSpan(wpfTextView.TextSnapshot, 0, wpfTextView.TextSnapshot.Length);
			wpfTextView.Selection.Select(newSel, false);
			// We don't move the caret to the end of the selection because then we can't press
			// Ctrl+A again to toggle between selecting all or just the current buffer.
			wpfTextView.Caret.EnsureVisible();
		}

		void HandleDelete() {
			if (!UpdateCaretForEdit())
				return;
			if (!wpfTextView.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				if (CaretOffset >= wpfTextView.TextSnapshot.Length)
					return;
				int start = FilterOffset(CaretOffset);
				int end = start + 1;
				var startLine = wpfTextView.TextSnapshot.GetLineFromPosition(start);
				var endLine = wpfTextView.TextSnapshot.GetLineFromPosition(end);
				if (startLine.Start != endLine.Start || end > endLine.End.Position) {
					endLine = wpfTextView.TextSnapshot.GetLineFromLineNumber(startLine.LineNumber + 1);
					end = FilterOffset(endLine.Start.Position);
				}
				AddUserInput(start, end, string.Empty);
			}
		}

		void HandleBackspace() {
			if (!UpdateCaretForEdit())
				return;
			if (!wpfTextView.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				int start = FilterOffset(offsetOfPrompt.Value);
				int offs = CaretOffset;
				if (offs <= start)
					return;
				int end = offs;
				start = end - 1;
				var line = wpfTextView.TextSnapshot.GetLineFromPosition(end);
				if (line.Start.Position == end || FilterOffset(start) != start) {
					var prevLine = wpfTextView.TextSnapshot.GetLineFromLineNumber(line.LineNumber - 1);
					start = prevLine.End.Position;
				}
				AddUserInput(start, end, string.Empty);
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

			if (wpfTextView.Caret.OverwriteMode && wpfTextView.Selection.IsEmpty &&
				wpfTextView.TextSnapshot.GetLineFromPosition(CaretOffset).End.Position > CaretOffset) {
				EditingCommands.SelectRightByCharacter.Execute(null, textEditor.TextArea);
			}
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
		internal static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		// Always returns at least one span, even if it's empty
		IEnumerable<SnapshotSpan> GetNormalizedSpansToReplaceWithText() {
			if (wpfTextView.Selection.IsEmpty)
				yield return new SnapshotSpan(wpfTextView.TextSnapshot, new Contracts.Text.Span(CaretOffset, 0));
			else {
				var selectedSpans = wpfTextView.Selection.SelectedSpans;
				Debug.Assert(selectedSpans.Count != 0);
				foreach (var s in selectedSpans)
					yield return s;
			}
		}

		void AddUserInput(string text, bool clearSearchText = true) {
			if (!UpdateCaretForEdit())
				return;
			var s = GetNewString(text);
			var firstSpan = default(SnapshotSpan);
			using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
				foreach (var span in GetNormalizedSpansToReplaceWithText()) {
					Debug.Assert(span.Snapshot != null);
					if (firstSpan.Snapshot == null)
						firstSpan = span;
					ed.Replace(span, s);
				}
				ed.Apply();
			}
			Debug.Assert(firstSpan.Snapshot != null);
			wpfTextView.Selection.Clear();
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, firstSpan.Start.Position + s.Length));
			wpfTextView.Caret.EnsureVisible();
			if (clearSearchText)
				SearchText = null;
		}

		void AddUserInput(int start, int end, string text, bool clearSearchText = true) {
			Debug.Assert(start <= end);
			if (!UpdateCaretForEdit())
				return;

			Debug.Assert(wpfTextView.Selection.IsEmpty);
			wpfTextView.Selection.Clear();

			var s = GetNewString(text);
			wpfTextView.TextBuffer.Replace(Contracts.Text.Span.FromBounds(start, end), s);
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, start + s.Length));
			wpfTextView.Caret.EnsureVisible();
			if (clearSearchText)
				SearchText = null;
		}

		public bool CanClear => true;

		public void Clear() {
			if (!CanClear)
				return;
			ClearPendingOutput();
			bool hasPrompt = offsetOfPrompt != null;
			AddNewDocument();
			ClearUndoRedoHistory();
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
			offsetOfPrompt = null;
			wpfTextView.TextBuffer.Replace(new Contracts.Text.Span(0, wpfTextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
			ClearUndoRedoHistory();
		}
		int docVersion;

		async void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
			if (!IsCommandMode)
				return;
			var buf = CreateReplCommandInput(e);
			if (buf == null)
				return;

			int baseOffset = this.offsetOfPrompt.Value;
			int totalLength = wpfTextView.TextSnapshot.Length - baseOffset;
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

		ReplCommandInput CreateReplCommandInput(TextContentChangedEventArgs e) {
			Debug.Assert(IsCommandMode);
			if (!IsCommandMode)
				return null;
			if (e.Changes.Length == 0)
				return null;
			return new ReplCommandInput(CurrentInput);
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

			MoveToEnd();
			var currentInput = CurrentInput;
			if (currentInput.Equals(command))
				return;

			wpfTextView.Selection.Clear();
			AddUserInput(FilterOffset(offsetOfPrompt.Value), wpfTextView.TextSnapshot.Length, command, clearSearchText);
		}

		void RawAppend(string text) =>
			wpfTextView.TextBuffer.Insert(wpfTextView.TextSnapshot.Length, text);

		void FlushScriptOutputUIThread() {
			dispatcher.VerifyAccess();

			var caretPos = wpfTextView.Caret.Position;
			bool caretIsInEditingArea = offsetOfPrompt != null && GetOffset(caretPos.VirtualBufferPosition) >= offsetOfPrompt.Value;

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
			MoveToEnd();
			if (isCommandMode) {
				int posBeforeNewLine = wpfTextView.TextSnapshot.Length;
				CreateEmptyLastLineIfNeededAndMoveCaret();
				int extraLen = wpfTextView.TextSnapshot.Length - posBeforeNewLine;

				PrintPrompt();
				AddUserInput(currentCommand);

				if (caretIsInEditingArea) {
					var newPos = new SnapshotPoint(wpfTextView.TextSnapshot, caretPos.BufferPosition.Position + sb.Length + extraLen);
					wpfTextView.Caret.MoveTo(newPos, caretPos.Affinity);
				}
			}

			wpfTextView.Caret.EnsureVisible();
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

		ITextSnapshotLine LastLine {
			get {
				var line = wpfTextView.TextSnapshot.GetLineFromLineNumber(wpfTextView.TextSnapshot.LineCount - 1);
				Debug.Assert(line.Length == line.LengthIncludingLineBreak);
				return line;
			}
		}

		void CreateEmptyLastLineIfNeededAndMoveCaret() {
			if (LastLine.Length != 0)
				RawAppend(Environment.NewLine);
			MoveToEnd();
			wpfTextView.Caret.EnsureVisible();
		}

		public void OnCommandExecuted() => PrintPrompt();

		void PrintPrompt() {
			// Can happen if we reset the script and it throws an OperationCanceledException
			if (offsetOfPrompt != null)
				return;

			CreateEmptyLastLineIfNeededAndMoveCaret();
			AddOrUpdateOutputSubBuffer();
			WriteOffsetOfPrompt(wpfTextView.TextSnapshot.Length);
			CommandHandler.OnNewCommand();
			RawAppend(PrimaryPrompt);
			ClearUndoRedoHistory();
			MoveToEnd();
			wpfTextView.Caret.EnsureVisible();

			// Somehow the caret isn't shown if we have word-wrap enabled and lots of text is shown
			// so the window gets scrolled, eg. try: typeof(IntPtr).GetMethods()
			var tempPos = wpfTextView.Caret.Position;
			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (wpfTextView.Caret.Position == tempPos)
					wpfTextView.Caret.EnsureVisible();
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

		public bool CanCopyCode => !wpfTextView.Selection.IsEmpty;

		public void CopyCode() {
			if (!CanCopyCode)
				return;

			int startOffset = GetOffset(wpfTextView.Selection.Start);
			int endOffset = GetOffset(wpfTextView.Selection.End);
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

			var firstLine = wpfTextView.TextSnapshot.GetLineFromPosition(buf.StartOffset);
			var startLine = wpfTextView.TextSnapshot.GetLineFromPosition(startOffset);
			var prompt = firstLine.Start == startLine.Start ? PrimaryPrompt : SecondaryPrompt;

			int offs = startOffset;
			while (offs < endOffset) {
				var line = wpfTextView.TextSnapshot.GetLineFromPosition(offs);
				int skipChars = offs - line.Start.Position;
				if (skipChars < prompt.Length)
					offs += prompt.Length - skipChars;
				int eol = line.EndIncludingLineBreak.Position;
				int end = eol;
				if (end >= endOffset)
					end = endOffset;
				if (offs >= end)
					break;
				var s = wpfTextView.TextSnapshot.GetText(offs, end - offs);
				Debug.Assert(s.Length == end - offs);
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
				int endOffset = wpfTextView.TextSnapshot.Length;
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
			Debug.Assert(wpfTextView.TextSnapshot.GetLineFromPosition(buffer.StartOffset).Start.Position == buffer.StartOffset);
			Debug.Assert(wpfTextView.TextSnapshot.GetLineFromPosition(buffer.EndOffset).Start.Position == buffer.EndOffset);
			if (buffer.Kind == BufferKind.Output && buffer.Length == 0)
				return;
			subBuffers.Add(buffer);
		}

		void AddCodeSubBuffer() {
			Debug.Assert(offsetOfPrompt != null);
			Debug.Assert(LastLine.Length == 0);
			AddSubBuffer(new SubBuffer(BufferKind.Code, offsetOfPrompt.Value, LastLine.Start.Position));
		}

		/// <summary>
		/// If the previous completed sub buffer is an output sub buffer, update it to include the
		/// new output, else create a new one.
		/// </summary>
		void AddOrUpdateOutputSubBuffer() {
			Debug.Assert(offsetOfPrompt == null);
			Debug.Assert(LastLine.Length == 0);

			int startOffset = subBuffers.Count == 0 ? 0 : subBuffers[subBuffers.Count - 1].EndOffset;
			int endOffset = LastLine.Start.Position;

			if (subBuffers.Count > 0 && subBuffers[subBuffers.Count - 1].Kind == BufferKind.Output) {
				var buf = subBuffers[subBuffers.Count - 1];
				Debug.Assert(buf.EndOffset == startOffset);
				buf.EndOffset = endOffset;
			}
			else
				AddSubBuffer(new SubBuffer(BufferKind.Output, startOffset, LastLine.Start.Position));
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
		readonly ReplEditor owner;
		readonly CachedTextTokenColors cachedColors;
		readonly int totalLength;

		public CachedTextTokenColorsCreator(ReplEditor owner, int totalLength) {
			this.owner = owner;
			this.cachedColors = new CachedTextTokenColors();
			this.totalLength = totalLength;
		}

		public CachedTextTokenColors Create(string command, List<ColorOffsetInfo> colorInfos) {
			if (owner.PrimaryPrompt.Length > totalLength)
				cachedColors.Append(BoxedOutputColor.ReplPrompt1, owner.PrimaryPrompt.Substring(0, totalLength));
			else
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
				int nlOffs = s.IndexOfAny(ReplEditor.newLineChars, so, end - so);
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

		public List<ColorOffsetInfo> ColorInfos { get; } = new List<ColorOffsetInfo>();

		public ReplCommandInput(string input) {
			Input = input;
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
