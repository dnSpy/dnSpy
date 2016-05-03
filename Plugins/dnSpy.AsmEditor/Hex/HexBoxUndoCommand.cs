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
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Shared.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	/// <summary>
	/// A command that gets added to <see cref="UndoCommandManager"/> after it's already been
	/// executed once.
	/// </summary>
	[DebuggerDisplay("{Description}")]
	sealed class HexBoxUndoCommand : IUndoCommand, IDisposable {
		readonly HexDocument doc;
		readonly WeakReference hexBoxWeakRef;
		readonly HexBoxPosition origCaretPos;
		HexBoxPosition newCaretPos;
		readonly ulong offset;
		byte[] origData;
		byte[] newData;
		bool canExecute;

		public HexBoxUndoCommand(HexBox hexBox, HexBoxPosition origCaretPos, ulong offset, byte[] origData, string descr) {
			this.doc = hexBox.Document;
			this.hexBoxWeakRef = new WeakReference(hexBox);
			this.origCaretPos = origCaretPos;
			this.newCaretPos = hexBox.CaretPosition;
			this.offset = offset;
			this.origData = origData;
			this.newData = doc.ReadBytes(offset, origData.Length);
			this.Description = descr;
			this.canExecute = false;
		}

		public string Description { get; }

		public IEnumerable<object> ModifiedObjects {
			get { yield return doc; }
		}

		void IDisposable.Dispose() {
			canNotAppend = true;
		}
		bool canNotAppend = false;

		public void Execute() {
			if (canExecute)
				WriteData(newData, newCaretPos);
			canExecute = true;
		}

		public void Undo() => WriteData(origData, origCaretPos);

		void WriteData(byte[] data, HexBoxPosition caretPos) {
			doc.Write(offset, data, 0, data.Length);
			var hexBox = (HexBox)hexBoxWeakRef.Target;
			if (hexBox != null) {
				hexBox.CaretPosition = caretPos;
				hexBox.BringCaretIntoView();
			}
		}

		public bool TryAppend(HexBox hexBox, HexBoxPosition posBeforeWrite, ulong startOffset, byte[] originalData) {
			if (canNotAppend)
				return false;
			if (hexBoxWeakRef.Target != hexBox)
				return false;
			if (hexBox.Document == null)
				return false;
			if (originalData.Length == 0)
				return false;
			if (newCaretPos != posBeforeWrite)
				return false;
			if (newCaretPos.Offset > hexBox.CaretPosition.Offset)
				return false;
			if (newCaretPos.Kind != hexBox.CaretPosition.Kind)
				return false;

			if (newCaretPos.Offset == posBeforeWrite.Offset && newCaretPos.Kind == HexBoxPositionKind.HexByte && newCaretPos.KindPosition == HexBoxPosition.INDEX_HEXBYTE_LAST) {
				if (posBeforeWrite.Kind != HexBoxPositionKind.HexByte || posBeforeWrite.KindPosition != HexBoxPosition.INDEX_HEXBYTE_LAST)
					return false;
				if (originalData.Length != 1)
					return false;
				newData[newData.Length - 1] = (byte)hexBox.Document.ReadByte(posBeforeWrite.Offset);
			}
			else {
				if (newCaretPos.Offset != posBeforeWrite.Offset)
					return false;
				ulong c = hexBox.CaretPosition.Kind == HexBoxPositionKind.HexByte && hexBox.CaretPosition.KindPosition == HexBoxPosition.INDEX_HEXBYTE_LAST ? 1UL : 0;
				if (hexBox.CaretPosition.Offset - newCaretPos.Offset + c != (ulong)originalData.Length)
					return false;
				int origLen = newData.Length;
				Array.Resize(ref newData, origLen + originalData.Length);
				Array.Resize(ref origData, origLen + originalData.Length);

				for (int i = 0; i < originalData.Length; i++) {
					newData[origLen + i] = (byte)hexBox.Document.ReadByte(posBeforeWrite.Offset + (ulong)i);
					origData[origLen + i] = originalData[i];
				}
			}

			newCaretPos = hexBox.CaretPosition;
			return true;
		}
	}
}
