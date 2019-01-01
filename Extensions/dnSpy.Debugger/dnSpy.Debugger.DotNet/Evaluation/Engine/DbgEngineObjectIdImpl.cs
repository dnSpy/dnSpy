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

		public override DbgEngineValue GetValue(DbgEvaluationInfo evalInfo) {
			var dispatcher = runtime.Dispatcher;
			if (dispatcher.CheckAccess())
				return GetValueCore(evalInfo);
			return GetValue(dispatcher, evalInfo);

			DbgEngineValue GetValue(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetValueCore(evalInfo2), out var result))
					result = CreateError();
				return result;
			}
		}

		static DbgEngineValue CreateError() => new DbgEngineValueImpl(new DbgDotNetValueError());

		DbgEngineValue GetValueCore(DbgEvaluationInfo evalInfo) {
			var dnValue = runtime.GetValue(evalInfo, dnObjectId);
			if (dnValue == null)
				return CreateError();
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
