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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeFactoryImpl : DbgValueNodeFactory {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineValueNodeFactory engineValueNodeFactory;

		public DbgValueNodeFactoryImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineValueNodeFactory engineValueNodeFactory) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineValueNodeFactory = engineValueNodeFactory ?? throw new ArgumentNullException(nameof(engineValueNodeFactory));
		}

		DbgCreateValueNodeResult CreateResult(DbgRuntime runtime, DbgCreateEngineValueNodeResult result) {
			if (result.EngineValueNode != null) {
				var valueNode = new DbgValueNodeImpl(Language, runtime, result.EngineValueNode);
				runtime.CloseOnContinue(valueNode);
				return new DbgCreateValueNodeResult(valueNode);
			}
			return new DbgCreateValueNodeResult(ConvertError(result.Error), result.Error == PredefinedDbgCreateEngineValueNodeResultErrors.ExpressionCausesSideEffects);
		}

		DbgCreateObjectIdValueNodeResult[] CreateResult(DbgRuntime runtime, DbgCreateEngineObjectIdValueNodeResult[] result, int expectedLength) {
			if (result.Length != expectedLength)
				throw new InvalidOperationException();
			var res = new DbgCreateObjectIdValueNodeResult[result.Length];
			for (int i = 0; i < res.Length; i++) {
				var info = result[i];
				if (info.EngineValueNode != null) {
					var valueNode = new DbgValueNodeImpl(Language, runtime, info.EngineValueNode);
					runtime.CloseOnContinue(valueNode);
					res[i] = new DbgCreateObjectIdValueNodeResult(valueNode);
				}
				else
					res[i] = new DbgCreateObjectIdValueNodeResult(info.Expression, ConvertError(info.Error));
			}
			return res;
		}

		static string ConvertError(string error) {
			switch (error) {
			case PredefinedDbgCreateEngineValueNodeResultErrors.ExpressionCausesSideEffects:
				return dnSpy_Debugger_Resources.ExpressionCausesSideEffectsNoEval;
			}
			return error;
		}

		public override DbgCreateValueNodeResult Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return CreateResult(frame.Runtime, engineValueNodeFactory.Create(context, frame, expression, options, cancellationToken));
		}

		public override void Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgCreateValueNodeResult> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueNodeFactory.Create(context, frame, expression, options, result => callback(CreateResult(frame.Runtime, result)), cancellationToken);
		}

		public override DbgCreateObjectIdValueNodeResult[] Create(DbgEvaluationContext context, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (objectIds == null)
				throw new ArgumentNullException(nameof(objectIds));
			if (objectIds.Length == 0)
				return Array.Empty<DbgCreateObjectIdValueNodeResult>();
			var runtime = objectIds[0].Runtime;
			if (runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			var engineObjectIds = new DbgEngineObjectId[objectIds.Length];
			for (int i = 0; i < objectIds.Length; i++)
				engineObjectIds[i] = ((DbgObjectIdImpl)objectIds[i]).EngineObjectId;
			return CreateResult(runtime, engineValueNodeFactory.Create(context, engineObjectIds, options, cancellationToken), engineObjectIds.Length);
		}

		public override void Create(DbgEvaluationContext context, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgCreateObjectIdValueNodeResult[]> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (objectIds == null)
				throw new ArgumentNullException(nameof(objectIds));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			var runtime = objectIds[0].Runtime;
			if (runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			var engineObjectIds = new DbgEngineObjectId[objectIds.Length];
			for (int i = 0; i < objectIds.Length; i++)
				engineObjectIds[i] = ((DbgObjectIdImpl)objectIds[i]).EngineObjectId;
			engineValueNodeFactory.Create(context, engineObjectIds, options, result => CreateResult(runtime, result, engineObjectIds.Length), cancellationToken);
		}
	}
}
