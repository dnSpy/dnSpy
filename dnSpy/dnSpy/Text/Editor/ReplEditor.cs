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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using dnSpy.Text.Editor.Operations;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	sealed class ReplEditor : IReplEditor2 {
		public object UIObject => wpfTextViewHost.HostControl;
		public IInputElement FocusedElement => wpfTextView.VisualElement;
		public FrameworkElement ScaleElement => wpfTextView.VisualElement;
		public object Tag { get; set; }
		public IReplEditorOperations ReplEditorOperations { get; }
		public ICommandTargetCollection CommandTarget => wpfTextView.CommandTarget;
		public IDnSpyWpfTextView TextView => wpfTextViewHost.TextView;
		public IDnSpyWpfTextViewHost TextViewHost => wpfTextViewHost;

		public string PrimaryPrompt { get; }
		public string SecondaryPrompt { get; }
		public IClassificationType TextClassificationType { get; }
		public IClassificationType ReplPrompt1ClassificationType { get; }
		public IClassificationType ReplPrompt2ClassificationType { get; }

		readonly Dispatcher dispatcher;
		readonly CachedColorsList cachedColorsList;
		readonly IDnSpyWpfTextViewHost wpfTextViewHost;
		readonly IDnSpyWpfTextView wpfTextView;
		readonly IPickSaveFilename pickSaveFilename;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly ReplEditor replEditorUI;

			public GuidObjectsProvider(ReplEditor replEditorUI) {
				this.replEditorUI = replEditorUI;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_REPL_EDITOR_GUID, replEditorUI);
			}
		}

		public ReplEditor(ReplEditorOptions options, IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService, IEditorOperationsFactoryService editorOperationsFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService, IClassificationTypeRegistryService classificationTypeRegistryService, IThemeClassificationTypes themeClassificationTypes, IPickSaveFilename pickSaveFilename) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.pickSaveFilename = pickSaveFilename;
			options = options?.Clone() ?? new ReplEditorOptions();
			options.CreateGuidObjects = CommonGuidObjectsProvider.Create(options.CreateGuidObjects, new GuidObjectsProvider(this));
			this.PrimaryPrompt = options.PrimaryPrompt;
			this.SecondaryPrompt = options.SecondaryPrompt;
			this.subBuffers = new List<ReplSubBuffer>();
			this.cachedColorsList = new CachedColorsList();
			TextClassificationType = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Text);
			ReplPrompt1ClassificationType = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplPrompt1);
			ReplPrompt2ClassificationType = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplPrompt2);

			var contentType = contentTypeRegistryService.GetContentType(options.ContentType, options.ContentTypeString) ?? textBufferFactoryService.TextContentType;
			var textBuffer = textBufferFactoryService.CreateTextBuffer(contentType);
			CachedColorsListTaggerProvider.AddColorizer(textBuffer, cachedColorsList);
			var roles = dnSpyTextEditorFactoryService.CreateTextViewRoleSet(options.Roles);
			var textView = dnSpyTextEditorFactoryService.CreateTextView(textBuffer, roles, editorOptionsFactoryService.GlobalOptions, options);
			var wpfTextViewHost = dnSpyTextEditorFactoryService.CreateTextViewHost(textView, false);
			this.wpfTextViewHost = wpfTextViewHost;
			this.wpfTextView = wpfTextViewHost.TextView;
			ReplEditorUtils.AddInstance(this, wpfTextView);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.REPL);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.DragDropEditingId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
			//TODO: ReplEditorOperations doesn't support virtual space
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, false);
			//TODO: Support box selection
			wpfTextView.Options.SetOptionValue(DefaultDnSpyTextViewOptions.AllowBoxSelectionId, false);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStylesConstants.DefaultValue);
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			wpfTextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			wpfTextView.Closed += WpfTextView_Closed;
			this.wpfTextView.TextBuffer.Changed += TextBuffer_Changed;
			AddNewDocument();
			WriteOffsetOfPrompt(null, true);
			ReplEditorOperations = new ReplEditorOperations(this, wpfTextView, editorOperationsFactoryService);
			wpfTextView.VisualElement.Loaded += WpfTextView_Loaded;
			UpdateRefreshScreenOnChange();
			CustomLineNumberMargin.SetOwner(wpfTextView, new ReplCustomLineNumberMarginOwner(this, themeClassificationTypes));
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			StopRefreshTimer();
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
			wpfTextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			wpfTextView.Closed -= WpfTextView_Closed;
			this.wpfTextView.TextBuffer.Changed -= TextBuffer_Changed;
			wpfTextView.VisualElement.Loaded -= WpfTextView_Loaded;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultReplEditorOptions.RefreshScreenOnChangeId.Name)
				UpdateRefreshScreenOnChange();
		}

		void UpdateRefreshScreenOnChange() {
			bool refresh = wpfTextView.Options.IsReplRefreshScreenOnChangeEnabled();
			if (!refresh)
				StopRefreshTimer();
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) {
			wpfTextView.VisualElement.Dispatcher.VerifyAccess();
			if (ChangedUserInput(e))
				DelayScreenRefresh();
		}

		bool ChangedUserInput(TextContentChangedEventArgs e) {
			if (OffsetOfPrompt == null)
				return false;
			int offs = OffsetOfPrompt.Value;
			foreach (var c in e.Changes) {
				if (c.OldEnd > offs)
					return true;
			}
			return false;
		}

		void StopRefreshTimer() {
			screenRefreshTimer?.Stop();
			screenRefreshTimer = null;
		}

		void DelayScreenRefresh() {
			if (wpfTextViewHost.IsClosed)
				return;
			if (screenRefreshTimer != null)
				return;
			int ms = wpfTextView.Options.GetReplRefreshScreenOnChangeWaitMilliSeconds();
			if (ms > 0)
				screenRefreshTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(ms), DispatcherPriority.Normal, RefreshScreenHandler, wpfTextView.VisualElement.Dispatcher);
			else
				RefreshScreen();
		}
		DispatcherTimer screenRefreshTimer;

		void RefreshScreen() {
			if (OffsetOfPrompt == null)
				return;
			int offs = OffsetOfPrompt.Value;
			var snapshot = wpfTextView.TextSnapshot;
			wpfTextView.InvalidateClassifications(new SnapshotSpan(snapshot, Span.FromBounds(offs, snapshot.Length)));
		}

		void RefreshScreenHandler(object sender, EventArgs e) {
			StopRefreshTimer();
			RefreshScreen();
		}

		void MoveToEnd() => MoveTo(wpfTextView.TextSnapshot.Length);
		void MoveTo(int offset) => wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, offset));
		int CaretOffset => wpfTextView.Caret.Position.BufferPosition.Position;

		void WriteOffsetOfPrompt(int? newValue, bool force = false) {
			if (force || OffsetOfPrompt.HasValue != newValue.HasValue) {
				if (newValue == null) {
					Debug.Assert(scriptOutputCachedTextColorsCollection == null);
					scriptOutputCachedTextColorsCollection = new CachedTextColorsCollection();
					Debug.Assert(LastLine.Length == 0);
					cachedColorsList.AddOrUpdate(wpfTextView.TextSnapshot.Length, scriptOutputCachedTextColorsCollection);
				}
				else {
					Debug.Assert(scriptOutputCachedTextColorsCollection != null);
					scriptOutputCachedTextColorsCollection?.Flush();
					scriptOutputCachedTextColorsCollection = null;
				}
			}
			OffsetOfPrompt = newValue;
		}
		CachedTextColorsCollection scriptOutputCachedTextColorsCollection;

		public int? OffsetOfPrompt { get; private set; }

		public void Reset() {
			ClearPendingOutput();
			ClearUndoRedoHistory();
			CreateEmptyLastLineIfNeededAndMoveCaret();
			if (OffsetOfPrompt != null)
				AddCodeSubBuffer();
			scriptOutputCachedTextColorsCollection = null;
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
			wpfTextView.Selection.Clear();
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

		public bool CanSaveText => true;
		public void SaveText(string filenameNoExtension, string fileExtension, string filesFilter) {
			if (filenameNoExtension == null)
				throw new ArgumentNullException(nameof(filenameNoExtension));
			if (fileExtension == null)
				throw new ArgumentNullException(nameof(fileExtension));
			if (!CanSaveText)
				return;
			SaveToFile(filenameNoExtension, fileExtension, filesFilter, TextView.TextSnapshot.GetText());
		}

		public bool CanSaveCode => true;
		public void SaveCode(string filenameNoExtension, string fileExtension, string filesFilter) {
			if (filenameNoExtension == null)
				throw new ArgumentNullException(nameof(filenameNoExtension));
			if (fileExtension == null)
				throw new ArgumentNullException(nameof(fileExtension));
			if (!CanSaveCode)
				return;
			SaveToFile(filenameNoExtension, fileExtension, filesFilter, GetCode());
		}

		void SaveToFile(string filenameNoExtension, string fileExtension, string filesFilter, string fileContents) {
			if (fileExtension.Length > 0 && fileExtension[0] == '.')
				fileExtension = fileExtension.Substring(1);
			var filename = pickSaveFilename.GetFilename(filenameNoExtension + "." + fileExtension, fileExtension, filesFilter);
			if (filename == null)
				return;
			try {
				File.WriteAllText(filename, fileContents);
			}
			catch (Exception ex) {
				MsgBox.Instance.Show(ex);
			}
		}

		void AddNewDocument() {
			docVersion++;
			prevCommandTextChangedState?.Cancel();
			subBuffers.Clear();
			scriptOutputCachedTextColorsCollection = null;
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
				var cachedColors = new CachedTextColorsCollectionBuilder(this, totalLength).Create(buf.Input, buf.ColorInfos);
				Debug.Assert(cachedColors.Length == totalLength);
				if (currentDocVersion == docVersion)
					cachedColorsList.AddOrUpdate(baseOffset, cachedColors);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken.Equals(changedState.CancellationToken)) {
			}
			catch (Exception ex) {
				Debug.Fail("Exception: " + ex.Message);
				if (currentDocVersion == docVersion)
					cachedColorsList.AddOrUpdate(baseOffset, new CachedTextColorsCollection());
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
			if (wpfTextViewHost.IsClosed)
				return;

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
				cachedColorsList.RemoveLastCachedTextColorsCollection();
				ClearCurrentInput(true);
			}
			if (newPendingOutput != null) {
				Debug.Assert(scriptOutputCachedTextColorsCollection != null);
				foreach (var info in newPendingOutput) {
					sb.Append(info.Text);
					scriptOutputCachedTextColorsCollection?.Append(info.Color, info.Text);
				}
				scriptOutputCachedTextColorsCollection?.Flush();
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

		void IReplEditor.OutputPrint(string text, TextColor color, bool startOnNewLine) =>
			((IReplEditor)this).OutputPrint(text, color.Box(), startOnNewLine);

		void IReplEditor.OutputPrint(string text, object color, bool startOnNewLine) {
			if (string.IsNullOrEmpty(text))
				return;

			lock (pendingScriptOutputLock) {
				if (startOnNewLine) {
					if (pendingScriptOutput.Count > 0) {
						var last = pendingScriptOutput[pendingScriptOutput.Count - 1];
						if (last.Text.Length > 0 && last.Text[last.Text.Length - 1] != '\n')
							pendingScriptOutput.Add(new ColorAndText(BoxedTextColor.Text, Environment.NewLine));
					}
					else if (LastLine.Length != 0)
						pendingScriptOutput.Add(new ColorAndText(BoxedTextColor.Text, Environment.NewLine));
				}
				pendingScriptOutput.Add(new ColorAndText(color, text));
			}

			FlushScriptOutput();
		}

		void IReplEditor.OutputPrintLine(string text, TextColor color, bool startOnNewLine) =>
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

		void ClearUndoRedoHistory() {
			//TODO:
		}

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

		public string GetCode() {
			int startOffset = 0;
			int endOffset = wpfTextView.TextSnapshot.Length;
			if (endOffset <= startOffset)
				return string.Empty;

			var sb = new StringBuilder();
			foreach (var buf in AllSubBuffers)
				AddCode(sb, buf, startOffset, endOffset);
			return sb.ToString();
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

		public bool CanCopyCode => !wpfTextView.Selection.IsEmpty;
		public void CopyCode() {
			if (!CanCopyCode)
				return;

			var code = GetCode();
			if (code.Length > 0) {
				try {
					Clipboard.SetText(code.ToString());
				}
				catch (ExternalException) { }
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
			if (!wpfTextViewHost.IsClosed)
				wpfTextViewHost.Close();
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

		public ReplSubBufferInfo FindBuffer(int offset) {
			int bufferKindIndex = -1;
			foreach (var buf in AllSubBuffers) {
				if (buf.Kind != ReplBufferKind.Code)
					bufferKindIndex = -1;
				else
					bufferKindIndex++;
				if (buf.Span.Start <= offset && offset < buf.Span.End)
					return new ReplSubBufferInfo(buf, bufferKindIndex);
			}
			var active = ActiveSubBuffer;
			if (active.Kind != ReplBufferKind.Code)
				bufferKindIndex = -1;
			else
				bufferKindIndex++;
			if (active.Span.Start <= offset && offset <= active.Span.End)
				return new ReplSubBufferInfo(active, bufferKindIndex);
			Debug.Fail("Couldn't find a buffer");
			return new ReplSubBufferInfo(active, bufferKindIndex);
		}

		public bool CanReplace(SnapshotSpan span, string newText) {
			Debug.Assert(span.Snapshot == TextView.TextSnapshot);
			if (OffsetOfPrompt == null)
				return false;
			if (span.Span.Start < OffsetOfPrompt.Value)
				return false;
			if (newText.IndexOfAny(newLineChars) >= 0)
				return false;
			var line = span.Start.GetContainingLine();
			// Don't allow removing the newline
			if (span.End > line.End)
				return false;
			var prompt = line.Start.Position == OffsetOfPrompt.Value ? PrimaryPrompt : SecondaryPrompt;
			return span.Start.Position >= line.Start.Position + prompt.Length;
		}
	}

	struct CachedTextColorsCollectionBuilder {
		readonly ReplEditor owner;
		readonly CachedTextColorsCollection cachedTextColorsCollection;
		readonly int totalLength;

		public CachedTextColorsCollectionBuilder(ReplEditor owner, int totalLength) {
			this.owner = owner;
			this.cachedTextColorsCollection = new CachedTextColorsCollection();
			this.totalLength = totalLength;
		}

		public CachedTextColorsCollection Create(string command, List<SpanAndClassificationType> colorInfos) {
			if (owner.PrimaryPrompt.Length > totalLength)
				cachedTextColorsCollection.Append(owner.ReplPrompt1ClassificationType, owner.PrimaryPrompt.Substring(0, totalLength));
			else
				cachedTextColorsCollection.Append(owner.ReplPrompt1ClassificationType, owner.PrimaryPrompt);
			int cmdOffs = 0;
			foreach (var cinfo in colorInfos) {
				Debug.Assert(cmdOffs <= cinfo.Offset);
				if (cmdOffs < cinfo.Offset)
					Append(owner.TextClassificationType, command, cmdOffs, cinfo.Offset - cmdOffs);
				Append(cinfo.ClassificationType, command, cinfo.Offset, cinfo.Length);
				cmdOffs = cinfo.Offset + cinfo.Length;
			}
			if (cmdOffs < command.Length)
				Append(owner.TextClassificationType, command, cmdOffs, command.Length - cmdOffs);

			cachedTextColorsCollection.Finish();
			return cachedTextColorsCollection;
		}

		void Append(IClassificationType classificationType, string s, int offset, int length) {
			object color = classificationType;
			if (color == owner.TextClassificationType)
				color = BoxedTextColor.Text;
			int so = offset;
			int end = offset + length;
			while (so < end) {
				int nlOffs = s.IndexOfAny(ReplEditor.newLineChars, so, end - so);
				if (nlOffs >= 0) {
					int nlLen = s[nlOffs] == '\r' && nlOffs + 1 < end && s[nlOffs + 1] == '\n' ? 2 : 1;
					cachedTextColorsCollection.Append(color, s, so, nlOffs - so + nlLen);
					so = nlOffs + nlLen;
					if (cachedTextColorsCollection.Length < totalLength)
						cachedTextColorsCollection.Append(owner.ReplPrompt2ClassificationType, owner.SecondaryPrompt);
				}
				else {
					cachedTextColorsCollection.Append(color, s, so, end - so);
					break;
				}
			}
		}
	}

	sealed class CommandTextChangedState : IDisposable {
		public CancellationToken CancellationToken { get; }
		readonly CancellationTokenSource cancellationTokenSource;
		readonly int version;
		bool hasDisposed;

		public CommandTextChangedState(int version) {
			this.cancellationTokenSource = new CancellationTokenSource();
			CancellationToken = cancellationTokenSource.Token;
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

		public List<SpanAndClassificationType> ColorInfos { get; } = new List<SpanAndClassificationType>();

		public ReplCommandInput(string input) {
			Input = input;
		}

		public void AddClassification(int offset, int length, IClassificationType classificationType) {
#if DEBUG
			//TODO: Remove this requirement
			Debug.Assert(offset >= nextMinOffset);
			if (offset < nextMinOffset)
				throw new InvalidOperationException("All classifications must be ordered and there must be no overlaps");
			nextMinOffset = offset + length;
#endif
			ColorInfos.Add(new SpanAndClassificationType(offset, length, classificationType));
		}
#if DEBUG
		int nextMinOffset;
#endif
	}

	struct SpanAndClassificationType {
		public int Offset { get; }
		public int Length { get; }
		public IClassificationType ClassificationType { get; }

		public SpanAndClassificationType(int offset, int length, IClassificationType classificationType) {
			Debug.Assert(offset + length >= offset && length >= 0);
			Offset = offset;
			Length = length;
			ClassificationType = classificationType;
		}
	}

	sealed class ReplCustomLineNumberMarginOwner : ICustomLineNumberMarginOwner {
		readonly IClassificationType replLineNumberInput1ClassificationType;
		readonly IClassificationType replLineNumberInput2ClassificationType;
		readonly IClassificationType replLineNumberOutputClassificationType;
		TextFormattingRunProperties replLineNumberInput1TextFormattingRunProperties;
		TextFormattingRunProperties replLineNumberInput2TextFormattingRunProperties;
		TextFormattingRunProperties replLineNumberOutputTextFormattingRunProperties;
		readonly IReplEditor2 replEditor;

		public ReplCustomLineNumberMarginOwner(IReplEditor2 replEditor, IThemeClassificationTypes themeClassificationTypes) {
			if (replEditor == null)
				throw new ArgumentNullException(nameof(replEditor));
			if (themeClassificationTypes == null)
				throw new ArgumentNullException(nameof(themeClassificationTypes));
			this.replEditor = replEditor;
			this.replLineNumberInput1ClassificationType = themeClassificationTypes.GetClassificationType(TextColor.ReplLineNumberInput1);
			this.replLineNumberInput2ClassificationType = themeClassificationTypes.GetClassificationType(TextColor.ReplLineNumberInput2);
			this.replLineNumberOutputClassificationType = themeClassificationTypes.GetClassificationType(TextColor.ReplLineNumberOutput);
		}

		sealed class ReplState {
			public ReplSubBufferInfo BufferInfo;
			public ITextSnapshotLine BufferStartLine;
		}

		public TextFormattingRunProperties GetDefaultTextFormattingRunProperties() => replLineNumberOutputTextFormattingRunProperties;

		public int? GetLineNumber(ITextViewLine viewLine, ITextSnapshotLine snapshotLine, ref object state) {
			if (!viewLine.IsFirstTextViewLineForSnapshotLine)
				return null;
			ReplState replState;
			if (state == null)
				state = replState = new ReplState();
			else
				replState = (ReplState)state;
			if (replState.BufferInfo.Buffer == null || viewLine.Start.Position < replState.BufferInfo.Buffer.Span.Start || viewLine.Start.Position >= replState.BufferInfo.Buffer.Span.End) {
				var subBufferInfo = replEditor.FindBuffer(viewLine.Start.Position);
				var snapshot = viewLine.Snapshot;
				Debug.Assert(subBufferInfo.Buffer.Span.Start <= snapshot.Length);
				if (subBufferInfo.Buffer.Span.Start > snapshot.Length)
					return null;
				replState.BufferInfo = subBufferInfo;
				replState.BufferStartLine = snapshot.GetLineFromPosition(subBufferInfo.Buffer.Span.Start);
			}
			int lineNumber = snapshotLine.LineNumber - replState.BufferStartLine.LineNumber;
			Debug.Assert(lineNumber >= 0);
			if (lineNumber < 0)
				return null;

			return lineNumber + 1;
		}

		public TextFormattingRunProperties GetLineNumberTextFormattingRunProperties(ITextViewLine viewLine, ITextSnapshotLine snapshotLine, int lineNumber, object state) {
			var replState = (ReplState)state;
			switch (replState.BufferInfo.Buffer.Kind) {
			case ReplBufferKind.Output:
				return replLineNumberOutputTextFormattingRunProperties;

			case ReplBufferKind.Code:
				switch (replState.BufferInfo.CodeBufferIndex % 2) {
				case 0:
					return replLineNumberInput1TextFormattingRunProperties;
				case 1:
					return replLineNumberInput2TextFormattingRunProperties;
				default:
					throw new InvalidOperationException();
				}

			default:
				throw new InvalidOperationException();
			}
		}

		public int? GetMaxLineNumberDigits() => null;

		public void OnTextPropertiesChanged(IClassificationFormatMap classificationFormatMap) {
			replLineNumberInput1TextFormattingRunProperties = classificationFormatMap.GetTextProperties(replLineNumberInput1ClassificationType);
			replLineNumberInput2TextFormattingRunProperties = classificationFormatMap.GetTextProperties(replLineNumberInput2ClassificationType);
			replLineNumberOutputTextFormattingRunProperties = classificationFormatMap.GetTextProperties(replLineNumberOutputClassificationType);
		}

		public void OnVisible() { }

		public void OnInvisible() {
			replLineNumberInput1TextFormattingRunProperties = null;
			replLineNumberInput2TextFormattingRunProperties = null;
			replLineNumberOutputTextFormattingRunProperties = null;
		}
	}
}
