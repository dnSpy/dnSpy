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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace dnSpy.Events {
	// Not thread safe, uses MethodInfo.Invoke() to invoke original method
	sealed class WeakEventList<TEventArgs> where TEventArgs : EventArgs {
		readonly List<Info> handlers;

		abstract class Info {
			public abstract bool IsAlive { get; }
			public abstract void Execute(object source, TEventArgs e);
			public abstract bool Equals(EventHandler<TEventArgs> h);
			public static Info Create(EventHandler<TEventArgs> h) {
				if (h.Target == null)
					return new HardRefInfo(h);

				// Need to add check for cases when there's no 'this' pointer in h.Target
				bool compilerGenerated = h.Target.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false);
				Debug.Assert(!compilerGenerated, string.Format("Event handler {0} is compiler generated (probably a lambda expression) and can't be removed from the event", h.Method));
				if (compilerGenerated)
					return new HardRefInfo(h);

				return new InstanceInfo(h);
			}
		}

		sealed class HardRefInfo : Info {
			public override bool IsAlive => true;
			public override void Execute(object source, TEventArgs e) => handler(source, e);
			public override bool Equals(EventHandler<TEventArgs> h) => handler == h;

			readonly EventHandler<TEventArgs> handler;

			public HardRefInfo(EventHandler<TEventArgs> handler) {
				this.handler = handler;
			}
		}

		sealed class InstanceInfo : Info {
			public override bool IsAlive => target.Target != null;

			public override void Execute(object source, TEventArgs e) {
				var self = target.Target;
				if (self != null)
					methodInfo.Invoke(self, new object[] { source, e });
			}

			public override bool Equals(EventHandler<TEventArgs> h) =>
				h.Target == target.Target && h.Method == methodInfo;

			readonly WeakReference target;
			readonly MethodInfo methodInfo;

			public InstanceInfo(EventHandler<TEventArgs> handler) {
				Debug.Assert(handler.Target != null);
				Debug.Assert(handler.GetInvocationList().Length == 1);
				this.target = new WeakReference(handler.Target);
				this.methodInfo = handler.Method;
			}
		}

		public WeakEventList() {
			this.handlers = new List<Info>();
		}

		public void Add(EventHandler<TEventArgs> h) {
			if (h == null)
				throw new ArgumentNullException(nameof(h));
			handlers.Add(Info.Create(h));
		}

		public void Remove(EventHandler<TEventArgs> h) {
			if (h == null)
				throw new ArgumentNullException(nameof(h));
			for (int i = 0; i < handlers.Count; i++) {
				if (handlers[i].Equals(h)) {
					handlers.RemoveAt(i);
					return;
				}
			}
		}

		public void Raise(object sender, TEventArgs e) {
			if (handlers.Count == 0)
				return;
			var newList = new List<Info>(handlers.Count);
			foreach (var info in handlers) {
				if (info.IsAlive)
					newList.Add(info);
			}
			if (newList.Count != handlers.Count) {
				handlers.Clear();
				handlers.AddRange(newList);
			}
			foreach (var info in newList)
				info.Execute(sender, e);
		}
	}
}
