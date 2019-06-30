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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineAutosProviderImpl : DbgEngineValueNodeProvider {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;

		public DbgEngineAutosProviderImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory) =>
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(evalInfo, options);
			return GetNodes2(dispatcher, evalInfo, options);

			DbgEngineValueNode[] GetNodes2(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgValueNodeEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetNodesCore(evalInfo2, options2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			//TODO: Show all autos...
			var res = new DbgEngineValueNode[1];
			try {
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					var name = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Error, "Error"));
					res[i] = valueNodeFactory.CreateError(evalInfo, name, "NYI", "NYI", false);
				}
			}
			catch (Exception ex) {
				evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => !(a is null)));
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				return valueNodeFactory.CreateInternalErrorResult(evalInfo);
			}
			return res;
		}
	}
}
