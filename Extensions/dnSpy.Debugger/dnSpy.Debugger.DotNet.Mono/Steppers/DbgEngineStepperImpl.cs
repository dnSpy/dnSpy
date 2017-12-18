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

		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly ThreadMirror monoThread;
		StepData stepData;

		sealed class StepData {
			public object Tag { get; }
			public StepEventRequest MonoStepper { get; }
			public StepData(object tag, StepEventRequest monoStepper) {
				Tag = tag;
				MonoStepper = monoStepper;
			}
		}

		sealed class StepIntoOverData {
			public bool IsStepInto { get; }
			public MethodMirror Method { get; }
			public DbgCodeRange[] StatementRanges { get; }
			public int StepCounter;
			public StepIntoOverData(bool isStepInto, MethodMirror method, DbgCodeRange[] statementRanges) {
				IsStepInto = isStepInto;
				Method = method;
				StatementRanges = statementRanges;
			}
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

		MDS.StackFrame GetFrame() {
			try {
				var frames = monoThread.GetFrames();
				return frames.Length == 0 ? null : frames[0];
			}
			catch (VMDisconnectedException) {
				return null;
			}
		}

		public override void Step(object tag, DbgEngineStepKind step) => engine.MonoDebugThread(() => Step_MonoDebug(tag, step));
		void Step_MonoDebug(object tag, DbgEngineStepKind step) {
			engine.VerifyMonoDebugThread();
			try {
				StepCore_MonoDebug(tag, step);
			}
			catch (VMDisconnectedException) {
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				RaiseStepComplete(thread, tag, $"Exception: {ex.GetType().FullName}: {ex.Message}");
			}
		}

		void StepCore_MonoDebug(object tag, DbgEngineStepKind step) {
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

			switch (step) {
			case DbgEngineStepKind.StepInto:
				GetStepRanges(tag, isStepInto: true);
				return;

			case DbgEngineStepKind.StepOver:
				GetStepRanges(tag, isStepInto: false);
				return;

			case DbgEngineStepKind.StepOut:
				var stepReq = engine.CreateStepRequest(monoThread, e => OnStepOutCompleted(e, tag));
				stepReq.Depth = StepDepth.Out;
				stepReq.Size = StepSize.Min;
				stepReq.Filter = GetStepFilterFlags();
				stepReq.Enable();
				SaveStepper(stepReq, tag, callRunCore: true);
				return;

			default:
				RaiseStepComplete(thread, tag, $"Unsupported step kind: {step}");
				return;
			}
		}

		static StepFilter GetStepFilterFlags() => StepFilter.StaticCtor;

		static string GetErrorMessage(StepCompleteEventArgs e) => e.ForciblyCanceled ? "Only one stepper can be active at a time" : null;

		void SaveStepper(StepEventRequest newMonoStepper, object tag, bool callRunCore) {
			engine.VerifyMonoDebugThread();
			if (newMonoStepper != null) {
				stepData = new StepData(tag, newMonoStepper);
				if (callRunCore)
					engine.RunCore();
			}
			else {
				// This should rarely if ever happen so the string doesn't need to be localized
				RaiseStepComplete(thread, tag, "Could not step");
			}
		}

		bool OnStepOutCompleted(StepCompleteEventArgs e, object tag) {
			engine.VerifyMonoDebugThread();
			if (stepData == null || stepData.MonoStepper != e.StepEventRequest || stepData.Tag != tag)
				return false;
			stepData = null;
			RaiseStepComplete(thread, tag, GetErrorMessage(e), e.ForciblyCanceled);
			return true;
		}

		void GetStepRanges(object tag, bool isStepInto) {
			engine.VerifyMonoDebugThread();
			var frame = GetFrame();
			var module = engine.TryGetModule(frame?.Method?.DeclaringType.Module);
			var offset = frame?.ILOffset;
			if (module == null || offset == null)
				SaveStepper(null, tag, callRunCore: true);
			else {
				uint continueCounter = engine.ContinueCounter;
				dbgDotNetCodeRangeService.GetCodeRanges(module, (uint)frame.Method.MetadataToken, (uint)offset.Value, GetCodeRangesOptions.None,
					result => engine.MonoDebugThread(() => GotStepRanges(frame, tag, isStepInto, result, continueCounter)));
			}
		}

		void GotStepRanges(MDS.StackFrame frame, object tag, bool isStepInto, GetCodeRangeResult result, uint continueCounter) {
			engine.VerifyMonoDebugThread();
			if (IsClosed)
				return;
			if (stepData != null)
				return;
			if (continueCounter != engine.ContinueCounter) {
				RaiseStepComplete(thread, tag, "Internal error");
				return;
			}
			// If we failed to find the statement ranges (result.Success == false), step anyway.
			// We'll just step until the next sequence point instead of not doing anything.
			var stepIntoOverData = new StepIntoOverData(isStepInto, frame.Method, result.StatementRanges);
			StartStepIntoOver(tag, stepIntoOverData);
		}

		void StartStepIntoOver(object tag, StepIntoOverData stepIntoOverData) {
			engine.VerifyMonoDebugThread();
			try {
				stepIntoOverData.StepCounter++;
				var stepReq = engine.CreateStepRequest(monoThread, e => OnStepIntoOverCompleted(e, tag, stepIntoOverData));
				//TODO: StepOver fails on mono unless there's a portable PDB file available
				stepReq.Depth = stepIntoOverData.IsStepInto ? StepDepth.Into : StepDepth.Over;
				stepReq.Size = StepSize.Min;
				stepReq.Filter = GetStepFilterFlags();
				stepReq.Enable();
				SaveStepper(stepReq, tag, callRunCore: stepIntoOverData.StepCounter == 1);
			}
			catch (VMDisconnectedException) {
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				RaiseStepComplete(thread, tag, $"Exception: {ex.GetType().FullName}: {ex.Message}");
			}
		}

		bool OnStepIntoOverCompleted(StepCompleteEventArgs e, object tag, StepIntoOverData stepIntoOverData) {
			engine.VerifyMonoDebugThread();
			if (stepData == null || stepData.MonoStepper != e.StepEventRequest || stepData.Tag != tag)
				return false;
			stepData = null;

			var frame = GetFrame();
			uint offset = (uint)(frame?.ILOffset ?? -1);

			if (!e.ForciblyCanceled && stepIntoOverData.StepCounter < MAX_STEPS && frame?.Method == stepIntoOverData.Method && IsInCodeRange(stepIntoOverData.StatementRanges, offset)) {
				StartStepIntoOver(tag, stepIntoOverData);
				return false;
			}
			else {
				RaiseStepComplete(thread, tag, GetErrorMessage(e), e.ForciblyCanceled);
				return true;
			}
		}

		static bool IsInCodeRange(DbgCodeRange[] ranges, uint offset) {
			foreach (var range in ranges) {
				if (range.Start <= offset && offset < range.End)
					return true;
			}
			return false;
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
