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

		public override DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => CreateCore(context, frame, expressions, cancellationToken));

		DbgEngineValueNode[] CreateCore(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, CancellationToken cancellationToken) {
			var res = expressions.Length == 0 ? Array.Empty<DbgEngineValueNode>() : new DbgEngineValueNode[expressions.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					ref var info = ref expressions[i];
					var evalRes = expressionEvaluator.EvaluateImpl(context, frame, info.Expression, info.Options, cancellationToken);
					bool causesSideEffects = (evalRes.Flags & DbgEvaluationResultFlags.SideEffects) != 0;
					DbgEngineValueNode newNode;
					if (evalRes.Error != null)
						newNode = valueNodeFactory.CreateError(context, frame, evalRes.Name, evalRes.Error, info.Expression, causesSideEffects, cancellationToken);
					else {
						bool isReadOnly = (evalRes.Flags & DbgEvaluationResultFlags.ReadOnly) != 0;
						newNode = valueNodeFactory.Create(context, frame, evalRes.Name, evalRes.Value, DbgEvaluationOptionsUtils.ToValueNodeEvaluationOptions(info.Options), info.Expression, evalRes.ImageName, isReadOnly, causesSideEffects, evalRes.Type, cancellationToken);
					}
					res[i] = newNode;
				}
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}

		public override DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => CreateCore(context, frame, objectIds, options, cancellationToken));

		DbgEngineValueNode[] CreateCore(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			DbgDotNetValue objectIdValue = null;
			var res = new DbgEngineValueNode[objectIds.Length];
			try {
				var output = ObjectCache.AllocDotNetTextOutput();
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					var objectId = (DbgEngineObjectIdImpl)objectIds[i];
					var dnObjectId = objectId.DotNetObjectId;
					objectIdValue = objectId.Runtime.GetValue(context, frame, dnObjectId, cancellationToken);

					formatter.FormatObjectIdName(context, output, dnObjectId.Id);
					var name = output.CreateAndReset();
					var expression = name.ToString();

					res[i] = valueNodeFactory.Create(context, frame, name, objectIdValue, options, expression, PredefinedDbgValueNodeImageNames.ObjectId, true, false, objectIdValue.Type, cancellationToken);
				}
				ObjectCache.Free(ref output);
				return res;
			}
			catch {
				context.Process.DbgManager.Close(res.Where(a => a != null));
				objectIdValue?.Dispose();
				throw;
			}
		}
	}
}
