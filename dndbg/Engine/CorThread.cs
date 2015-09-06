/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorThread : COMObject<ICorDebugThread>, IEquatable<CorThread> {
		/// <summary>
		/// Gets the process or null
		/// </summary>
		public CorProcess Process {
			get {
				ICorDebugProcess process;
				int hr = obj.GetProcess(out process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		public int ThreadId {
			get {
				int tid;
				int hr = obj.GetID(out tid);
				return hr < 0 ? -1 : tid;
			}
		}

		/// <summary>
		/// Gets the AppDomain or null
		/// </summary>
		public CorAppDomain AppDomain {
			get {
				ICorDebugAppDomain appDomain;
				int hr = obj.GetAppDomain(out appDomain);
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
				int tid;
				int hr = th2.GetVolatileOSThreadID(out tid);
				return hr < 0 ? -1 : tid;
			}
		}

		/// <summary>
		/// Gets the active chain or null
		/// </summary>
		public CorChain ActiveChain {
			get {
				ICorDebugChain chain;
				int hr = obj.GetActiveChain(out chain);
				return hr < 0 || chain == null ? null : new CorChain(chain);
			}
		}

		/// <summary>
		/// Gets the active frame or null
		/// </summary>
		public CorFrame ActiveFrame {
			get {
				ICorDebugFrame frame;
				int hr = obj.GetActiveFrame(out frame);
				return hr < 0 || frame == null ? null : new CorFrame(frame);
			}
		}

		/// <summary>
		/// Gets all chains
		/// </summary>
		public IEnumerable<CorChain> Chains {
			get {
				ICorDebugChainEnum chainEnum;
				int hr = obj.EnumerateChains(out chainEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugChain chain = null;
					uint count;
					hr = chainEnum.Next(1, out chain, out count);
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
				IntPtr handle;
				int hr = obj.GetHandle(out handle);
				return hr < 0 ? IntPtr.Zero : handle;
			}
		}

		/// <summary>
		/// Gets/sets the thread state
		/// </summary>
		public CorDebugThreadState State {
			get {
				CorDebugThreadState state;
				int hr = obj.GetDebugState(out state);
				return hr < 0 ? 0 : state;
			}
			set {
				int hr = obj.SetDebugState(value);
			}
		}

		/// <summary>
		/// Gets the user state of this thread
		/// </summary>
		public CorDebugUserState UserState {
			get {
				CorDebugUserState state;
				int hr = obj.GetUserState(out state);
				return hr < 0 ? 0 : state;
			}
		}

		internal CorThread(ICorDebugThread thread)
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

		public static bool operator ==(CorThread a, CorThread b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorThread a, CorThread b) {
			return !(a == b);
		}

		public bool Equals(CorThread other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorThread);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Thread] TID={0}, VTID={1} State={2} UserState={3}", ThreadId, VolatileThreadId, State, UserState);
		}
	}
}
