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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgEvaluationContextImpl : DbgEvaluationContext {
		public override DbgLanguage Language { get; }
		public override DbgRuntime Runtime { get; }
		public override TimeSpan FuncEvalTimeout { get; }
		public override DbgEvaluationContextOptions Options { get; }

		public override DbgEvaluationSession Session {
			get {
				lock (lockObj)
					return session;
			}
		}

		readonly object lockObj;
		DbgEvaluationSession session;

		public DbgEvaluationContextImpl(DbgLanguage language, DbgRuntime runtime, TimeSpan funcEvalTimeout, DbgEvaluationContextOptions options) {
			lockObj = new object();
			Language = language ?? throw new ArgumentNullException(nameof(language));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			FuncEvalTimeout = funcEvalTimeout;
			Options = options;
			session = new DbgEvaluationSessionImpl();
			session.Closed += DbgEvaluationSession_Closed;
			runtime.CloseOnContinue(session);
		}

		void DbgEvaluationSession_Closed(object sender, EventArgs e) {
			session.Closed -= DbgEvaluationSession_Closed;
			if (!IsClosed && !Runtime.IsClosed) {
				lock (lockObj) {
					session = new DbgEvaluationSessionImpl();
					session.Closed += DbgEvaluationSession_Closed;
					Runtime.CloseOnContinue(session);
				}
			}
		}

		protected override void CloseCore() => session.Close(Process.DbgManager.Dispatcher);
	}
}
