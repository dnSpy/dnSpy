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
using System.Linq;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.DAC;
using dnSpy.Debugger.DotNet.CorDebug.Properties;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class DbgThreadData {
			public DnThread DnThread { get; }
			public ThreadProperties Last { get; set; }
			public bool HasNewName { get; set; }
			public bool IsMainThread { get; set; }
			public DbgThreadData(DnThread dnThread, bool isMainThread) {
				DnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
				IsMainThread = isMainThread;
			}
		}

		internal DnThread GetThread(DbgThread thread) =>
			thread?.GetData<DbgThreadData>()?.DnThread ?? throw new InvalidOperationException();

		ThreadProperties GetThreadProperties_CorDebug(DnThread thread, ThreadProperties oldProperties, bool isCreateThread, bool forceReadName, bool isMainThread) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.AppDomainOrNull)?.AppDomain;
			ulong id = (uint)thread.VolatileThreadId;

			// If it was created, it could already have a new name so always try to read it
			if (isCreateThread)
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
			var kind = GetThreadKind(thread, isMainThread);
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, userState);
		}

		static CorValue TryGetThreadObject(DnThread thread) {
			var threadObj = thread.CorThread.Object;
			if (threadObj == null || threadObj.IsNull)
				return null;
			threadObj = threadObj.DereferencedValue;
			if (threadObj == null || threadObj.IsNull)
				return null;
			return threadObj;
		}

		static ulong? GetManagedId(DnThread thread) {
			var threadObj = TryGetThreadObject(thread);
			if (threadObj == null)
				return null;
			// mscorlib 2.0 and 4.0 and CoreCLR all use this field name
			if (!EvalReflectionUtils.ReadValue(threadObj, "m_ManagedThreadId", out int managedId))
				return null;
			return (uint)managedId;
		}

		ulong? GetManagedId_ClrDac(DnThread thread) {
			Debug.Assert(clrDacInitd);
			var info = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (info == null)
				return null;
			return (uint)info.Value.ManagedThreadId;
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

		string GetThreadKind(DnThread thread, bool isMainThread) {
			if (isMainThread)
				return PredefinedThreadKinds.Main;

			var s = GetThreadKind_ClrDac(thread);
			if (s != null)
				return s;

			if (thread.CorThread.IsStopped)
				return PredefinedThreadKinds.Terminated;
			if (thread.CorThread.IsThreadPool)
				return PredefinedThreadKinds.ThreadPool;
			return PredefinedThreadKinds.Unknown;
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
			const DAC.ClrDacThreadFlags threadPoolBits = DAC.ClrDacThreadFlags.IsThreadpoolCompletionPort |
				DAC.ClrDacThreadFlags.IsThreadpoolGate | DAC.ClrDacThreadFlags.IsThreadpoolTimer |
				DAC.ClrDacThreadFlags.IsThreadpoolWait | DAC.ClrDacThreadFlags.IsThreadpoolWorker;
			if ((info.Flags & threadPoolBits) != 0)
				return PredefinedThreadKinds.ThreadPool;
			return PredefinedThreadKinds.WorkerThread;
		}

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug_NoLock(DnThread thread) {
			debuggerThread.VerifyAccess();
			bool b = toEngineThread.TryGetValue(thread, out var engineThread);
			Debug.Assert(b);
			if (!b)
				return null;
			return UpdateThreadProperties_CorDebug_NoLock(engineThread);
		}

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug_NoLock(DbgEngineThread engineThread) {
			debuggerThread.VerifyAccess();
			var threadData = engineThread.Thread.GetData<DbgThreadData>();
			var newProps = GetThreadProperties_CorDebug(threadData.DnThread, threadData.Last, isCreateThread: false, forceReadName: threadData.HasNewName, isMainThread: threadData.IsMainThread);
			threadData.HasNewName = false;
			var updateOptions = threadData.Last.Compare(newProps);
			if (updateOptions == DbgEngineThread.UpdateOptions.None)
				return null;
			threadData.Last = newProps;
			return (engineThread, updateOptions, newProps);
		}

		void NotifyThreadPropertiesChanged_CorDebug(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props) {
			debuggerThread.VerifyAccess();
			ReadOnlyCollection<DbgStateInfo> state = null;
			if ((updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
				state = DnThreadUtils.GetState(props.UserState);
			engineThread.Update(updateOptions, appDomain: props.AppDomain, kind: props.Kind, id: props.Id, managedId: props.ManagedId, name: props.Name, suspendedCount: props.SuspendedCount, state: state);
		}

		void UpdateThreadProperties_CorDebug() {
			debuggerThread.VerifyAccess();
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
				bool isMainThread = IsMainThread(e.Thread);
				var props = GetThreadProperties_CorDebug(e.Thread, null, isCreateThread: true, forceReadName: false, isMainThread: isMainThread);
				var threadData = new DbgThreadData(e.Thread, isMainThread) { Last = props };
				var state = DnThreadUtils.GetState(props.UserState);
				e.ShouldPause = true;
				var engineThread = objectFactory.CreateThread(props.AppDomain, props.Kind, props.Id, props.ManagedId, props.Name, props.SuspendedCount, state, GetMessageFlags(), data: threadData);
				lock (lockObj)
					toEngineThread.Add(e.Thread, engineThread);
			}
			else {
				DbgEngineThread engineThread;
				lock (lockObj) {
					if (toEngineThread.TryGetValue(e.Thread, out engineThread))
						toEngineThread.Remove(e.Thread);
				}
				if (engineThread != null) {
					e.ShouldPause = true;
					engineThread.Remove(GetMessageFlags());
				}
			}
		}

		bool IsMainThread(DnThread thread) {
			// We'll detect this later
			if (StartKind == DbgStartKind.Attach)
				return false;

			if (alreadyKnowsMainThread)
				return false;

			if (IsNotMainThread(thread))
				return false;

			alreadyKnowsMainThread = true;
			return true;
		}
		bool alreadyKnowsMainThread;

		bool IsNotMainThread(DnThread thread) {
			var info = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (info != null) {
				var flags = info.Value.Flags;
				const ClrDacThreadFlags NotMainThreadFlags =
					ClrDacThreadFlags.IsFinalizer |
					ClrDacThreadFlags.IsGC |
					ClrDacThreadFlags.IsDebuggerHelper |
					ClrDacThreadFlags.IsThreadpoolTimer |
					ClrDacThreadFlags.IsThreadpoolCompletionPort |
					ClrDacThreadFlags.IsThreadpoolWorker |
					ClrDacThreadFlags.IsThreadpoolWait |
					ClrDacThreadFlags.IsThreadpoolGate;
				if ((flags & NotMainThreadFlags) != 0)
					return true;
			}

			return false;
		}

		void DetectMainThread() {
			debuggerThread.VerifyAccess();
			Debug.Assert(StartKind == DbgStartKind.Attach);
			if (alreadyKnowsMainThread)
				return;
			(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? info = null;
			lock (lockObj) {
				var list = new List<(DnThread thread, DbgThreadData data)>();
				foreach (var kv in toEngineThread) {
					var thread = kv.Key;
					if (IsNotMainThread(thread))
						continue;
					var data = kv.Value.Thread.GetData<DbgThreadData>();
					list.Add((thread, data));
				}

				if (list.Count > 0) {
					const ulong defaultManagedId = ulong.MaxValue;
					// The main thread should have a low managed ID, very often MID=1
					list.Sort((a, b) => (a.data.Last.ManagedId ?? defaultManagedId).CompareTo(b.data.Last.ManagedId ?? defaultManagedId));
					// If we fail to detect it, just pick the one with the smallest MID
					var mainThreadInfo = list[0];
					foreach (var threadInfo in list) {
						if (IsMainThreadCheckCallStack(threadInfo.thread)) {
							mainThreadInfo = threadInfo;
							break;
						}
					}
					alreadyKnowsMainThread = true;
					mainThreadInfo.data.IsMainThread = true;
					info = UpdateThreadProperties_CorDebug_NoLock(mainThreadInfo.thread);
				}
			}
			if (info != null)
				NotifyThreadPropertiesChanged_CorDebug(info.Value.engineThread, info.Value.updateOptions, info.Value.props);
		}

		bool IsMainThreadCheckCallStack(DnThread dnThread) {
			var lastFrame = dnThread.AllFrames.LastOrDefault();
			var func = lastFrame?.Function;
			var corModule = func?.Module;
			if (corModule == null)
				return false;
			var module = TryGetModule(corModule);
			if (module == null)
				return false;
			if (!module.IsExe)
				return false;
			var reflectionModule = module.GetReflectionModule();
			if (reflectionModule == null)
				return false;
			var ep = reflectionModule.Assembly.EntryPoint;
			if (ep?.Module != reflectionModule || ep.MetadataToken != func.Token)
				return false;

			// Seems to be the main assembly
			return true;
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
			debuggerThread.VerifyAccess();
			var engineThread = TryGetEngineThread(thread);
			if (engineThread == null)
				return;
			engineThread.Thread.GetData<DbgThreadData>().HasNewName = true;
		}

		public override void Freeze(DbgThread thread) => CorDebugThread(() => FreezeThawCore(thread, freeze: true));
		public override void Thaw(DbgThread thread) => CorDebugThread(() => FreezeThawCore(thread, freeze: false));
		void FreezeThawCore(DbgThread thread, bool freeze) {
			debuggerThread.VerifyAccess();
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

		public override DbgEngineStackWalker CreateStackWalker(DbgThread thread) {
			var threadData = thread.GetData<DbgThreadData>();
			var engineThread = TryGetEngineThread(threadData.DnThread);
			if (engineThread == null || threadData.DnThread.HasExited)
				return new NullDbgEngineStackWalker();
			return new DbgEngineStackWalkerImpl(dbgDotNetNativeCodeLocationFactory, dbgDotNetCodeLocationFactory, this, threadData.DnThread, thread, GetFramesBuffer());
		}
		const int framesBufferSize = 0x100;
		ICorDebugFrame[] framesBuffer = new ICorDebugFrame[framesBufferSize];
		ICorDebugFrame[] GetFramesBuffer() => Interlocked.Exchange(ref framesBuffer, null) ?? new ICorDebugFrame[framesBufferSize];
		internal void ReturnFramesBuffer(ref ICorDebugFrame[] framesBuffer) {
			Debug.Assert(framesBuffer != null);
			Interlocked.Exchange(ref this.framesBuffer, framesBuffer);
			framesBuffer = null;
		}

		public override void SetIP(DbgThread thread, DbgCodeLocation location) =>
			CorDebugThread(() => SetIP_CorDebug(thread, location));

		void SetIP_CorDebug(DbgThread thread, DbgCodeLocation location) {
			debuggerThread.VerifyAccess();

			bool framesInvalidated = false;
			if (TryGetFrameForSetIP_CorDebug(thread, location, out var corFrame, out uint offset) is string error) {
				SendMessage(new DbgMessageSetIPComplete(thread, framesInvalidated, messageFlags: GetMessageFlags(), error: error));
				return;
			}

			framesInvalidated = true;
			bool failed = !corFrame.SetILFrameIP(offset);
			if (failed) {
				SendMessage(new DbgMessageSetIPComplete(thread, framesInvalidated, messageFlags: GetMessageFlags(), error: dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement));
				return;
			}

			SendMessage(new DbgMessageSetIPComplete(thread, framesInvalidated, messageFlags: GetMessageFlags(), error: null));
		}

		public override bool CanSetIP(DbgThread thread, DbgCodeLocation location) =>
			InvokeCorDebugThread(() => CanSetIP_CorDebug(thread, location));

		bool CanSetIP_CorDebug(DbgThread thread, DbgCodeLocation location) => TryGetFrameForSetIP_CorDebug(thread, location, out var corFrame, out uint offset) == null;

		string TryGetFrameForSetIP_CorDebug(DbgThread thread, DbgCodeLocation location, out CorFrame corFrame, out uint offset) {
			debuggerThread.VerifyAccess();
			corFrame = null;
			offset = 0;

			if (dnDebugger.ProcessState != DebuggerProcessState.Paused)
				return dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement;

			if (!TryGetFrame(thread, out corFrame))
				return dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement;

			if (!TryGetLocation(location, out var moduleId, out uint token, out offset))
				return dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement;

			var frameModuleId = TryGetModuleId(corFrame);
			if (frameModuleId == null || frameModuleId.Value.ToModuleId() != moduleId || corFrame.Token != token)
				return dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement;

			return null;
		}

		internal DnModuleId? TryGetModuleId(CorFrame frame) => dnDebugger.TryGetModuleId(frame.Function?.Module);

		bool TryGetFrame(DbgThread thread, out CorFrame frame) {
			debuggerThread.VerifyAccess();
			frame = null;
			if (!thread.TryGetData(out DbgThreadData data))
				return false;
			if (data.DnThread.HasExited)
				return false;
			frame = data.DnThread.AllFrames.FirstOrDefault();
			if (frame == null || !frame.IsILFrame)
				return false;
			return true;
		}

		static bool TryGetLocation(DbgCodeLocation location, out ModuleId moduleId, out uint token, out uint offset) {
			switch (location) {
			case DbgDotNetCodeLocation dnLoc:
				moduleId = dnLoc.Module;
				token = dnLoc.Token;
				offset = dnLoc.Offset;
				return true;

			case DbgDotNetNativeCodeLocation nativeLoc:
				if (nativeLoc.ILOffsetMapping != DbgILOffsetMapping.Exact && nativeLoc.ILOffsetMapping != DbgILOffsetMapping.Approximate)
					break;
				moduleId = nativeLoc.Module;
				token = nativeLoc.Token;
				offset = nativeLoc.Offset;
				return true;
			}

			moduleId = default;
			token = 0;
			offset = 0;
			return false;
		}
	}
}
