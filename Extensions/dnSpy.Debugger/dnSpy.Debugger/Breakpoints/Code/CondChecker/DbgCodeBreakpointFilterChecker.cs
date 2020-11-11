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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.FilterExpressionEvaluator;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	abstract class DbgCodeBreakpointFilterChecker {
		public abstract DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointFilter filter);
	}

	[Export(typeof(DbgCodeBreakpointFilterChecker))]
	sealed class DbgCodeBreakpointFilterCheckerImpl : DbgCodeBreakpointFilterChecker {
		readonly DbgFilterExpressionEvaluatorService dbgFilterExpressionEvaluatorService;
		readonly DbgFilterEEVariableProviderImpl dbgFilterEEVariableProvider;

		[ImportingConstructor]
		DbgCodeBreakpointFilterCheckerImpl(DbgFilterExpressionEvaluatorService dbgFilterExpressionEvaluatorService) {
			this.dbgFilterExpressionEvaluatorService = dbgFilterExpressionEvaluatorService;
			dbgFilterEEVariableProvider = new DbgFilterEEVariableProviderImpl();
		}

		sealed class DbgFilterEEVariableProviderImpl : DbgFilterEEVariableProvider {
			public override string MachineName => Environment.MachineName;
			public override int ProcessId => process!.Id;
			public override string ProcessName => process!.Filename;
			public override ulong ThreadId => thread?.Id ?? ulong.MaxValue;
			public override string? ThreadName => thread?.UIName;

			DbgProcess? process;
			DbgThread? thread;

			public void Initialize(DbgProcess process, DbgThread thread) {
				this.process = process ?? throw new ArgumentNullException(nameof(process));
				this.thread = thread;
			}

			public void Clear() {
				process = null;
				thread = null;
			}
		}

		public override DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointFilter filter) {
			if (!dbgFilterExpressionEvaluatorService.HasExpressionEvaluator) {
				// There's no need to localize it, the FEE is exported by the Roslyn code
				return new DbgCodeBreakpointCheckResult("There's no filter expression evaluator");
			}

			var expr = filter.Filter;
			Debug2.Assert(expr is not null);
			if (expr is null)
				return new DbgCodeBreakpointCheckResult("Missing expression");

			try {
				dbgFilterEEVariableProvider.Initialize(boundBreakpoint.Process, thread);
				var res = dbgFilterExpressionEvaluatorService.Evaluate(expr, dbgFilterEEVariableProvider);
				if (res.HasError)
					return new DbgCodeBreakpointCheckResult(res.Error!);
				return new DbgCodeBreakpointCheckResult(res.Result);
			}
			finally {
				dbgFilterEEVariableProvider.Clear();
			}
		}
	}
}
