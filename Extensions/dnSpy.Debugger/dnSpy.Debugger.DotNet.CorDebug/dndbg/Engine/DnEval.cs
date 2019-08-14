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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	[Serializable]
	class EvalException : Exception {
		public int HR { get; }

		public EvalException()
			: this(-1, null, null) {
		}

		public EvalException(int hr)
			: this(hr, null, null) {
		}

		public EvalException(int hr, string msg)
			: this(hr, msg, null) {
		}

		public EvalException(int hr, string? msg, Exception? ex)
			: base(msg, ex) {
			HResult = hr;
			HR = hr;
		}
	}

	struct EvalResult {
		public bool NormalResult => !WasException && !WasCustomNotification && !WasCancelled;
		public bool WasException { get; }
		public bool WasCustomNotification { get; }
		public bool WasCancelled { get; }
		public CorValue? ResultOrException { get; }

		public EvalResult(bool wasException, bool wasCustomNotification, bool wasCancelled, CorValue? resultOrException) {
			WasException = wasException;
			WasCustomNotification = wasCustomNotification;
			WasCancelled = wasCancelled;
			ResultOrException = resultOrException;
		}
	}

	class EvalEventArgs : EventArgs {
	}

	sealed class DnEval : IDisposable {
		readonly DnDebugger debugger;
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly List<(DnModule module, CorClass cls)> customNotificationList;
		readonly CancellationToken cancellationToken;
		DnThread thread;
		CorEval eval;
		DateTime? startTime;
		DateTime endTime;
		TimeSpan initialTimeOut;

		const int ABORT_TIMEOUT_MS = 3000;
		const int RUDE_ABORT_TIMEOUT_MS = 1000;

		public bool EvalTimedOut { get; private set; }
		public bool SuspendOtherThreads { get; }
		public event EventHandler<EvalEventArgs>? EvalEvent;

		internal DnEval(DnDebugger debugger, IDebugMessageDispatcher debugMessageDispatcher, bool suspendOtherThreads, List<(DnModule module, CorClass cls)> customNotificationList, CancellationToken cancellationToken) {
			thread = null!;
			eval = null!;
			this.debugger = debugger;
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.customNotificationList = customNotificationList;
			SuspendOtherThreads = suspendOtherThreads;
			useTotalTimeout = true;
			initialTimeOut = TimeSpan.FromMilliseconds(1000);
			this.cancellationToken = cancellationToken;

			// This is only enabled during func-eval. If it's always enabled, everything gets slower.
			// It took about 50% longer to start VS.
			foreach (var info in customNotificationList)
				info.module.Process.CorProcess.SetEnableCustomNotification(info.cls, enable: true);
		}

		public void SetNoTotalTimeout() => useTotalTimeout = false;
		bool useTotalTimeout;

		public void SetTimeout(TimeSpan timeout) => initialTimeOut = timeout;

		public void SetThread(DnThread thread) {
			if (thread is null)
				throw new InvalidOperationException();

			int hr = thread.CorThread.RawObject.CreateEval(out var ce);
			if (hr < 0 || ce is null)
				throw new EvalException(hr, $"Could not create an evaluator, HR=0x{hr:X8}");
			this.thread = thread;
			eval = new CorEval(ce);
		}

		public CorValue CreateNull() => eval.CreateValue(CorElementType.Class) ?? throw new InvalidOperationException();

		public CorValue? Box(CorValue value, CorType? valueType = null) {
			var et = valueType ?? value?.ExactType;
			if (et is null)
				return null;
			if (value is null || !value.IsGeneric || value.IsBox || value.IsHeap)
				return value;
			var cls = et?.Class;
			if (cls is null)
				return null;
			if (valueType is null)
				return null;
			var res = WaitForResult(eval.NewParameterizedObjectNoConstructor(cls, valueType.TypeParameters.ToArray()));
			if (res is null || !res.Value.NormalResult) {
				res?.ResultOrException?.DisposeHandle();
				return null;
			}
			var newObj = res.Value.ResultOrException!;
			var r = newObj.GetDereferencedValue(out int hr);
			var vb = r?.GetBoxedValue(out hr);
			if (vb is null) {
				newObj.DisposeHandle();
				return null;
			}
			hr = vb.WriteGenericValue(value.ReadGenericValue(), thread.CorThread.Process);
			if (hr < 0)
				return null;
			return newObj;
		}

		public EvalResult? CreateDontCallConstructor(CorType type, out int hr) {
			if (!type.HasClass) {
				hr = -1;
				return null;
			}
			return WaitForResult(hr = eval.NewParameterizedObjectNoConstructor(type.Class!, type.TypeParameters.ToArray()));
		}

		public EvalResult? CallConstructor(CorFunction func, CorType[] typeArgs, CorValue[] args, out int hr) => WaitForResult(hr = eval.NewParameterizedObject(func, typeArgs, args));
		public EvalResult? Call(CorFunction func, CorType[] typeArgs, CorValue[] args, out int hr) => WaitForResult(hr = eval.CallParameterizedFunction(func, typeArgs, args));
		public EvalResult? CreateString(string s, out int hr) => WaitForResult(hr = eval.NewString(s));
		public EvalResult? CreateSZArray(CorType type, int numElems, out int hr) => WaitForResult(hr = eval.NewParameterizedArray(type, new uint[1] { (uint)numElems }));

		EvalResult? WaitForResult(int hr) {
			if (hr < 0)
				return null;
			InitializeStartTime();

			return SyncWait();
		}

		void InitializeStartTime() {
			if (!(startTime is null))
				return;

			startTime = DateTime.UtcNow;
			endTime = startTime.Value + initialTimeOut;
		}

		struct ThreadInfo {
			public readonly CorThread Thread;
			public readonly CorDebugThreadState State;

			public ThreadInfo(CorThread thread) {
				Thread = thread;
				State = thread.State;
			}
		}

		struct ThreadInfos {
			readonly CorThread thread;
			readonly List<ThreadInfo> list;
			readonly bool suspendOtherThreads;

			public ThreadInfos(CorThread thread, bool suspendOtherThreads) {
				this.thread = thread;
				list = GetThreadInfos(thread);
				this.suspendOtherThreads = suspendOtherThreads;
			}

			static List<ThreadInfo> GetThreadInfos(CorThread thread) {
				var process = thread.Process;
				var list = new List<ThreadInfo>();
				if (process is null) {
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
					if (info.Thread.Equals(thread))
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
			Debug2.Assert(!(startTime is null));

			var now = DateTime.UtcNow;
			if (now >= endTime)
				now = endTime;
			var timeLeft = endTime - now;
			if (!useTotalTimeout)
				timeLeft = initialTimeOut;

			var infos = new ThreadInfos(thread.CorThread, SuspendOtherThreads);
			EvalResultKind dispResult;
			debugger.DebugCallbackEvent += Debugger_DebugCallbackEvent;
			try {
				infos.EnableThread();

				debugger.EvalStarted();
				var res = debugMessageDispatcher.DispatchQueue(timeLeft, out bool timedOut);
				if (timedOut) {
					AbortEval(timedOut);
					throw new TimeoutException();
				}
				Debug2.Assert(!(res is null));
				dispResult = (EvalResultKind)res;
				if (dispResult == EvalResultKind.CustomNotification) {
					if (!AbortEval(false))
						throw new TimeoutException();
					if (debugger.ProcessState != DebuggerProcessState.Paused)
						debugger.TryBreakProcesses();
				}
			}
			finally {
				debugger.DebugCallbackEvent -= Debugger_DebugCallbackEvent;
				infos.RestoreThreads();
				debugger.EvalStopped();
			}
			bool wasException = dispResult == EvalResultKind.Exception;
			bool wasCustomNotification = dispResult == EvalResultKind.CustomNotification;
			bool wasCancelled = dispResult == EvalResultKind.Cancelled;

			return new EvalResult(wasException, wasCustomNotification, wasCancelled, wasCustomNotification ? null : eval.Result);
		}

		enum EvalResultKind {
			Normal,
			Exception,
			CustomNotification,
			Cancelled,
		}

		bool AbortEval(bool forceBreakProcesses) {
			bool timedOut = false;
			int hr = eval.Abort();
			if (hr >= 0) {
				debugMessageDispatcher.DispatchQueue(TimeSpan.FromMilliseconds(ABORT_TIMEOUT_MS), out timedOut);
				if (timedOut) {
					hr = eval.RudeAbort();
					if (hr >= 0)
						debugMessageDispatcher.DispatchQueue(TimeSpan.FromMilliseconds(RUDE_ABORT_TIMEOUT_MS), out _);
				}
			}
			if (timedOut || forceBreakProcesses) {
				hr = debugger.TryBreakProcesses();
				Debug.WriteLineIf(hr != 0, $"Eval timed out and TryBreakProcesses() failed: hr=0x{hr:X8}");
				EvalTimedOut = true;
			}
			return !timedOut;
		}

		void Debugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			switch (e.Kind) {
			case DebugCallbackKind.EvalComplete:
			case DebugCallbackKind.EvalException:
				var ee = (EvalDebugCallbackEventArgs)e;
				if (ee.Eval == eval.RawObject) {
					debugger.DebugCallbackEvent -= Debugger_DebugCallbackEvent;
					e.AddPauseReason(DebuggerPauseReason.Eval);
					debugMessageDispatcher.CancelDispatchQueue(ee.WasException ? EvalResultKind.Exception : EvalResultKind.Normal);
					return;
				}
				break;

			case DebugCallbackKind.CustomNotification:
				if (!SuspendOtherThreads)
					break;
				var cne = (CustomNotificationDebugCallbackEventArgs)e;
				var value = cne.CorThread?.GetCurrentCustomDebuggerNotification();
				if (!(value is null)) {
					debugMessageDispatcher.CancelDispatchQueue(EvalResultKind.CustomNotification);
					debugger.DisposeHandle(value);
					return;
				}
				debugger.DisposeHandle(value);
				break;
			}
			if (cancellationToken.IsCancellationRequested)
				debugMessageDispatcher.CancelDispatchQueue(EvalResultKind.Cancelled);
		}

		public void SignalEvalComplete() => EvalEvent?.Invoke(this, new EvalEventArgs());

		public void Dispose() {
			foreach (var info in customNotificationList)
				info.module.Process.CorProcess.SetEnableCustomNotification(info.cls, enable: false);
			SignalEvalComplete();
		}
	}
}
