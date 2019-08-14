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
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.DAC;
using dnSpy.Debugger.DotNet.CorDebug.Properties;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class DbgThreadData {
			public DnThread DnThread { get; }
			public ThreadProperties? Last { get; set; }
			public bool HasNewName { get; set; }
			public bool IsMainThread { get; set; }
			public DbgThreadData(DnThread dnThread, bool isMainThread) {
				DnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
				IsMainThread = isMainThread;
			}
		}

		DbgThreadData? TryGetThreadData(DbgThread thread) {
			if (!(thread is null) && thread.TryGetData(out DbgThreadData? data))
				return data;
			return null;
		}

		internal DnThread GetThread(DbgThread thread) =>
			TryGetThreadData(thread)?.DnThread ?? throw new InvalidOperationException();

		ThreadProperties GetThreadProperties_CorDebug(DnThread thread, ThreadProperties? oldProperties, bool isCreateThread, bool forceReadName, bool isMainThread) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(thread.AppDomain)?.AppDomain;
			ulong id = (uint)thread.VolatileThreadId;

			// If it was created, it could already have a new name so always try to read it
			if (isCreateThread)
				forceReadName = true;

			var managedId = oldProperties?.ManagedId;
			// Always retry calling ClrDac since it sometimes fails the first time we call it
			if (managedId is null)
				managedId = GetManagedId_ClrDac(thread);
			// If we should read the name, it means the CLR thread object is available so we
			// should also read the managed ID.
			if (managedId is null && forceReadName)
				managedId = GetManagedId(thread, appDomain);

			// If it's a new thread, it has no name (m_Name is null)
			var name = forceReadName ? GetThreadName(thread, appDomain) : oldProperties?.Name;

			int suspendedCount = thread.CorThread.IsSuspended ? 1 : 0;
			var userState = thread.CorThread.UserState;
			var kind = GetThreadKind(thread, isMainThread);
			return new ThreadProperties(appDomain, kind, id, managedId, name, suspendedCount, userState);
		}

		static CorValue? TryGetThreadObject(DnThread thread) {
			var threadObj = thread.CorThread.Object;
			if (threadObj is null || threadObj.IsNull)
				return null;
			threadObj = threadObj.GetDereferencedValue(out int hr);
			if (threadObj is null || threadObj.IsNull)
				return null;
			return threadObj;
		}

		ulong? GetManagedId(DnThread thread, DbgAppDomain? appDomain) {
			var res = ReadField_CorDebug(TryGetThreadObject(thread), appDomain, KnownMemberNames.Thread_ManagedThreadId_FieldName1, KnownMemberNames.Thread_ManagedThreadId_FieldName2);
			if (res is null || !res.HasValue)
				return null;
			if (res.Value.ValueType == DbgSimpleValueType.Int32)
				return (uint)(int?)res.Value.RawValue!;
			if (res.Value.ValueType == DbgSimpleValueType.UInt32)
				return (uint?)res.Value.RawValue;
			return null;
		}

		ulong? GetManagedId_ClrDac(DnThread thread) {
			Debug.Assert(clrDacInitd);
			var info = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (info is null)
				return null;
			return (uint)info.Value.ManagedThreadId;
		}

		string? GetThreadName(DnThread thread, DbgAppDomain? appDomain) {
			var res = ReadField_CorDebug(TryGetThreadObject(thread), appDomain, KnownMemberNames.Thread_Name_FieldName1, KnownMemberNames.Thread_Name_FieldName2);
			if (res is null || !res.HasValue)
				return null;
			if (res.Value.ValueType == DbgSimpleValueType.StringUtf16)
				return (string?)res.Value.RawValue;
			return null;
		}

		string GetThreadKind(DnThread thread, bool isMainThread) {
			if (isMainThread)
				return PredefinedThreadKinds.Main;

			var s = GetThreadKind_ClrDac(thread);
			if (!(s is null))
				return s;

			if ((thread.CorThread.UserState & CorDebugUserState.USER_STOPPED) != 0)
				return PredefinedThreadKinds.Terminated;
			if ((thread.CorThread.UserState & CorDebugUserState.USER_THREADPOOL) != 0)
				return PredefinedThreadKinds.ThreadPool;
			return PredefinedThreadKinds.Unknown;
		}

		string? GetThreadKind_ClrDac(DnThread thread) {
			Debug.Assert(clrDacInitd);
			var tmp = clrDac.GetThreadInfo(thread.VolatileThreadId);
			if (tmp is null)
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

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug(DnThread thread) {
			debuggerThread.VerifyAccess();
			var engineThread = TryGetEngineThread(thread);
			Debug2.Assert(!(engineThread is null));
			if (engineThread is null)
				return null;
			return UpdateThreadProperties_CorDebug(engineThread);
		}

		(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)? UpdateThreadProperties_CorDebug(DbgEngineThread engineThread) {
			debuggerThread.VerifyAccess();
			var threadData = engineThread.Thread.GetData<DbgThreadData>();
			var newProps = GetThreadProperties_CorDebug(threadData.DnThread, threadData.Last, isCreateThread: false, forceReadName: threadData.HasNewName, isMainThread: threadData.IsMainThread);
			threadData.HasNewName = false;
			var updateOptions = newProps.Compare(threadData.Last);
			if (updateOptions == DbgEngineThread.UpdateOptions.None)
				return null;
			threadData.Last = newProps;
			return (engineThread, updateOptions, newProps);
		}

		void NotifyThreadPropertiesChanged_CorDebug(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props) {
			debuggerThread.VerifyAccess();
			ReadOnlyCollection<DbgStateInfo>? state = null;
			if ((updateOptions & DbgEngineThread.UpdateOptions.State) != 0)
				state = DnThreadUtils.GetState(props.UserState);
			engineThread.Update(updateOptions, appDomain: props.AppDomain, kind: props.Kind, id: props.Id, managedId: props.ManagedId, name: props.Name, suspendedCount: props.SuspendedCount, state: state);
		}

		void UpdateThreadProperties_CorDebug() {
			debuggerThread.VerifyAccess();
			List<(DbgEngineThread engineThread, DbgEngineThread.UpdateOptions updateOptions, ThreadProperties props)>? threadsToUpdate = null;
			KeyValuePair<DnThread, DbgEngineThread>[] infos;
			lock (lockObj)
				infos = toEngineThread.ToArray();
			foreach (var kv in infos) {
				var info = UpdateThreadProperties_CorDebug(kv.Value);
				if (info is null)
					continue;
				if (threadsToUpdate is null)
					threadsToUpdate = new List<(DbgEngineThread, DbgEngineThread.UpdateOptions, ThreadProperties)>();
				threadsToUpdate.Add(info.Value);
			}
			if (!(threadsToUpdate is null)) {
				foreach (var info in threadsToUpdate)
					NotifyThreadPropertiesChanged_CorDebug(info.engineThread, info.updateOptions, info.props);
			}
		}

		void DnDebugger_OnThreadAdded(object? sender, ThreadDebuggerEventArgs e) {
			Debug2.Assert(!(objectFactory is null));
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
				DbgEngineThread? engineThread;
				lock (lockObj) {
					if (toEngineThread.TryGetValue(e.Thread, out engineThread))
						toEngineThread.Remove(e.Thread);
				}
				if (!(engineThread is null)) {
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
			if (!(info is null)) {
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
			(DnThread thread, DbgThreadData data) mainThreadInfo = default;
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
					list.Sort((a, b) => (a.data.Last?.ManagedId ?? defaultManagedId).CompareTo(b.data.Last?.ManagedId ?? defaultManagedId));
					alreadyKnowsMainThread = true;
					foreach (var threadInfo in list) {
						if (IsMainThreadCheckCallStack(threadInfo.thread)) {
							mainThreadInfo = threadInfo;
							break;
						}
					}
				}
			}
			if (!(mainThreadInfo.thread is null)) {
				mainThreadInfo.data!.IsMainThread = true;
				var info = UpdateThreadProperties_CorDebug(mainThreadInfo.thread);
				if (!(info is null))
					NotifyThreadPropertiesChanged_CorDebug(info.Value.engineThread, info.Value.updateOptions, info.Value.props);
			}
		}

		bool IsMainThreadCheckCallStack(DnThread dnThread) {
			var lastFrame = dnThread.AllFrames.LastOrDefault();
			var func = lastFrame?.Function;
			var corModule = func?.Module;
			if (corModule is null)
				return false;
			var module = TryGetModule(corModule);
			if (module is null)
				return false;
			if (!module.IsExe)
				return false;
			var reflectionModule = module.GetReflectionModule();
			if (reflectionModule is null)
				return false;
			var ep = reflectionModule.Assembly.EntryPoint;
			if (ep?.Module != reflectionModule || ep?.MetadataToken != func?.Token)
				return false;

			// Seems to be the main assembly
			return true;
		}

		DbgEngineThread? TryGetEngineThread(DnThread? thread) {
			if (thread is null)
				return null;
			DbgEngineThread? engineThread;
			bool b;
			lock (lockObj)
				b = toEngineThread.TryGetValue(thread, out engineThread);
			Debug.Assert(b);
			return engineThread;
		}

		void OnNewThreadName_CorDebug(DnThread? thread) {
			debuggerThread.VerifyAccess();
			var engineThread = TryGetEngineThread(thread);
			if (engineThread is null)
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
			var threadData = TryGetThreadData(thread);
			if (threadData is null)
				return;
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
			var info = UpdateThreadProperties_CorDebug(threadData.DnThread);
			if (!(info is null))
				NotifyThreadPropertiesChanged_CorDebug(info.Value.engineThread, info.Value.updateOptions, info.Value.props);
		}

		public override DbgEngineStackWalker CreateStackWalker(DbgThread thread) {
			var threadData = TryGetThreadData(thread);
			var engineThread = TryGetEngineThread(threadData?.DnThread);
			if (engineThread is null || threadData!.DnThread.HasExited)
				return new NullDbgEngineStackWalker();
			return new DbgEngineStackWalkerImpl(dbgDotNetNativeCodeLocationFactory, dbgDotNetCodeLocationFactory, this, threadData.DnThread, thread, GetFramesBuffer());
		}
		const int framesBufferSize = 0x100;
		ICorDebugFrame[]? framesBuffer = new ICorDebugFrame[framesBufferSize];
		ICorDebugFrame[] GetFramesBuffer() => Interlocked.Exchange(ref framesBuffer, null) ?? new ICorDebugFrame[framesBufferSize];
		internal void ReturnFramesBuffer(ref ICorDebugFrame[]? framesBuffer) {
			Debug2.Assert(!(framesBuffer is null));
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
			Debug2.Assert(!(corFrame is null));

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

		bool CanSetIP_CorDebug(DbgThread thread, DbgCodeLocation location) => TryGetFrameForSetIP_CorDebug(thread, location, out var corFrame, out uint offset) is null;

		string? TryGetFrameForSetIP_CorDebug(DbgThread thread, DbgCodeLocation location, out CorFrame? corFrame, out uint offset) {
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
			if (frameModuleId is null || frameModuleId.Value.ToModuleId() != moduleId || corFrame.Token != token)
				return dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotSetNextStatement;

			return null;
		}

		internal DnModuleId? TryGetModuleId(CorFrame frame) => dnDebugger.TryGetModuleId(frame.Function?.Module);

		bool TryGetFrame(DbgThread thread, [NotNullWhen(true)] out CorFrame? frame) {
			debuggerThread.VerifyAccess();
			frame = null;
			var data = TryGetThreadData(thread);
			if (data is null)
				return false;
			if (data.DnThread.HasExited)
				return false;
			frame = data.DnThread.AllFrames.FirstOrDefault();
			if (frame is null || !frame.IsILFrame)
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
