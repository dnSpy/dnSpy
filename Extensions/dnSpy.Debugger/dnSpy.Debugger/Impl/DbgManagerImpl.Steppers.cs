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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.Steppers;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		internal void Step(DbgStepperImpl stepper, object stepperTag, DbgEngineStepKind step, bool singleProcess) =>
			DbgThread(() => Step_DbgThread(stepper, stepperTag, step, singleProcess));

		void Step_DbgThread(DbgStepperImpl stepper, object stepperTag, DbgEngineStepKind step, bool singleProcess) {
			Dispatcher.VerifyAccess();

			var infos = new List<EngineInfo>();
			EngineInfo stepperEngineInfo = null;
			lock (lockObj) {
				var process = stepper.Process;
				var runtime = stepper.Runtime;
				foreach (var info in engines) {
					if (info.Runtime == runtime)
						stepperEngineInfo = info;
					if (info.Process == process || !singleProcess)
						infos.Add(info);
				}
			}

			if (stepperEngineInfo?.Process?.State != DbgProcessState.Paused) {
				RaiseStepperError_DbgThread(stepper, dnSpy_Debugger_Resources.ProcessIsNotPaused);
				return;
			}

			Debug.Assert(infos.Contains(stepperEngineInfo));
			RunEngines_DbgThread(infos.ToArray(), info => {
				if (info == stepperEngineInfo)
					stepper.EngineStepper.Step(stepperTag, step);
				else
					info.Engine.Run();
			});
		}

		void RaiseStepperError_DbgThread(DbgStepperImpl stepper, string error) {
			Dispatcher.VerifyAccess();
			stepper.RaiseError_DbgThread(string.Format(dnSpy_Debugger_Resources.DebugStepProcessError, error));
		}

		internal void StepComplete_DbgThread(DbgThreadImpl thread, string error, bool forciblyCanceled) {
			Dispatcher.VerifyAccess();
			var engine = thread.RuntimeImpl.Engine;
			if (engine.IsClosed)
				return;
			Debug.Assert(IsOurEngine(engine));
			if (!IsOurEngine(engine))
				return;
			var e = new DbgMessageStepCompleteEventArgs(thread, error);
			e.Pause = !forciblyCanceled;
			OnConditionalBreak_DbgThread(engine, e, thread, DbgEngineMessageFlags.None);
		}
	}
}
