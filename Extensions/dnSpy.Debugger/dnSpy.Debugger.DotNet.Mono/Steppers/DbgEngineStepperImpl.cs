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
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;
using MDS = Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Steppers {
	sealed class DbgEngineStepperImpl : DbgEngineStepper {
		const int MAX_STEPS = 1000;

		public override event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		sealed class StepErrorException : Exception {
			public StepErrorException(string message) : base(message) { }
		}
		sealed class ForciblyCanceledException : Exception { }

		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly ThreadMirror monoThread;
		StepData stepData;

		sealed class StepData {
			public object Tag { get; }
			public StepEventRequest MonoStepper { get; set; }
			public StepData(object tag) => Tag = tag;
		}

		public DbgEngineStepperImpl(DbgDotNetCodeRangeService dbgDotNetCodeRangeService, DbgEngineImpl engine, DbgThread thread, ThreadMirror monoThread) {
			this.dbgDotNetCodeRangeService = dbgDotNetCodeRangeService ?? throw new ArgumentNullException(nameof(dbgDotNetCodeRangeService));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.monoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
		}

		void RaiseStepComplete(DbgThread thread, object tag, string error, bool forciblyCanceled = false) {
			if (IsClosed)
				return;
			Debug.Assert(StepComplete != null);
			StepComplete?.Invoke(this, new DbgEngineStepCompleteEventArgs(thread, tag, error, forciblyCanceled));
		}

		MDS.StackFrame GetFrame(ThreadMirror thread) {
			try {
				var frames = thread.GetFrames();
				return frames.Length == 0 ? null : frames[0];
			}
			catch (VMDisconnectedException) {
				return null;
			}
		}

		public override void Step(object tag, DbgEngineStepKind step) => engine.MonoDebugThread(() => Step_MonoDebug(tag, step));
		void Step_MonoDebug(object tag, DbgEngineStepKind step) {
			engine.VerifyMonoDebugThread();

			if (stepData != null) {
				Debug.Fail("The previous step hasn't been canceled");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "The previous step hasn't been canceled");
				return;
			}

			if (!engine.IsPaused) {
				Debug.Fail("Process is not paused");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "Process is not paused");
				return;
			}

			StepAsync(tag, step).ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			});
		}

		static StepFilter GetStepFilterFlags() => StepFilter.StaticCtor;

		static string GetErrorMessage(bool forciblyCanceled) => forciblyCanceled ? "Only one stepper can be active at a time" : null;

		Task StepAsync(object tag, DbgEngineStepKind step) {
			engine.VerifyMonoDebugThread();
			switch (step) {
			case DbgEngineStepKind.StepInto:	return StepIntoAsync(tag);
			case DbgEngineStepKind.StepOver:	return StepOverAsync(tag);
			case DbgEngineStepKind.StepOut:		return StepOutAsync(tag);
			default:
				RaiseStepComplete(thread, tag, $"Unsupported step kind: {step}");
				return Task.CompletedTask;
			}
		}

		async Task StepIntoAsync(object tag) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData == null);
			try {
				stepData = new StepData(tag);
				var result = await GetStepRangesAsync(monoThread);
				// If we failed to find the statement ranges (result.Success == false), step anyway.
				// We'll just step until the next sequence point instead of not doing anything.
				var ranges = result.Result.Success ? result.Result.StatementRanges : new[] { new DbgCodeRange(result.Offset, result.Offset + 1) };
				var stepRes = await StepIntoCoreAsync(result.Thread, ranges);
				StepCompleted(stepRes.Thread, stepRes.ForciblyCanceled, tag);
			}
			catch (VMDisconnectedException) {
			}
			catch (ForciblyCanceledException) {
				StepCompleted(monoThread, true, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				StepFailed(ex, tag);
			}
		}

		async Task StepOverAsync(object tag) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData == null);
			try {
				stepData = new StepData(tag);
				var result = await GetStepRangesAsync(monoThread);
				// If we failed to find the statement ranges (result.Success == false), step anyway.
				// We'll just step until the next sequence point instead of not doing anything.
				var ranges = result.Result.Success ? result.Result.StatementRanges : new[] { new DbgCodeRange(result.Offset, result.Offset + 1) };
				var stepRes = await StepOverCoreAsync(result.Thread, ranges);
				StepCompleted(stepRes.Thread, stepRes.ForciblyCanceled, tag);
			}
			catch (VMDisconnectedException) {
			}
			catch (ForciblyCanceledException) {
				StepCompleted(monoThread, true, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				StepFailed(ex, tag);
			}
		}

		async Task StepOutAsync(object tag) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData == null);
			try {
				stepData = new StepData(tag);
				var result = await StepOutCoreAsync(monoThread);
				StepCompleted(result.Thread, result.ForciblyCanceled, tag);
			}
			catch (VMDisconnectedException) {
			}
			catch (ForciblyCanceledException) {
				StepCompleted(monoThread, true, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				StepFailed(ex, tag);
			}
		}

		readonly struct StepResult {
			public ThreadMirror Thread { get; }
			public bool ForciblyCanceled { get; }
			public StepResult(ThreadMirror thread, bool forciblyCanceled) {
				Thread = thread ?? throw new ArgumentNullException(nameof(thread));
				ForciblyCanceled = forciblyCanceled;
			}
		}

		Task<StepResult> StepIntoCoreAsync(ThreadMirror thread, DbgCodeRange[] ranges) {
			engine.VerifyMonoDebugThread();
			return StepCoreAsync(thread, ranges, isStepInto: true);
		}

		Task<StepResult> StepOverCoreAsync(ThreadMirror thread, DbgCodeRange[] ranges) {
			engine.VerifyMonoDebugThread();
			return StepCoreAsync(thread, ranges, isStepInto: false);
		}

		async Task<StepResult> StepCoreAsync(ThreadMirror thread, DbgCodeRange[] ranges, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData != null);
			var method = GetFrame(thread)?.Method;
			Debug.Assert(method != null);
			if (method == null)
				throw new StepErrorException("Internal error");

			for (int i = 0; i < MAX_STEPS; i++) {
				var result = await StepCore2Async(thread, ranges, isStepInto);
				thread = result.Thread;
				if (result.ForciblyCanceled)
					throw new ForciblyCanceledException();
				var frame = GetFrame(thread);
				uint offset = (uint)(frame?.ILOffset ?? -1);
				if (frame?.Method != method || !IsInCodeRange(ranges, offset))
					break;
			}
			return new StepResult(thread, false);
		}

		Task<StepResult> StepCore2Async(ThreadMirror thread, DbgCodeRange[] ranges, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData != null);
			var tcs = new TaskCompletionSource<StepResult>();
			var stepReq = engine.CreateStepRequest(thread, e => {
				if (engine.IsClosed)
					tcs.SetCanceled();
				else
					tcs.SetResult(new StepResult(thread, e.ForciblyCanceled));
				return true;
			});
			stepData.MonoStepper = stepReq;
			//TODO: StepOver fails on mono unless there's a portable PDB file available
			stepReq.Depth = isStepInto ? StepDepth.Into : StepDepth.Over;
			stepReq.Size = StepSize.Min;
			stepReq.Filter = GetStepFilterFlags();
			stepReq.Enable();
			engine.RunCore();
			return tcs.Task;
		}

		Task<StepResult> StepOutCoreAsync(ThreadMirror thread) {
			engine.VerifyMonoDebugThread();
			Debug.Assert(stepData != null);
			var tcs = new TaskCompletionSource<StepResult>();
			var stepReq = engine.CreateStepRequest(thread, e => {
				if (engine.IsClosed)
					tcs.SetCanceled();
				else
					tcs.SetResult(new StepResult(thread, e.ForciblyCanceled));
				return true;
			});
			stepData.MonoStepper = stepReq;
			stepReq.Depth = StepDepth.Out;
			stepReq.Size = StepSize.Min;
			stepReq.Filter = GetStepFilterFlags();
			stepReq.Enable();
			engine.RunCore();
			return tcs.Task;
		}

		readonly struct GetStepRangesAsyncResult {
			public GetCodeRangeResult Result { get; }
			public ThreadMirror Thread { get; }
			public uint Offset { get; }
			public GetStepRangesAsyncResult(in GetCodeRangeResult result, ThreadMirror thread, uint offset) {
				Result = result;
				Thread = thread ?? throw new ArgumentNullException(nameof(thread));
				Offset = offset;
			}
		}

		async Task<GetStepRangesAsyncResult> GetStepRangesAsync(ThreadMirror thread) {
			engine.VerifyMonoDebugThread();
			var frame = GetFrame(thread);
			var module = engine.TryGetModule(frame?.Method?.DeclaringType.Module);
			var offset = frame?.ILOffset;
			if (module == null || offset == null)
				throw new StepErrorException("Internal error");

			uint continueCounter = engine.ContinueCounter;
			var result = await dbgDotNetCodeRangeService.GetCodeRangesAsync(module, (uint)frame.Method.MetadataToken, (uint)offset.Value, GetCodeRangesOptions.None);
			if (continueCounter != engine.ContinueCounter)
				throw new StepErrorException("Internal error");
			return new GetStepRangesAsyncResult(result, thread, (uint)offset.Value);
		}

		static bool IsInCodeRange(DbgCodeRange[] ranges, uint offset) {
			foreach (var range in ranges) {
				if (range.Start <= offset && offset < range.End)
					return true;
			}
			return false;
		}

		void StepCompleted(ThreadMirror thread, bool forciblyCanceled, object tag) {
			engine.VerifyMonoDebugThread();
			if (stepData == null || stepData.Tag != tag)
				return;
			stepData = null;
			var pausedThread = thread == monoThread ? this.thread : engine.TryGetThread(thread);
			Debug.Assert(engine.TryGetThread(thread) == pausedThread);
			RaiseStepComplete(pausedThread, tag, GetErrorMessage(forciblyCanceled), forciblyCanceled);
		}

		void StepError(string errorMessage, object tag) {
			engine.VerifyMonoDebugThread();
			if (stepData == null || stepData.Tag != tag)
				return;
			stepData = null;
			var pausedThread = thread.IsClosed ? null : thread;
			RaiseStepComplete(pausedThread, tag, errorMessage);
		}

		void StepFailed(Exception exception, object tag) {
			engine.VerifyMonoDebugThread();
			StepError("Internal error: " + exception.Message, tag);
		}

		public override void Cancel(object tag) => engine.MonoDebugThread(() => Cancel_MonoDebug(tag));
		void Cancel_MonoDebug(object tag) {
			engine.VerifyMonoDebugThread();
			var oldStepperData = stepData;
			if (oldStepperData == null)
				return;
			if (oldStepperData.Tag != tag)
				return;
			ForceCancel_MonoDebug();
		}

		void ForceCancel_MonoDebug() {
			engine.VerifyMonoDebugThread();
			var oldStepperData = stepData;
			stepData = null;
			if (oldStepperData != null)
				engine.CancelStepper(oldStepperData.MonoStepper);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			if (stepData != null)
				engine.MonoDebugThread(() => ForceCancel_MonoDebug());
		}
	}
}
