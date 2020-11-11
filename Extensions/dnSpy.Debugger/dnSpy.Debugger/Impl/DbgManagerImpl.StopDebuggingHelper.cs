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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		sealed class StopDebuggingHelper {
			const int stopDebuggingTimeoutMilliseconds = 5000;
			readonly object lockObj;
			readonly DbgManagerImpl owner;
			readonly List<DbgProcess> processes;
			Action<bool> onCompleted;
			DispatcherTimer? timer;

			public StopDebuggingHelper(DbgManagerImpl owner, Action<bool> onCompleted) {
				lockObj = new object();
				this.owner = owner;
				this.onCompleted = onCompleted;
				processes = new List<DbgProcess>();
			}

			// Random thread
			public void Initialize() {
				bool raiseEvent = false;
				lock (lockObj) {
					owner.ProcessesChanged += DbgManager_ProcessesChanged;
					processes.AddRange(owner.Processes.Where(a => a.State != DbgProcessState.Terminated));
					if (processes.Count == 0)
						raiseEvent = true;
					else {
						timer = new DispatcherTimer(owner.InternalDispatcher, TimeSpan.FromMilliseconds(stopDebuggingTimeoutMilliseconds));
						timer.Tick += Timer_Tick_DbgThread;
						timer.Start();
						owner.StopDebuggingAll();
					}
				}
				if (raiseEvent)
					NotifyOwner(success: true);
			}

			// Random thread
			void StopTimer() {
				lock (lockObj) {
					if (timer is not null) {
						timer.Tick -= Timer_Tick_DbgThread;
						timer.Stop();
						timer = null;
					}
				}
			}

			// DbgManager thread
			void Timer_Tick_DbgThread(object? sender, EventArgs e) {
				lock (lockObj) {
					if (timer != sender)
						return;
					StopTimer();
				}
				NotifyOwner(success: false);
			}

			// DbgManager thread
			void DbgManager_ProcessesChanged(object? sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
				if (!e.Added) {
					bool raiseEvent = false;
					lock (lockObj) {
						if (processes.Count != 0) {
							for (int i = processes.Count - 1; i >= 0; i--) {
								if (e.Objects.Contains(processes[i]))
									processes.RemoveAt(i);
							}
							raiseEvent = processes.Count == 0;
						}
					}
					if (raiseEvent)
						NotifyOwner(success: true);
				}
			}

			// Random thread
			void NotifyOwner(bool success) {
				Action<bool> onCompletedLocal;
				lock (lockObj) {
					StopTimer();
					processes.Clear();
					owner.ProcessesChanged -= DbgManager_ProcessesChanged;
					onCompletedLocal = onCompleted;
					onCompleted = null!;
				}
				onCompletedLocal?.Invoke(success);
			}
		}
	}
}
