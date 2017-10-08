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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineReturnValuesProviderImpl : DbgEngineValueNodeProvider {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;

		public DbgEngineReturnValuesProviderImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory) =>
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(context, frame, options, cancellationToken);
			return dispatcher.Invoke(() => GetNodesCore(context, frame, options, cancellationToken));
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			var returnValues = runtime.GetReturnValues(context, frame, cancellationToken);
			if (returnValues.Length == 0)
				return Array.Empty<DbgEngineValueNode>();

			var res = new DbgEngineValueNode[returnValues.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					var info = returnValues[i];
					res[i] = valueNodeFactory.CreateReturnValue(context, frame, info.Id, info.Value, options, info.Method, cancellationToken);
				}
			}
			catch {
				context.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}
	}
}
