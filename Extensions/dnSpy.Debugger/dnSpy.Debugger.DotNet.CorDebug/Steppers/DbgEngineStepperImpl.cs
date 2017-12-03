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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.DotNet.CorDebug.Impl;

namespace dnSpy.Debugger.DotNet.CorDebug.Steppers {
	sealed class DbgEngineStepperImpl : DbgEngineStepper {
		public override event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly DnThread dnThread;
		StepData stepData;

		sealed class StepData {
			public object Tag { get; }
			public CorStepper CorStepper { get; }
			public StepData(object tag, CorStepper corStepper) {
				Tag = tag;
				CorStepper = corStepper;
			}
		}

		public DbgEngineStepperImpl(DbgDotNetCodeRangeService dbgDotNetCodeRangeService, DbgEngineImpl engine, DbgThread thread, DnThread dnThread) {
			this.dbgDotNetCodeRangeService = dbgDotNetCodeRangeService ?? throw new ArgumentNullException(nameof(dbgDotNetCodeRangeService));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.dnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
		}

		void RaiseStepComplete(DbgThread thread, object tag, string error) {
			if (IsClosed)
				return;
			Debug.Assert(StepComplete != null);
			StepComplete?.Invoke(this, new DbgEngineStepCompleteEventArgs(thread, tag, error));
		}

		public override void Step(object tag, DbgEngineStepKind step) => engine.CorDebugThread(() => Step_CorDebug(tag, step));
		void Step_CorDebug(object tag, DbgEngineStepKind step) {
			engine.VerifyCorDebugThread();

			if (stepData != null) {
				Debug.Fail("The previous step hasn't been canceled");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "The previous step hasn't been canceled");
				return;
			}

			var dbg = dnThread.Debugger;
			if (dbg.ProcessState != DebuggerProcessState.Paused) {
				Debug.Fail("Process is not paused");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "Process is not paused");
				return;
			}

			var frame = GetILFrame();
			if (frame == null) {
				// No frame? Just let the process run.
				engine.Continue_CorDebug();
				return;
			}

			CorStepper newCorStepper = null;
			switch (step) {
			case DbgEngineStepKind.StepInto:
				GetStepRanges(frame, tag, isStepInto: true);
				return;

			case DbgEngineStepKind.StepOver:
				GetStepRanges(frame, tag, isStepInto: false);
				return;

			case DbgEngineStepKind.StepOut:
				newCorStepper = dbg.StepOut(frame, (_, e) => StepCompleted(e, newCorStepper, tag));
				SaveStepper(newCorStepper, tag);
				return;

			default:
				RaiseStepComplete(thread, tag, $"Unsupported step kind: {step}");
				return;
			}
		}

		void SaveStepper(CorStepper newCorStepper, object tag) {
			engine.VerifyCorDebugThread();
			if (newCorStepper != null) {
				stepData = new StepData(tag, newCorStepper);
				engine.Continue_CorDebug();
			}
			else {
				// This should rarely if ever happen so the string doesn't need to be localized
				RaiseStepComplete(thread, tag, "Could not step");
			}
		}

		void GetStepRanges(CorFrame frame, object tag, bool isStepInto) {
			engine.VerifyCorDebugThread();
			var module = engine.TryGetModule(frame.Function?.Module);
			var offset = GetILOffset(frame);
			if (module == null || offset == null)
				SaveStepper(null, tag);
			else {
				uint continueCounter = dnThread.Debugger.ContinueCounter;
				dbgDotNetCodeRangeService.GetCodeRanges(module, frame.Token, offset.Value,
					result => engine.CorDebugThread(() => GotStepRanges(frame, offset.Value, tag, isStepInto, result, continueCounter)));
			}
		}

		void GotStepRanges(CorFrame frame, uint offset, object tag, bool isStepInto, GetCodeRangeResult result, uint continueCounter) {
			engine.VerifyCorDebugThread();
			if (IsClosed)
				return;
			if (stepData != null)
				return;
			if (continueCounter != dnThread.Debugger.ContinueCounter || frame.IsNeutered) {
				RaiseStepComplete(thread, tag, "Internal error");
				return;
			}
			// If we failed to find the statement ranges (result.Success == false), step anyway.
			// We'll just step until the next sequence point instead of not doing anything.
			var ranges = result.Success ? ToStepRanges(result.StatementRanges) : new StepRange[] { new StepRange(offset, offset + 1) };
			CorStepper newCorStepper = null;
			var dbg = dnThread.Debugger;
			if (isStepInto)
				newCorStepper = dbg.StepInto(frame, ranges, (_, e) => StepCompleted(e, newCorStepper, tag));
			else
				newCorStepper = dbg.StepOver(frame, ranges, (_, e) => StepCompleted(e, newCorStepper, tag));
			SaveStepper(newCorStepper, tag);
		}

		static StepRange[] ToStepRanges(DbgCodeRange[] ranges) {
			var result = new StepRange[ranges.Length];
			for (int i = 0; i < result.Length; i++) {
				var r = ranges[i];
				result[i] = new StepRange(r.Start, r.End);
			}
			return result;
		}

		static uint? GetILOffset(CorFrame frame) {
			var ip = frame.ILFrameIP;
			if (ip.IsExact || ip.IsApproximate)
				return ip.Offset;
			if (ip.IsProlog)
				return DbgDotNetCodeRangeService.PROLOG;
			if (ip.IsEpilog)
				return DbgDotNetCodeRangeService.EPILOG;
			return null;
		}

		CorFrame GetILFrame() {
			engine.VerifyCorDebugThread();
			foreach (var frame in dnThread.AllFrames) {
				if (frame.IsILFrame)
					return frame;
			}
			return null;
		}

		void StepCompleted(StepCompleteDebugCallbackEventArgs e, CorStepper corStepper, object tag) {
			engine.VerifyCorDebugThread();
			if (stepData == null || stepData.CorStepper != corStepper || stepData.Tag != tag)
				return;
			stepData = null;
			e.AddPauseReason(DebuggerPauseReason.Other);
			var pausedThread = e.CorThread == dnThread.CorThread ? thread : engine.TryGetThread(e.CorThread);
			Debug.Assert(engine.TryGetThread(e.CorThread) == pausedThread);
			RaiseStepComplete(pausedThread, tag, null);
		}

		public override void Cancel(object tag) => engine.CorDebugThread(() => Cancel_CorDebug(tag));
		void Cancel_CorDebug(object tag) {
			engine.VerifyCorDebugThread();
			var oldStepperData = stepData;
			if (oldStepperData == null)
				return;
			if (oldStepperData.Tag != tag)
				return;
			ForceCancel_CorDebug();
		}

		void ForceCancel_CorDebug() {
			engine.VerifyCorDebugThread();
			var oldDnStepperData = stepData;
			stepData = null;
			if (oldDnStepperData != null)
				dnThread.Debugger.CancelStep(oldDnStepperData.CorStepper);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			if (stepData != null)
				engine.CorDebugThread(() => ForceCancel_CorDebug());
		}
	}
}
