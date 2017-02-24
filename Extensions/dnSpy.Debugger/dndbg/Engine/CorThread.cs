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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorThread : COMObject<ICorDebugThread>, IEquatable<CorThread> {
		/// <summary>
		/// Gets the process or null
		/// </summary>
		public CorProcess Process {
			get {
				int hr = obj.GetProcess(out var process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		public int ThreadId {
			get {
				int hr = obj.GetID(out int tid);
				return hr < 0 ? -1 : tid;
			}
		}

		/// <summary>
		/// Gets the AppDomain or null
		/// </summary>
		public CorAppDomain AppDomain {
			get {
				int hr = obj.GetAppDomain(out var appDomain);
				return hr < 0 || appDomain == null ? null : new CorAppDomain(appDomain);
			}
		}

		/// <summary>
		/// Gets the OS thread ID (calls ICorDebugThread2::GetVolatileOSThreadID()) or -1. This value
		/// can change during execution of the thread.
		/// </summary>
		public int VolatileThreadId {
			get {
				var th2 = obj as ICorDebugThread2;
				if (th2 == null)
					return -1;
				int hr = th2.GetVolatileOSThreadID(out int tid);
				return hr < 0 ? -1 : tid;
			}
		}

		/// <summary>
		/// Gets the active chain or null
		/// </summary>
		public CorChain ActiveChain {
			get {
				int hr = obj.GetActiveChain(out var chain);
				return hr < 0 || chain == null ? null : new CorChain(chain);
			}
		}

		/// <summary>
		/// Gets the active frame or null
		/// </summary>
		public CorFrame ActiveFrame {
			get {
				int hr = obj.GetActiveFrame(out var frame);
				return hr < 0 || frame == null ? null : new CorFrame(frame);
			}
		}

		/// <summary>
		/// Gets all chains
		/// </summary>
		public IEnumerable<CorChain> Chains {
			get {
				int hr = obj.EnumerateChains(out var chainEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = chainEnum.Next(1, out var chain, out uint count);
					if (hr != 0 || chain == null)
						break;
					yield return new CorChain(chain);
				}
			}
		}

		/// <summary>
		/// Gets all frames in all chains
		/// </summary>
		public IEnumerable<CorFrame> AllFrames {
			get {
				foreach (var chain in Chains) {
					foreach (var frame in chain.Frames)
						yield return frame;
				}
			}
		}

		/// <summary>
		/// Gets the current thread handle. It's owned by the CLR debugger. The handle may change as
		/// the process executes, and may be different for different parts of the thread.
		/// </summary>
		public IntPtr Handle {
			get {
				int hr = obj.GetHandle(out var handle);
				return hr < 0 ? IntPtr.Zero : handle;
			}
		}

		/// <summary>
		/// Gets the task ID. It's 0 unless the thread is associated with a connection.
		/// </summary>
		public ulong TaskId {
			get {
				var t2 = obj as ICorDebugThread2;
				if (t2 == null)
					return 0;
				int hr = t2.GetTaskID(out ulong taskId);
				return hr < 0 ? 0 : taskId;
			}
		}

		/// <summary>
		/// true if the thread is running
		/// </summary>
		public bool IsRunning => State == CorDebugThreadState.THREAD_RUN;

		/// <summary>
		/// true if the thread is suspended
		/// </summary>
		public bool IsSuspended => State == CorDebugThreadState.THREAD_SUSPEND;

		/// <summary>
		/// Gets/sets the thread state
		/// </summary>
		public CorDebugThreadState State {
			get {
				int hr = obj.GetDebugState(out var state);
				return hr < 0 ? 0 : state;
			}
			set {
				int hr = obj.SetDebugState(value);
			}
		}

		/// <summary>
		/// Gets the current exception or null
		/// </summary>
		public CorValue CurrentException {
			get {
				int hr = obj.GetCurrentException(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		/// <summary>
		/// true if a termination of the thread has been requested.
		/// </summary>
		public bool StopRequested => (UserState & CorDebugUserState.USER_STOP_REQUESTED) != 0;

		/// <summary>
		/// true if a suspension of the thread has been requested.
		/// </summary>
		public bool SuspendRequested => (UserState & CorDebugUserState.USER_SUSPEND_REQUESTED) != 0;

		/// <summary>
		/// true if the thread is running in the background.
		/// </summary>
		public bool IsBackground => (UserState & CorDebugUserState.USER_BACKGROUND) != 0;

		/// <summary>
		/// true if the thread has not started executing.
		/// </summary>
		public bool IsUnstarted => (UserState & CorDebugUserState.USER_UNSTARTED) != 0;

		/// <summary>
		/// true if the thread has been terminated.
		/// </summary>
		public bool IsStopped => (UserState & CorDebugUserState.USER_STOPPED) != 0;

		/// <summary>
		/// true if the thread is waiting for another thread to complete a task.
		/// </summary>
		public bool IsWaitSleepJoin => (UserState & CorDebugUserState.USER_WAIT_SLEEP_JOIN) != 0;

		/// <summary>
		/// true if the thread has been suspended. Use <see cref="IsSuspended"/> instead of this property.
		/// </summary>
		public bool IsUserStateSuspended => (UserState & CorDebugUserState.USER_SUSPENDED) != 0;

		/// <summary>
		/// true if the thread is at an unsafe point. That is, the thread is at a point in execution where it may block garbage collection.
		/// 
		/// Debug events may be dispatched from unsafe points, but suspending a thread at an unsafe point will very likely cause a deadlock until the thread is resumed. The safe and unsafe points are determined by the just-in-time (JIT) and garbage collection implementation.
		/// </summary>
		public bool IsUnsafePoint => (UserState & CorDebugUserState.USER_UNSAFE_POINT) != 0;

		/// <summary>
		/// true if the thread is from the thread pool.
		/// </summary>
		public bool IsThreadPool => (UserState & CorDebugUserState.USER_THREADPOOL) != 0;

		/// <summary>
		/// Gets the user state of this thread
		/// </summary>
		public CorDebugUserState UserState {
			get {
				int hr = obj.GetUserState(out var state);
				return hr < 0 ? 0 : state;
			}
		}

		/// <summary>
		/// Gets the CLR thread object
		/// </summary>
		public CorValue Object {
			get {
				int hr = obj.GetObject(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorThread(ICorDebugThread thread)
			: base(thread) {
			//TODO: ICorDebugThread3
			//TODO: ICorDebugThread4
		}

		public bool InterceptCurrentException(CorFrame frame) {
			var t2 = obj as ICorDebugThread2;
			if (t2 == null)
				return false;
			int hr = t2.InterceptCurrentException(frame.RawObject);
			return hr >= 0;
		}

		/// <summary>
		/// Returns a new <see cref="CorEval"/> or null if there was an error
		/// </summary>
		/// <returns></returns>
		public CorEval CreateEval() {
			int hr = obj.CreateEval(out var eval);
			return hr < 0 || eval == null ? null : new CorEval(eval);
		}

		public static bool operator ==(CorThread a, CorThread b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorThread a, CorThread b) => !(a == b);
		public bool Equals(CorThread other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorThread);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => string.Format("[Thread] TID={0}, VTID={1} State={2} UserState={3}", ThreadId, VolatileThreadId, State, UserState);
	}
}
