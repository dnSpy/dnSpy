/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerThread : IThread {
		public IAppDomain AppDomain {
			get {
				return debugger.Dispatcher.UI(() => {
					var ad = thread.AppDomainOrNull;
					return ad == null ? null : new DebuggerAppDomain(debugger, ad);
				});
			}
		}

		public IEnumerable<IStackFrame> Frames {
			get { return debugger.Dispatcher.UIIter(GetFramesUI); }
		}

		IEnumerable<IStackFrame> GetFramesUI() {
			int frameNo = 0;
			foreach (var f in thread.AllFrames)
				yield return new StackFrame(debugger, f, frameNo++);
		}

		public IStackFrame ActiveFrame {
			get {
				return debugger.Dispatcher.UI(() => {
					var frame = thread.AllFrames.FirstOrDefault();
					return frame == null ? null : new StackFrame(debugger, frame, 0);
				});
			}
		}

		public IStackFrame ActiveILFrame {
			get {
				return debugger.Dispatcher.UI(() => {
					int frameNo = 0;
					foreach (var f in thread.AllFrames) {
						if (f.IsILFrame)
							return new StackFrame(debugger, f, frameNo);
						frameNo++;
					}
					return null;
				});
			}
		}

		public bool HasExited {
			get { return debugger.Dispatcher.UI(() => thread.HasExited); }
		}

		public int IncrementedId {
			get { return incrementedId; }
		}

		public int ThreadId {
			get { return debugger.Dispatcher.UI(() => thread.ThreadId); }
		}

		public int VolatileThreadId {
			get { return debugger.Dispatcher.UI(() => thread.VolatileThreadId); }
		}

		public IntPtr Handle {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.Handle); }
		}

		public bool IsRunning {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsRunning); }
			set { debugger.Dispatcher.UI(() => thread.CorThread.State = value ? CorDebugThreadState.THREAD_RUN : CorDebugThreadState.THREAD_SUSPEND); }
		}

		public bool IsSuspended {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsSuspended); }
			set { debugger.Dispatcher.UI(() => thread.CorThread.State = !value ? CorDebugThreadState.THREAD_RUN : CorDebugThreadState.THREAD_SUSPEND); }
		}

		public ThreadState State {
			get { return debugger.Dispatcher.UI(() => (ThreadState)thread.CorThread.State); }
		}

		public bool StopRequested {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.StopRequested); }
		}

		public bool SuspendRequested {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.SuspendRequested); }
		}

		public bool IsBackground {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsBackground); }
		}

		public bool IsUnstarted {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsUnstarted); }
		}

		public bool IsStopped {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsStopped); }
		}

		public bool IsWaitSleepJoin {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsWaitSleepJoin); }
		}

		public bool IsUserStateSuspended {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsUserStateSuspended); }
		}

		public bool IsUnsafePoint {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsUnsafePoint); }
		}

		public bool IsThreadPool {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsThreadPool); }
		}

		public ThreadUserState UserState {
			get { return debugger.Dispatcher.UI(() => (ThreadUserState)thread.CorThread.UserState); }
		}

		public IDebuggerValue Object {
			get {
				return debugger.Dispatcher.UI(() => {
					var value = thread.CorThread.Object;
					return value == null ? null : new DebuggerValue(debugger, value);
				});
			}
		}

		public IDebuggerValue CurrentException {
			get {
				return debugger.Dispatcher.UI(() => {
					var value = thread.CorThread.CurrentException;
					return value == null ? null : new DebuggerValue(debugger, value);
				});
			}
		}

		public IStackChain ActiveChain {
			get {
				return debugger.Dispatcher.UI(() => {
					var chain = thread.CorThread.ActiveChain;
					return chain == null ? null : new StackChain(debugger, chain);
				});
			}
		}

		public IEnumerable<IStackChain> Chains {
			get { return debugger.Dispatcher.UIIter(GetChainsUI); }
		}

		IEnumerable<IStackChain> GetChainsUI() {
			foreach (var c in thread.CorThread.Chains)
				yield return new StackChain(debugger, c);
		}

		readonly Debugger debugger;
		readonly int hashCode;
		readonly int incrementedId;

		public DnThread DnThread {
			get { return thread; }
		}
		readonly DnThread thread;

		public DebuggerThread(Debugger debugger, DnThread thread) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.thread = thread;
			this.incrementedId = thread.IncrementedId;
			this.hashCode = thread.GetHashCode();
		}

		public bool InterceptCurrentException(IStackFrame frame) {
			return debugger.Dispatcher.UI(() => thread.CorThread.InterceptCurrentException(((StackFrame)frame).CorFrame));
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerThread;
			return other != null && other.thread == thread;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => thread.ToString());
		}
	}
}
