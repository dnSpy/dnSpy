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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineObjectIdImpl : DbgEngineObjectId {
		public override uint Id => dnObjectId.Id;

		internal DbgDotNetObjectId DotNetObjectId => dnObjectId;
		internal IDbgDotNetRuntime Runtime => runtime;

		readonly IDbgDotNetRuntime runtime;
		readonly DbgDotNetObjectId dnObjectId;

		public DbgEngineObjectIdImpl(IDbgDotNetRuntime runtime, DbgDotNetObjectId dnObjectId) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.dnObjectId = dnObjectId ?? throw new ArgumentNullException(nameof(dnObjectId));
		}

		public override DbgEngineValue GetValue(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) =>
			runtime.Dispatcher.Invoke(() => GetValueCore(context, frame, cancellationToken));

		public override void GetValue(DbgEvaluationContext context, DbgStackFrame frame, Action<DbgEngineValue> callback, CancellationToken cancellationToken) =>
			runtime.Dispatcher.BeginInvoke(() => callback(GetValueCore(context, frame, cancellationToken)));

		DbgEngineValue GetValueCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			var dnValue = runtime.GetValue(context, frame, dnObjectId, cancellationToken);
			try {
				return new DbgEngineValueImpl(dnValue);
			}
			catch {
				dnValue.Dispose();
				throw;
			}
		}

		protected override void CloseCore(DbgDispatcher dispatcher) => dnObjectId.Dispose();
	}
}
