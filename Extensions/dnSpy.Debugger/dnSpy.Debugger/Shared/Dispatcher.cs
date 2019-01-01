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
using System.Threading;
using System.Threading.Tasks;

namespace dnSpy.Debugger.Shared {
	sealed class Dispatcher {
		public bool HasShutdownStarted => hasShutdownStarted;
		public bool HasShutdownFinished => hasShutdownFinished;

		readonly object lockObj;
		Thread thread;
		Queue<Action> queue;
		AutoResetEvent queueEvent;
		volatile bool beginShutdownCalled;
		volatile bool hasShutdownStarted;
		volatile bool hasShutdownFinished;

		public Dispatcher() {
			lockObj = new object();
			thread = Thread.CurrentThread;
			queue = new Queue<Action>();
			queueEvent = new AutoResetEvent(false);
		}

		public bool CheckAccess() => thread == Thread.CurrentThread;

		public void VerifyAccess() {
			if (!CheckAccess())
				throw new InvalidOperationException("Invalid thread");
		}

		public void BeginInvokeShutdown() => BeginInvoke(BeginInvokeShutdownCore);

		void BeginInvokeShutdownCore() {
			VerifyAccess();
			if (beginShutdownCalled)
				return;
			beginShutdownCalled = true;
			lock (lockObj)
				hasShutdownStarted = true;
		}

		public void BeginInvoke(Action callback) => BeginInvoke(callback, throwIfShutdownStarted: false);

		void BeginInvoke(Action callback, bool throwIfShutdownStarted) {
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			lock (lockObj) {
				if (hasShutdownStarted) {
					if (throwIfShutdownStarted)
						throw new TaskCanceledException();
					return;
				}
				queue.Enqueue(callback);
			}
			queueEvent.Set();
		}

		public TResult Invoke<TResult>(Func<TResult> callback) {
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			if (CheckAccess()) {
				var prevContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(new SynchronizationContextImpl(this));
				try {
					return callback();
				}
				finally {
					SynchronizationContext.SetSynchronizationContext(prevContext);
				}
			}
			else {
				using (var ev = new ManualResetEvent(false)) {
					TResult result = default;
					BeginInvoke(() => {
						result = callback();
						ev.Set();
					}, throwIfShutdownStarted: true);
					ev.WaitOne();
					return result;
				}
			}
		}

		public void Invoke(Action callback) {
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			Invoke<object>(() => { callback(); return null; });
		}

		bool TryDequeue(out Action callback) {
			lock (lockObj) {
				if (queue.Count == 0) {
					callback = null;
					return false;
				}
				else {
					callback = queue.Dequeue();
					return true;
				}
			}
		}

		public void Run() {
			VerifyAccess();

			while (true) {
				queueEvent.WaitOne();
				RunCallbacks();
				if (hasShutdownStarted)
					break;
			}
			RunCallbacks();
			hasShutdownFinished = true;
			thread = null;
			queue = null;
			queueEvent.Dispose();
			queueEvent = null;
		}

		void RunCallbacks() {
			var prevContext = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContextImpl(this));
			try {
				while (TryDequeue(out var callback))
					callback();
			}
			finally {
				SynchronizationContext.SetSynchronizationContext(prevContext);
			}
		}
	}

	sealed class SynchronizationContextImpl : SynchronizationContext {
		readonly Dispatcher dispatcher;

		public SynchronizationContextImpl(Dispatcher dispatcher) =>
			this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

		public override void Send(SendOrPostCallback d, object state) =>
			dispatcher.Invoke(() => d.Invoke(state));

		public override void Post(SendOrPostCallback d, object state) =>
			dispatcher.BeginInvoke(() => d.Invoke(state));

		public override SynchronizationContext CreateCopy() =>
			new SynchronizationContextImpl(dispatcher);
	}
}
