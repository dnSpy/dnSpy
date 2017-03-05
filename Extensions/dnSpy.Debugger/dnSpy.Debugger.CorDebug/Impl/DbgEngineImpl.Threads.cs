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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class DbgThreadData {
			public ThreadProperties Last { get; set; }
		}

		ThreadProperties GetThreadProperties_CorDebug(DnThread thread) {
			debuggerThread.Dispatcher.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.AppDomainOrNull)?.AppDomain;
			var kind = GetThreadKind(thread);
			var id = thread.VolatileThreadId;
			int? managedId = null;//TODO:
			string name = null;//TODO:
			int suspendedCount = thread.CorThread.IsSuspended ? 1 : 0;
			var userState = thread.CorThread.UserState;
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, userState);
		}

		string GetThreadKind(DnThread thread) {
			if (thread.CorThread.IsStopped)
				return PredefinedThreadKinds.Terminated;
			if (thread.CorThread.IsThreadPool)
				return PredefinedThreadKinds.ThreadPool;
			if (IsMainThread(thread))
				return PredefinedThreadKinds.Main;
			if (IsWorkerThread(thread))
				return PredefinedThreadKinds.WorkerThread;
			return PredefinedThreadKinds.Unknown;

			//TODO: This check is not correct
			bool IsWorkerThread(DnThread t) => t.CorThread.IsBackground;

			//TODO: This isn't 100% accurate
			bool IsMainThread(DnThread t) {
				if (!t.Process.WasAttached)
					return t.UniqueIdProcess == 0;

				var ad = t.AppDomainOrNull;
				if (ad == null || ad.Id != 1)
					return false;
				if (t.CorThread.IsBackground)
					return false;
				return t.UniqueIdProcess == 0;
			}
		}

		void UpdateThreadProperties_CorDebug() {
			debuggerThread.Dispatcher.VerifyAccess();
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)> threadsToUpdate = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var threadData = kv.Value.Thread.GetData<DbgThreadData>();
					var newProps = GetThreadProperties_CorDebug(kv.Key);
					var updateOptions = threadData.Last.Compare(newProps);
					if (updateOptions == DbgEngineThread.UpdateOptions.None)
						continue;
					threadData.Last = newProps;
					if (threadsToUpdate == null)
						threadsToUpdate = new List<(DbgEngineThread, DbgEngineThread.UpdateOptions, ThreadProperties)>();
					threadsToUpdate.Add((kv.Value, updateOptions, newProps));
				}
			}
			if (threadsToUpdate != null) {
				foreach (var info in threadsToUpdate) {
					ReadOnlyCollection<DbgStateInfo> state = null;
					if ((info.updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
						state = DnThreadUtils.GetState(info.props.UserState);
					info.engineThread.Update(info.updateOptions, kind: info.props.Kind, id: info.props.Id, managedId: info.props.ManagedId, name: info.props.Name, suspendedCount: info.props.SuspendedCount, state: state);
				}
			}
		}

		void DnDebugger_OnThreadAdded(object sender, ThreadDebuggerEventArgs e) {
			Debug.Assert(objectFactory != null);
			if (e.Added) {
				var props = GetThreadProperties_CorDebug(e.Thread);
				var threadData = new DbgThreadData { Last = props };
				var state = DnThreadUtils.GetState(props.UserState);
				var engineThread = objectFactory.CreateThread(props.AppDomain, props.Kind, props.Id, props.ManagedId, props.Name, props.SuspendedCount, state, threadData);
				lock (lockObj)
					toEngineThread.Add(e.Thread, engineThread);
			}
			else {
				DbgEngineThread engineThread;
				lock (lockObj) {
					if (toEngineThread.TryGetValue(e.Thread, out engineThread))
						toEngineThread.Remove(e.Thread);
				}
				engineThread?.Remove();
			}
		}
	}
}
