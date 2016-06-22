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
using System.Windows.Threading;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Text;
using dnSpy.Text.Editor.Operations;

namespace dnSpy.Text.Editor {
	sealed class ReplEditor : IReplEditor2 {
		public object UIObject => wpfTextView.VisualElement;
		public IInputElement FocusedElement => wpfTextView.VisualElement;
		public FrameworkElement ScaleElement => wpfTextView.VisualElement;
		public object Tag { get; set; }
		public IReplEditorOperations ReplEditorOperations { get; }
		public ICommandTargetCollection CommandTarget => wpfTextView.CommandTarget;
		public ITextView TextView => wpfTextView;

		public string PrimaryPrompt { get; }
		public string SecondaryPrompt { get; }

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
			this.subBuffers = new List<ReplSubBuffer>();
			this.cachedColorsList = new CachedColorsList();

			var contentType = contentTypeRegistryService.GetContentType((object)options.ContentType ?? options.ContentTypeGuid) ?? textBufferFactoryService.TextContentType;
			var textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			CachedColorsListColorizerProvider.AddColorizer(textBuffer, cachedColorsList);
			var roles = textEditorFactoryService2.CreateTextViewRoleSet(options.Roles);
			var wpfTextView = textEditorFactoryService2.CreateTextView(textBuffer, roles, options, () => new GuidObjectsCreator(this));
			ReplEditorUtils.AddInstance(this, wpfTextView);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.REPL);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.DragDropEditingId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
			//TODO: ReplEditorOperations doesn't support virtual space
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, false);
			//TODO: Support box selection
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.AllowBoxSelectionId, false);
			this.wpfTextView = wpfTextView;
			this.wpfTextView.TextBuffer.Changed += TextBuffer_Changed;
			this.textEditor = wpfTextView.DnSpyTextEditor;
			AddNewDocument();
			WriteOffsetOfPrompt(null, true);
			ReplEditorOperations = new ReplEditorOperations(this, wpfTextView);
			wpfTextView.VisualElement.Loaded += WpfTextView_Loaded;
		}

		void MoveToEnd() => MoveTo(wpfTextView.TextSnapshot.Length);
		void MoveTo(int offset) => wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, offset));
		int CaretOffset => wpfTextView.Caret.Position.BufferPosition.Position;

		void WriteOffsetOfPrompt(int? newValue, bool force = false) {
			if (force || OffsetOfPrompt.HasValue != newValue.HasValue) {
				if (newValue == null) {
					Debug.Assert(scriptOutputCachedTextTokenColors == null);
					scriptOutputCachedTextTokenColors = new CachedTextTokenColors();
					Debug.Assert(LastLine.Length == 0);
					cachedColorsList.AddOrUpdate(wpfTextView.TextSnapshot.Length, scriptOutputCachedTextTokenColors);
				}
				else {
					Debug.Assert(scriptOutputCachedTextTokenColors != null);
					scriptOutputCachedTextTokenColors?.Flush();
					scriptOutputCachedTextTokenColors = null;
				}
			}
			OffsetOfPrompt = newValue;
		}
		CachedTextTokenColors scriptOutputCachedTextTokenColors;

		public int? OffsetOfPrompt { get; private set; }

		public void Reset() {
			ClearPendingOutput();
			ClearUndoRedoHistory();
			CreateEmptyLastLineIfNeededAndMoveCaret();
			if (OffsetOfPrompt != null)
				AddCodeSubBuffer();
			scriptOutputCachedTextTokenColors = null;
			WriteOffsetOfPrompt(null, true);
		}

		public bool IsAtEditingPosition {
			get {
				if (!IsCommandMode)
					return false;
				return CaretOffset >= OffsetOfPrompt.Value;
			}
		}

		public int FilterOffset(int offset) {
			Debug.Assert(OffsetOfPrompt != null);
			if (offset < OffsetOfPrompt.Value)
				offset = OffsetOfPrompt.Value;
			var line = wpfTextView.TextSnapshot.GetLineFromPosition(offset);
			var prefixString = line.Start.Position == OffsetOfPrompt.Value ? PrimaryPrompt : SecondaryPrompt;
			int col = offset - line.Start.Position;
			if (col < prefixString.Length)
				offset = line.Start.Position + prefixString.Length;
			if (offset > wpfTextView.TextSnapshot.Length)
				offset = wpfTextView.TextSnapshot.Length;
			return offset;
		}

		public void ClearInput() => ClearCurrentInput(false);
		void ClearCurrentInput(bool removePrompt) {
			if (!IsCommandMode)
				return;
			int offs = removePrompt ? OffsetOfPrompt.Value : FilterOffset(OffsetOfPrompt.Value);
			MoveTo(offs);
			var span = Span.FromBounds(offs, wpfTextView.TextSnapshot.Length);

			if (removePrompt) {
				var oldValue = OffsetOfPrompt;
				OffsetOfPrompt = null;

				wpfTextView.TextBuffer.Delete(span);
				wpfTextView.Caret.EnsureVisible();
				SearchText = null;

				OffsetOfPrompt = oldValue;
				WriteOffsetOfPrompt(null);
			}
			else {
				wpfTextView.TextBuffer.Delete(span);
				wpfTextView.Caret.EnsureVisible();
				SearchText = null;
			}
		}

		public bool TrySubmit(bool force) {
			var input = CurrentInput;
			bool isCmd = force || this.CommandHandler.IsCommand(input);
			if (!isCmd)
				return false;

			SearchText = null;
			if (!string.IsNullOrEmpty(input))
				replCommands.Add(input);
			RawAppend(Environment.NewLine);
			MoveToEnd();
			wpfTextView.Caret.EnsureVisible();
			AddCodeSubBuffer();
			WriteOffsetOfPrompt(null);
			ClearUndoRedoHistory();
			wpfTextView.Caret.EnsureVisible();
			commandVersion++;
			this.CommandHandler.ExecuteCommand(input);
			return true;
		}
		int commandVersion = 0;

		string CurrentInput {
			get {
				Debug.Assert(IsCommandMode);
				if (!IsCommandMode)
					return string.Empty;

				string s = wpfTextView.TextBuffer.CurrentSnapshot.GetText(OffsetOfPrompt.Value, wpfTextView.TextSnapshot.Length - OffsetOfPrompt.Value);
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
		internal static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		public bool CanClearScreen => true;
		public void ClearScreen() {
			if (!CanClearScreen)
				return;
			ClearPendingOutput();
			bool hasPrompt = OffsetOfPrompt != null;
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
			OffsetOfPrompt = null;
			wpfTextView.TextBuffer.Replace(new Span(0, wpfTextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
			ClearUndoRedoHistory();
		}
		int docVersion;

		async void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
			if (!IsCommandMode)
				return;
			var buf = CreateReplCommandInput(e);
			if (buf == null)
				return;

			int baseOffset = this.OffsetOfPrompt.Value;
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
			if (e.Changes.Count == 0)
				return null;
			return new ReplCommandInput(CurrentInput);
		}

		readonly ReplCommands replCommands = new ReplCommands();
		public bool CanSelectPreviousCommand => IsCommandMode && replCommands.CanSelectPrevious;
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

		public string SearchText {
			get { return searchText ?? (searchText = CurrentInput); }
			set { searchText = value; }
		}
		string searchText = string.Empty;

		public void SelectSameTextPreviousCommand() {
			if (!IsCommandMode)
				return;

			replCommands.SelectPrevious(SearchText);
			UpdateCommand(false);
		}

		public void SelectSameTextNextCommand() {
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
			ReplEditorOperations.AddUserInput(Span.FromBounds(FilterOffset(OffsetOfPrompt.Value), wpfTextView.TextSnapshot.Length), command, clearSearchText);
		}

		void RawAppend(string text) =>
			wpfTextView.TextBuffer.Insert(wpfTextView.TextSnapshot.Length, text);

		void FlushScriptOutputUIThread() {
			dispatcher.VerifyAccess();

			var caretPos = wpfTextView.Caret.Position;
			bool caretIsInEditingArea = OffsetOfPrompt != null && CaretOffset >= OffsetOfPrompt.Value;

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
				ReplEditorOperations.AddUserInput(currentCommand);

				if (caretIsInEditingArea) {
					var newPos = new SnapshotPoint(wpfTextView.TextSnapshot, caretPos.BufferPosition.Position + sb.Length + extraLen);
					wpfTextView.Caret.MoveTo(newPos, caretPos.Affinity);
				}
			}

			wpfTextView.Caret.EnsureVisible();
		}

		// This fixes a problem when one or more lines have been added to the window before it's fully
		// visible. The first lines won't be shown at all (you have to scroll up to see them). I could
		// only reproduce it at startup when the REPL editor was the active tool window and at least one
		// other decompilation tab was opened at the same time.
		void WpfTextView_Loaded(object sender, RoutedEventArgs e) {
			wpfTextView.VisualElement.Loaded -= WpfTextView_Loaded;
			if (wpfTextView.TextSnapshot.Length != 0) {
				wpfTextView.DisplayTextLineContainingBufferPosition(new SnapshotPoint(wpfTextView.TextSnapshot, 0), 0, ViewRelativePosition.Top);
				wpfTextView.Caret.EnsureVisible();
			}
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
			if (OffsetOfPrompt != null)
				return;

			CreateEmptyLastLineIfNeededAndMoveCaret();
			AddOrUpdateOutputSubBuffer();
			WriteOffsetOfPrompt(wpfTextView.TextSnapshot.Length);
			CommandHandler.OnNewCommand();
			RawAppend(PrimaryPrompt);
			ClearUndoRedoHistory();
			MoveToEnd();
			wpfTextView.Caret.EnsureVisible();
		}

		/// <summary>
		/// true if we're reading user input and new user commands get executed when enter is pressed
		/// </summary>
		bool IsCommandMode => OffsetOfPrompt != null;

		/// <summary>
		/// true if the script is executing and we don't accept any user input
		/// </summary>
		bool IsExecMode => OffsetOfPrompt == null;

		public bool CanCopyCode => !wpfTextView.Selection.IsEmpty;
		public void CopyCode() {
			if (!CanCopyCode)
				return;

			int startOffset = wpfTextView.Selection.Start.Position;
			int endOffset = wpfTextView.Selection.End.Position;
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

		void AddCode(StringBuilder sb, ReplSubBuffer buf, int startOffset, int endOffset) {
			if (buf.Kind != ReplBufferKind.Code)
				return;
			startOffset = Math.Max(startOffset, buf.Span.Start);
			endOffset = Math.Min(endOffset, buf.Span.End);
			if (startOffset >= endOffset)
				return;

			var firstLine = wpfTextView.TextSnapshot.GetLineFromPosition(buf.Span.Start);
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

		IEnumerable<ReplSubBuffer> AllSubBuffers {
			get {
				foreach (var b in subBuffers)
					yield return b;
				yield return ActiveSubBuffer;
			}
		}

		ReplSubBuffer ActiveSubBuffer {
			get {
				int startOffset = subBuffers.Count == 0 ? 0 : subBuffers[subBuffers.Count - 1].Span.End;
				int endOffset = wpfTextView.TextSnapshot.Length;
				return new ReplSubBuffer(IsCommandMode ? ReplBufferKind.Code : ReplBufferKind.Output, startOffset, endOffset);
			}
		}

		public void Dispose() {
			if (!wpfTextView.IsClosed)
				wpfTextView.Close();
		}

		/// <summary>
		/// Doesn't include the active buffer, use <see cref="AllSubBuffers"/> instead
		/// </summary>
		readonly List<ReplSubBuffer> subBuffers;

		void AddSubBuffer(ReplSubBuffer buffer) {
			Debug.Assert(subBuffers.Count == 0 || subBuffers[subBuffers.Count - 1].Span.End == buffer.Span.Start);
			// AddOrUpdateOutputSubBuffer() should be called to merge output sub buffers
			Debug.Assert(buffer.Kind == ReplBufferKind.Code || subBuffers.Count == 0 || subBuffers[subBuffers.Count - 1].Kind != ReplBufferKind.Output);
			Debug.Assert(wpfTextView.TextSnapshot.GetLineFromPosition(buffer.Span.Start).Start.Position == buffer.Span.Start);
			Debug.Assert(wpfTextView.TextSnapshot.GetLineFromPosition(buffer.Span.End).Start.Position == buffer.Span.End);
			if (buffer.Kind == ReplBufferKind.Output && buffer.Span.Length == 0)
				return;
			subBuffers.Add(buffer);
		}

		void AddCodeSubBuffer() {
			Debug.Assert(OffsetOfPrompt != null);
			Debug.Assert(LastLine.Length == 0);
			AddSubBuffer(new ReplSubBuffer(ReplBufferKind.Code, OffsetOfPrompt.Value, LastLine.Start.Position));
		}

		/// <summary>
		/// If the previous completed sub buffer is an output sub buffer, update it to include the
		/// new output, else create a new one.
		/// </summary>
		void AddOrUpdateOutputSubBuffer() {
			Debug.Assert(OffsetOfPrompt == null);
			Debug.Assert(LastLine.Length == 0);

			int start = subBuffers.Count == 0 ? 0 : subBuffers[subBuffers.Count - 1].Span.End;
			int end = LastLine.Start.Position;

			if (subBuffers.Count > 0 && subBuffers[subBuffers.Count - 1].Kind == ReplBufferKind.Output) {
				var buf = subBuffers[subBuffers.Count - 1];
				Debug.Assert(buf.Span.End == start);
				subBuffers[subBuffers.Count - 1] = new ReplSubBuffer(buf.Kind, buf.Span.Start, end);
			}
			else
				AddSubBuffer(new ReplSubBuffer(ReplBufferKind.Output, start, LastLine.Start.Position));
		}

		public ReplSubBuffer FindBuffer(int offset) {
			foreach (var buf in AllSubBuffers) {
				if (buf.Span.Start <= offset && offset < buf.Span.End)
					return buf;
			}
			var active = ActiveSubBuffer;
			if (active.Span.Start <= offset && offset <= active.Span.End)
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
