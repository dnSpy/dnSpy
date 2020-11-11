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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;
using MDS = Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Steppers {
	sealed class DbgDotNetEngineStepperImpl : DbgDotNetEngineStepper {
		const string forciblyCanceledErrorMessage = "Only one stepper can be active at a time";
		const int MAX_STEPS = 1000;

		sealed class SessionImpl : SessionBase {
			public SessionImpl(object? tag) : base(tag) { }
			public StepEventRequest? MonoStepper { get; set; }
		}

		readonly DbgEngineImpl engine;
		SessionImpl? session;

		public DbgDotNetEngineStepperImpl(DbgEngineImpl engine) =>
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

		static MDS.StackFrame? GetFrame(ThreadMirror thread) {
			try {
				var frames = thread.GetFrames();
				return frames.Length == 0 ? null : frames[0];
			}
			catch (VMDisconnectedException) {
				return null;
			}
		}

		static StepFilter GetStepFilterFlags() => StepFilter.StaticCtor;

		public override SessionBase? Session {
			get => session;
			set => session = (SessionImpl?)value;
		}

		public override SessionBase CreateSession(object? tag) => new SessionImpl(tag);

		public override bool IsRuntimePaused => engine.IsPaused;
		public override uint ContinueCounter => engine.ContinueCounter;

		sealed class DbgDotNetEngineStepperFrameInfoImpl : DbgDotNetEngineStepperFrameInfo {
			public override bool SupportsReturnValues => false;
			public override DbgThread Thread { get; }
			internal ThreadMirror MonoThread { get; }

			readonly DbgEngineImpl engine;
			readonly MDS.StackFrame? frame;
			readonly MethodMirror? frameMethod;

			public DbgDotNetEngineStepperFrameInfoImpl(DbgEngineImpl engine, DbgThread thread) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				Thread = thread ?? throw new ArgumentNullException(nameof(thread));
				MonoThread = engine.GetThread(thread);
				frame = GetFrame(MonoThread);
				frameMethod = frame?.Method;
			}

			public override bool TryGetLocation([NotNullWhen(true)] out DbgModule? module, out uint token, out uint offset) {
				engine.VerifyMonoDebugThread();
				module = engine.TryGetModule(frameMethod?.DeclaringType.Module);
				token = (uint)(frameMethod?.MetadataToken ?? 0);
				var offs = frame?.ILOffset;
				offset = (uint)(offs ?? 0);
				return module is not null && token != 0 && offs is not null;
			}

			public override bool Equals(DbgDotNetEngineStepperFrameInfo other) {
				var otherImpl = (DbgDotNetEngineStepperFrameInfoImpl)other;
				// There's no address so this isn't 100% reliable
				return otherImpl.frameMethod == frameMethod &&
					otherImpl.frame?.IsDebuggerInvoke == frame?.IsDebuggerInvoke &&
					otherImpl.frame?.IsNativeTransition == frame?.IsNativeTransition;
			}
		}

		public override DbgDotNetEngineStepperFrameInfo? TryGetFrameInfo(DbgThread thread) => new DbgDotNetEngineStepperFrameInfoImpl(engine, thread);
		public override void Continue() => engine.RunCore();

		public override Task<DbgThread> StepIntoAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyMonoDebugThread();
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			return StepCoreAsync(frameImpl.MonoThread, ranges, isStepInto: true);
		}

		public override Task<DbgThread> StepOverAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyMonoDebugThread();
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			return StepCoreAsync(frameImpl.MonoThread, ranges, isStepInto: false);
		}

		async Task<DbgThread> StepCoreAsync(ThreadMirror thread, DbgCodeRange[] ranges, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			Debug2.Assert(session is not null);
			var method = GetFrame(thread)?.Method;
			Debug2.Assert(method is not null);
			if (method is null)
				throw new StepErrorException("Internal error");

			for (int i = 0; i < MAX_STEPS; i++) {
				thread = await StepCore2Async(thread, ranges, isStepInto);
				var frame = GetFrame(thread);
				uint offset = (uint)(frame?.ILOffset ?? -1);
				if (frame?.Method != method || !IsInCodeRange(ranges, offset))
					break;
			}
			return engine.TryGetThread(thread) ?? throw new InvalidOperationException();
		}

		Task<ThreadMirror> StepCore2Async(ThreadMirror thread, DbgCodeRange[] ranges, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			Debug2.Assert(session is not null);
			var tcs = new TaskCompletionSource<ThreadMirror>();
			var stepReq = engine.CreateStepRequest(thread, e => {
				if (engine.IsClosed || e.Canceled)
					tcs.SetCanceled();
				else if (e.ForciblyCanceled)
					tcs.SetException(new ForciblyCanceledException(forciblyCanceledErrorMessage));
				else
					tcs.SetResult(thread);
				return true;
			});
			session.MonoStepper = stepReq;
			stepReq.Depth = isStepInto ? StepDepth.Into : StepDepth.Over;
			stepReq.Size = StepSize.Min;
			stepReq.Filter = GetStepFilterFlags();
			stepReq.Enable();
			engine.RunCore();
			return tcs.Task;
		}

		public override Task<DbgThread> StepOutAsync(DbgDotNetEngineStepperFrameInfo frame) {
			engine.VerifyMonoDebugThread();
			Debug2.Assert(session is not null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepReq = engine.CreateStepRequest(frameImpl.MonoThread, e => {
				if (engine.IsClosed || e.Canceled)
					tcs.SetCanceled();
				else if (e.ForciblyCanceled)
					tcs.SetException(new ForciblyCanceledException(forciblyCanceledErrorMessage));
				else {
					var thread = engine.TryGetThread(frameImpl.MonoThread);
					if (thread is not null)
						tcs.SetResult(thread);
					else
						tcs.SetException(new InvalidOperationException());
				}
				return true;
			});
			session.MonoStepper = stepReq;
			stepReq.Depth = StepDepth.Out;
			stepReq.Size = StepSize.Min;
			stepReq.Filter = GetStepFilterFlags();
			stepReq.Enable();
			engine.RunCore();
			return tcs.Task;
		}

		static bool IsInCodeRange(DbgCodeRange[] ranges, uint offset) {
			foreach (var range in ranges) {
				if (range.Start <= offset && offset < range.End)
					return true;
			}
			return false;
		}

		public override DbgDotNetStepperBreakpoint CreateBreakpoint(DbgThread? thread, DbgModule module, uint token, uint offset) {
			engine.VerifyMonoDebugThread();
			return new DbgDotNetStepperBreakpointImpl(engine, thread, module, token, offset);
		}

		public override void RemoveBreakpoints(DbgDotNetStepperBreakpoint[] breakpoints) {
			engine.VerifyMonoDebugThread();
			foreach (DbgDotNetStepperBreakpointImpl bp in breakpoints)
				bp.Dispose();
		}

		public override void CollectReturnValues(DbgDotNetEngineStepperFrameInfo frame, DbgILInstruction[][] statementInstructions) { }
		public override void ClearReturnValues() { }
		public override void OnStepComplete() { }

		public override void OnCanceled(SessionBase session) {
			engine.VerifyMonoDebugThread();
			CancelStepper(session);
		}

		public override void CancelLastStep() {
			engine.VerifyMonoDebugThread();
			CancelStepper(session);
		}

		void CancelStepper(SessionBase? session) {
			engine.VerifyMonoDebugThread();
			if (session is null)
				return;
			var sessionImpl = (SessionImpl)session;
			var stepper = sessionImpl.MonoStepper;
			sessionImpl.MonoStepper = null;
			if (stepper is not null)
				engine.CancelStepper(stepper);
		}

		public override bool IgnoreException(Exception exception) => exception is VMDisconnectedException;
		public override void Close(DbgDispatcher dispatcher) { }
	}
}
