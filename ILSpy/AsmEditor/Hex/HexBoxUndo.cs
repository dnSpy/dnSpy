/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.HexEditor;
using dnSpy.Tabs;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexBoxUndo : IPlugin {
		public void OnLoaded() {
			UndoCommandManager.Instance.OnEvent += UndoCommandManager_OnEvent;
			MainWindow.Instance.OnTabStateAdded += OnTabStateAdded;
			MainWindow.Instance.OnTabStateRemoved += OnTabStateRemoved;
			foreach (var tabState in MainWindow.Instance.AllTabStates)
				OnTabStateAdded(tabState);
		}

		void OnTabStateAdded(object sender, MainWindow.TabStateEventArgs e) {
			OnTabStateAdded(e.TabState);
		}

		void OnTabStateRemoved(object sender, MainWindow.TabStateEventArgs e) {
			OnTabStateRemoved(e.TabState);
		}

		void OnTabStateAdded(TabState tabState) {
			var hts = tabState as HexTabState;
			if (hts == null)
				return;

			hts.HexBox.OnWrite += HexBox_OnWrite;
		}

		void OnTabStateRemoved(TabState tabState) {
			var hts = tabState as HexTabState;
			if (hts == null)
				return;

			hts.HexBox.OnWrite -= HexBox_OnWrite;
		}

		sealed class UndoInfo {
			public byte[] OriginalData;
			public HexBoxPosition OriginalCaretPosition;
		}

		void HexBox_OnWrite(object sender, HexBoxWriteEventArgs e) {
			const string key = "HexBoxUndo";
			var hts = (HexTabState)TabState.GetTabState((HexBox)sender);
			var doc = hts.HexBox.Document;
			if (doc == null)
				return;
			if (e.IsBeforeWrite) {
				var info = new UndoInfo();
				info.OriginalData = hts.HexBox.Document.Read(e.StartOffset, e.Size);
				info.OriginalCaretPosition = hts.HexBox.CaretPosition;
				e.Context[key] = info;
			}
			else {
				var info = (UndoInfo)e.Context[key];

				bool updated = TryUpdateOldTextInputCommand(e.Type, hts.HexBox, info.OriginalCaretPosition, e.StartOffset, info.OriginalData);
				if (!updated) {
					ClearTextInputCommand();
					var cmd = new HexBoxUndoCommand(hts.HexBox, info.OriginalCaretPosition, e.StartOffset, info.OriginalData, GetDescription(e));
					UndoCommandManager.Instance.Add(cmd);
					if (e.Type == HexWriteType.ByteInput || e.Type == HexWriteType.AsciiInput)
						SetTextInputCommand(cmd);
				}
			}
		}

		bool TryUpdateOldTextInputCommand(HexWriteType type, HexBox hexBox, HexBoxPosition posBeforeWrite, ulong startOffset, byte[] originalData) {
			if (type != HexWriteType.ByteInput && type != HexWriteType.AsciiInput)
				return false;

			var cmd = (HexBoxUndoCommand)prevTextInputCmd.Target;
			if (cmd == null)
				return false;

			return cmd.TryAppend(hexBox, posBeforeWrite, startOffset, originalData);
		}
		readonly WeakReference prevTextInputCmd = new WeakReference(null);

		void UndoCommandManager_OnEvent(object sender, UndoCommandManagerEventArgs e) {
			ClearTextInputCommand();
		}

		void ClearTextInputCommand() {
			prevTextInputCmd.Target = null;
		}

		void SetTextInputCommand(HexBoxUndoCommand cmd) {
			prevTextInputCmd.Target = cmd;
		}

		static string GetDescription(HexBoxWriteEventArgs e) {
			switch (e.Type) {
			case HexWriteType.Paste:		return string.Format("Paste {0} bytes @ {1:X8}", e.Size, e.StartOffset);
			case HexWriteType.ByteInput:	return "Insert Bytes";
			case HexWriteType.AsciiInput:	return "Insert ASCII";
			case HexWriteType.Fill:			return string.Format("Fill {0} bytes @ {1:X8}", e.Size, e.StartOffset);
			default:						return null;
			}
		}
	}
}
