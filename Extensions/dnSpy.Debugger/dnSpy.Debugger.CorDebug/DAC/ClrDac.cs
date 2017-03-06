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

namespace dnSpy.Debugger.CorDebug.DAC {
	abstract class ClrDac {
		public abstract ClrDacThreadInfo? GetThreadInfo(int tid);
	}

	struct ClrDacThreadInfo {
		public int ManagedThreadId { get; }
		public ClrDacThreadFlags Flags { get; }
		public bool IsFinalizer => (Flags & ClrDacThreadFlags.IsFinalizer) != 0;
		public bool IsAlive => (Flags & ClrDacThreadFlags.IsAlive) != 0;
		public bool IsGC => (Flags & ClrDacThreadFlags.IsGC) != 0;
		public bool IsDebuggerHelper => (Flags & ClrDacThreadFlags.IsDebuggerHelper) != 0;
		public bool IsThreadpoolTimer => (Flags & ClrDacThreadFlags.IsThreadpoolTimer) != 0;
		public bool IsThreadpoolCompletionPort => (Flags & ClrDacThreadFlags.IsThreadpoolCompletionPort) != 0;
		public bool IsThreadpoolWorker => (Flags & ClrDacThreadFlags.IsThreadpoolWorker) != 0;
		public bool IsThreadpoolWait => (Flags & ClrDacThreadFlags.IsThreadpoolWait) != 0;
		public bool IsThreadpoolGate => (Flags & ClrDacThreadFlags.IsThreadpoolGate) != 0;
		public bool IsSuspendingEE => (Flags & ClrDacThreadFlags.IsSuspendingEE) != 0;
		public bool IsShutdownHelper => (Flags & ClrDacThreadFlags.IsShutdownHelper) != 0;
		public bool IsAbortRequested => (Flags & ClrDacThreadFlags.IsAbortRequested) != 0;
		public bool IsAborted => (Flags & ClrDacThreadFlags.IsAborted) != 0;
		public bool IsGCSuspendPending => (Flags & ClrDacThreadFlags.IsGCSuspendPending) != 0;
		public bool IsUserSuspended => (Flags & ClrDacThreadFlags.IsUserSuspended) != 0;
		public bool IsDebugSuspended => (Flags & ClrDacThreadFlags.IsDebugSuspended) != 0;
		public bool IsBackground => (Flags & ClrDacThreadFlags.IsBackground) != 0;
		public bool IsUnstarted => (Flags & ClrDacThreadFlags.IsUnstarted) != 0;
		public bool IsCoInitialized => (Flags & ClrDacThreadFlags.IsCoInitialized) != 0;
		public bool IsSTA => (Flags & ClrDacThreadFlags.IsSTA) != 0;
		public bool IsMTA => (Flags & ClrDacThreadFlags.IsMTA) != 0;
		public ClrDacThreadInfo(int managedThreadId, ClrDacThreadFlags flags) {
			ManagedThreadId = managedThreadId;
			Flags = flags;
		}
	}

	[Flags]
	enum ClrDacThreadFlags {
		None						= 0,
		IsFinalizer					= 0x00000001,
		IsAlive						= 0x00000002,
		IsGC						= 0x00000004,
		IsDebuggerHelper			= 0x00000008,
		IsThreadpoolTimer			= 0x00000010,
		IsThreadpoolCompletionPort	= 0x00000020,
		IsThreadpoolWorker			= 0x00000040,
		IsThreadpoolWait			= 0x00000080,
		IsThreadpoolGate			= 0x00000100,
		IsSuspendingEE				= 0x00000200,
		IsShutdownHelper			= 0x00000400,
		IsAbortRequested			= 0x00000800,
		IsAborted					= 0x00001000,
		IsGCSuspendPending			= 0x00002000,
		IsUserSuspended				= 0x00004000,
		IsDebugSuspended			= 0x00008000,
		IsBackground				= 0x00010000,
		IsUnstarted					= 0x00020000,
		IsCoInitialized				= 0x00040000,
		IsSTA						= 0x00080000,
		IsMTA						= 0x00100000,
	}
}
