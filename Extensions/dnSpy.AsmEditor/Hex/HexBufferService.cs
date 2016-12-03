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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using dnlib.IO;
using dnlib.PE;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex {
	interface IHexBufferService {
		HexBuffer GetOrCreate(IPEImage peImage);
		HexBuffer GetOrCreate(string filename);
		HexBuffer[] GetBuffers();
		HexBuffer TryGet(string filename);
		HexBuffer[] Clear();
	}

	interface IHexBufferServiceListener {
		void BufferCreated(HexBuffer buffer);
		void BuffersCleared(IEnumerable<HexBuffer> buffers);
	}

	[Export(typeof(IHexBufferService))]
	sealed class HexBufferService : IHexBufferService {
		readonly object lockObj = new object();
		readonly Dictionary<string, object> filenameToBuffer = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		readonly HexBufferFactoryService hexBufferFactoryService;
		readonly Lazy<IHexBufferServiceListener>[] hexBufferServiceListeners;

		[ImportingConstructor]
		HexBufferService(IUndoCommandService undoCommandService, HexBufferFactoryService hexBufferFactoryService, [ImportMany] IEnumerable<Lazy<IHexBufferServiceListener>> hexBufferServiceListeners) {
			this.hexBufferFactoryService = hexBufferFactoryService;
			this.hexBufferServiceListeners = hexBufferServiceListeners.ToArray();
			undoCommandService.OnEvent += UndoCommandService_OnEvent;
		}

		void UndoCommandService_OnEvent(object sender, UndoCommandServiceEventArgs e) {
			var buffer = HexUndoableDocumentsProvider.TryGetHexBuffer(e.UndoObject);
			if (buffer == null)
				return;

			if (e.Type == UndoCommandServiceEventType.Saved)
				OnDocumentSaved(buffer);
			else if (e.Type == UndoCommandServiceEventType.Dirty)
				OnDocumentDirty(buffer);
		}

		void OnDocumentSaved(HexBuffer buffer) {
			lock (lockObj) {
				object dictObj;
				bool b = filenameToBuffer.TryGetValue(buffer.Name, out dictObj);
				Debug.Assert(b);
				if (!b)
					return;
				if (dictObj is WeakReference) {
					Debug.Assert(((WeakReference)dictObj).Target == buffer);
					return;
				}
				Debug.Assert(buffer == dictObj);
				filenameToBuffer[buffer.Name] = new WeakReference(buffer);
			}
		}

		void OnDocumentDirty(HexBuffer buffer) {
			lock (lockObj) {
				object dictObj;
				bool b = filenameToBuffer.TryGetValue(buffer.Name, out dictObj);
				Debug.Assert(b);
				if (!b)
					return;
				filenameToBuffer[buffer.Name] = buffer;
			}
		}

		HexBuffer[] IHexBufferService.Clear() {
			object[] objs;
			lock (lockObj) {
				objs = filenameToBuffer.Values.ToArray();
				filenameToBuffer.Clear();
			}
			var buffersToDispose = new List<HexBuffer>(objs.Length);
			foreach (var obj in objs) {
				var buffer = TryGetBuffer(obj);
				if (buffer != null)
					buffersToDispose.Add(buffer);
			}
			foreach (var lz in hexBufferServiceListeners)
				lz.Value.BuffersCleared(buffersToDispose);
			return buffersToDispose.ToArray();
		}

		HexBuffer IHexBufferService.TryGet(string filename) {
			filename = GetFullPath(filename);

			lock (lockObj)
				return TryGet_NoLock(filename);
		}

		HexBuffer TryGet_NoLock(string filename) {
			object obj;
			if (!filenameToBuffer.TryGetValue(filename, out obj))
				return null;
			return TryGetBuffer(obj);
		}

		HexBuffer TryGetBuffer(object obj) {
			var buffer = obj as HexBuffer;
			if (buffer != null)
				return buffer;
			var weakRef = obj as WeakReference;
			Debug.Assert(weakRef != null);
			return weakRef?.Target as HexBuffer;
		}

		HexBuffer GetOrCreate(string filename) {
			if (!File.Exists(filename))
				return null;
			filename = GetFullPath(filename);

			HexBuffer buffer;
			lock (lockObj) {
				buffer = TryGet_NoLock(filename);
				if (buffer != null)
					return buffer;

				byte[] data;
				try {
					data = File.ReadAllBytes(filename);
				}
				catch {
					return null;
				}

				buffer = hexBufferFactoryService.Create(data, filename, hexBufferFactoryService.DefaultFileTags);
				filenameToBuffer[filename] = new WeakReference(buffer);
			}
			return NotifyBufferCreated(buffer);
		}

		HexBuffer NotifyBufferCreated(HexBuffer buffer) {
			foreach (var lz in hexBufferServiceListeners)
				lz.Value.BufferCreated(buffer);
			return buffer;
		}

		HexBuffer GetOrCreate(IPEImage peImage) {
			var filename = GetFullPath(peImage.FileName);

			HexBuffer buffer;
			lock (lockObj) {
				buffer = TryGet_NoLock(filename);
				if (buffer != null)
					return buffer;

				using (var stream = peImage.CreateFullStream()) {
					var data = stream.ReadAllBytes();
					buffer = hexBufferFactoryService.Create(data, filename, hexBufferFactoryService.DefaultFileTags);
					filenameToBuffer[filename] = new WeakReference(buffer);
				}
			}
			return NotifyBufferCreated(buffer);
		}

		HexBuffer IHexBufferService.GetOrCreate(IPEImage peImage) => GetOrCreate(peImage);
		HexBuffer IHexBufferService.GetOrCreate(string filename) => GetOrCreate(filename);

		HexBuffer[] IHexBufferService.GetBuffers() {
			lock (lockObj)
				return filenameToBuffer.Values.Select(a => TryGetBuffer(a)).Where(a => a != null).ToArray();
		}

		static string GetFullPath(string filename) {
			if (!File.Exists(filename))
				return filename ?? string.Empty;
			try {
				return Path.GetFullPath(filename);
			}
			catch (ArgumentException) {
			}
			catch (IOException) {
			}
			catch (SecurityException) {
			}
			catch (NotSupportedException) {
			}
			return filename;
		}
	}
}
