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
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Threading;
using dndbg.Engine;

namespace dnSpy.Debugger {
	sealed class WpfDebugMessageDispatcher : IDebugMessageDispatcher {
		public static readonly WpfDebugMessageDispatcher Instance = new WpfDebugMessageDispatcher();

		readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
		int callingEmptyQueue;
		readonly Dispatcher dispatcher;

		WpfDebugMessageDispatcher() {
			this.dispatcher = Dispatcher.CurrentDispatcher;
		}

		Dispatcher Dispatcher => !dispatcher.HasShutdownFinished && !dispatcher.HasShutdownStarted ? dispatcher : null;

		public void ExecuteAsync(Action action) {
			var disp = Dispatcher;
			if (disp == null)
				return;
			queue.Enqueue(action);
			dispatchQueueEvent.Set();
			if (disp.CheckAccess())
				EmptyQueue();
			else if (callingEmptyQueue == 0) {
				Interlocked.Increment(ref callingEmptyQueue);
				disp.BeginInvoke(DispatcherPriority.Send, new Action(() => {
					Interlocked.Decrement(ref callingEmptyQueue);
					EmptyQueue();
				}));
			}
		}

		void EmptyQueue() {
			var disp = Dispatcher;
			if (disp == null)
				return;
			disp.VerifyAccess();

			Action action;
			while (queue.TryDequeue(out action))
				action();
		}

		public object DispatchQueue(TimeSpan waitTime, out bool timedOut) {
			var disp = Dispatcher;
			if (disp == null) {
				timedOut = true;
				return null;
			}
			disp.VerifyAccess();
			try {
				if (Interlocked.Increment(ref counterDispatchQueue) != 1)
					throw new InvalidOperationException("DispatchQueue can't be nested");

				timedOut = false;
				resultDispatchQueue = null;
				cancelDispatchQueue = false;
				var startTime = DateTime.UtcNow;
				var infTime = TimeSpan.FromMilliseconds(-1);
				var endTime = startTime + waitTime;
				while (!cancelDispatchQueue) {
					Action action;
					while (queue.TryDequeue(out action))
						action();
					if (cancelDispatchQueue)
						break;

					var wait = waitTime;
					if (waitTime != infTime) {
						var now = DateTime.UtcNow;
						if (now >= endTime)
							wait = TimeSpan.Zero;
						else
							wait = endTime - now;
					}
					bool signaled = dispatchQueueEvent.WaitOne(waitTime);
					if (!signaled) {
						timedOut = true;
						return null;
					}
				}

				return resultDispatchQueue;
			}
			finally {
				Interlocked.Decrement(ref counterDispatchQueue);
				// Make sure we don't hold onto stuff that the caller might want to get GC'd
				resultDispatchQueue = null;
			}
		}
		readonly AutoResetEvent dispatchQueueEvent = new AutoResetEvent(false);
		volatile object resultDispatchQueue;
		volatile bool cancelDispatchQueue;
		int counterDispatchQueue;

		public void CancelDispatchQueue(object result) {
			resultDispatchQueue = result;
			cancelDispatchQueue = true;
			dispatchQueueEvent.Set();
		}
	}
}
