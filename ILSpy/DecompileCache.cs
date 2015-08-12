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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy {
	/// <summary>
	/// Caches decompiled output
	/// </summary>
	sealed class DecompileCache {
		public static readonly DecompileCache Instance = new DecompileCache();

		// How often ClearOld() is called
		const int CLEAR_OLD_ITEMS_EVERY_MS = 30 * 1000;

		// All items older than this value automatically get deleted in ClearOld()
		const int OLD_ITEM_MS = 5 * 60 * 1000;

		readonly object lockObj = new object();
		readonly Dictionary<Key, Item> cachedItems = new Dictionary<Key, Item>();

		sealed class Item {
			public AvalonEditTextOutput TextOutput;
			public WeakReference WeakTextOutput;
			DateTime LastHitUTC;

			/// <summary>
			/// Age since last hit
			/// </summary>
			public TimeSpan Age {
				get { return DateTime.UtcNow - LastHitUTC; }
			}

			public Item(AvalonEditTextOutput textOutput) {
				this.TextOutput = textOutput;
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
			public readonly Language Language;
			public readonly ILSpyTreeNode[] TreeNodes;
			public readonly DecompilationOptions Options;

			public Key(Language language, ILSpyTreeNode[] treeNodes, DecompilationOptions options) {
				this.Language = language;
				this.TreeNodes = new List<ILSpyTreeNode>(treeNodes).ToArray();
				this.Options = Clone(options);
			}

			static DecompilationOptions Clone(DecompilationOptions options) {
				var newOpts = options.SimpleClone();
				newOpts.DecompilerSettings = (DecompilerSettings)options.DecompilerSettings.Clone();
				newOpts.TextViewState = null;   // Ignore it; we don't use it
				return newOpts;
			}

			public bool Equals(Key other) {
				if (Language != other.Language)
					return false;

				if (TreeNodes.Length != other.TreeNodes.Length)
					return false;
				for (int i = 0; i < TreeNodes.Length; i++) {
					if ((object)TreeNodes[i] != (object)other.TreeNodes[i])
						return false;
				}

				if (!Options.Equals(other.Options))
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
				h = Language.Name.GetHashCode();
				foreach (var node in TreeNodes)
					h ^= node.GetHashCode();
				h ^= Options.GetHashCode();
				return h;
			}
		}

		public DecompileCache() {
			AddTimerWait(this);
		}

		static void AddTimerWait(DecompileCache dc) {
			Timer timer = null;
			WeakReference weakSelf = new WeakReference(dc);
			timer = new Timer(a => {
				timer.Dispose();
				var self = (DecompileCache)weakSelf.Target;
				if (self != null) {
					self.ClearOld();
					AddTimerWait(self);
				}
			}, null, CLEAR_OLD_ITEMS_EVERY_MS, Timeout.Infinite);
		}

		public AvalonEditTextOutput Lookup(Language language, ILSpyTreeNode[] treeNodes, DecompilationOptions options) {
			lock (lockObj) {
				var key = new Key(language, treeNodes, options);

				Item item;
				if (cachedItems.TryGetValue(key, out item)) {
					item.Hit();
					var to = item.TextOutput;
					if (to == null)
						cachedItems.Remove(key);
					return to;
				}
			}
			return null;
		}

		public void Cache(Language language, ILSpyTreeNode[] treeNodes, DecompilationOptions options, AvalonEditTextOutput textOutput) {
			if (!textOutput.CanBeCached)
				return;
			lock (lockObj) {
				var key = new Key(language, treeNodes, options);
				cachedItems[key] = new Item(textOutput);
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

		public void Clear(LoadedAssembly asm) {
			Clear(new HashSet<LoadedAssembly>(new[] { asm }));
		}

		public void Clear(HashSet<LoadedAssembly> asms) {
			lock (lockObj) {
				foreach (var kv in cachedItems.ToArray()) {
					if (IsInModifiedAssembly(asms, kv.Key.TreeNodes) ||
						IsInModifiedAssembly(asms, kv.Value)) {
						cachedItems.Remove(kv.Key);
						continue;
					}
				}
			}
		}

		internal static bool IsInModifiedAssembly(HashSet<LoadedAssembly> asms, ILSpyTreeNode[] nodes) {
			foreach (var node in nodes) {
				var asmNode = MainWindow.GetAssemblyTreeNode(node);
				if (asmNode == null || asms.Contains(asmNode.LoadedAssembly))
					return true;
			}

			return false;
		}

		static bool IsInModifiedAssembly(HashSet<LoadedAssembly> asms, Item item) {
			var textOutput = item.TextOutput;
			if (textOutput == null && item.WeakTextOutput != null)
				textOutput = (AvalonEditTextOutput)item.WeakTextOutput.Target;
			if (textOutput == null)
				return true;

			return IsInModifiedAssembly(asms, textOutput.References);
		}

		internal static bool IsInModifiedAssembly(HashSet<LoadedAssembly> asms, TextSegmentCollection<ReferenceSegment> references) {
			if (references == null)
				return false;
			var checkedAsmRefs = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			foreach (var refSeg in references) {
				var r = refSeg.Reference;
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
					var asm = MainWindow.Instance.CurrentAssemblyList.FindAssemblyByAssemblyName(asmRef.FullName);
					if (asm != null && asms.Contains(asm))
						return true;
				}
			}

			return false;
		}
	}
}
