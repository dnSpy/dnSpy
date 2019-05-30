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
using System.Linq;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IUndoableDocumentsProvider))]
	[Export(typeof(IHexBufferServiceListener))]
	sealed class HexUndoableDocumentsProvider : IUndoableDocumentsProvider, IHexBufferServiceListener {
		static readonly object undoObjectKey = new object();
		readonly Lazy<IHexBufferService> hexBufferService;

		[ImportingConstructor]
		HexUndoableDocumentsProvider(Lazy<IHexBufferService> hexBufferService) => this.hexBufferService = hexBufferService;

		IEnumerable<IUndoObject> IUndoableDocumentsProvider.GetObjects() => hexBufferService.Value.GetBuffers().Select(a => TryGetUndoObject(a)).Where(a => !(a is null));

		IUndoObject? IUndoableDocumentsProvider.GetUndoObject(object obj) {
			if (obj is HexBuffer buffer)
				return TryGetUndoObject(buffer);
			return null;
		}

		bool IUndoableDocumentsProvider.OnExecutedOneCommand(IUndoObject obj) => !(TryGetHexBuffer(obj) is null);
		object? IUndoableDocumentsProvider.GetDocument(IUndoObject obj) => TryGetHexBuffer(obj);
		internal static HexBuffer? TryGetHexBuffer(IUndoObject? iuo) => (iuo as UndoObject)?.Value as HexBuffer;

		static IUndoObject TryGetUndoObject(HexBuffer buffer) {
			buffer.Properties.TryGetProperty(undoObjectKey, out IUndoObject undoObject);
			return undoObject;
		}

		void IHexBufferServiceListener.BufferCreated(HexBuffer buffer) =>
			buffer.Properties.AddProperty(undoObjectKey, new UndoObject(buffer));

		void IHexBufferServiceListener.BuffersCleared(IEnumerable<HexBuffer> buffers) {
			foreach (var buffer in buffers)
				buffer.Properties.RemoveProperty(undoObjectKey);
		}
	}
}
