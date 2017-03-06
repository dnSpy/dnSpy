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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.DAC;

namespace dnSpy.Debugger.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class DbgThreadData {
			public ThreadProperties Last { get; set; }
			public bool HasNewName { get; set; }
		}

		ThreadProperties GetThreadProperties_CorDebug(DnThread thread, ThreadProperties oldProperties, bool isCreateThread, bool forceReadName) {
			debuggerThread.Dispatcher.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.AppDomainOrNull)?.AppDomain;
			var id = thread.VolatileThreadId;

			if (isCreateThread && !IsNewCreatedThread(thread))
				forceReadName = true;

			var managedId = oldProperties?.ManagedId;
			// Always retry calling ClrDac since it sometimes fails the first time we call it
			if (managedId == null)
				managedId = GetManagedId_ClrDac(thread);
			// If we should read the name, it means the CLR thread object is available so we
			// should also read the managed ID.
			if (managedId == null && forceReadName)
				managedId = GetManagedId(thread);

			// If it's a new thread, it has no name (m_Name is null)
			var name = forceReadName ? GetThreadName(thread) : oldProperties?.Name;

			int suspendedCount = thread.CorThread.IsSuspended ? 1 : 0;
			var userState = thread.CorThread.UserState;
			var kind = GetThreadKind(thread, managedId);
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, userState);
		}

		// If we attached to the process, we don't know whether this thread is a new thread
		// that got created after we attached to the process or if the CLR debugger is still
		// notifying us of existing threads.
		bool IsNewCreatedThread(DnThread thread) => StartKind != DbgStartKind.Attach;

		static CorValue TryGetThreadObject(DnThread thread) {
			var threadObj = thread.CorThread.Object?.NeuterCheckDereferencedValue;
			if (threadObj == null || threadObj.IsNull)
				return null;
			return threadObj;
		}

		static int? GetManagedId(DnThread thread) {
			var threadObj = TryGetThreadObject(thread);
			if (threadObj == null)
				return null;
			// mscorlib 2.0 and 4.0 and CoreCLR all use this field name
			if (!EvalReflectionUtils.ReadValue(threadObj, "m_ManagedThreadId", out int managedId))
				return null;
			return managedId;
		}

		int? GetManagedId_ClrDac(DnThread thread) {
			Debug.Assert(clrDac != null);
			if (clrDac == null)
				return null;
			var info = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (info == null)
				return null;
			return info.Value.ManagedThreadId;
		}

		static string GetThreadName(DnThread thread) {
			var threadObj = TryGetThreadObject(thread);
			if (threadObj == null)
				return null;
			// mscorlib 2.0 and 4.0 and CoreCLR all use this field name. It's unlikely to change since
			// VS' debugger probably hard codes the name as well.
			if (!EvalReflectionUtils.ReadValue(threadObj, "m_Name", out string name))
				return null;
			return name;
		}

		string GetThreadKind(DnThread thread, int? managedId) {
			if (managedId == 1)
				return PredefinedThreadKinds.Main;

			var s = GetThreadKind_ClrDac(thread);
			if (s != null)
				return s;

			if (thread.CorThread.IsStopped)
				return PredefinedThreadKinds.Terminated;
			if (thread.CorThread.IsThreadPool)
				return PredefinedThreadKinds.ThreadPool;
			if (managedId == 1 || IsMainThread(thread))
				return PredefinedThreadKinds.Main;
			if (IsWorkerThread(thread))
				return PredefinedThreadKinds.WorkerThread;
			return PredefinedThreadKinds.Unknown;

			// This check is not correct but will only be called if ClrDac fails
			bool IsWorkerThread(DnThread t) => t.CorThread.IsBackground;

			// Not 100% accurate but it's only called if ClrDac fails
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

		string GetThreadKind_ClrDac(DnThread thread) {
			Debug.Assert(clrDac != null);
			if (clrDac == null)
				return null;
			var tmp = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (tmp == null)
				return null;
			var info = tmp.Value;
			if (!info.IsAlive)
				return PredefinedThreadKinds.Terminated;
			if (info.IsGC)
				return PredefinedThreadKinds.GC;
			if (info.IsFinalizer)
				return PredefinedThreadKinds.Finalizer;
			if (info.IsThreadpoolWorker)
				return PredefinedThreadKinds.ThreadPool;
			return PredefinedThreadKinds.WorkerThread;
		}

		void UpdateThreadProperties_CorDebug() {
			debuggerThread.Dispatcher.VerifyAccess();
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)> threadsToUpdate = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var threadData = kv.Value.Thread.GetData<DbgThreadData>();
					var newProps = GetThreadProperties_CorDebug(kv.Key, threadData.Last, isCreateThread: false, forceReadName: threadData.HasNewName);
					threadData.HasNewName = false;
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
					info.engineThread.Update(info.updateOptions, appDomain: info.props.AppDomain, kind: info.props.Kind, id: info.props.Id, managedId: info.props.ManagedId, name: info.props.Name, suspendedCount: info.props.SuspendedCount, state: state);
				}
			}
		}

		void DnDebugger_OnThreadAdded(object sender, ThreadDebuggerEventArgs e) {
			Debug.Assert(objectFactory != null);
			if (e.Added) {
				var props = GetThreadProperties_CorDebug(e.Thread, null, isCreateThread: true, forceReadName: false);
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

		DbgEngineThread TryGetEngineThread(DnThread thread) {
			if (thread == null)
				return null;
			DbgEngineThread engineThread;
			bool b;
			lock (lockObj)
				b = toEngineThread.TryGetValue(thread, out engineThread);
			Debug.Assert(b);
			return engineThread;
		}

		void OnNewThreadName_CorDebug(DnThread thread) {
			debuggerThread.Dispatcher.VerifyAccess();
			var engineThread = TryGetEngineThread(thread);
			if (engineThread == null)
				return;
			engineThread.Thread.GetData<DbgThreadData>().HasNewName = true;
		}
	}
}
