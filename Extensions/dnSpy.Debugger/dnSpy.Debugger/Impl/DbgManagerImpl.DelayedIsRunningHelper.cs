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
	sealed partial class DbgManagerImpl {
		sealed class DelayedIsRunningHelper {
			const int delayedIsRunningMilliseconds = 1000;
			readonly DbgManagerImpl owner;
			readonly DispatcherTimer timer;

			public DelayedIsRunningHelper(DbgManagerImpl owner) {
				this.owner = owner;
				timer = new DispatcherTimer(DispatcherPriority.Send, owner.dispatcherThread.Dispatcher);
				timer.Interval = TimeSpan.FromMilliseconds(delayedIsRunningMilliseconds);
				timer.Tick += Timer_Tick_DbgThread;
				owner.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
				owner.IsRunningChanged += DbgManager_IsRunningChanged;
			}

			// DbgManager thread
			void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
				if (!owner.IsDebugging)
					timer.Stop();
			}

			// DbgManager thread
			void DbgManager_IsRunningChanged(object sender, EventArgs e) {
				timer.Stop();
				if (owner.IsRunning)
					timer.Start();
			}

			// DbgManager thread
			void Timer_Tick_DbgThread(object sender, EventArgs e) {
				timer.Stop();
				owner.RaiseDelayedIsRunningChanged_DbgThread();
			}
		}
	}
}
