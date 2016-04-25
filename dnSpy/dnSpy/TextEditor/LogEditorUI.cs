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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.TextEditor {
	sealed class LogEditorUI : ILogEditorUI {
		public object UIObject => textEditor;
		public IInputElement FocusedElement => textEditor.FocusedElement;
		public FrameworkElement ScaleElement => textEditor.TextArea;
		public object Tag { get; set; }

		public bool WordWrap {
			get { return textEditor.WordWrap; }
			set { textEditor.WordWrap = value; }
		}

		public bool ShowLineNumbers {
			get { return textEditor.ShowLineNumbers; }
			set {
				if (textEditor.ShowLineNumbers != value) {
					textEditor.ShowLineNumbers = value;
					UpdatePaddingElement();
					textEditor.OnShowLineNumbersChanged();
				}
			}
		}

		void UpdatePaddingElement() {
			Debug.Assert(paddingElement != null);
			if (textEditor.ShowLineNumbers)
				this.textEditor.TextArea.LeftMargins.Remove(paddingElement);
			else {
				Debug.Assert(!this.textEditor.TextArea.LeftMargins.Contains(paddingElement));
				this.textEditor.TextArea.LeftMargins.Insert(0, paddingElement);
			}
		}
		readonly FrameworkElement paddingElement;

		readonly DnSpyTextEditor textEditor;
		readonly LogEditorOptions options;
		readonly Dispatcher dispatcher;
		TextTokenInfo textTokenInfo;

		const int LEFT_MARGIN = 15;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly LogEditorUI logEditorUI;
			readonly Func<GuidObject, bool, IEnumerable<GuidObject>> createGuidObjects;

			public GuidObjectsCreator(LogEditorUI logEditorUI, Func<GuidObject, bool, IEnumerable<GuidObject>> createGuidObjects) {
				this.logEditorUI = logEditorUI;
				this.createGuidObjects = createGuidObjects;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_LOG_EDITOR_GUID, logEditorUI);

				var teCtrl = (DnSpyTextEditor)creatorObject.Object;
				var position = openedFromKeyboard ? teCtrl.TextArea.Caret.Position : teCtrl.GetPositionFromMousePosition();
				if (position != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORLOCATION_GUID, new TextEditorLocation(position.Value.Line, position.Value.Column));

				if (createGuidObjects != null) {
					foreach (var guidObject in createGuidObjects(creatorObject, openedFromKeyboard))
						yield return guidObject;
				}
			}
		}

		public LogEditorUI(LogEditorOptions options, IThemeManager themeManager, IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorSettings textEditorSettings) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.paddingElement = new FrameworkElement { Margin = new Thickness(LEFT_MARGIN, 0, 0, 0) };
			this.options = (options ?? new LogEditorOptions()).Clone();
			this.textEditor = new DnSpyTextEditor(themeManager, textEditorSettings);
			SetNewDocument();
			this.textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".il");
			this.textEditor.TextArea.AllowDrop = false;
			UpdatePaddingElement(); ;
			this.textEditor.IsReadOnly = true;
			// Setting IsReadOnly to true doesn't mean it's readonly since undo/redo still works.
			// Fix that by removing the commands.
			Remove(this.textEditor.TextArea.CommandBindings, ApplicationCommands.Undo);
			Remove(this.textEditor.TextArea.CommandBindings, ApplicationCommands.Redo);

			if (this.options.TextEditorCommandGuid != null)
				wpfCommandManager.Add(this.options.TextEditorCommandGuid.Value, this.textEditor);
			if (this.options.TextAreaCommandGuid != null)
				wpfCommandManager.Add(this.options.TextAreaCommandGuid.Value, this.textEditor.TextArea);
			if (this.options.MenuGuid != null)
				menuManager.InitializeContextMenu(this.textEditor, this.options.MenuGuid.Value, new GuidObjectsCreator(this, this.options.CreateGuidObjects), new ContextMenuInitializer(this.textEditor, this.textEditor));
		}

		static void Remove(CommandBindingCollection bindings, ICommand cmd) {
			for (int i = bindings.Count - 1; i >= 0; i--) {
				var b = bindings[i];
				if (b.Command == cmd)
					bindings.RemoveAt(i);
			}
		}

		void SetNewDocument() {
			textTokenInfo = new TextTokenInfo();
			textEditor.TextArea.Document = new TextDocument();
			textEditor.SetDocumentColorInfo(textTokenInfo, false);
		}

		public void Clear() {
			ClearPendingOutput();
			SetNewDocument();
		}

		public string GetText() => textEditor.TextArea.Document.Text;
		public void Write(string text, OutputColor color) => OutputPrint(text, color);

		public void WriteLine(string text, OutputColor color) {
			OutputPrint(text, color);
			OutputPrint(Environment.NewLine, color);
		}

		public void Write(IEnumerable<ColorAndText> text) {
			var list = text as IList<ColorAndText>;
			if (list == null)
				list = text.ToArray();
			if (list.Count == 0)
				return;

			lock (pendingOutputLock)
				pendingOutput.AddRange(list);

			FlushOutput();
		}

		void OutputPrint(string text, OutputColor color, bool startOnNewLine = false) {
			if (string.IsNullOrEmpty(text))
				return;

			lock (pendingOutputLock) {
				if (startOnNewLine) {
					if (pendingOutput.Count > 0) {
						var last = pendingOutput[pendingOutput.Count - 1];
						if (last.Text.Length > 0 && last.Text[last.Text.Length - 1] != '\n')
							pendingOutput.Add(new ColorAndText(OutputColor.Text, Environment.NewLine));
					}
					else if (LastLine.Length != 0)
						pendingOutput.Add(new ColorAndText(OutputColor.Text, Environment.NewLine));
				}
				pendingOutput.Add(new ColorAndText(color, text));
			}

			FlushOutput();
		}

		DocumentLine LastLine {
			get {
				var doc = textEditor.TextArea.Document;
				return doc.GetLineByNumber(doc.LineCount);
			}
		}

		void RawAppend(string text) => textEditor.TextArea.Document.Insert(LastLine.EndOffset, text);

		void FlushOutputUIThread() {
			dispatcher.VerifyAccess();

			var currentLine = textEditor.TextArea.Document.GetLineByOffset(textEditor.TextArea.Caret.Offset);
			bool canMoveCaret = currentLine == LastLine;

			ColorAndText[] newPendingOutput;
			var sb = new StringBuilder();
			lock (pendingOutputLock) {
				pendingOutput_dispatching = false;
				newPendingOutput = pendingOutput.ToArray();
				pendingOutput.Clear();
			}

			foreach (var info in newPendingOutput) {
				sb.Append(info.Text);
				textTokenInfo.Append(info.Color.ToTextTokenKind(), info.Text);
			}
			if (sb.Length == 0)
				return;

			textTokenInfo.Flush();
			RawAppend(sb.ToString());

			if (canMoveCaret) {
				textEditor.TextArea.Caret.Offset = LastLine.EndOffset;
				textEditor.TextArea.Caret.BringCaretToView();
			}
		}

		void FlushOutput() {
			if (!dispatcher.CheckAccess()) {
				lock (pendingOutputLock) {
					if (pendingOutput_dispatching)
						return;
					pendingOutput_dispatching = true;
					try {
						dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(FlushOutputUIThread));
					}
					catch {
						pendingOutput_dispatching = false;
						throw;
					}
				}
			}
			else
				FlushOutputUIThread();
		}
		readonly object pendingOutputLock = new object();
		List<ColorAndText> pendingOutput = new List<ColorAndText>();
		bool pendingOutput_dispatching;

		void ClearPendingOutput() {
			lock (pendingOutputLock) {
				pendingOutput = new List<ColorAndText>();
				pendingOutput_dispatching = false;
			}
		}
	}
}
