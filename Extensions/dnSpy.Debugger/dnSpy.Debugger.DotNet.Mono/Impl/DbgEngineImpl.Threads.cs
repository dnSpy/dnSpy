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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using Mono.Debugger.Soft;
using ST = System.Threading;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		const string FinalizerName = "Finalizer";
		const int CheckNewThreadNameDelayMilliseconds = 1000;

		sealed class DbgThreadData {
			public ThreadMirror MonoThread { get; }
			public ThreadProperties Last { get; set; }
			public bool HasNewName { get; set; }
			public bool IsMainThread { get; }
			public bool IsFinalizerThread { get; }
			public DateTime LastNameUpdateTime;
			public DbgThreadData(ThreadMirror monoThread, bool isMainThread, bool isFinalizerThread) {
				MonoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
				IsMainThread = isMainThread;
				IsFinalizerThread = isFinalizerThread;
			}
		}

		DbgThreadData TryGetThreadData(DbgThread thread) {
			if (thread != null && thread.TryGetData(out DbgThreadData data))
				return data;
			return null;
		}

		internal ThreadMirror GetThread(DbgThread thread) =>
			TryGetThreadData(thread)?.MonoThread ?? throw new InvalidOperationException();

		ThreadProperties GetThreadProperties_MonoDebug(ThreadMirror thread, ThreadProperties oldProperties, ref DateTime lastNameUpdateTime, bool isCreateThread, bool forceReadName, bool isMainThread, bool isFinalizerThread) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.Domain)?.AppDomain;
			ulong id = (uint)thread.TID;

			if (isCreateThread)
				forceReadName = true;

			var managedId = oldProperties?.ManagedId;
			if (managedId == null && forceReadName)
				managedId = GetManagedId(thread);

			var time = DateTime.UtcNow;
			if (oldProperties?.Name == null && (lastNameUpdateTime == default || time - lastNameUpdateTime >= TimeSpan.FromMilliseconds(CheckNewThreadNameDelayMilliseconds)))
				forceReadName = true;

			string name;
			if (forceReadName) {
				name = GetThreadName(thread);
				lastNameUpdateTime = time;
			}
			else
				name = oldProperties?.Name;

			int suspendedCount = (thread.ThreadState & ST.ThreadState.Suspended) != 0 ? 1 : 0;
			var threadState = thread.ThreadState;
			var kind = GetThreadKind(thread, isMainThread, isFinalizerThread);
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, threadState);
		}

		sealed class GetManagedIdState {
			public MethodMirror ManagedIdGetter;
		}
		ulong? GetManagedId(ThreadMirror thread) {
			var appDomain = TryGetEngineAppDomain(thread.Domain)?.AppDomain;
			Debug.Assert(appDomain != null);
			if (appDomain != null) {
				try {
					var state = appDomain.GetOrCreateData<GetManagedIdState>();
					if (state.ManagedIdGetter == null) {
						var threadType = thread.Domain.Corlib.GetType("System.Threading.Thread", false, false);
						Debug.Assert(threadType != null);
						state.ManagedIdGetter = threadType?.GetMethod("get_" + nameof(ST.Thread.ManagedThreadId));
					}
					if (state.ManagedIdGetter != null) {
						if (!TryGetManagedId(thread, thread, state.ManagedIdGetter, out ulong? managedId))
							return null;
						if (managedId != null)
							return managedId;

						foreach (var t in thread.VirtualMachine.GetThreads()) {
							if (t == thread)
								continue;
							if (!TryGetManagedId(t, thread, state.ManagedIdGetter, out managedId))
								return null;
							if (managedId != null)
								return managedId;
						}
					}
				}
				catch (Exception ex) {
					Debug.Fail(ex.ToString());
				}
			}
			return null;
		}

		bool TryGetManagedId(ThreadMirror thread, ThreadMirror threadObj, MethodMirror managedIdGetter, out ulong? managedId) {
			debuggerThread.VerifyAccess();

			if (thread.Domain != threadObj.Domain) {
				managedId = null;
				return true;
			}

			try {
				var res = TryInvokeMethod(thread, threadObj, managedIdGetter, Array.Empty<Value>(), out bool timedOut);
				if (timedOut) {
					managedId = null;
					return false;
				}

				if (res is PrimitiveValue pv && pv.Value is int) {
					managedId = (uint)(int)pv.Value;
					return true;
				}
			}
			catch (CommandException ce) when (ce.ErrorCode == ErrorCode.ERR_NO_INVOCATION) {
			}
			catch (VMNotSuspendedException) {
				// 1. The process is not suspended (should never be the case)
				// 2. The thread is running native code (eg. it's the finalizer thread)
				// 3. There's a pending invoke on this thread
			}

			managedId = null;
			return true;
		}

		static string GetThreadName(ThreadMirror thread) {
			thread.InvalidateName();
			var name = thread.Name;
			if (name == string.Empty)
				return null;
			return name;
		}

		string GetThreadKind(ThreadMirror thread, bool isMainThread, bool isFinalizerThread) {
			if (isMainThread)
				return PredefinedThreadKinds.Main;
			if (isFinalizerThread)
				return PredefinedThreadKinds.Finalizer;

			if ((thread.ThreadState & ST.ThreadState.Stopped) != 0)
				return PredefinedThreadKinds.Terminated;
			if (thread.IsThreadPoolThread)
				return PredefinedThreadKinds.ThreadPool;
			return PredefinedThreadKinds.WorkerThread;
		}

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_MonoDebug_NoLock(DbgEngineThread engineThread) {
			debuggerThread.VerifyAccess();
			var threadData = engineThread.Thread.GetData<DbgThreadData>();
			var newProps = GetThreadProperties_MonoDebug(threadData.MonoThread, threadData.Last, ref threadData.LastNameUpdateTime, isCreateThread: false, forceReadName: threadData.HasNewName, isMainThread: threadData.IsMainThread, isFinalizerThread: threadData.IsFinalizerThread);
			threadData.HasNewName = false;
			var updateOptions = threadData.Last.Compare(newProps);
			if (updateOptions == DbgEngineThread.UpdateOptions.None)
				return null;
			threadData.Last = newProps;
			return (engineThread, updateOptions, newProps);
		}

		void NotifyThreadPropertiesChanged_MonoDebug(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props) {
			debuggerThread.VerifyAccess();
			ReadOnlyCollection<DbgStateInfo> state = null;
			if ((updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
				state = ThreadMirrorUtils.GetState(props.ThreadState);
			engineThread.Update(updateOptions, appDomain: props.AppDomain, kind: props.Kind, id: props.Id, managedId: props.ManagedId, name: props.Name, suspendedCount: props.SuspendedCount, state: state);
		}

		void UpdateThreadProperties_MonoDebug() {
			debuggerThread.VerifyAccess();
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)> threadsToUpdate = null;
			lock (lockObj) {
				foreach (var kv in toEngineThread) {
					var info = UpdateThreadProperties_MonoDebug_NoLock(kv.Value);
					if (info == null)
						continue;
					if (threadsToUpdate == null)
						threadsToUpdate = new List<(DbgEngineThread, DbgEngineThread.UpdateOptions, ThreadProperties)>();
					threadsToUpdate.Add(info.Value);
				}
			}
			if (threadsToUpdate != null) {
				foreach (var info in threadsToUpdate)
					NotifyThreadPropertiesChanged_MonoDebug(info.engineThread, info.updateOptions, info.props);
			}
		}

		void CreateThread(ThreadMirror monoThread) {
			debuggerThread.VerifyAccess();
			bool isMainThread = IsMainThread(monoThread);
			bool isFinalizerThread = !isMainThread && IsFinalizerThread(monoThread);
			var threadData = new DbgThreadData(monoThread, isMainThread, isFinalizerThread);
			var props = GetThreadProperties_MonoDebug(monoThread, null, ref threadData.LastNameUpdateTime, isCreateThread: true, forceReadName: false, isMainThread: isMainThread, isFinalizerThread: isFinalizerThread);
			threadData.Last = props;
			var state = ThreadMirrorUtils.GetState(props.ThreadState);
			var engineThread = objectFactory.CreateThread(props.AppDomain, props.Kind, props.Id, props.ManagedId, props.Name, props.SuspendedCount, state, GetMessageFlags(), data: threadData);
			lock (lockObj)
				toEngineThread.Add(monoThread, engineThread);
		}

		void DestroyThread(ThreadMirror monoThread) {
			debuggerThread.VerifyAccess();
			DbgEngineThread engineThread;
			lock (lockObj) {
				if (toEngineThread.TryGetValue(monoThread, out engineThread))
					toEngineThread.Remove(monoThread);
			}
			if (engineThread != null)
				engineThread.Remove(GetMessageFlags());
		}

		bool IsMainThread(ThreadMirror thread) {
			if (alreadyKnowsMainThread)
				return false;
			if (IsNotMainThread(thread))
				return false;
			alreadyKnowsMainThread = true;
			return true;
		}
		bool alreadyKnowsMainThread;

		bool IsFinalizerThread(ThreadMirror thread) {
			if (seenFinalizer)
				return false;
			if (thread.IsThreadPoolThread)
				return false;
			if (thread.Name != FinalizerName)
				return false;

			seenFinalizer = true;
			return true;
		}
		bool seenFinalizer;

		bool IsNotMainThread(ThreadMirror thread) {
			if (thread.IsThreadPoolThread)
				return true;

			if (!string.IsNullOrEmpty(thread.Name))
				return true;

			return false;
		}

		DbgEngineThread TryGetEngineThread(ThreadMirror thread) {
			if (thread == null)
				return null;
			DbgEngineThread engineThread;
			bool b;
			lock (lockObj)
				b = toEngineThread.TryGetValue(thread, out engineThread);
			Debug.Assert(b);
			return engineThread;
		}

		public override void Freeze(DbgThread thread) {
			//TODO:
		}

		public override void Thaw(DbgThread thread) {
			//TODO:
		}

		public override DbgEngineStackWalker CreateStackWalker(DbgThread thread) {
			var threadData = TryGetThreadData(thread);
			var engineThread = TryGetEngineThread(threadData?.MonoThread);
			if (engineThread == null)
				return new NullDbgEngineStackWalker();
			return new DbgEngineStackWalkerImpl(dbgDotNetCodeLocationFactory, this, threadData.MonoThread, thread);
		}

		public override void SetIP(DbgThread thread, DbgCodeLocation location) {
			//TODO:
		}

		public override bool CanSetIP(DbgThread thread, DbgCodeLocation location) {
			return false;//TODO:
		}
	}
}
