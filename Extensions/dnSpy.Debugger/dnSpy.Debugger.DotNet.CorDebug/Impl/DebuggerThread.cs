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
using System.Threading;
using dndbg.Engine;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DebuggerThread {
		Dispatcher Dispatcher { get; set; }

		readonly Thread debuggerThread;
		volatile bool terminate;
		AutoResetEvent callDispatcherRunEvent;
		readonly string threadName;

		DebugMessageDispatcher DebugMessageDispatcher {
			get {
				if (__debugMessageDispatcher is null)
					Interlocked.CompareExchange(ref __debugMessageDispatcher, new DebugMessageDispatcher(Dispatcher), null);
				return __debugMessageDispatcher!;
			}
		}
		volatile DebugMessageDispatcher? __debugMessageDispatcher;

		public DebuggerThread(string threadName) {
			Dispatcher = null!;
			this.threadName = threadName;
			var autoResetEvent = new AutoResetEvent(false);
			callDispatcherRunEvent = new AutoResetEvent(false);
			debuggerThread = new Thread(() => DebuggerThreadProc(autoResetEvent));
			debuggerThread.IsBackground = true;
			debuggerThread.SetApartmentState(ApartmentState.STA);
			debuggerThread.Start();

			try {
				autoResetEvent.WaitOne();
				autoResetEvent.Dispose();
			}
			catch {
				Terminate();
				throw;
			}
		}

		void DebuggerThreadProc(AutoResetEvent autoResetEvent) {
			Thread.CurrentThread.Name = threadName;
			Dispatcher = new Dispatcher();
			autoResetEvent.Set();

			callDispatcherRunEvent.WaitOne();
			callDispatcherRunEvent.Close();
			callDispatcherRunEvent = null!;

			if (!terminate)
				Dispatcher.Run();
		}

		internal void CallDispatcherRun() => callDispatcherRunEvent.Set();

		internal void Terminate() {
			terminate = true;
			try { callDispatcherRunEvent?.Set(); } catch (ObjectDisposedException) { }
			if (Dispatcher is not null && !Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
				Dispatcher.BeginInvokeShutdown();
		}

		public IDebugMessageDispatcher GetDebugMessageDispatcher() => DebugMessageDispatcher;

		public bool HasShutdownStarted => Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished;
		public bool CheckAccess() => Dispatcher.CheckAccess();
		public void VerifyAccess() => Dispatcher.VerifyAccess();
		public T Invoke<T>(Func<T> callback) {
			System.Diagnostics.Debugger.NotifyOfCrossThreadDependency();
			return Dispatcher.Invoke(callback);
		}
		public void Invoke(Action callback) {
			System.Diagnostics.Debugger.NotifyOfCrossThreadDependency();
			Dispatcher.Invoke(callback);
		}
		public void BeginInvoke(Action callback) {
			if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished) {
				// We don't use Dispatcher here because we could get a CreateThread event,
				// which will notify DbgManager. It will then call engine.Run() which must
				// continue the process even if we're func evaluating. If we use Dispatcher,
				// we'll block here since WpfDebugMessageDispatcher is waiting for an event.
				DebugMessageDispatcher.ExecuteAsync(callback);
			}
		}
	}
}
