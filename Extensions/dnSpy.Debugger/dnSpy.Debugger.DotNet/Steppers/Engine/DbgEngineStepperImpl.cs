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
using System.Linq;
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

		DbgThread CurrentThread {
			get => __DONT_USE_currentThread;
			set => __DONT_USE_currentThread = value ?? throw new InvalidOperationException();
		}
		DbgThread __DONT_USE_currentThread;

		public DbgEngineStepperImpl(DbgDotNetDebugInfoService dbgDotNetDebugInfoService, DebuggerSettings debuggerSettings, IDbgDotNetRuntime runtime, DbgDotNetEngineStepper stepper, DbgThread thread) {
			this.dbgDotNetDebugInfoService = dbgDotNetDebugInfoService ?? throw new ArgumentNullException(nameof(dbgDotNetDebugInfoService));
			this.debuggerSettings = debuggerSettings ?? throw new ArgumentNullException(nameof(debuggerSettings));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.stepper = stepper ?? throw new ArgumentNullException(nameof(stepper));
			CurrentThread = thread ?? throw new ArgumentNullException(nameof(thread));
		}

		void RaiseStepComplete(object tag, string error, bool forciblyCanceled = false) {
			runtime.Dispatcher.VerifyAccess();
			SetAsyncStepOverState(null);
			if (IsClosed)
				return;
			var thread = CurrentThread.IsClosed ? null : CurrentThread;
			Debug.Assert(StepComplete != null);
			StepComplete?.Invoke(this, new DbgEngineStepCompleteEventArgs(thread, tag, error, forciblyCanceled));
		}

		public override void Step(object tag, DbgEngineStepKind step) => runtime.Dispatcher.BeginInvoke(() => Step_EngineThread(tag, step));
		void Step_EngineThread(object tag, DbgEngineStepKind step) {
			runtime.Dispatcher.VerifyAccess();

			if (stepper.Session != null) {
				Debug.Fail("The previous step hasn't been canceled");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(tag, "The previous step hasn't been canceled");
				return;
			}

			if (!stepper.IsRuntimePaused) {
				Debug.Fail("Process is not paused");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(tag, "Process is not paused");
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
				RaiseStepComplete(tag, $"Unsupported step kind: {step}");
				return Task.CompletedTask;
			}
		}

		async Task StepIntoAsync(object tag) {
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session == null);
			try {
				var frame = stepper.TryGetFrameInfo(CurrentThread);
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				var result = await GetStepRangesAsync(frame, isStepInto: true);
				CurrentThread = await stepper.StepIntoAsync(result.Frame, result.StatementRanges);
				StepCompleted(null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(fce.Message, tag);
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
				var frame = stepper.TryGetFrameInfo(CurrentThread);
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				CurrentThread = await StepOverCoreAsync(frame);
				StepCompleted(null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(fce.Message, tag);
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

		async Task<DbgThread> StepOverCoreAsync(DbgDotNetEngineStepperFrameInfo frame) {
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session != null);

			DbgThread thread;
			var result = await GetStepRangesAsync(frame, isStepInto: false);
			var asyncStepInfos = GetAsyncStepInfos(result);
			Debug.Assert(asyncStepInfos == null || asyncStepInfos.Count != 0);
			if (asyncStepInfos != null) {
				if (!frame.TryGetLocation(out var module, out var token, out _))
					throw new InvalidOperationException();

				try {
					var asyncState = SetAsyncStepOverState(new AsyncStepOverState(stepper));
					foreach (var stepInfo in asyncStepInfos)
						asyncState.AddYieldBreakpoint(frame.Thread, module, token, stepInfo);
					var yieldBreakpointTask = asyncState.Task;

					stepper.CollectReturnValues(result.Frame, result.StatementInstructions);
					var stepOverTask = stepper.StepOverAsync(result.Frame, result.StatementRanges);
					var completedTask = await Task.WhenAny(stepOverTask, yieldBreakpointTask);
					if (completedTask == stepOverTask) {
						asyncState.Dispose();
						thread = stepOverTask.Result;
					}
					else {
						stepper.CancelLastStep();
						asyncState.ClearYieldBreakpoints();
						var resumeBpTask = asyncState.SetResumeBreakpoint(module, token);
						stepper.Continue();
						thread = await resumeBpTask;
						asyncState.Dispose();

						var newFrame = stepper.TryGetFrameInfo(thread);
						Debug.Assert(newFrame != null);
						if (newFrame != null && newFrame.TryGetLocation(out var newModule, out var newToken, out var newOffset)) {
							Debug.Assert(newModule == module && token == newToken);
							if (newModule == module && token == newToken) {
								var newStatement = result.DebugInfo.GetSourceStatementByCodeOffset(newOffset);
								// If we're not on an existing statement (very likely), we need to step over the
								// hidden instructions until we reach the next statement.
								if (newStatement == null) {
									var ranges = CreateStepRanges(result.DebugInfo.GetUnusedRanges());
									if (ranges.Length != 0)
										thread = await stepper.StepOverAsync(newFrame, ranges);
								}
							}
						}
					}
				}
				finally {
					SetAsyncStepOverState(null);
					stepper.CancelLastStep();
				}
			}
			else {
				stepper.CollectReturnValues(result.Frame, result.StatementInstructions);
				thread = await stepper.StepOverAsync(result.Frame, result.StatementRanges);
			}

			return thread;
		}

		static List<AsyncStepInfo> GetAsyncStepInfos(in GetStepRangesAsyncResult result) {
			if (result.DebugInfo?.AsyncInfo == null)
				return null;
			List<AsyncStepInfo> asyncStepInfos = null;
			GetAsyncStepInfos(ref asyncStepInfos, result.DebugInfo.AsyncInfo, result.ExactStatementRanges);
			foreach (var ranges in GetHiddenRanges(result.ExactStatementRanges, result.DebugInfo.GetUnusedRanges()))
				GetAsyncStepInfos(ref asyncStepInfos, result.DebugInfo.AsyncInfo, ranges);
			return asyncStepInfos;
		}

		AsyncStepOverState SetAsyncStepOverState(AsyncStepOverState state) {
			runtime.Dispatcher.VerifyAccess();
			__DONT_USE_asyncStepOverState?.Dispose();
			__DONT_USE_asyncStepOverState = state;
			return state;
		}
		AsyncStepOverState __DONT_USE_asyncStepOverState;

		sealed class AsyncStepOverState {
			readonly DbgDotNetEngineStepper stepper;
			readonly List<AsyncBreakpointState> yieldBreakpoints;
			readonly TaskCompletionSource<AsyncBreakpointState> yieldTaskCompletionSource;
			DbgDotNetStepperBreakpoint resumeBreakpoint;

			public Task Task => yieldTaskCompletionSource.Task;

			public AsyncStepOverState(DbgDotNetEngineStepper stepper) {
				this.stepper = stepper;
				yieldBreakpoints = new List<AsyncBreakpointState>();
				yieldTaskCompletionSource = new TaskCompletionSource<AsyncBreakpointState>();
			}

			public void AddYieldBreakpoint(DbgThread thread, DbgModule module, uint token, AsyncStepInfo stepInfo) {
				var yieldBreakpoint = stepper.CreateBreakpoint(thread, module, token, stepInfo.YieldOffset);
				try {
					var bpState = new AsyncBreakpointState(yieldBreakpoint, stepInfo.ResumeOffset);
					bpState.Hit += AsyncBreakpointState_Hit;
					yieldBreakpoints.Add(bpState);
				}
				catch {
					stepper.RemoveBreakpoints(new[] { yieldBreakpoint });
					throw;
				}
			}

			void AsyncBreakpointState_Hit(object sender, AsyncBreakpointState bpState) => yieldTaskCompletionSource.SetResult(bpState);

			internal Task<DbgThread> SetResumeBreakpoint(DbgModule module, uint token) {
				Debug.Assert(yieldTaskCompletionSource.Task.IsCompleted);
				Debug.Assert(resumeBreakpoint == null);
				if (resumeBreakpoint != null)
					throw new InvalidOperationException();
				var bpState = yieldTaskCompletionSource.Task.GetAwaiter().GetResult();
				// The thread can change so pass in null == any thread
				resumeBreakpoint = stepper.CreateBreakpoint(null, module, token, bpState.ResumeOffset);
				var tcs = new TaskCompletionSource<DbgThread>();
				resumeBreakpoint.Hit += (s, e) => tcs.SetResult(e.Thread);
				return tcs.Task;
			}

			internal void ClearYieldBreakpoints() {
				var bps = yieldBreakpoints.Select(a => a.Breakpoint).ToArray();
				yieldBreakpoints.Clear();
				stepper.RemoveBreakpoints(bps);
			}

			internal void Dispose() {
				ClearYieldBreakpoints();
				if (resumeBreakpoint != null) {
					stepper.RemoveBreakpoints(new[] { resumeBreakpoint });
					resumeBreakpoint = null;
				}
			}
		}

		sealed class AsyncBreakpointState {
			internal readonly DbgDotNetStepperBreakpoint Breakpoint;
			internal readonly uint ResumeOffset;

			public event EventHandler<AsyncBreakpointState> Hit;

			public AsyncBreakpointState(DbgDotNetStepperBreakpoint yieldBreakpoint, uint resumeOffset) {
				Breakpoint = yieldBreakpoint;
				ResumeOffset = resumeOffset;
				yieldBreakpoint.Hit += YieldBreakpoint_Hit;
			}

			void YieldBreakpoint_Hit(object sender, DbgDotNetStepperBreakpointEventArgs e) {
				Debug.Assert(Hit != null);
				Hit?.Invoke(this, this);
			}
		}

		static IEnumerable<DbgCodeRange[]> GetHiddenRanges(DbgCodeRange[] statements, BinSpan[] unusedSpans) {
#if DEBUG
			for (int i = 1; i < statements.Length; i++)
				Debug.Assert(statements[i - 1].End <= statements[i].Start);
			for (int i = 1; i < unusedSpans.Length; i++)
				Debug.Assert(unusedSpans[i - 1].End <= unusedSpans[i].Start);
#endif
			int si = 0;
			int ui = 0;
			while (si < statements.Length && ui < unusedSpans.Length) {
				while (ui < unusedSpans.Length && statements[si].End > unusedSpans[ui].Start)
					ui++;
				if (ui >= unusedSpans.Length)
					break;
				// If a hidden range immediately follows a normal statement, the hidden part could be the removed
				// async code and should be part of this statement.
				if (statements[si].End == unusedSpans[ui].Start)
					yield return new[] { new DbgCodeRange(unusedSpans[ui].Start, unusedSpans[ui].End) };
				si++;
			}
		}

		static void GetAsyncStepInfos(ref List<AsyncStepInfo> result, AsyncMethodDebugInfo asyncInfo, DbgCodeRange[] ranges) {
			var stepInfos = asyncInfo.StepInfos;
			for (int i = 0; i < stepInfos.Length; i++) {
				ref readonly var stepInfo = ref stepInfos[i];
				if (Contains(ranges, stepInfo)) {
					if (result == null)
						result = new List<AsyncStepInfo>();
					result.Add(stepInfo);
				}
			}
		}

		static bool Contains(DbgCodeRange[] ranges, in AsyncStepInfo stepInfo) {
			for (int i = 0; i < ranges.Length; i++) {
				ref readonly var range = ref ranges[i];
				if (range.Contains(stepInfo.YieldOffset) || range.Contains(stepInfo.ResumeOffset))
					return true;
			}
			return false;
		}

		async Task StepOutAsync(object tag) {
			runtime.Dispatcher.VerifyAccess();
			Debug.Assert(stepper.Session == null);
			try {
				var frame = stepper.TryGetFrameInfo(CurrentThread);
				if (frame == null) {
					// No frame? Just let the process run.
					stepper.Continue();
					return;
				}

				stepper.Session = stepper.CreateSession(tag);
				CurrentThread = await stepper.StepOutAsync(frame);
				StepCompleted(null, tag);
			}
			catch (ForciblyCanceledException fce) {
				StepCompleted(fce.Message, tag);
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
			public DbgCodeRange[] ExactStatementRanges { get; }
			public DbgILInstruction[][] StatementInstructions { get; }
			public GetStepRangesAsyncResult(MethodDebugInfo debugInfo, DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] statementRanges, DbgCodeRange[] exactStatementRanges, DbgILInstruction[][] statementInstructions) {
				DebugInfo = debugInfo;
				Frame = frame ?? throw new ArgumentNullException(nameof(frame));
				StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
				ExactStatementRanges = exactStatementRanges ?? throw new ArgumentNullException(nameof(exactStatementRanges));
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
			var exactCodeRanges = Array.Empty<DbgCodeRange>();
			var instructions = Array.Empty<DbgILInstruction[]>();
			if (methodDebugInfo != null) {
				var sourceStatement = methodDebugInfo.GetSourceStatementByCodeOffset(offset);
				BinSpan[] ranges;
				if (sourceStatement == null)
					ranges = methodDebugInfo.GetUnusedRanges();
				else {
					var sourceStatements = methodDebugInfo.GetBinSpansOfStatement(sourceStatement.Value.TextSpan);
					Debug.Assert(sourceStatements.Any(a => a == sourceStatement.Value.BinSpan));
					exactCodeRanges = CreateStepRanges(sourceStatements);
					ranges = methodDebugInfo.GetRanges(sourceStatements);
				}

				codeRanges = CreateStepRanges(ranges);
				if (!isStepInto && debuggerSettings.ShowReturnValues && frame.SupportsReturnValues)
					instructions = GetInstructions(methodDebugInfo.Method, exactCodeRanges) ?? Array.Empty<DbgILInstruction[]>();
			}
			if (codeRanges.Length == 0)
				codeRanges = new[] { new DbgCodeRange(offset, offset + 1) };
			if (exactCodeRanges.Length == 0)
				exactCodeRanges = new[] { new DbgCodeRange(offset, offset + 1) };
			return new GetStepRangesAsyncResult(methodDebugInfo, frame, codeRanges, exactCodeRanges, instructions);
		}

		static DbgILInstruction[][] GetInstructions(MethodDef method, DbgCodeRange[] ranges) {
			var body = method.Body;
			if (body == null)
				return null;
			var instrs = body.Instructions;
			int instrsIndex = 0;

			var res = new DbgILInstruction[ranges.Length][];
			var list = new List<DbgILInstruction>();
			for (int i = 0; i < res.Length; i++) {
				list.Clear();

				ref readonly var span = ref ranges[i];
				uint start = span.Start;
				uint end = span.End;

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

		static DbgCodeRange[] CreateStepRanges(BinSpan[] binSpans) {
			if (binSpans.Length == 0)
				return Array.Empty<DbgCodeRange>();
			var stepRanges = new DbgCodeRange[binSpans.Length];
			for (int i = 0; i < stepRanges.Length; i++) {
				ref readonly var span = ref binSpans[i];
				stepRanges[i] = new DbgCodeRange(span.Start, span.End);
			}
			return stepRanges;
		}

		void StepCompleted(string forciblyCanceledErrorMessage, object tag) {
			runtime.Dispatcher.VerifyAccess();
			if (stepper.Session == null || stepper.Session.Tag != tag)
				return;
			if (forciblyCanceledErrorMessage == null)
				stepper.OnStepComplete();
			stepper.Session = null;
			RaiseStepComplete(tag, forciblyCanceledErrorMessage, forciblyCanceled: forciblyCanceledErrorMessage != null);
		}

		void StepError(string errorMessage, object tag) {
			runtime.Dispatcher.VerifyAccess();
			if (stepper.Session == null || stepper.Session.Tag != tag)
				return;
			stepper.Session = null;
			RaiseStepComplete(tag, errorMessage);
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
			SetAsyncStepOverState(null);
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
