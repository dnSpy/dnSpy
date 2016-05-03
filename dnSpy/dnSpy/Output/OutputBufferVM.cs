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
using System.Windows;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.TextEditor;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Output {
	sealed class OutputBufferVM : ViewModelBase, IOutputTextPane {
		public IInputElement FocusedElement => logEditorUI.FocusedElement;
		public object TextEditorUIObject => logEditorUI.UIObject;

		public Guid Guid { get; }
		public string Name { get; }

		public int Index {
			get { return index; }
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged("KeyboardShortcut");
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

		public bool WordWrap {
			get { return logEditorUI.WordWrap; }
			set { logEditorUI.WordWrap = value; }
		}

		public bool ShowLineNumbers {
			get { return logEditorUI.ShowLineNumbers; }
			set { logEditorUI.ShowLineNumbers = value; }
		}

		public bool ShowTimestamps { get; set; }

		readonly ILogEditorUI logEditorUI;

		public OutputBufferVM(Guid guid, string name, ILogEditorUI logEditorUI) {
			Guid = guid;
			Name = name;
			this.logEditorUI = logEditorUI;
			this.index = -1;
			this.needTimestamp = true;
		}

		public void Clear() {
			logEditorUI.Clear();
			needTimestamp = true;
		}
		bool needTimestamp;

		public string GetText() => logEditorUI.GetText();
		public ICachedWriter CreateWriter() => new CachedWriter(this);
		public void Write(object color, string s) => WriteInternal(color, s);
		public void Write(OutputColor color, string s) => WriteInternal(color.Box(), s);
		public void WriteLine(OutputColor color, string s) => WriteLine(color.Box(), s);

		public void WriteLine(object color, string s) {
			WriteInternal(color, s);
			WriteInternal(BoxedOutputColor.Text, Environment.NewLine);
		}

		public void Write(IEnumerable<ColorAndText> text) {
			foreach (var t in text)
				WriteInternal(t.Color, t.Text);
		}

		void WriteInternal(object color, string text) {
			int so = 0;
			while (so < text.Length) {
				if (needTimestamp) {
					needTimestamp = false;
					if (ShowTimestamps) {
						logEditorUI.Write(DateTime.Now.ToLongTimeString(), BoxedOutputColor.DebugLogTimestamp);
						logEditorUI.Write(" ", BoxedOutputColor.Text);
					}
				}

				int nlOffs = text.IndexOfAny(newLineChars, so);
				if (nlOffs >= 0) {
					int nlLen = text[nlOffs] == '\r' && nlOffs + 1 < text.Length && text[nlOffs + 1] == '\n' ? 2 : 1;
					int soNext = nlOffs + nlLen;
					logEditorUI.Write(text.Substring(so, soNext - so), color);
					so = soNext;
					needTimestamp = true;
				}
				else {
					logEditorUI.Write(so == 0 ? text : text.Substring(so, text.Length - so), color);
					break;
				}
			}
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n' };
	}
}
