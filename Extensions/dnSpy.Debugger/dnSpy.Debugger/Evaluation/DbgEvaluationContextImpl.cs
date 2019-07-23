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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgEvaluationContextImpl : DbgEvaluationContext {
		public override DbgLanguage Language { get; }
		public override DbgRuntime Runtime { get; }
		public override TimeSpan FuncEvalTimeout { get; }
		public override DbgEvaluationContextOptions Options { get; }

		public override DbgObject ContinueContext {
			get {
				lock (lockObj)
					return continueContext;
			}
		}

		sealed class DbgContinueContext : DbgObject {
			protected override void CloseCore(DbgDispatcher dispatcher) { }
		}

		readonly object lockObj;
		DbgObject continueContext;

		public DbgEvaluationContextImpl(DbgLanguage language, DbgRuntime runtime, TimeSpan funcEvalTimeout, DbgEvaluationContextOptions options) {
			lockObj = new object();
			Language = language ?? throw new ArgumentNullException(nameof(language));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			FuncEvalTimeout = funcEvalTimeout;
			Options = options;
			continueContext = new DbgContinueContext();
			continueContext.Closed += DbgContinueContext_Closed;
			runtime.CloseOnContinue(continueContext);
		}

		void DbgContinueContext_Closed(object? sender, EventArgs e) {
			continueContext.Closed -= DbgContinueContext_Closed;
			if (!IsClosed && !Runtime.IsClosed) {
				lock (lockObj) {
					continueContext = new DbgContinueContext();
					continueContext.Closed += DbgContinueContext_Closed;
					Runtime.CloseOnContinue(continueContext);
				}
			}
		}

		protected override void CloseCore(DbgDispatcher dispatcher) => continueContext.Close(dispatcher);
	}
}
