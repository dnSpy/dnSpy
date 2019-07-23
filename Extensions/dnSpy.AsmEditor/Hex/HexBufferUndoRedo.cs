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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IHexBufferServiceListener))]
	sealed class HexBufferUndoRedo : IHexBufferServiceListener {
		internal static object UndoRedoObject = new object();
		readonly IUndoCommandService undoCommandService;

		[ImportingConstructor]
		HexBufferUndoRedo(IUndoCommandService undoCommandService) => this.undoCommandService = undoCommandService;

		void IHexBufferServiceListener.BufferCreated(HexBuffer buffer) => buffer.Changed += Buffer_Changed;

		void IHexBufferServiceListener.BuffersCleared(IEnumerable<HexBuffer> buffers) {
			foreach (var buffer in buffers)
				buffer.Changed -= Buffer_Changed;
		}

		void Buffer_Changed(object? sender, HexContentChangedEventArgs e) {
			if (e.EditTag == HexBufferUndoRedo.UndoRedoObject)
				return;
			var buffer = (HexBuffer)sender!;
			var desc = dnSpy_AsmEditor_Resources.Hex_Undo_Message_InsertBytes;
			var cmd = new HexBufferUndoCommand(buffer, e.Changes, e.BeforeVersion.ReiteratedVersionNumber, e.AfterVersion.ReiteratedVersionNumber, desc);
			undoCommandService.Add(cmd);
		}
	}

	sealed class HexBufferUndoCommand : IUndoCommand {
		public string Description { get; }

		readonly HexBuffer buffer;
		readonly NormalizedHexChangeCollection changes;
		readonly int beforeReiteratedVersionNumber;
		readonly int afterReiteratedVersionNumber;
		bool canExecute;

		public HexBufferUndoCommand(HexBuffer buffer, NormalizedHexChangeCollection changes, int beforeReiteratedVersionNumber, int afterReiteratedVersionNumber, string description) {
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			this.changes = changes ?? throw new ArgumentNullException(nameof(changes));
			this.beforeReiteratedVersionNumber = beforeReiteratedVersionNumber;
			this.afterReiteratedVersionNumber = afterReiteratedVersionNumber;
			Description = description ?? throw new ArgumentNullException(nameof(description));
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return buffer; }
		}

		public void Execute() {
			// This instance gets created after the buffer has already been modified
			// so don't exec this method the first time it gets called.
			if (!canExecute) {
				canExecute = true;
				return;
			}
			using (var ed = buffer.CreateEdit(afterReiteratedVersionNumber, HexBufferUndoRedo.UndoRedoObject)) {
				foreach (var change in changes) {
					bool b = ed.Replace(change.OldPosition, change.NewData);
					Debug.Assert(b);
				}
				ed.Apply();
			}
		}

		public void Undo() {
			using (var ed = buffer.CreateEdit(beforeReiteratedVersionNumber, HexBufferUndoRedo.UndoRedoObject)) {
				foreach (var change in changes) {
					bool b = ed.Replace(change.NewPosition, change.OldData);
					Debug.Assert(b);
				}
				ed.Apply();
			}
		}
	}
}
