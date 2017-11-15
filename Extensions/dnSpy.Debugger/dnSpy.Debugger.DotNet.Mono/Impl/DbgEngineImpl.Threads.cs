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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using Mono.Debugger.Soft;
using ST = System.Threading;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		const string FinalizerName = "Finalizer";

		sealed class DbgThreadData {
			public ThreadMirror MonoThread { get; }
			public ThreadProperties Last { get; set; }
			public bool HasNewName { get; set; }
			public bool IsMainThread { get; }
			public bool IsFinalizerThread { get; }
			public DbgThreadData(ThreadMirror monoThread, bool isMainThread, bool isFinalizerThread) {
				MonoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
				IsMainThread = isMainThread;
				IsFinalizerThread = isFinalizerThread;
			}
		}

		internal ThreadMirror GetThread(DbgThread thread) =>
			thread?.GetData<DbgThreadData>()?.MonoThread ?? throw new InvalidOperationException();

		ThreadProperties GetThreadProperties_MonoDebug(ThreadMirror thread, ThreadProperties oldProperties, bool isCreateThread, bool forceReadName, bool isMainThread, bool isFinalizerThread) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.Domain)?.AppDomain;
			ulong id = (uint)thread.TID;

			if (isCreateThread)
				forceReadName = true;

			var managedId = oldProperties?.ManagedId;
			if (managedId == null && forceReadName)
				managedId = GetManagedId(thread);

			var name = forceReadName ? GetThreadName(thread) : oldProperties?.Name;

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
			catch (VMNotSuspendedException) {
				// 1. The process is not suspended (should never be the case)
				// 2. The thread is running native code (eg. it's the finalizer thread)
				// 3. There's a pending invoke on this thread
			}

			managedId = null;
			return true;
		}

		static string GetThreadName(ThreadMirror thread) {
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

		void CreateThread(ThreadMirror monoThread) {
			debuggerThread.VerifyAccess();
			bool isMainThread = IsMainThread(monoThread);
			bool isFinalizerThread = !isMainThread && IsFinalizerThread(monoThread);
			var props = GetThreadProperties_MonoDebug(monoThread, null, isCreateThread: true, forceReadName: false, isMainThread: isMainThread, isFinalizerThread: isFinalizerThread);
			var threadData = new DbgThreadData(monoThread, isMainThread, isFinalizerThread) { Last = props };
			var state = ThreadMirrorUtils.GetState(props.ThreadState);
			var engineThread = objectFactory.CreateThread(props.AppDomain, props.Kind, props.Id, props.ManagedId, props.Name, props.SuspendedCount, state, GetMessageFlags(), data: threadData);
			lock (lockObj)
				toEngineThread.Add(monoThread, engineThread);
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

		public override void Freeze(DbgThread thread) {
			//TODO:
		}

		public override void Thaw(DbgThread thread) {
			//TODO:
		}

		public override DbgEngineStackWalker CreateStackWalker(DbgThread thread) {
			return new DbgEngineStackWalkerImpl();
		}

		public override void SetIP(DbgThread thread, DbgCodeLocation location) {
			//TODO:
		}

		public override bool CanSetIP(DbgThread thread, DbgCodeLocation location) {
			return false;//TODO:
		}
	}
}
