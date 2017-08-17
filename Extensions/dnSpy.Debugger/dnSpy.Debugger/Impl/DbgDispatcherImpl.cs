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
using System.ComponentModel.Composition;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.Impl {
	abstract class DbgDispatcher2 : DbgDispatcher {
		public abstract T Invoke<T>(Func<T> callback);
	}

	[Export(typeof(DbgDispatcher))]
	sealed class DbgDispatcherImpl : DbgDispatcher2 {
		const DispatcherPriority execPriority = DispatcherPriority.Send;
		readonly DebuggerThread debuggerThread;

		internal Dispatcher WpfDispatcher => debuggerThread.Dispatcher;

		public DbgDispatcherImpl() {
			debuggerThread = new DebuggerThread("Debugger");
			debuggerThread.CallDispatcherRun();
		}

		public override bool CheckAccess() => WpfDispatcher.CheckAccess();

		public override void BeginInvoke(Action callback) {
			if (WpfDispatcher.HasShutdownStarted || WpfDispatcher.HasShutdownFinished)
				return;
			WpfDispatcher.BeginInvoke(execPriority, callback);
		}

		public override T Invoke<T>(Func<T> callback) {
			System.Diagnostics.Debugger.NotifyOfCrossThreadDependency();
			if (WpfDispatcher.HasShutdownStarted || WpfDispatcher.HasShutdownFinished)
				return default;
			return WpfDispatcher.Invoke(callback, execPriority);
		}
	}
}
