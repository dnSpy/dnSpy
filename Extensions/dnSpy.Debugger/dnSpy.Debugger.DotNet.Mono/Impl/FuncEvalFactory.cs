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
using System.Runtime.ExceptionServices;
using System.Threading;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed class FuncEvalFactory {
		internal sealed class FuncEvalState {
			internal volatile int isEvaluatingCounter;
			internal volatile int methodInvokeCounter;
		}

		public bool IsEvaluating => funcEvalState.isEvaluatingCounter > 0;
		public int MethodInvokeCounter => funcEvalState.methodInvokeCounter;

		readonly FuncEvalState funcEvalState;
		readonly IDebugMessageDispatcher debugMessageDispatcher;

		public FuncEvalFactory(IDebugMessageDispatcher debugMessageDispatcher) {
			funcEvalState = new FuncEvalState();
			this.debugMessageDispatcher = debugMessageDispatcher;
		}

		public FuncEval CreateFuncEval(Action<FuncEval> onEvalComplete, ThreadMirror thread, TimeSpan funcEvalTimeout, bool suspendOtherThreads, CancellationToken cancellationToken) =>
			new FuncEvalImpl(debugMessageDispatcher, funcEvalState, onEvalComplete, thread, funcEvalTimeout, suspendOtherThreads, cancellationToken);
	}

	[Flags]
	enum FuncEvalOptions {
		None				= 0,
		ReturnOutThis		= 0x00000001,
		ReturnOutArgs		= 0x00000002,
		Virtual				= 0x00000004,
	}

	abstract class FuncEval : IDisposable {
		public abstract bool EvalTimedOut { get; }
		public abstract InvokeResult CreateInstance(MethodMirror method, IList<Value> arguments, FuncEvalOptions options);
		public abstract InvokeResult CallMethod(MethodMirror method, Value? obj, IList<Value> arguments, FuncEvalOptions options);
		public abstract void Dispose();
	}

	sealed class FuncEvalImpl : FuncEval {
		public override bool EvalTimedOut => evalTimedOut;
		bool evalTimedOut;

		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly FuncEvalFactory.FuncEvalState funcEvalState;
		readonly Action<FuncEval> onEvalComplete;
		readonly ThreadMirror thread;
		readonly bool suspendOtherThreads;
		readonly CancellationToken cancellationToken;
		readonly DateTime endTime;

		public FuncEvalImpl(IDebugMessageDispatcher debugMessageDispatcher, FuncEvalFactory.FuncEvalState funcEvalState, Action<FuncEval> onEvalComplete, ThreadMirror thread, TimeSpan funcEvalTimeout, bool suspendOtherThreads, CancellationToken cancellationToken) {
			this.debugMessageDispatcher = debugMessageDispatcher ?? throw new ArgumentNullException(nameof(debugMessageDispatcher));
			this.funcEvalState = funcEvalState ?? throw new ArgumentNullException(nameof(funcEvalState));
			this.onEvalComplete = onEvalComplete ?? throw new ArgumentNullException(nameof(onEvalComplete));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			endTime = DateTime.UtcNow + funcEvalTimeout;
			this.suspendOtherThreads = suspendOtherThreads;
			this.cancellationToken = cancellationToken;
		}

		InvokeOptions GetInvokeOptions(FuncEvalOptions funcEvalOptions) {
			var options = InvokeOptions.DisableBreakpoints;
			if (suspendOtherThreads)
				options |= InvokeOptions.SingleThreaded;
			if ((funcEvalOptions & FuncEvalOptions.ReturnOutThis) != 0)
				options |= InvokeOptions.ReturnOutThis;
			if ((funcEvalOptions & FuncEvalOptions.ReturnOutArgs) != 0)
				options |= InvokeOptions.ReturnOutArgs;
			if ((funcEvalOptions & FuncEvalOptions.Virtual) != 0)
				options |= InvokeOptions.Virtual;
			return options;
		}

		public override InvokeResult CreateInstance(MethodMirror method, IList<Value> arguments, FuncEvalOptions options) =>
			CallCore(method, null, arguments, options, isNewobj: true);

		public override InvokeResult CallMethod(MethodMirror method, Value? obj, IList<Value> arguments, FuncEvalOptions options) =>
			CallCore(method, obj, arguments, options, isNewobj: false);

		InvokeResult CallCore(MethodMirror method, Value? obj, IList<Value> arguments, FuncEvalOptions options, bool isNewobj) {
			if (evalTimedOut)
				throw new TimeoutException();

			IInvokeAsyncResult? asyncRes = null;
			bool done = false;
			try {
				funcEvalState.isEvaluatingCounter++;

				var currTime = DateTime.UtcNow;
				var timeLeft = endTime - currTime;
				if (timeLeft >= TimeSpan.Zero) {
					funcEvalState.methodInvokeCounter++;

					Debug2.Assert(!isNewobj || obj is null);
					bool isInvokeInstanceMethod = !(obj is null) && !isNewobj;

					AsyncCallback asyncCallback = asyncRes2 => {
						if (done)
							return;
						InvokeResult resTmp;
						try {
							if (isInvokeInstanceMethod)
								resTmp = obj!.EndInvokeMethodWithResult(asyncRes2);
							else
								resTmp = method.DeclaringType.EndInvokeMethodWithResult(asyncRes2);
							debugMessageDispatcher.CancelDispatchQueue(resTmp);
						}
						catch (Exception ex) {
							debugMessageDispatcher.CancelDispatchQueue(ExceptionDispatchInfo.Capture(ex));
						}
					};

					if (isInvokeInstanceMethod)
						asyncRes = obj!.BeginInvokeMethod(thread, method, arguments, GetInvokeOptions(options), asyncCallback, null);
					else
						asyncRes = method.DeclaringType.BeginInvokeMethod(thread, method, arguments, GetInvokeOptions(options), asyncCallback, null);

					var res = debugMessageDispatcher.DispatchQueue(timeLeft, out bool timedOut);
					if (timedOut) {
						evalTimedOut = true;
						try {
							asyncRes.Abort();
						}
						catch (CommandException ce) when (ce.ErrorCode == ErrorCode.ERR_NO_INVOCATION) { }
						throw new TimeoutException();
					}
					if (res is ExceptionDispatchInfo exInfo)
						exInfo.Throw();
					Debug.Assert(res is InvokeResult);
					return res as InvokeResult ?? throw new InvalidOperationException();
				}
				else {
					evalTimedOut = true;
					throw new TimeoutException();
				}
			}
			finally {
				done = true;
				funcEvalState.isEvaluatingCounter--;
				asyncRes?.Dispose();
			}
		}

		public override void Dispose() => onEvalComplete(this);
	}
}
