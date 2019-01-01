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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Steppers {
	sealed class DbgDotNetStepperBreakpointImpl : DbgDotNetStepperBreakpoint {
		public override event EventHandler<DbgDotNetStepperBreakpointEventArgs> Hit;

		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly BreakpointEventRequest breakpoint;

		public DbgDotNetStepperBreakpointImpl(DbgEngineImpl engine, DbgThread thread, DbgModule module, uint token, uint offset) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread;
			engine.VerifyMonoDebugThread();
			breakpoint = engine.CreateBreakpointForStepper(module, token, offset, OnBreakpointHit);
		}

		bool OnBreakpointHit(DbgThread thread) {
			if (this.thread == null || thread == this.thread) {
				var e = new DbgDotNetStepperBreakpointEventArgs(thread);
				Hit?.Invoke(this, e);
				return e.Pause;
			}
			else
				return false;
		}

		internal void Dispose() {
			engine.VerifyMonoDebugThread();
			engine.RemoveBreakpointForStepper(breakpoint);
		}
	}
}
