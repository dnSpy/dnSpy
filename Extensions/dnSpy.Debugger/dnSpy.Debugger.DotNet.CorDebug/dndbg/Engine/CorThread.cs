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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorThread : COMObject<ICorDebugThread>, IEquatable<CorThread> {
		public CorProcess Process {
			get {
				int hr = obj.GetProcess(out var process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		public int ThreadId {
			get {
				int hr = obj.GetID(out int tid);
				return hr < 0 ? -1 : tid;
			}
		}

		public CorAppDomain AppDomain {
			get {
				int hr = obj.GetAppDomain(out var appDomain);
				return hr < 0 || appDomain == null ? null : new CorAppDomain(appDomain);
			}
		}

		public int VolatileThreadId {
			get {
				var th2 = obj as ICorDebugThread2;
				if (th2 == null)
					return -1;
				int hr = th2.GetVolatileOSThreadID(out int tid);
				return hr < 0 ? -1 : tid;
			}
		}

		public CorChain ActiveChain {
			get {
				int hr = obj.GetActiveChain(out var chain);
				return hr < 0 || chain == null ? null : new CorChain(chain);
			}
		}

		public CorFrame ActiveFrame {
			get {
				int hr = obj.GetActiveFrame(out var frame);
				return hr < 0 || frame == null ? null : new CorFrame(frame);
			}
		}

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

		public IEnumerable<CorFrame> AllFrames => GetAllFrames(new ICorDebugFrame[1]);

		public IEnumerable<CorFrame> GetAllFrames(ICorDebugFrame[] frames) {
			foreach (var chain in Chains) {
				foreach (var frame in chain.GetFrames(frames))
					yield return frame;
			}
		}

		public IntPtr Handle {
			get {
				int hr = obj.GetHandle(out var handle);
				return hr < 0 ? IntPtr.Zero : handle;
			}
		}

		public bool IsRunning => State == CorDebugThreadState.THREAD_RUN;
		public bool IsSuspended => State == CorDebugThreadState.THREAD_SUSPEND;

		public CorDebugThreadState State {
			get {
				int hr = obj.GetDebugState(out var state);
				return hr < 0 ? 0 : state;
			}
			set {
				int hr = obj.SetDebugState(value);
			}
		}

		public CorValue CurrentException {
			get {
				int hr = obj.GetCurrentException(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorDebugUserState UserState {
			get {
				int hr = obj.GetUserState(out var state);
				return hr < 0 ? 0 : state;
			}
		}

		public CorValue Object {
			get {
				int hr = obj.GetObject(out var value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		public CorThread(ICorDebugThread thread)
			: base(thread) {
		}

		public bool InterceptCurrentException(CorFrame frame) {
			var t2 = obj as ICorDebugThread2;
			if (t2 == null)
				return false;
			int hr = t2.InterceptCurrentException(frame.RawObject);
			return hr >= 0;
		}

		public CorValue GetCurrentCustomDebuggerNotification() {
			var t4 = obj as ICorDebugThread4;
			if (t4 == null)
				return null;
			int hr = t4.GetCurrentCustomDebuggerNotification(out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

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
		public override string ToString() => $"[Thread] TID={ThreadId}, VTID={VolatileThreadId} State={State} UserState={UserState}";
	}
}
