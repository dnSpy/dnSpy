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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	[Export(typeof(DebuggerRuntimeFactory))]
	sealed class DebuggerRuntimeFactoryImpl : DebuggerRuntimeFactory {
		readonly DbgObjectIdService dbgObjectIdService;
		readonly DotNetClassHookFactory[] dotNetClassHookFactories;

		[ImportingConstructor]
		DebuggerRuntimeFactoryImpl(DbgObjectIdService dbgObjectIdService, [ImportMany] IEnumerable<DotNetClassHookFactory> dotNetClassHookFactories) {
			this.dbgObjectIdService = dbgObjectIdService;
			this.dotNetClassHookFactories = dotNetClassHookFactories.ToArray();
		}

		sealed class State {
			public DebuggerRuntimeImpl DebuggerRuntime;
		}

		public override DebuggerRuntime2 Create(DbgRuntime runtime) {
			var state = StateWithKey<State>.GetOrCreate(runtime, this);
			if (state.DebuggerRuntime == null)
				state.DebuggerRuntime = new DebuggerRuntimeImpl(dbgObjectIdService, runtime.GetDotNetRuntime(), runtime.Process.PointerSize, dotNetClassHookFactories);
			return state.DebuggerRuntime;
		}
	}
}
