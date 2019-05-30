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
using System.Collections.Concurrent;
using System.Threading;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed class WpfDebugMessageDispatcher : IDebugMessageDispatcher {
		readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
		int callingEmptyQueue;
		readonly Dispatcher dispatcher;

		public WpfDebugMessageDispatcher(Dispatcher dispatcher) => this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

		Dispatcher? Dispatcher => !dispatcher.HasShutdownFinished && !dispatcher.HasShutdownStarted ? dispatcher : null;

		public void ExecuteAsync(Action callback) {
			var disp = Dispatcher;
			if (disp is null)
				return;
			queue.Enqueue(callback);
			dispatchQueueEvent.Set();
			if (disp.CheckAccess())
				EmptyQueue();
			else if (callingEmptyQueue == 0) {
				Interlocked.Increment(ref callingEmptyQueue);
				disp.BeginInvoke(() => {
					Interlocked.Decrement(ref callingEmptyQueue);
					EmptyQueue();
				});
			}
		}

		void EmptyQueue() {
			var disp = Dispatcher;
			if (disp is null)
				return;
			disp.VerifyAccess();

			while (queue.TryDequeue(out var action))
				action();
		}

		public object? DispatchQueue(TimeSpan waitTime, out bool timedOut) {
			var disp = Dispatcher;
			if (disp is null) {
				timedOut = true;
				return null;
			}
			bool timedOutTmp = true;
			var res = disp.Invoke(() => DispatchQueueCore(waitTime, out timedOutTmp));
			timedOut = timedOutTmp;
			return res;
		}

		object? DispatchQueueCore(TimeSpan waitTime, out bool timedOut) {
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
					while (queue.TryDequeue(out var action))
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
		volatile object? resultDispatchQueue;
		volatile bool cancelDispatchQueue;
		int counterDispatchQueue;

		public void CancelDispatchQueue(object? result) {
			resultDispatchQueue = result;
			cancelDispatchQueue = true;
			dispatchQueueEvent.Set();
		}
	}

	interface IDebugMessageDispatcher {
		/// <summary>
		/// Executes <paramref name="action"/> on the engine thread.
		/// </summary>
		/// <param name="action">Code to execute on the dndbg thread</param>
		void ExecuteAsync(Action action);

		/// <summary>
		/// Empty the queue and wait for <see cref="CancelDispatchQueue(object)"/> to get called.
		/// The return value is the input to <see cref="CancelDispatchQueue(object)"/> unless it
		/// timed out.
		/// </summary>
		/// <param name="waitTime">Time to wait or -1 to wait forever</param>
		/// <param name="timedOut">Set to true if it timed out</param>
		/// <returns></returns>
		object? DispatchQueue(TimeSpan waitTime, out bool timedOut);

		/// <summary>
		/// Cancels <see cref="DispatchQueue(TimeSpan,out bool)"/>
		/// </summary>
		/// <param name="result">Result</param>
		void CancelDispatchQueue(object? result);
	}
}
