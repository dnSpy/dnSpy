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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.DotNet.Mono.Steppers;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		readonly struct StepperInfo {
			public Func<StepCompleteEventArgs, bool> OnStep { get; }
			public StepperInfo(Func<StepCompleteEventArgs, bool> onStep) => OnStep = onStep;
		}

		public override DbgEngineStepper CreateStepper(DbgThread thread) {
			var data = thread.GetData<DbgThreadData>();
			return dbgEngineStepperFactory.Create(DotNetRuntime, new DbgDotNetEngineStepperImpl(this), thread);
		}

		internal StepEventRequest CreateStepRequest(ThreadMirror monoThread, Func<StepCompleteEventArgs, bool> onStep) {
			debuggerThread.VerifyAccess();
			var stepReq = vm!.CreateStepRequest(monoThread);

			// There can be at most one stepper active at a time. This is a limitation of mono.
			foreach (var kv in toStepper) {
				kv.Key.Disable();
				kv.Value.OnStep(new StepCompleteEventArgs(kv.Key, true, false));
			}
			toStepper.Clear();

			toStepper.Add(stepReq, new StepperInfo(onStep));
			return stepReq;
		}

		bool OnStep(StepEventRequest? stepReq) {
			debuggerThread.VerifyAccess();
			if (stepReq is null)
				return false;
			bool b = toStepper.TryGetValue(stepReq, out var info);
			Debug.Assert(b);
			if (!b)
				return false;
			toStepper.Remove(stepReq);
			stepReq.Disable();
			return info.OnStep(new StepCompleteEventArgs(stepReq, false, false));
		}

		internal void CancelStepper(StepEventRequest stepReq) {
			debuggerThread.VerifyAccess();
			if (!(stepReq is null)) {
				try {
					using (TempBreak())
						stepReq.Disable();
				}
				catch {
				}
				if (toStepper.TryGetValue(stepReq, out var info)) {
					toStepper.Remove(stepReq);
					info.OnStep(new StepCompleteEventArgs(stepReq, false, true));
				}
			}
		}
	}

	readonly struct StepCompleteEventArgs {
		public StepEventRequest StepEventRequest { get; }
		public bool ForciblyCanceled { get; }
		public bool Canceled { get; }
		public StepCompleteEventArgs(StepEventRequest stepEventRequest, bool forciblyCanceled, bool canceled) {
			StepEventRequest = stepEventRequest;
			ForciblyCanceled = forciblyCanceled;
			Canceled = canceled;
		}
	}
}
