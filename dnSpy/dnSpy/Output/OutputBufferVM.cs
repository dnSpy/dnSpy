/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Output {
	sealed class OutputBufferVM : ViewModelBase, IOutputTextPane {
		public IInputElement FocusedElement => logEditor.FocusedElement;
		public object TextEditorUIObject => logEditor.UIObject;
		public double ZoomLevel => logEditor.TextView.ZoomLevel;

		public Guid Guid { get; }
		public string Name { get; }

		public int Index {
			get => index;
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged(nameof(KeyboardShortcut));
				}
			}
		}
		int index;

		public string KeyboardShortcut {
			get {
				if ((uint)index >= 10)
					return string.Empty;
				// Ctrl+0 is bound to reset-zoom, so won't work.
				if (index == 9)
					return string.Empty;
				return "(" + string.Format(dnSpy_Resources.ShortCutKeyCtrlDIGIT, (index + 1) % 10) + ")";
			}
		}

		public WordWrapStyles WordWrapStyle {
			get => logEditor.WordWrapStyle;
			set => logEditor.WordWrapStyle = value;
		}

		public bool ShowLineNumbers {
			get => logEditor.ShowLineNumbers;
			set => logEditor.ShowLineNumbers = value;
		}

		bool ShowTimestamps => logEditor.TextView.Options.GetOptionValue(DefaultOutputOptions.ShowTimestampsId);
		string TimestampDateTimeFormat => logEditor.TextView.Options.GetOptionValue(DefaultOutputOptions.TimestampDateTimeFormatId);

		readonly ILogEditor logEditor;
		readonly IEditorOperations editorOperations;

		public OutputBufferVM(IEditorOperationsFactoryService editorOperationsFactoryService, Guid guid, string name, ILogEditor logEditor) {
			editorOperations = editorOperationsFactoryService.GetEditorOperations(logEditor.TextView);
			Guid = guid;
			Name = name;
			this.logEditor = logEditor;
			index = -1;
			needTimestamp = true;
		}

		public bool CanCopy {
			get {
				if (!logEditor.TextView.Selection.IsEmpty)
					return true;

				var line = logEditor.TextView.Caret.Position.BufferPosition.GetContainingLine();
				bool cutEmptyLines = logEditor.TextView.Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
				return cutEmptyLines || !string.IsNullOrWhiteSpace(line.GetText());
			}
		}

		public void Copy() => editorOperations.CopySelection();

		public void Clear() {
			logEditor.Clear();
			needTimestamp = true;
		}
		bool needTimestamp;

		public string GetText() => logEditor.GetText();
		public ICachedWriter CreateWriter() => new CachedWriter(this);
		public void Write(object color, string s) => WriteInternal(color, s);
		public void Write(TextColor color, string s) => WriteInternal(color.Box(), s);
		public void WriteLine(TextColor color, string s) => WriteLine(color.Box(), s);

		public void WriteLine(object color, string s) {
			WriteInternal(color, s);
			WriteInternal(BoxedTextColor.Text, Environment.NewLine);
		}

		public void Write(IEnumerable<ColorAndText> text) {
			foreach (var t in text)
				WriteInternal(t.Color, t.Text);
		}

		void WriteInternal(object color, string text) {
			if (text == null)
				return;
			int so = 0;
			while (so < text.Length) {
				if (needTimestamp) {
					needTimestamp = false;
					if (ShowTimestamps) {
						var format = TimestampDateTimeFormat;
						if (!string.IsNullOrWhiteSpace(format)) {
							logEditor.Write(DateTime.Now.ToString(format), BoxedTextColor.DebugLogTimestamp);
							logEditor.Write(" ", BoxedTextColor.Text);
						}
					}
				}

				int nlOffs = text.IndexOfAny(LineConstants.newLineChars, so);
				if (nlOffs >= 0) {
					int nlLen = text[nlOffs] == '\r' && nlOffs + 1 < text.Length && text[nlOffs + 1] == '\n' ? 2 : 1;
					int soNext = nlOffs + nlLen;
					logEditor.Write(text.Substring(so, soNext - so), color);
					so = soNext;
					needTimestamp = true;
				}
				else {
					logEditor.Write(so == 0 ? text : text.Substring(so, text.Length - so), color);
					break;
				}
			}
		}
	}
}
