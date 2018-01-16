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
using System.Diagnostics;
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
			public SessionImpl(object tag) : base(tag) { }
			public StepEventRequest MonoStepper { get; set; }
		}

		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly ThreadMirror monoThread;
		SessionImpl session;

		public DbgDotNetEngineStepperImpl(DbgEngineImpl engine, DbgThread thread, ThreadMirror monoThread) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.monoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
		}

		static MDS.StackFrame GetFrame(ThreadMirror thread) {
			try {
				var frames = thread.GetFrames();
				return frames.Length == 0 ? null : frames[0];
			}
			catch (VMDisconnectedException) {
				return null;
			}
		}

		static StepFilter GetStepFilterFlags() => StepFilter.StaticCtor;

		public override SessionBase Session {
			get => session;
			set => session = (SessionImpl)value;
		}

		public override SessionBase CreateSession(object tag) => new SessionImpl(tag);

		public override bool IsRuntimePaused => engine.IsPaused;
		public override uint ContinueCounter => engine.ContinueCounter;

		sealed class DbgDotNetEngineStepperFrameInfoImpl : DbgDotNetEngineStepperFrameInfo {
			public override bool SupportsReturnValues => false;
			internal ThreadMirror MonoThread { get; }

			readonly DbgEngineImpl engine;

			public DbgDotNetEngineStepperFrameInfoImpl(DbgEngineImpl engine, ThreadMirror thread) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				MonoThread = thread ?? throw new ArgumentNullException(nameof(thread));
			}

			public override bool TryGetLocation(out DbgModule module, out uint token, out uint offset) {
				engine.VerifyMonoDebugThread();
				var frame = GetFrame(MonoThread);
				module = engine.TryGetModule(frame?.Method?.DeclaringType.Module);
				token = (uint)(frame?.Method?.MetadataToken ?? 0);
				var offs = frame?.ILOffset;
				offset = (uint)(offs ?? 0);
				return module != null && token != 0 && offs != null;
			}
		}

		public override DbgDotNetEngineStepperFrameInfo TryGetFrameInfo() => new DbgDotNetEngineStepperFrameInfoImpl(engine, monoThread);
		public override void Continue() => throw new InvalidOperationException("Not reachable");

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
			Debug.Assert(session != null);
			var method = GetFrame(thread)?.Method;
			Debug.Assert(method != null);
			if (method == null)
				throw new StepErrorException("Internal error");

			for (int i = 0; i < MAX_STEPS; i++) {
				thread = await StepCore2Async(thread, ranges, isStepInto);
				var frame = GetFrame(thread);
				uint offset = (uint)(frame?.ILOffset ?? -1);
				if (frame?.Method != method || !IsInCodeRange(ranges, offset))
					break;
			}
			return engine.TryGetThread(thread) ?? this.thread;
		}

		Task<ThreadMirror> StepCore2Async(ThreadMirror thread, DbgCodeRange[] ranges, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(session != null);
			var tcs = new TaskCompletionSource<ThreadMirror>();
			var stepReq = engine.CreateStepRequest(thread, e => {
				if (engine.IsClosed)
					tcs.SetCanceled();
				else if (e.ForciblyCanceled)
					tcs.SetException(new ForciblyCanceledException(forciblyCanceledErrorMessage));
				else
					tcs.SetResult(thread);
				return true;
			});
			session.MonoStepper = stepReq;
			//TODO: StepOver fails on mono unless there's a portable PDB file available
			stepReq.Depth = isStepInto ? StepDepth.Into : StepDepth.Over;
			stepReq.Size = StepSize.Min;
			stepReq.Filter = GetStepFilterFlags();
			stepReq.Enable();
			engine.RunCore();
			return tcs.Task;
		}

		public override Task<DbgThread> StepOutAsync(DbgDotNetEngineStepperFrameInfo frame) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(session != null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepReq = engine.CreateStepRequest(frameImpl.MonoThread, e => {
				if (engine.IsClosed)
					tcs.SetCanceled();
				else if (e.ForciblyCanceled)
					tcs.SetException(new ForciblyCanceledException(forciblyCanceledErrorMessage));
				else
					tcs.SetResult(engine.TryGetThread(frameImpl.MonoThread) ?? thread);
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

		public override void CollectReturnValues(DbgDotNetEngineStepperFrameInfo frame, DbgILInstruction[][] statementInstructions) { }
		public override void OnStepComplete() { }

		public override void OnCanceled(SessionBase session) {
			engine.VerifyMonoDebugThread();
			var stepper = ((SessionImpl)session).MonoStepper;
			if (stepper != null)
				engine.CancelStepper(stepper);
		}

		public override bool IgnoreException(Exception exception) => exception is VMDisconnectedException;
		public override void Close(DbgDispatcher dispatcher) { }
	}
}
