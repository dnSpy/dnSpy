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
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Shared;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		sealed class BreakAllHelper {
			const int breakTimeoutMilliseconds = 5000;
			readonly DbgManagerImpl owner;
			readonly List<Info> infos;
			DispatcherTimer? timer;

			sealed class Info {
				public EngineInfo EngineInfo { get; }
				public bool Done { get; set; }
				public Info(EngineInfo engineInfo) {
					EngineInfo = engineInfo;
					Done = engineInfo.EngineState == EngineState.Paused;
				}
			}

			public BreakAllHelper(DbgManagerImpl owner) {
				this.owner = owner;
				infos = new List<Info>();
			}

			public void Start_NoLock() {
				foreach (var engineInfo in owner.engines)
					infos.Add(new Info(engineInfo));
				// Make a copy of infos in the very unlikely event we get a new message
				// that modifies the list (the engine got disconnected) when we call Break()
				// inside the lock below.
				foreach (var info in infos.ToArray()) {
					if (!info.Done)
						info.EngineInfo.Engine.Break();
				}
				if (!CheckIsDone_NoLock()) {
					timer = new DispatcherTimer(owner.InternalDispatcher, TimeSpan.FromMilliseconds(breakTimeoutMilliseconds));
					timer.Tick += Timer_Tick_DbgThread;
					timer.Start();
				}
			}

			void DoneStep1_NoLock(out bool canNotify) {
				if (timer is not null) {
					timer.Tick -= Timer_Tick_DbgThread;
					timer.Stop();
					timer = null;
				}
				canNotify = owner.breakAllHelper is not null;
				owner.breakAllHelper = null;
			}

			void DoneStep2_DbgThread(bool success) {
				owner.Dispatcher.VerifyAccess();
				// All of them could've been disconnected, and if so, there's nothing to do
				if (infos.Count > 0)
					owner.BreakCompleted_DbgThread(success);
			}

			void Timer_Tick_DbgThread(object? sender, EventArgs e) {
				bool canNotify;
				lock (owner.lockObj)
					DoneStep1_NoLock(out canNotify);
				if (canNotify)
					DoneStep2_DbgThread(success: false);
			}

			bool CheckIsDone_NoLock() {
				if (!IsDone_NoLock())
					return false;
				DoneStep1_NoLock(out bool canNotify);
				if (canNotify)
					owner.Dispatcher.BeginInvoke(() => DoneStep2_DbgThread(success: true));
				return true;
			}

			bool IsDone_NoLock() {
				foreach (var info in infos) {
					if (!info.Done)
						return false;
				}
				return true;
			}

			public void OnConnected_DbgThread_NoLock(EngineInfo engineInfo) {
				foreach (var info2 in infos) {
					if (info2.EngineInfo == engineInfo) {
						OnBreak_DbgThread(engineInfo.Engine);
						return;
					}
				}
				var info = new Info(engineInfo);
				infos.Add(info);
				info.EngineInfo.Engine.Break();
				CheckIsDone_NoLock();
			}

			public void OnDisconnected_DbgThread_NoLock(DbgEngine engine) {
				for (int i = 0; i < infos.Count; i++) {
					if (infos[i].EngineInfo.Engine == engine) {
						infos.RemoveAt(i);
						break;
					}
				}
				CheckIsDone_NoLock();
			}

			public void OnBreak_DbgThread(DbgEngine engine) {
				bool done, canNotify = false;
				lock (owner.lockObj) {
					foreach (var info in infos) {
						if (info.EngineInfo.Engine == engine) {
							info.Done = true;
							break;
						}
					}
					done = IsDone_NoLock();
					if (done)
						DoneStep1_NoLock(out canNotify);
				}
				if (done && canNotify)
					DoneStep2_DbgThread(success: true);
			}
		}
	}
}
