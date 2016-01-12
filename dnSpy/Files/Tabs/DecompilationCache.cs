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
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Files.Tabs {
	[ExportFileListListener]
	sealed class DecompilationCacheFileListListener : IFileListListener {
		readonly IDecompilationCache decompilationCache;

		[ImportingConstructor]
		DecompilationCacheFileListListener(IDecompilationCache decompilationCache) {
			this.decompilationCache = decompilationCache;
		}

		public bool CanLoad {
			get { return true; }
		}

		public bool CanReload {
			get { return true; }
		}

		public void BeforeLoad(bool isReload) {
		}

		public void AfterLoad(bool isReload) {
			decompilationCache.ClearAll();
		}

		public bool CheckCanLoad(bool isReload) {
			return true;
		}
	}

	[Export(typeof(IDecompilationCache)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DecompilationCache : IDecompilationCache {
		// How often ClearOld() is called
		const int CLEAR_OLD_ITEMS_EVERY_MS = 30 * 1000;

		// All items older than this value automatically get deleted in ClearOld()
		const int OLD_ITEM_MS = 5 * 60 * 1000;

		readonly object lockObj = new object();
		readonly Dictionary<Key, Item> cachedItems = new Dictionary<Key, Item>();

		sealed class Item {
			public AvalonEditTextOutput TextOutput;
			public IHighlightingDefinition Highlighting;
			public WeakReference WeakTextOutput;
			DateTime LastHitUTC;

			/// <summary>
			/// Age since last hit
			/// </summary>
			public TimeSpan Age {
				get { return DateTime.UtcNow - LastHitUTC; }
			}

			public Item(AvalonEditTextOutput textOutput, IHighlightingDefinition highlighting) {
				this.TextOutput = textOutput;
				this.Highlighting = highlighting;
				this.LastHitUTC = DateTime.UtcNow;
			}

			public void Hit() {
				LastHitUTC = DateTime.UtcNow;
				if (WeakTextOutput != null) {
					TextOutput = (AvalonEditTextOutput)WeakTextOutput.Target;
					WeakTextOutput = null;
				}
			}

			public void MakeWeakReference() {
				var textOutput = Interlocked.CompareExchange(ref this.TextOutput, null, this.TextOutput);
				if (textOutput != null)
					this.WeakTextOutput = new WeakReference(textOutput);
			}
		}

		struct Key : IEquatable<Key> {
			public readonly ILanguage ILanguage;
			public readonly IFileTreeNodeData[] Nodes;
			public readonly DecompilationOptions Options;

			public Key(ILanguage language, IFileTreeNodeData[] nodes, DecompilationOptions options) {
				this.ILanguage = language;
				this.Nodes = new List<IFileTreeNodeData>(nodes).ToArray();
				this.Options = Clone(options);
			}

			static DecompilationOptions Clone(DecompilationOptions options) {
				var newOpts = new DecompilationOptions();
				newOpts.CancellationToken = CancellationToken.None;
				newOpts.DecompilerSettings = options.DecompilerSettings.Clone();
				newOpts.DontShowCreateMethodBodyExceptions = options.DontShowCreateMethodBodyExceptions;
				return newOpts;
			}

			public bool Equals(Key other) {
				if (ILanguage != other.ILanguage)
					return false;

				if (Nodes.Length != other.Nodes.Length)
					return false;
				for (int i = 0; i < Nodes.Length; i++) {
					if ((object)Nodes[i] != (object)other.Nodes[i])
						return false;
				}

				if (!Equals(Options, other.Options))
					return false;

				return true;
			}

			public override bool Equals(object obj) {
				if (!(obj is Key))
					return false;
				return Equals((Key)obj);
			}

			public override int GetHashCode() {
				int h = 0;
				h = ILanguage.UniqueGuid.GetHashCode();
				foreach (var node in Nodes)
					h ^= node.GetHashCode();
				h ^= GetHashCode(Options);
				return h;
			}

			static int GetHashCode(DecompilationOptions options) {
				int h = 0;

				// Ignore: ProjectOptions
				// Ignore: CancellationToken

				h ^= options.DecompilerSettings.GetHashCode();
				h ^= options.DontShowCreateMethodBodyExceptions ? int.MinValue : 0;

				return h;
			}

			static bool Equals(DecompilationOptions a, DecompilationOptions b) {
				if (a == b)
					return true;
				if (a == null || b == null)
					return false;

				// Ignore: ProjectOptions
				// Ignore: CancellationToken

				if (!a.DecompilerSettings.Equals(b.DecompilerSettings))
					return false;

				if (a.DontShowCreateMethodBodyExceptions != b.DontShowCreateMethodBodyExceptions)
					return false;

				return true;
			}
		}

		readonly IFileManager fileManager;

		[ImportingConstructor]
		DecompilationCache(IFileManager fileManager) {
			this.fileManager = fileManager;
			AddTimerWait(this);
		}

		static void AddTimerWait(DecompilationCache dc) {
			Timer timer = null;
			WeakReference weakSelf = new WeakReference(dc);
			timer = new Timer(a => {
				timer.Dispose();
				var self = (DecompilationCache)weakSelf.Target;
				if (self != null) {
					self.ClearOld();
					AddTimerWait(self);
				}
			}, null, CLEAR_OLD_ITEMS_EVERY_MS, Timeout.Infinite);
		}

		public AvalonEditTextOutput Lookup(ILanguage language, IFileTreeNodeData[] nodes, DecompilationOptions options, out IHighlightingDefinition highlighting) {
			highlighting = null;
			lock (lockObj) {
				var key = new Key(language, nodes, options);

				Item item;
				if (cachedItems.TryGetValue(key, out item)) {
					highlighting = item.Highlighting;
					item.Hit();
					var to = item.TextOutput;
					if (to == null)
						cachedItems.Remove(key);
					return to;
				}
			}
			return null;
		}

		public void Cache(ILanguage language, IFileTreeNodeData[] nodes, DecompilationOptions options, AvalonEditTextOutput textOutput, IHighlightingDefinition highlighting) {
			if (!textOutput.CanBeCached)
				return;
			lock (lockObj) {
				var key = new Key(language, nodes, options);
				cachedItems[key] = new Item(textOutput, highlighting);
			}
		}

		void ClearOld() {
			lock (lockObj) {
				foreach (var kv in new List<KeyValuePair<Key, Item>>(cachedItems)) {
					if (kv.Value.Age.TotalMilliseconds > OLD_ITEM_MS) {
						kv.Value.MakeWeakReference();
						if (kv.Value.WeakTextOutput != null && kv.Value.WeakTextOutput.Target == null)
							cachedItems.Remove(kv.Key);
					}
				}
			}
		}

		public void ClearAll() {
			lock (lockObj)
				cachedItems.Clear();
		}

		public void Clear(HashSet<IDnSpyFile> modules) {
			lock (lockObj) {
				foreach (var kv in cachedItems.ToArray()) {
					if (InModifiedModuleHelper.IsInModifiedModule(modules, kv.Key.Nodes) ||
						IsInModifiedModule(fileManager, modules, kv.Value)) {
						cachedItems.Remove(kv.Key);
						continue;
					}
				}
			}
		}

		static bool IsInModifiedModule(IFileManager fileManager, HashSet<IDnSpyFile> modules, Item item) {
			var textOutput = item.TextOutput;
			if (textOutput == null && item.WeakTextOutput != null)
				textOutput = (AvalonEditTextOutput)item.WeakTextOutput.Target;
			if (textOutput == null)
				return true;
			var refs = textOutput.References;
			if (refs == null)
				return false;
			return InModifiedModuleHelper.IsInModifiedModule(fileManager, modules, refs.Select(a => a.Reference));
		}
	}

	static class InModifiedModuleHelper {
		public static bool IsInModifiedModule(HashSet<IDnSpyFile> modules, IEnumerable<IFileTreeNodeData> nodes) {
			foreach (var node in nodes) {
				var modNode = (IDnSpyFileNode)node.GetModuleNode() ?? node.GetAssemblyNode();
				if (modNode == null || modules.Contains(modNode.DnSpyFile))
					return true;
			}

			return false;
		}

		public static bool IsInModifiedModule(IFileManager fileManager, HashSet<IDnSpyFile> modules, IEnumerable<object> references) {
			var checkedAsmRefs = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			foreach (var r in references) {
				IAssembly asmRef = null;
				if (r is IType)
					asmRef = (r as IType).DefinitionAssembly;
				if (asmRef == null && r is IMemberRef) {
					var type = ((IMemberRef)r).DeclaringType;
					if (type != null)
						asmRef = type.DefinitionAssembly;
				}
				if (asmRef != null && !checkedAsmRefs.Contains(asmRef)) {
					checkedAsmRefs.Add(asmRef);
					var asm = fileManager.FindAssembly(asmRef);
					if (asm != null && modules.Contains(asm))
						return true;
				}
			}

			return false;
		}
	}
}
