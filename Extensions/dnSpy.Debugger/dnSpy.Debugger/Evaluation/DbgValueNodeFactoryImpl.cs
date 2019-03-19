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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeFactoryImpl : DbgValueNodeFactory {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineValueNodeFactory engineValueNodeFactory;

		public DbgValueNodeFactoryImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineValueNodeFactory engineValueNodeFactory) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineValueNodeFactory = engineValueNodeFactory ?? throw new ArgumentNullException(nameof(engineValueNodeFactory));
		}

		DbgCreateValueNodeResult[] CreateResult(DbgRuntime runtime, DbgEngineValueNode[] results) {
			if (results.Length == 0)
				return Array.Empty<DbgCreateValueNodeResult>();
			var res = new DbgCreateValueNodeResult[results.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					var result = results[i];
					var valueNode = new DbgValueNodeImpl(Language, runtime, result);
					runtime.CloseOnContinue(valueNode);
					res[i] = new DbgCreateValueNodeResult(valueNode, result.CausesSideEffects);
				}
			}
			catch {
				runtime.Process.DbgManager.Close(res.Select(a => a.ValueNode).Where(a => a != null));
				throw;
			}
			return res;
		}

		DbgValueNode[] CreateResult(DbgRuntime runtime, DbgEngineValueNode[] result, int expectedLength) {
			if (result.Length != expectedLength)
				throw new InvalidOperationException();
			return DbgValueNodeUtils.ToValueNodeArray(Language, runtime, result);
		}

		public override DbgCreateValueNodeResult[] Create(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (expressions == null)
				throw new ArgumentNullException(nameof(expressions));
			return CreateResult(evalInfo.Frame.Runtime, engineValueNodeFactory.Create(evalInfo, expressions));
		}

		public override DbgValueNode[] Create(DbgEvaluationInfo evalInfo, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (objectIds == null)
				throw new ArgumentNullException(nameof(objectIds));
			if (objectIds.Length == 0)
				return Array.Empty<DbgValueNode>();
			var runtime = objectIds[0].Runtime;
			if (runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			var engineObjectIds = new DbgEngineObjectId[objectIds.Length];
			for (int i = 0; i < objectIds.Length; i++)
				engineObjectIds[i] = ((DbgObjectIdImpl)objectIds[i]).EngineObjectId;
			return CreateResult(runtime, engineValueNodeFactory.Create(evalInfo, engineObjectIds, options), engineObjectIds.Length);
		}
	}
}
