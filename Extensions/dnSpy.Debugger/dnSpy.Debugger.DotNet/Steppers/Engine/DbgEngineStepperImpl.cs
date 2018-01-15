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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;

namespace dnSpy.Debugger.DotNet.Steppers.Engine {
	sealed class DbgEngineStepperImpl : DbgEngineStepper {
		public override event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DebuggerSettings debuggerSettings;
		readonly IDbgDotNetRuntime runtime;
		readonly DbgDotNetEngineStepper stepper;
		readonly DbgThread thread;

		public DbgEngineStepperImpl(DbgDotNetCodeRangeService dbgDotNetCodeRangeService, DebuggerSettings debuggerSettings, IDbgDotNetRuntime runtime, DbgDotNetEngineStepper stepper, DbgThread thread) {
			this.dbgDotNetCodeRangeService = dbgDotNetCodeRangeService ?? throw new ArgumentNullException(nameof(dbgDotNetCodeRangeService));
			this.debuggerSettings = debuggerSettings ?? throw new ArgumentNullException(nameof(debuggerSettings));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.stepper = stepper ?? throw new ArgumentNullException(nameof(stepper));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
		}

		void RaiseStepComplete(DbgThread thread, object tag, string error, bool forciblyCanceled = false) {
			if (IsClosed)
				return;
			Debug.Assert(StepComplete != null);
			StepComplete?.Invoke(this, new DbgEngineStepCompleteEventArgs(thread, tag, error, forciblyCanceled));
		}

		public override void Step(object tag, DbgEngineStepKind step) => runtime.Dispatcher.BeginInvoke(() => Step_EngineThread(tag, step));
		void Step_EngineThread(object tag, DbgEngineStepKind step) {
			runtime.Dispatcher.VerifyAccess();

			if (stepper.Session != null) {
				Debug.Fail("The previous step hasn't been canceled");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "The previous step hasn't been canceled");
				return;
			}

			if (!stepper.IsRuntimePaused) {
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

		Task StepAsync(object tag, DbgEngineStepKind step) {
			runtime.Dispatcher.VerifyAccess();
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
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session == null);
			try {
				var frame = stepper.TryGetFrameInfo();
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				var result = await GetStepRangesAsync(frame, isStepInto: true);
				// If we failed to find the statement ranges (result.Success == false), step anyway.
				// We'll just step until the next sequence point instead of not doing anything.
				var ranges = result.Result.Success ? result.Result.StatementRanges : new[] { new DbgCodeRange(result.Offset, result.Offset + 1) };
				var thread = await stepper.StepIntoAsync(result.Frame, ranges);
				StepCompleted(thread, null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(thread, fce.Message, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				if (stepper.IgnoreException(ex))
					return;
				StepFailed(ex, tag);
			}
		}

		async Task StepOverAsync(object tag) {
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session == null);
			try {
				var frame = stepper.TryGetFrameInfo();
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				var result = await GetStepRangesAsync(frame, isStepInto: false);
				// If we failed to find the statement ranges (result.Success == false), step anyway.
				// We'll just step until the next sequence point instead of not doing anything.
				var ranges = result.Result.Success ? result.Result.StatementRanges : new[] { new DbgCodeRange(result.Offset, result.Offset + 1) };
				stepper.CollectReturnValues(result.Frame, result.Result);
				var thread = await stepper.StepOverAsync(result.Frame, ranges);
				StepCompleted(thread, null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(thread, fce.Message, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				if (stepper.IgnoreException(ex))
					return;
				StepFailed(ex, tag);
			}
		}

		async Task StepOutAsync(object tag) {
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session == null);
			try {
				var frame = stepper.TryGetFrameInfo();
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				var thread = await stepper.StepOutAsync(frame);
				StepCompleted(thread, null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(thread, fce.Message, tag);
			}
			catch (StepErrorException see) {
				StepError(see.Message, tag);
			}
			catch (Exception ex) {
				if (stepper.IgnoreException(ex))
					return;
				StepFailed(ex, tag);
			}
		}

		readonly struct GetStepRangesAsyncResult {
			public GetCodeRangeResult Result { get; }
			public DbgDotNetEngineStepperFrameInfo Frame { get; }
			public uint Offset { get; }
			public GetStepRangesAsyncResult(in GetCodeRangeResult result, DbgDotNetEngineStepperFrameInfo frame, uint offset) {
				Result = result;
				Frame = frame ?? throw new ArgumentNullException(nameof(frame));
				Offset = offset;
			}
		}

		async Task<GetStepRangesAsyncResult> GetStepRangesAsync(DbgDotNetEngineStepperFrameInfo frame, bool isStepInto) {
			runtime.Dispatcher.VerifyAccess();
			if (!frame.TryGetLocation(out var module, out uint token, out uint offset))
				throw new StepErrorException("Internal error");

			uint continueCounter = stepper.ContinueCounter;
			var options = isStepInto || !debuggerSettings.ShowReturnValues || !frame.SupportsReturnValues ?
				GetCodeRangesOptions.None : GetCodeRangesOptions.Instructions;
			var result = await dbgDotNetCodeRangeService.GetCodeRangesAsync(module, token, offset, options);
			if (continueCounter != stepper.ContinueCounter)
				throw new StepErrorException("Internal error");
			return new GetStepRangesAsyncResult(result, frame, offset);
		}

		void StepCompleted(DbgThread thread, string forciblyCanceledErrorMessage, object tag) {
			runtime.Dispatcher.VerifyAccess();
			if (stepper.Session == null || stepper.Session.Tag != tag)
				return;
			if (forciblyCanceledErrorMessage == null)
				stepper.OnStepComplete();
			stepper.Session = null;
			RaiseStepComplete(thread, tag, forciblyCanceledErrorMessage, forciblyCanceled: forciblyCanceledErrorMessage != null);
		}

		void StepError(string errorMessage, object tag) {
			runtime.Dispatcher.VerifyAccess();
			if (stepper.Session == null || stepper.Session.Tag != tag)
				return;
			stepper.Session = null;
			var pausedThread = thread.IsClosed ? null : thread;
			RaiseStepComplete(pausedThread, tag, errorMessage);
		}

		void StepFailed(Exception exception, object tag) {
			runtime.Dispatcher.VerifyAccess();
			StepError("Internal error: " + exception.Message, tag);
		}

		public override void Cancel(object tag) => runtime.Dispatcher.BeginInvoke(() => Cancel_EngineThread(tag));
		void Cancel_EngineThread(object tag) {
			runtime.Dispatcher.VerifyAccess();
			var oldStepperData = stepper.Session;
			if (oldStepperData == null)
				return;
			if (oldStepperData.Tag != tag)
				return;
			ForceCancel_EngineThread();
		}

		void ForceCancel_EngineThread() {
			runtime.Dispatcher.VerifyAccess();
			var oldSession = stepper.Session;
			stepper.Session = null;
			if (oldSession != null)
				stepper.OnCanceled(oldSession);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			if (stepper.Session != null)
				runtime.Dispatcher.BeginInvoke(() => ForceCancel_EngineThread());
			stepper.Close(dispatcher);
		}
	}
}
