/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Threading;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DebuggerThread {
		public sealed class Input {
			public volatile Dispatcher Dispatcher;
			public AutoResetEvent AutoResetEvent = new AutoResetEvent(false);
		}

		readonly Thread debuggerThread;
		volatile bool terminate;
		AutoResetEvent callDispatcherRunEvent;

		public DebuggerThread(Input threadInput) {
			callDispatcherRunEvent = new AutoResetEvent(false);
			debuggerThread = new Thread(() => DebuggerThreadProc(threadInput));
			debuggerThread.IsBackground = true;
			debuggerThread.SetApartmentState(ApartmentState.STA);
			debuggerThread.Start();
		}

		public void DebuggerThreadProc(Input threadInput) {
			Thread.CurrentThread.Name = "CorDebug";
			threadInput.Dispatcher = Dispatcher.CurrentDispatcher;
			threadInput.AutoResetEvent.Set();

			callDispatcherRunEvent.WaitOne();
			callDispatcherRunEvent.Close();
			callDispatcherRunEvent = null;

			if (!terminate)
				Dispatcher.Run();
		}

		internal void CallDispatcherRun() => callDispatcherRunEvent.Set();

		internal void Terminate(Input threadInput) {
			terminate = true;
			try { callDispatcherRunEvent?.Set(); } catch (ObjectDisposedException) { }
			threadInput.Dispatcher?.BeginInvokeShutdown(DispatcherPriority.Send);
		}
	}
}
