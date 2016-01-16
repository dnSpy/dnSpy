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
using System.ComponentModel.Composition;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	interface IHexBoxUndoManager {
		void Initialize(HexBox hexBox);
		void Uninitialize(HexBox hexBox);
	}

	[Export, Export(typeof(IHexBoxUndoManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class HexBoxUndoManager : IHexBoxUndoManager {
		readonly IUndoCommandManager undoCommandManager;

		[ImportingConstructor]
		HexBoxUndoManager(IUndoCommandManager undoCommandManager) {
			this.undoCommandManager = undoCommandManager;
			undoCommandManager.OnEvent += UndoCommandManager_OnEvent;
		}

		public void Initialize(HexBox hexBox) {
			hexBox.OnWrite += HexBox_OnWrite;
		}

		public void Uninitialize(HexBox hexBox) {
			hexBox.OnWrite -= HexBox_OnWrite;
		}

		sealed class UndoInfo {
			public byte[] OriginalData;
			public HexBoxPosition OriginalCaretPosition;
		}

		static readonly object contextKey = new object();
		void HexBox_OnWrite(object sender, HexBoxWriteEventArgs e) {
			var hexBox = (HexBox)sender;
			var doc = hexBox.Document;
			if (doc == null)
				return;
			if (e.IsBeforeWrite) {
				var info = new UndoInfo();
				info.OriginalData = hexBox.Document.ReadBytes(e.StartOffset, e.Size);
				info.OriginalCaretPosition = hexBox.CaretPosition;
				e.Context[contextKey] = info;
			}
			else {
				var info = (UndoInfo)e.Context[contextKey];

				bool updated = TryUpdateOldTextInputCommand(e.Type, hexBox, info.OriginalCaretPosition, e.StartOffset, info.OriginalData);
				if (!updated) {
					ClearTextInputCommand();
					var cmd = new HexBoxUndoCommand(hexBox, info.OriginalCaretPosition, e.StartOffset, info.OriginalData, GetDescription(e));
					undoCommandManager.Add(cmd);
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
			case HexWriteType.Paste:		return string.Format(dnSpy_AsmEditor_Resources.Hex_Undo_Message_PasteBytesAtAddress, e.Size, e.StartOffset);
			case HexWriteType.ByteInput:	return dnSpy_AsmEditor_Resources.Hex_Undo_Message_InsertBytes;
			case HexWriteType.AsciiInput:	return dnSpy_AsmEditor_Resources.Hex_Undo_Message_InsertASCII;
			case HexWriteType.Fill:			return string.Format(dnSpy_AsmEditor_Resources.Hex_Undo_Message_FillBytesAtAddress, e.Size, e.StartOffset);
			default:						return null;
			}
		}
	}
}
