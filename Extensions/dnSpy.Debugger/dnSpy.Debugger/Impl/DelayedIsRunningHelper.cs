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
using System.Windows.Threading;

namespace dnSpy.Debugger.Impl {
	interface IIsRunningProvider {
		bool IsRunning { get; }
		event EventHandler IsRunningChanged;
		bool IsDebugging { get; }
		event EventHandler IsDebuggingChanged;
	}

	sealed class DelayedIsRunningHelper {
		const int delayedIsRunningMilliseconds = 1000;
		readonly IIsRunningProvider owner;
		readonly DispatcherTimer timer;
		readonly Action raiseDelayedIsRunningChanged_DbgThread;

		public DelayedIsRunningHelper(IIsRunningProvider owner, Dispatcher dispatcher, Action raiseDelayedIsRunningChanged_DbgThread) {
			this.owner = owner;
			this.raiseDelayedIsRunningChanged_DbgThread = raiseDelayedIsRunningChanged_DbgThread;
			timer = new DispatcherTimer(DispatcherPriority.Send, dispatcher);
			timer.Interval = TimeSpan.FromMilliseconds(delayedIsRunningMilliseconds);
			timer.Tick += Timer_Tick_DbgThread;
			owner.IsDebuggingChanged += OnIsDebuggingChanged;
			owner.IsRunningChanged += OnIsRunningChanged;
		}

		// DbgManager thread
		void OnIsDebuggingChanged(object sender, EventArgs e) {
			if (!owner.IsDebugging)
				timer.Stop();
		}

		// DbgManager thread
		void OnIsRunningChanged(object sender, EventArgs e) {
			timer.Stop();
			if (owner.IsRunning)
				timer.Start();
		}

		// DbgManager thread
		void Timer_Tick_DbgThread(object sender, EventArgs e) {
			timer.Stop();
			raiseDelayedIsRunningChanged_DbgThread();
		}
	}
}
