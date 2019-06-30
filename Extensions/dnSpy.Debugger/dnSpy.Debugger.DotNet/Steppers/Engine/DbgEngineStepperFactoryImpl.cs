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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Code;

namespace dnSpy.Debugger.DotNet.Steppers.Engine {
	[Export(typeof(DbgEngineStepperFactory))]
	sealed class DbgEngineStepperFactoryImpl : DbgEngineStepperFactory {
		readonly DbgLanguageService dbgLanguageService;
		readonly DbgDotNetDebugInfoService dbgDotNetDebugInfoService;
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		DbgEngineStepperFactoryImpl(DbgLanguageService dbgLanguageService, DbgDotNetDebugInfoService dbgDotNetDebugInfoService, DebuggerSettings debuggerSettings) {
			this.dbgLanguageService = dbgLanguageService;
			this.dbgDotNetDebugInfoService = dbgDotNetDebugInfoService;
			this.debuggerSettings = debuggerSettings;
		}

		public override DbgEngineStepper Create(IDbgDotNetRuntime runtime, DbgDotNetEngineStepper stepper, DbgThread thread) {
			if (runtime is null)
				throw new ArgumentNullException(nameof(runtime));
			if (stepper is null)
				throw new ArgumentNullException(nameof(stepper));
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));
			return new DbgEngineStepperImpl(dbgLanguageService, dbgDotNetDebugInfoService, debuggerSettings, runtime, stepper, thread);
		}
	}
}
