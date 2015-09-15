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
using System.Diagnostics;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	[Serializable]
	public class EvalException : Exception {
		public readonly int HR;

		public EvalException()
			: this(-1, null, null) {
		}

		public EvalException(int hr)
			: this(hr, null, null) {
		}

		public EvalException(int hr, string msg)
			: this(hr, msg, null) {
		}

		public EvalException(int hr, string msg, Exception ex)
			: base(msg, ex) {
			HResult = hr;
			HR = hr;
		}
	}

	public struct EvalResult {
		public readonly bool WasException;
		public readonly CorValue ResultOrException;

		public EvalResult(bool wasException, CorValue resultOrException) {
			this.WasException = wasException;
			this.ResultOrException = resultOrException;
		}
	}

	public class EvalEventArgs : EventArgs {
	}

	public sealed class DnEval {
		readonly DnDebugger debugger;
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		CorThread thread;
		CorEval eval;
		DateTime? startTime;
		DateTime endTime;

		const int TIMEOUT_MS = 1000;
		const int ABORT_TIMEOUT_MS = 3000;
		const int RUDE_ABORT_TIMEOUT_MS = 1000;

		public bool SuspendOtherThreads {
			get { return suspendOtherThreads; }
			set { suspendOtherThreads = value; }
		}
		bool suspendOtherThreads;

		public event EventHandler<EvalEventArgs> EvalEvent;

		internal DnEval(DnDebugger debugger, IDebugMessageDispatcher debugMessageDispatcher) {
			this.debugger = debugger;
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.suspendOtherThreads = true;
		}

		public void SetThread(DnThread thread) {
			SetThread(thread.CorThread);
		}

		public void SetThread(CorThread thread) {
			if (thread == null)
				throw new InvalidOperationException();

			ICorDebugEval ce;
			int hr = thread.RawObject.CreateEval(out ce);
			if (hr < 0 || ce == null)
				throw new EvalException(hr, string.Format("Could not create an evaluator, HR=0x{0:X8}", hr));
			this.thread = thread;
			this.eval = new CorEval(ce);
		}

		public CorValueResult CallResult(CorFunction func, CorValue[] args) {
			return CallResult(func, null, args);
		}

		public CorValueResult CallResult(CorFunction func, CorType[] typeArgs, CorValue[] args) {
			var res = Call(func, typeArgs, args);
			if (res.WasException || res.ResultOrException == null)
				return new CorValueResult();
			return res.ResultOrException.Value;
		}

		public CorValueResult CallResult(CorFunction func, CorType[] typeArgs, CorValue[] args, out int hr) {
			var res = Call(func, typeArgs, args, out hr);
			if (res == null || res.Value.WasException || res.Value.ResultOrException == null)
				return new CorValueResult();
			return res.Value.ResultOrException.Value;
		}

		public EvalResult Call(CorFunction func, CorValue[] args) {
			return Call(func, null, args);
		}

		public EvalResult Call(CorFunction func, CorType[] typeArgs, CorValue[] args) {
			int hr;
			var res = Call(func, typeArgs, args, out hr);
			if (res != null)
				return res.Value;
			throw new EvalException(hr, string.Format("Could not call method {0:X8}, HR=0x{1:X8}", func.Token, hr));
		}

		public EvalResult? Call(CorFunction func, CorType[] typeArgs, CorValue[] args, out int hr) {
			hr = eval.CallParameterizedFunction(func, typeArgs, args);
			if (hr < 0)
				return null;
			InitializeStartTime();

			return SyncWait();
		}

		void InitializeStartTime() {
			if (startTime != null)
				return;

			startTime = DateTime.UtcNow;
			endTime = startTime.Value.AddMilliseconds(TIMEOUT_MS);
		}

		struct ThreadInfo {
			public readonly CorThread Thread;
			public readonly CorDebugThreadState State;

			public ThreadInfo(CorThread thread) {
				this.Thread = thread;
				this.State = thread.State;
			}
		}

		struct ThreadInfos {
			readonly CorThread thread;
			readonly List<ThreadInfo> list;
			readonly bool suspendOtherThreads;

			public ThreadInfos(CorThread thread, bool suspendOtherThreads) {
				this.thread = thread;
				this.list = GetThreadInfos(thread);
				this.suspendOtherThreads = suspendOtherThreads;
			}

			static List<ThreadInfo> GetThreadInfos(CorThread thread) {
				var process = thread.Process;
				var list = new List<ThreadInfo>();
				if (process == null) {
					list.Add(new ThreadInfo(thread));
					return list;
				}

				foreach (var t in process.Threads)
					list.Add(new ThreadInfo(t));

				return list;
			}

			public void EnableThread() {
				foreach (var info in list) {
					CorDebugThreadState newState;
					if (info.Thread == thread)
						newState = CorDebugThreadState.THREAD_RUN;
					else if (suspendOtherThreads)
						newState = CorDebugThreadState.THREAD_SUSPEND;
					else
						continue;
					if (info.State != newState)
						info.Thread.State = newState;
				}
			}

			public void RestoreThreads() {
				foreach (var info in list)
					info.Thread.State = info.State;
			}
		}

		EvalResult SyncWait() {
			Debug.Assert(startTime != null);

			var now = DateTime.UtcNow;
			if (now >= endTime)
				throw new TimeoutException("Evaluation timed out");
			var timeLeft = now - startTime.Value;

			var infos = new ThreadInfos(thread, SuspendOtherThreads);
			object dispResult;
			var oldThreadState = thread.State;
			debugger.DebugCallbackEvent += Debugger_DebugCallbackEvent;
			bool timedOut;
			try {
				infos.EnableThread();

				debugger.EvalStarted();
				dispResult = debugMessageDispatcher.DispatchQueue(timeLeft, out timedOut);
				if (timedOut) {
					bool timedOutTmp;
					int hr = eval.Abort();
					if (hr >= 0) {
						debugMessageDispatcher.DispatchQueue(TimeSpan.FromMilliseconds(ABORT_TIMEOUT_MS), out timedOutTmp);
						if (timedOutTmp) {
							hr = eval.RudeAbort();
							if (hr >= 0)
								debugMessageDispatcher.DispatchQueue(TimeSpan.FromMilliseconds(RUDE_ABORT_TIMEOUT_MS), out timedOutTmp);
						}
					}
					throw new TimeoutException();
				}
			}
			finally {
				debugger.DebugCallbackEvent -= Debugger_DebugCallbackEvent;
				infos.RestoreThreads();
				debugger.EvalStopped();
			}
			Debug.Assert(dispResult is bool);
			bool wasException = (bool)dispResult;

			return new EvalResult(wasException, eval.Result);
		}

		void Debugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			var ee = e as EvalDebugCallbackEventArgs;
			if (ee == null)
				return;

			if (ee.Eval == eval.RawObject) {
				debugger.DebugCallbackEvent -= Debugger_DebugCallbackEvent;
				e.AddStopReason(DebuggerStopReason.Eval);
				debugMessageDispatcher.CancelDispatchQueue(ee.WasException);
				return;
			}
		}

		public void SignalEvalComplete() {
			var e = EvalEvent;
			if (e != null)
				e(this, new EvalEventArgs());
		}
	}
}
