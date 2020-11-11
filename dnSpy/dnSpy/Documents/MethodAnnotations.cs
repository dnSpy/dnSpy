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
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents {
	[Export(typeof(IMethodAnnotations))]
	sealed class MethodAnnotations : IMethodAnnotations {
		const int DELETE_GCD_ITEMS_EVERY_MS = 5 * 60 * 1000;
		readonly object lockObj = new object();
		readonly Dictionary<Key, bool> infos = new Dictionary<Key, bool>();

		readonly struct Key : IEquatable<Key> {
			public readonly WeakReference method;
			readonly int hc;

			public Key(MethodDef method) {
				this.method = new WeakReference(method);
				hc = method.GetHashCode();
			}

			public bool Equals(Key other) {
				var m = method.Target;
				var om = other.method.Target;
				if (m is null || om is null)
					return false;
				return m == om;
			}

			public override bool Equals(object? obj) {
				if (!(obj is Key))
					return false;
				return Equals((Key)obj);
			}

			public override int GetHashCode() => hc;
			public override string? ToString() => method.Target?.ToString();
		}

		MethodAnnotations() => AddTimerWait(this);

		static void AddTimerWait(MethodAnnotations ma) {
			Timer? timer = null;
			WeakReference weakSelf = new WeakReference(ma);
			timer = new Timer(a => {
				Debug2.Assert(timer is not null);
				timer.Dispose();
				if (weakSelf.Target is MethodAnnotations self) {
					self.ClearGarbageCollectedItems();
					AddTimerWait(self);
				}
			}, null, Timeout.Infinite, Timeout.Infinite);
			timer.Change(DELETE_GCD_ITEMS_EVERY_MS, Timeout.Infinite);
		}

		public bool IsBodyModified(MethodDef method) {
			bool modified;
			lock (lockObj)
				infos.TryGetValue(new Key(method), out modified);
			return modified;
		}

		public void SetBodyModified(MethodDef method, bool isModified) {
			var key = new Key(method);
			lock (lockObj) {
				if (isModified)
					infos[key] = isModified;
				else
					infos.Remove(key);
			}
		}

		void ClearGarbageCollectedItems() {
			lock (lockObj) {
				foreach (var kv in new List<KeyValuePair<Key, bool>>(infos)) {
					if (kv.Key.method.Target is null)
						infos.Remove(kv.Key);
				}
			}
		}
	}
}
