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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Debugger.DotNet.Code;

namespace dnSpy.Debugger.DotNet.Steppers.Engine {
	sealed class DbgEngineStepperImpl : DbgEngineStepper {
		public override event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		readonly DbgDotNetDebugInfoService dbgDotNetDebugInfoService;
		readonly DebuggerSettings debuggerSettings;
		readonly IDbgDotNetRuntime runtime;
		readonly DbgDotNetEngineStepper stepper;
		readonly DbgThread thread;

		public DbgEngineStepperImpl(DbgDotNetDebugInfoService dbgDotNetDebugInfoService, DebuggerSettings debuggerSettings, IDbgDotNetRuntime runtime, DbgDotNetEngineStepper stepper, DbgThread thread) {
			this.dbgDotNetDebugInfoService = dbgDotNetDebugInfoService ?? throw new ArgumentNullException(nameof(dbgDotNetDebugInfoService));
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
				var thread = await stepper.StepIntoAsync(result.Frame, result.StatementRanges);
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
				stepper.CollectReturnValues(result.Frame, result.StatementInstructions);
				var thread = await stepper.StepOverAsync(result.Frame, result.StatementRanges);
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
			public MethodDebugInfo DebugInfo { get; }
			public DbgDotNetEngineStepperFrameInfo Frame { get; }
			public DbgCodeRange[] StatementRanges { get; }
			public DbgILInstruction[][] StatementInstructions { get; }
			public GetStepRangesAsyncResult(MethodDebugInfo debugInfo, DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] statementRanges, DbgILInstruction[][] statementInstructions) {
				DebugInfo = debugInfo;
				Frame = frame ?? throw new ArgumentNullException(nameof(frame));
				StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
				StatementInstructions = statementInstructions ?? throw new ArgumentNullException(nameof(statementInstructions));
			}
		}

		async Task<GetStepRangesAsyncResult> GetStepRangesAsync(DbgDotNetEngineStepperFrameInfo frame, bool isStepInto) {
			runtime.Dispatcher.VerifyAccess();
			if (!frame.TryGetLocation(out var module, out uint token, out uint offset))
				throw new StepErrorException("Internal error");

			uint continueCounter = stepper.ContinueCounter;
			var methodDebugInfo = await dbgDotNetDebugInfoService.GetMethodDebugInfoAsync(module, token, offset);
			if (continueCounter != stepper.ContinueCounter)
				throw new StepErrorException("Internal error");

			var codeRanges = Array.Empty<DbgCodeRange>();
			var instructions = Array.Empty<DbgILInstruction[]>();
			if (methodDebugInfo != null) {
				var sourceStatement = methodDebugInfo.GetSourceStatementByCodeOffset(offset);
				uint[] ranges;
				if (sourceStatement == null)
					ranges = methodDebugInfo.GetUnusedRanges();
				else
					ranges = methodDebugInfo.GetRanges(sourceStatement.Value);

				codeRanges = CreateStepRanges(ranges);
				if (!isStepInto && debuggerSettings.ShowReturnValues && frame.SupportsReturnValues)
					instructions = GetInstructions(methodDebugInfo.Method, ranges) ?? Array.Empty<DbgILInstruction[]>();
			}
			if (codeRanges.Length == 0)
				codeRanges = new[] { new DbgCodeRange(offset, offset + 1) };
			return new GetStepRangesAsyncResult(methodDebugInfo, frame, codeRanges, instructions);
		}

		static DbgILInstruction[][] GetInstructions(MethodDef method, uint[] ranges) {
			var body = method.Body;
			if (body == null)
				return null;
			var instrs = body.Instructions;
			int instrsIndex = 0;

			var res = new DbgILInstruction[ranges.Length / 2][];
			var list = new List<DbgILInstruction>();
			for (int i = 0; i < res.Length; i++) {
				list.Clear();

				uint start = ranges[i * 2];
				uint end = ranges[i * 2 + 1];

				while (instrsIndex < instrs.Count && instrs[instrsIndex].Offset < start)
					instrsIndex++;
				while (instrsIndex < instrs.Count && instrs[instrsIndex].Offset < end) {
					var instr = instrs[instrsIndex];
					list.Add(new DbgILInstruction(instr.Offset, (ushort)instr.OpCode.Code, (instr.Operand as IMDTokenProvider)?.MDToken.Raw ?? 0));
					instrsIndex++;
				}

				res[i] = list.ToArray();
			}
			return res;
		}

		static DbgCodeRange[] CreateStepRanges(uint[] ilSpans) {
			if (ilSpans.Length <= 1)
				return Array.Empty<DbgCodeRange>();
			var stepRanges = new DbgCodeRange[ilSpans.Length / 2];
			for (int i = 0; i < stepRanges.Length; i++)
				stepRanges[i] = new DbgCodeRange(ilSpans[i * 2], ilSpans[i * 2 + 1]);
			return stepRanges;
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
