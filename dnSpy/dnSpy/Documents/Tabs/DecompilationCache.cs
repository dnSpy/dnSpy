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
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs {
	[ExportDocumentListListener]
	sealed class DecompilationCacheDocumentListListener : IDocumentListListener {
		readonly IDecompilationCache decompilationCache;

		[ImportingConstructor]
		DecompilationCacheDocumentListListener(IDecompilationCache decompilationCache) => this.decompilationCache = decompilationCache;

		public bool CanLoad => true;
		public bool CanReload => true;
		public void BeforeLoad(bool isReload) { }
		public void AfterLoad(bool isReload) => decompilationCache.ClearAll();
		public bool CheckCanLoad(bool isReload) => true;
	}

	[Export(typeof(IDecompilationCache))]
	sealed class DecompilationCache : IDecompilationCache {
		// How often ClearOld() is called
		const int CLEAR_OLD_ITEMS_EVERY_MS = 30 * 1000;

		// All items older than this value automatically get deleted in ClearOld()
		const int OLD_ITEM_MS = 5 * 60 * 1000;

		readonly object lockObj = new object();
		readonly Dictionary<Key, Item> cachedItems = new Dictionary<Key, Item>();

		sealed class Item {
			public DocumentViewerContent? Content;
			public IContentType ContentType;
			public WeakReference? WeakContent;
			DateTime LastHitUTC;

			/// <summary>
			/// Age since last hit
			/// </summary>
			public TimeSpan Age => DateTime.UtcNow - LastHitUTC;

			public Item(DocumentViewerContent content, IContentType contentType) {
				Content = content;
				ContentType = contentType;
				LastHitUTC = DateTime.UtcNow;
			}

			public void Hit() {
				LastHitUTC = DateTime.UtcNow;
				if (!(WeakContent is null)) {
					Content = (DocumentViewerContent?)WeakContent.Target;
					WeakContent = null;
				}
			}

			public void MakeWeakReference() {
				var content = Interlocked.CompareExchange(ref Content, null, Content);
				if (!(content is null))
					WeakContent = new WeakReference(content);
			}
		}

		readonly struct Key : IEquatable<Key> {
			public readonly IDecompiler Decompiler;
			public readonly DocumentTreeNodeData[] Nodes;
			public readonly DecompilerSettingsBase Settings;

			public Key(IDecompiler decompiler, DocumentTreeNodeData[] nodes, DecompilerSettingsBase settings) {
				Decompiler = decompiler;
				Nodes = new List<DocumentTreeNodeData>(nodes).ToArray();
				Settings = settings.Clone();
			}

			public bool Equals(Key other) {
				if (Decompiler != other.Decompiler)
					return false;

				if (Nodes.Length != other.Nodes.Length)
					return false;
				for (int i = 0; i < Nodes.Length; i++) {
					if ((object)Nodes[i] != (object)other.Nodes[i])
						return false;
				}

				if (!Settings.Equals(other.Settings))
					return false;

				return true;
			}

			public override bool Equals(object? obj) {
				if (!(obj is Key))
					return false;
				return Equals((Key)obj);
			}

			public override int GetHashCode() {
				int h = 0;
				h = Decompiler.UniqueGuid.GetHashCode();
				foreach (var node in Nodes)
					h ^= node.GetHashCode();
				h ^= Settings.GetHashCode();
				return h;
			}
		}

		readonly IDsDocumentService documentService;

		[ImportingConstructor]
		DecompilationCache(IDsDocumentService documentService) {
			this.documentService = documentService;
			AddTimerWait(this);
		}

		static void AddTimerWait(DecompilationCache dc) {
			Timer? timer = null;
			var weakSelf = new WeakReference(dc);
			timer = new Timer(a => {
				timer!.Dispose();
				if (weakSelf.Target is DecompilationCache self) {
					self.ClearOld();
					AddTimerWait(self);
				}
			}, null, Timeout.Infinite, Timeout.Infinite);
			timer.Change(CLEAR_OLD_ITEMS_EVERY_MS, Timeout.Infinite);
		}

		public DocumentViewerContent? Lookup(IDecompiler decompiler, DocumentTreeNodeData[] nodes, out IContentType? contentType) {
			var settings = decompiler.Settings;
			lock (lockObj) {
				var key = new Key(decompiler, nodes, settings);

				if (cachedItems.TryGetValue(key, out var item)) {
					contentType = item.ContentType;
					item.Hit();
					var content = item.Content;
					if (content is null)
						cachedItems.Remove(key);
					return content;
				}
			}
			contentType = null;
			return null;
		}

		public void Cache(IDecompiler decompiler, DocumentTreeNodeData[] nodes, DocumentViewerContent content, IContentType contentType) {
			var settings = decompiler.Settings;
			lock (lockObj) {
				var key = new Key(decompiler, nodes, settings);
				cachedItems[key] = new Item(content, contentType);
			}
		}

		void ClearOld() {
			lock (lockObj) {
				foreach (var kv in new List<KeyValuePair<Key, Item>>(cachedItems)) {
					if (kv.Value.Age.TotalMilliseconds > OLD_ITEM_MS) {
						kv.Value.MakeWeakReference();
						if (kv.Value.WeakContent is WeakReference wc && wc.Target is null)
							cachedItems.Remove(kv.Key);
					}
				}
			}
		}

		public void ClearAll() {
			lock (lockObj)
				cachedItems.Clear();
		}

		public void Clear(HashSet<IDsDocument?> modules) {
			lock (lockObj) {
				foreach (var kv in cachedItems.ToArray()) {
					if (InModifiedModuleHelper.IsInModifiedModule(modules, kv.Key.Nodes) ||
						IsInModifiedModule(documentService, modules, kv.Value)) {
						cachedItems.Remove(kv.Key);
						continue;
					}
				}
			}
		}

		static bool IsInModifiedModule(IDsDocumentService documentService, HashSet<IDsDocument?> modules, Item item) {
			var result = item.Content;
			if (result is null && !(item.WeakContent is null))
				result = (DocumentViewerContent?)item.WeakContent.Target;
			var refs = result?.ReferenceCollection;
			if (refs is null)
				return false;
			return InModifiedModuleHelper.IsInModifiedModule(documentService, modules, refs.Select(a => a.Data.Reference));
		}
	}

	static class InModifiedModuleHelper {
		public static bool IsInModifiedModule(HashSet<IDsDocument?> modules, IEnumerable<DocumentTreeNodeData> nodes) {
			foreach (var node in nodes) {
				var modNode = (DsDocumentNode?)node.GetModuleNode() ?? node.GetAssemblyNode();
				if (modNode is null || modules.Contains(modNode.Document))
					return true;
			}

			return false;
		}

		public static bool IsInModifiedModule(IDsDocumentService documentService, HashSet<IDsDocument?> modules, IEnumerable<object?> references) {
			var checkedAsmRefs = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			foreach (var r in references.Distinct()) {
				IAssembly? asmRef = null;
				if (r is IType t)
					asmRef = t.DefinitionAssembly;
				if (asmRef is null && r is IMemberRef) {
					var type = ((IMemberRef)r).DeclaringType;
					if (!(type is null))
						asmRef = type.DefinitionAssembly;
				}
				if (!(asmRef is null) && !checkedAsmRefs.Contains(asmRef)) {
					checkedAsmRefs.Add(asmRef);
					var asm = documentService.FindAssembly(asmRef);
					if (!(asm is null) && modules.Contains(asm))
						return true;
				}
			}

			return false;
		}
	}
}
