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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class DbgThreadData {
			public DnThread DnThread { get; }
			public ThreadProperties Last { get; set; }
			public bool HasNewName { get; set; }
			public DbgThreadData(DnThread dnThread) => DnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
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
			Debug.Assert(clrDacInitd);
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
			Debug.Assert(clrDacInitd);
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

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug_NoLock(DnThread thread) {
			debuggerThread.Dispatcher.VerifyAccess();
			bool b = toEngineThread.TryGetValue(thread, out var engineThread);
			Debug.Assert(b);
			if (!b)
				return null;
			return UpdateThreadProperties_CorDebug_NoLock(engineThread);
		}

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug_NoLock(DbgEngineThread engineThread) {
			debuggerThread.Dispatcher.VerifyAccess();
			var threadData = engineThread.Thread.GetData<DbgThreadData>();
			var newProps = GetThreadProperties_CorDebug(threadData.DnThread, threadData.Last, isCreateThread: false, forceReadName: threadData.HasNewName);
			threadData.HasNewName = false;
			var updateOptions = threadData.Last.Compare(newProps);
			if (updateOptions == DbgEngineThread.UpdateOptions.None)
				return null;
			threadData.Last = newProps;
			return (engineThread, updateOptions, newProps);
		}

		void NotifyThreadPropertiesChanged_CorDebug(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props) {
			debuggerThread.Dispatcher.VerifyAccess();
			ReadOnlyCollection<DbgStateInfo> state = null;
			if ((updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
				state = DnThreadUtils.GetState(props.UserState);
			engineThread.Update(updateOptions, appDomain: props.AppDomain, kind: props.Kind, id: props.Id, managedId: props.ManagedId, name: props.Name, suspendedCount: props.SuspendedCount, state: state);
		}

		void UpdateThreadProperties_CorDebug() {
			debuggerThread.Dispatcher.VerifyAccess();
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)> threadsToUpdate = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var info = UpdateThreadProperties_CorDebug_NoLock(kv.Value);
					if (info == null)
						continue;
					if (threadsToUpdate == null)
						threadsToUpdate = new List<(DbgEngineThread, DbgEngineThread.UpdateOptions, ThreadProperties)>();
					threadsToUpdate.Add(info.Value);
				}
			}
			if (threadsToUpdate != null) {
				foreach (var info in threadsToUpdate)
					NotifyThreadPropertiesChanged_CorDebug(info.engineThread, info.updateOptions, info.props);
			}
		}

		void DnDebugger_OnThreadAdded(object sender, ThreadDebuggerEventArgs e) {
			Debug.Assert(objectFactory != null);
			if (e.Added) {
				var props = GetThreadProperties_CorDebug(e.Thread, null, isCreateThread: true, forceReadName: false);
				var threadData = new DbgThreadData(e.Thread) { Last = props };
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

		public override void Freeze(DbgThread thread) => CorDebugThread(() => FreezeThawCore(thread, freeze: true));
		public override void Thaw(DbgThread thread) => CorDebugThread(() => FreezeThawCore(thread, freeze: false));
		void FreezeThawCore(DbgThread thread, bool freeze) {
			Dispatcher.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState != DebuggerProcessState.Paused)
				return;
			var threadData = thread.GetData<DbgThreadData>();
			var corThread = threadData.DnThread.CorThread;
			if (freeze) {
				if (corThread.IsSuspended)
					return;
				corThread.State = CorDebugThreadState.THREAD_SUSPEND;
			}
			else {
				if (!corThread.IsSuspended)
					return;
				corThread.State = CorDebugThreadState.THREAD_RUN;
			}
			(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? info;
			lock (lockObj)
				info = UpdateThreadProperties_CorDebug_NoLock(threadData.DnThread);
			if (info != null)
				NotifyThreadPropertiesChanged_CorDebug(info.Value.engineThread, info.Value.updateOptions, info.Value.props);
		}
	}
}
