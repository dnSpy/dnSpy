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
using dnSpy.Contracts.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	interface IHexDocumentService {
		HexDocument GetOrCreate(IPEImage peImage);
		HexDocument GetOrCreate(string filename);
		AsmEdHexDocument[] GetDocuments();
		AsmEdHexDocument TryGet(string filename);
		void Clear();
	}

	[Export(typeof(IHexDocumentService))]
	sealed class HexDocumentService : IHexDocumentService {
		readonly object lockObj = new object();
		readonly Dictionary<string, object> filenameToDoc = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		readonly IUndoCommandService undoCommandService;

		[ImportingConstructor]
		HexDocumentService(IUndoCommandService undoCommandService) {
			this.undoCommandService = undoCommandService;
			undoCommandService.OnEvent += UndoCommandService_OnEvent;
		}

		void UndoCommandService_OnEvent(object sender, UndoCommandServiceEventArgs e) {
			var doc = HexUndoableDocumentsProvider.TryGetAsmEdHexDocument(e.UndoObject);
			if (doc == null)
				return;

			if (e.Type == UndoCommandServiceEventType.Saved)
				OnDocumentSaved(doc);
			else if (e.Type == UndoCommandServiceEventType.Dirty)
				OnDocumentDirty(doc);
		}

		void OnDocumentSaved(AsmEdHexDocument doc) {
			lock (lockObj) {
				object dictObj;
				bool b = filenameToDoc.TryGetValue(doc.Name, out dictObj);
				Debug.Assert(b);
				if (!b)
					return;
				if (dictObj is WeakReference) {
					Debug.Assert(((WeakReference)dictObj).Target == doc);
					return;
				}
				Debug.Assert(doc == dictObj);
				filenameToDoc[doc.Name] = new WeakReference(doc);
			}
		}

		void OnDocumentDirty(AsmEdHexDocument doc) {
			lock (lockObj) {
				object dictObj;
				bool b = filenameToDoc.TryGetValue(doc.Name, out dictObj);
				Debug.Assert(b);
				if (!b)
					return;
				filenameToDoc[doc.Name] = doc;
			}
		}

		public bool Exists(string filename) => TryGet(filename) != null;

		public void Clear() {
			lock (lockObj)
				filenameToDoc.Clear();
		}

		public AsmEdHexDocument TryGet(string filename) {
			filename = GetFullPath(filename);

			lock (lockObj)
				return TryGet_NoLock(filename);
		}

		AsmEdHexDocument TryGet_NoLock(string filename) {
			object obj;
			if (!filenameToDoc.TryGetValue(filename, out obj))
				return null;
			return TryGetDoc(obj);
		}

		AsmEdHexDocument TryGetDoc(object obj) {
			var doc = obj as AsmEdHexDocument;
			if (doc != null)
				return doc;
			var weakRef = obj as WeakReference;
			Debug.Assert(weakRef != null);
			return weakRef == null ? null : weakRef.Target as AsmEdHexDocument;
		}

		public AsmEdHexDocument GetOrCreate(string filename) {
			if (!File.Exists(filename))
				return null;
			filename = GetFullPath(filename);

			lock (lockObj) {
				var doc = TryGet_NoLock(filename);
				if (doc != null)
					return doc;

				byte[] data;
				try {
					data = File.ReadAllBytes(filename);
				}
				catch {
					return null;
				}

				doc = new AsmEdHexDocument(undoCommandService, data, filename);
				filenameToDoc[filename] = new WeakReference(doc);
				return doc;
			}
		}

		public AsmEdHexDocument GetOrCreate(IPEImage peImage) {
			var filename = GetFullPath(peImage.FileName);

			lock (lockObj) {
				var doc = TryGet_NoLock(filename);
				if (doc != null)
					return doc;

				using (var stream = peImage.CreateFullStream()) {
					var data = stream.ReadAllBytes();
					doc = new AsmEdHexDocument(undoCommandService, data, filename);
					filenameToDoc[filename] = new WeakReference(doc);
					return doc;
				}
			}
		}

		HexDocument IHexDocumentService.GetOrCreate(IPEImage peImage) => GetOrCreate(peImage);
		HexDocument IHexDocumentService.GetOrCreate(string filename) => GetOrCreate(filename);

		public AsmEdHexDocument[] GetDocuments() {
			lock (lockObj)
				return filenameToDoc.Values.Select(a => TryGetDoc(a)).Where(a => a != null).ToArray();
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
