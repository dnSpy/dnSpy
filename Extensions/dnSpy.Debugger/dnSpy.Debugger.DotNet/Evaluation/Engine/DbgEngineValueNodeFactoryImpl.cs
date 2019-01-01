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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeFactoryImpl : DbgEngineValueNodeFactory {
		readonly DbgEngineExpressionEvaluatorImpl expressionEvaluator;
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetFormatter formatter;

		public DbgEngineValueNodeFactoryImpl(DbgEngineExpressionEvaluatorImpl expressionEvaluator, DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetFormatter formatter) {
			this.expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		public override DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return CreateCore(evalInfo, expressions);
			return Create(dispatcher, evalInfo, expressions);

			DbgEngineValueNode[] Create(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgExpressionEvaluationInfo[] expressions2) {
				if (!dispatcher2.TryInvokeRethrow(() => CreateCore(evalInfo2, expressions2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] CreateCore(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions) {
			var res = expressions.Length == 0 ? Array.Empty<DbgEngineValueNode>() : new DbgEngineValueNode[expressions.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					ref readonly var info = ref expressions[i];
					var evalRes = expressionEvaluator.EvaluateImpl(evalInfo, info.Expression, info.Options, info.ExpressionEvaluatorState);
					bool causesSideEffects = (evalRes.Flags & DbgEvaluationResultFlags.SideEffects) != 0;
					DbgEngineValueNode newNode;
					if (evalRes.Error != null)
						newNode = valueNodeFactory.CreateError(evalInfo, evalRes.Name, evalRes.Error, info.Expression, causesSideEffects);
					else {
						bool isReadOnly = (evalRes.Flags & DbgEvaluationResultFlags.ReadOnly) != 0;
						newNode = valueNodeFactory.Create(evalInfo, evalRes.Name, evalRes.Value, evalRes.FormatSpecifiers, info.NodeOptions, info.Expression, evalRes.ImageName, isReadOnly, causesSideEffects, evalRes.Type);
					}
					res[i] = newNode;
				}
			}
			catch (Exception ex) {
				evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				return valueNodeFactory.CreateInternalErrorResult(evalInfo);
			}
			return res;
		}

		public override DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return CreateCore(evalInfo, objectIds, options);
			return Create(dispatcher, evalInfo, objectIds, options);

			DbgEngineValueNode[] Create(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgEngineObjectId[] objectIds2, DbgValueNodeEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => CreateCore(evalInfo2, objectIds2, options2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] CreateCore(DbgEvaluationInfo evalInfo, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options) {
			DbgDotNetValue objectIdValue = null;
			var res = new DbgEngineValueNode[objectIds.Length];
			try {
				var output = ObjectCache.AllocDotNetTextOutput();
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					var objectId = (DbgEngineObjectIdImpl)objectIds[i];
					var dnObjectId = objectId.DotNetObjectId;
					objectIdValue = objectId.Runtime.GetValue(evalInfo, dnObjectId);

					formatter.FormatObjectIdName(evalInfo.Context, output, dnObjectId.Id);
					var name = output.CreateAndReset();
					var expression = name.ToString();

					if (objectIdValue == null)
						res[i] = valueNodeFactory.CreateError(evalInfo, name, "Could not get Object ID value", expression, false);
					else
						res[i] = valueNodeFactory.Create(evalInfo, name, objectIdValue, null, options, expression, PredefinedDbgValueNodeImageNames.ObjectId, true, false, objectIdValue.Type);
				}
				ObjectCache.Free(ref output);
				return res;
			}
			catch (Exception ex) {
				evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				objectIdValue?.Dispose();
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				return valueNodeFactory.CreateInternalErrorResult(evalInfo);
			}
		}
	}
}
