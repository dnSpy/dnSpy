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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;
using MDS = Mono.Debugger.Soft;
using ST = System.Threading;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		const string FinalizerName = "Finalizer";
		const int CheckNewThreadNameDelayMilliseconds = 1000;
		const int CheckNewManagedIdDelayMilliseconds = 100;
		const int MaxCheckNewManagedId = 2;

		sealed class DbgThreadData {
			public ThreadMirror MonoThread { get; }
			public ThreadProperties? Last { get; set; }
			public bool HasNewName { get; set; }
			public bool IsMainThread { get; }
			public bool IsFinalizerThread { get; }
			public DateTime LastNameUpdateTime;
			public DateTime LastManagedIdUpdateTime;
			public int GetManagedIdCounter;
			public DbgThreadData(ThreadMirror monoThread, bool isMainThread, bool isFinalizerThread) {
				MonoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
				IsMainThread = isMainThread;
				IsFinalizerThread = isFinalizerThread;
			}
		}

		DbgThreadData? TryGetThreadData(DbgThread thread) {
			if (thread is not null && thread.TryGetData(out DbgThreadData? data))
				return data;
			return null;
		}

		internal ThreadMirror GetThread(DbgThread thread) =>
			TryGetThreadData(thread)?.MonoThread ?? throw new InvalidOperationException();

		ThreadProperties GetThreadProperties_MonoDebug(ThreadMirror thread, DbgThreadData threadData, bool isCreateThread, bool forceReadName, bool isMainThread, bool isFinalizerThread, bool canFuncEval) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.Domain)?.AppDomain;

			ulong id;
			if (thread.VirtualMachine.Version.AtLeast(2, 3))
				id = (uint)thread.TID;
			else if (thread.VirtualMachine.Version.AtLeast(2, 1))
				id = (ulong)thread.ThreadId;
			else
				id = 0;

			if (isCreateThread)
				forceReadName = true;

			var time = DateTime.UtcNow;

			var managedId = threadData.Last?.ManagedId;
			if (managedId is null && canFuncEval) {
				bool getManagedId = forceReadName;
				if (!getManagedId && threadData.GetManagedIdCounter < MaxCheckNewManagedId)
					getManagedId = time - threadData.LastManagedIdUpdateTime >= TimeSpan.FromMilliseconds(CheckNewManagedIdDelayMilliseconds);
				if (getManagedId) {
					threadData.GetManagedIdCounter++;
					managedId = GetManagedId(thread);
					threadData.LastManagedIdUpdateTime = time;
				}
			}

			if (threadData.Last?.Name is null && time - threadData.LastNameUpdateTime >= TimeSpan.FromMilliseconds(CheckNewThreadNameDelayMilliseconds))
				forceReadName = true;

			string? name;
			if (forceReadName) {
				name = GetThreadName(thread);
				threadData.LastNameUpdateTime = time;
			}
			else
				name = threadData.Last?.Name;

			int suspendedCount = (thread.ThreadState & ST.ThreadState.Suspended) != 0 ? 1 : 0;
			var threadState = thread.ThreadState;
			var kind = GetThreadKind(thread, isMainThread, isFinalizerThread);
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, threadState);
		}

		bool getManagedIdFuncEvalTimedOut;
		sealed class GetManagedIdState {
			public MethodMirror? ManagedIdGetter;
		}
		ulong? GetManagedId(ThreadMirror thread) {
			// Too many problems with Unity, its Mono fork is old and buggy and sometimes crashes.
			// The managed ID isn't that important so don't try to get it.
			bool dontFuncEval = true;
			if (dontFuncEval)
				return null;

			var appDomain = TryGetEngineAppDomain(thread.Domain)?.AppDomain;
			if (appDomain is not null) {
				try {
					var state = appDomain.GetOrCreateData<GetManagedIdState>();
					if (state.ManagedIdGetter is null) {
						var threadType = thread.Domain.Corlib.GetType("System.Threading.Thread", false, false);
						Debug2.Assert(threadType is not null);
						state.ManagedIdGetter = threadType?.GetMethod("get_" + nameof(ST.Thread.ManagedThreadId));
					}
					if (state.ManagedIdGetter is not null) {
						if (!TryGetManagedId(thread, thread, state.ManagedIdGetter, out ulong? managedId))
							return null;
						if (managedId is not null)
							return managedId;

						foreach (var t in thread.VirtualMachine.GetThreads()) {
							if (t == thread)
								continue;
							if (!TryGetManagedId(t, thread, state.ManagedIdGetter, out managedId))
								return null;
							if (managedId is not null)
								return managedId;
						}
					}
				}
				catch (VMDisconnectedException) {
				}
				catch (Exception ex) {
					Debug.Fail(ex.ToString());
				}
			}
			return null;
		}

		bool TryGetManagedId(ThreadMirror thread, ThreadMirror threadObj, MethodMirror managedIdGetter, out ulong? managedId) {
			debuggerThread.VerifyAccess();

			if (getManagedIdFuncEvalTimedOut || thread.Domain != threadObj.Domain) {
				managedId = null;
				return true;
			}

			try {
				var res = TryInvokeMethod(thread, threadObj, managedIdGetter, Array.Empty<Value>(), out bool timedOut);
				if (timedOut) {
					getManagedIdFuncEvalTimedOut = true;
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

		static string? GetThreadName(ThreadMirror thread) {
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

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_MonoDebug(DbgEngineThread engineThread) {
			debuggerThread.VerifyAccess();
			var threadData = engineThread.Thread.GetData<DbgThreadData>();
			var newProps = GetThreadProperties_MonoDebug(threadData.MonoThread, threadData, isCreateThread: false, forceReadName: threadData.HasNewName, isMainThread: threadData.IsMainThread, isFinalizerThread: threadData.IsFinalizerThread, canFuncEval: true);
			threadData.HasNewName = false;
			var updateOptions = newProps.Compare(threadData.Last);
			if (updateOptions == DbgEngineThread.UpdateOptions.None)
				return null;
			threadData.Last = newProps;
			return (engineThread, updateOptions, newProps);
		}

		void NotifyThreadPropertiesChanged_MonoDebug(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props) {
			debuggerThread.VerifyAccess();
			ReadOnlyCollection<DbgStateInfo>? state = null;
			if ((updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
				state = ThreadMirrorUtils.GetState(props.ThreadState);
			engineThread.Update(updateOptions, appDomain: props.AppDomain, kind: props.Kind, id: props.Id, managedId: props.ManagedId, name: props.Name, suspendedCount: props.SuspendedCount, state: state);
		}

		void UpdateThreadProperties_MonoDebug() {
			debuggerThread.VerifyAccess();
			getManagedIdFuncEvalTimedOut = false;
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)>? threadsToUpdate = null;
			KeyValuePair<ThreadMirror, DbgEngineThread>[] infos;
			lock (lockObj)
				infos = toEngineThread.ToArray();
			foreach (var kv in infos) {
				var info = UpdateThreadProperties_MonoDebug(kv.Value);
				if (info is null)
					continue;
				if (threadsToUpdate is null)
					threadsToUpdate = new List<(DbgEngineThread, DbgEngineThread.UpdateOptions, ThreadProperties)>();
				threadsToUpdate.Add(info.Value);
			}
			if (threadsToUpdate is not null) {
				foreach (var info in threadsToUpdate)
					NotifyThreadPropertiesChanged_MonoDebug(info.engineThread, info.updateOptions, info.props);
			}
		}

		bool CreateThread(ThreadMirror monoThread) {
			debuggerThread.VerifyAccess();

			lock (lockObj) {
				if (toEngineThread.ContainsKey(monoThread))
					return false;
			}

			bool isMainThread = IsMainThread(monoThread);
			bool isFinalizerThread = !isMainThread && IsFinalizerThread(monoThread);
			var threadData = new DbgThreadData(monoThread, isMainThread, isFinalizerThread);
			// Disable func-eval so this method isn't called recursively (we can get dupes)
			var props = GetThreadProperties_MonoDebug(monoThread, threadData, isCreateThread: true, forceReadName: false, isMainThread: isMainThread, isFinalizerThread: isFinalizerThread, canFuncEval: false);
			threadData.Last = props;
			var state = ThreadMirrorUtils.GetState(props.ThreadState);
			var engineThread = objectFactory!.CreateThread(props.AppDomain, props.Kind, props.Id, props.ManagedId, props.Name, props.SuspendedCount, state, GetMessageFlags(), data: threadData);
			lock (lockObj)
				toEngineThread.Add(monoThread, engineThread);
			return true;
		}

		void DestroyThread(ThreadMirror? monoThread) {
			debuggerThread.VerifyAccess();
			if (monoThread is null)
				return;
			DbgEngineThread? engineThread;
			lock (lockObj) {
				if (toEngineThread.TryGetValue(monoThread, out engineThread))
					toEngineThread.Remove(monoThread);
			}
			if (engineThread is not null)
				engineThread.Remove(GetMessageFlags() | DbgEngineMessageFlags.Running);
		}

		bool IsMainThread(ThreadMirror thread) {
			if (alreadyKnowsMainThread)
				return false;
			// Don't try to guess the main thread if it's Unity since any thread could be the main thread
			if (monoDebugRuntimeKind == MonoDebugRuntimeKind.Unity)
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

		DbgEngineThread? TryGetEngineThread(ThreadMirror? thread) {
			if (thread is null)
				return null;
			DbgEngineThread? engineThread;
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
			if (engineThread is null)
				return new NullDbgEngineStackWalker();
			return new DbgEngineStackWalkerImpl(dbgDotNetCodeLocationFactory, this, threadData!.MonoThread, thread);
		}

		public override void SetIP(DbgThread thread, DbgCodeLocation location) =>
			MonoDebugThread(() => SetIP_MonoDebug(thread, location));

		void SetIP_MonoDebug(DbgThread thread, DbgCodeLocation location) {
			debuggerThread.VerifyAccess();
			if (!vm!.Version.AtLeast(2, 29)) {
				SendMessage(new DbgMessageSetIPComplete(thread, false, dnSpy_Debugger_DotNet_Mono_Resources.Error_RuntimeDoesNotSupportSettingNewStatement, GetMessageFlags()));
				return;
			}
			var threadData = TryGetThreadData(thread);
			if (isUnhandledException || threadData is null || !(location is IDbgDotNetCodeLocation loc)) {
				SendMessage(new DbgMessageSetIPComplete(thread, false, dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotSetNextStatement, GetMessageFlags()));
				return;
			}
			Debug.Assert(CanSetIP(thread, location));

			try {
				var error = SetIPCore_MonoDebug(threadData, loc, out bool framesInvalidated);
				SendMessage(new DbgMessageSetIPComplete(thread, framesInvalidated, error, GetMessageFlags()));
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				SendMessage(new DbgMessageSetIPComplete(thread, true, dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotSetNextStatement, GetMessageFlags()));
			}
		}

		string? SetIPCore_MonoDebug(DbgThreadData threadData, IDbgDotNetCodeLocation location, out bool framesInvalidated) {
			debuggerThread.VerifyAccess();
			framesInvalidated = false;

			if (!VerifySetIPLocation(threadData, location, out var monoMethod, out var frame))
				return dnSpy_Debugger_DotNet_Mono_Resources.Error_CouldNotSetNextStatement;

			framesInvalidated = true;
			threadData.MonoThread.SetIP(monoMethod, location.Offset);
			frame.SetILOffset((int)location.Offset);
			return null;
		}

		bool VerifySetIPLocation(DbgThreadData threadData, IDbgDotNetCodeLocation location, [NotNullWhen(true)] out MethodMirror? monoMethod, [NotNullWhen(true)] out MDS.StackFrame? frame) {
			monoMethod = null;
			frame = null;
			var frames = threadData.MonoThread.GetFrames();
			if (frames.Length == 0)
				return false;
			frame = frames[0];
			monoMethod = frame.Method;
			if (monoMethod.MetadataToken != (int)location.Token)
				return false;
			var module = TryGetModule(monoMethod.DeclaringType.Module);
			if (module is null)
				return false;

			if (location.DbgModule is not null) {
				if (location.DbgModule != module)
					return false;
			}
			else {
				if (TryGetModuleId(module) != location.Module)
					return false;
			}

			return true;
		}

		public override bool CanSetIP(DbgThread thread, DbgCodeLocation location) =>
			!isUnhandledException && location is IDbgDotNetCodeLocation && vm!.Version.AtLeast(2, 29);
	}
}
