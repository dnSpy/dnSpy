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
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	[DebuggerDisplay("{Description}")]
	sealed class WriteHexUndoCommand : IUndoCommand {
		readonly HexDocument doc;
		readonly ulong offset;
		readonly byte[] newData;
		readonly byte[] origData;
		readonly string descr;

		public static void AddAndExecute(IUndoCommandService undoCommandService, IHexDocumentService hexDocumentService, string filename, ulong offset, byte[] data, string descr = null) {
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentException();
			var doc = hexDocumentService.GetOrCreate(filename);
			if (doc == null)
				return;
			AddAndExecute(undoCommandService, doc, offset, data, descr);
		}

		public static void AddAndExecute(IUndoCommandService undoCommandService, HexDocument doc, ulong offset, byte[] data, string descr = null) {
			if (doc == null)
				throw new ArgumentNullException(nameof(doc));
			if (data == null || data.Length == 0)
				return;
			undoCommandService.Add(new WriteHexUndoCommand(doc, offset, data, descr));
		}

		WriteHexUndoCommand(HexDocument doc, ulong offset, byte[] data, string descr) {
			this.doc = doc;
			this.offset = offset;
			this.newData = (byte[])data.Clone();
			this.origData = this.doc.ReadBytes(offset, data.Length);
			this.descr = descr;
		}

		public string Description => descr ?? string.Format(dnSpy_AsmEditor_Resources.Hex_Undo_Message_Write_Bytes, newData.Length, offset);

		public IEnumerable<object> ModifiedObjects {
			get { yield return doc; }
		}

		public void Execute() => WriteData(newData);
		public void Undo() => WriteData(origData);
		void WriteData(byte[] data) => doc.Write(offset, data, 0, data.Length);
	}
}
