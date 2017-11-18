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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DmdEvaluatorImpl : DmdEvaluator {
		readonly DbgEngineImpl engine;

		public DmdEvaluatorImpl(DbgEngineImpl engine) =>
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

		IDmdEvaluatorContext GetEvaluatorContext(object context) {
			const string errorMessage = nameof(context) + " must not be null and must implement " + nameof(IDmdEvaluatorContext) + ", see class " + nameof(DmdEvaluatorContext);
			if (context == null)
				throw new ArgumentNullException(nameof(context), errorMessage);
			var evalCtx = context as IDmdEvaluatorContext;
			if (evalCtx == null)
				throw new ArgumentException(errorMessage, nameof(context));
			return evalCtx;
		}

		DbgDotNetValue GetDotNetValue(object obj) {
			if (obj == null)
				return null;
			var dnValue = obj as DbgDotNetValue;
			if (dnValue == null)
				throw new ArgumentException("Value must be a " + nameof(DbgDotNetValue));
			return dnValue;
		}

		object GetValueThrow(DbgDotNetValueResult result) {
			if (result.ErrorMessage != null)
				throw new DmdEvaluatorException(result.ErrorMessage);
			if (result.ValueIsException) {
				var msg = "An exception was thrown: " + result.Value.Type.FullName;
				result.Value.Dispose();
				throw new DmdEvaluatorException(msg);
			}
			return result.Value;
		}

		public override object CreateInstance(object context, DmdConstructorInfo ctor, object[] arguments) {
			var evalCtx = GetEvaluatorContext(context);
			var res = engine.DotNetRuntime.CreateInstance(evalCtx.EvaluationContext, evalCtx.Frame, ctor, arguments, DbgDotNetInvokeOptions.None, evalCtx.CancellationToken);
			return GetValueThrow(res);
		}

		public override object Invoke(object context, DmdMethodBase method, object obj, object[] arguments) {
			var evalCtx = GetEvaluatorContext(context);
			var res = engine.DotNetRuntime.Call(evalCtx.EvaluationContext, evalCtx.Frame, GetDotNetValue(obj), method, arguments, DbgDotNetInvokeOptions.None, evalCtx.CancellationToken);
			return GetValueThrow(res);
		}

		public override object LoadField(object context, DmdFieldInfo field, object obj) {
			var evalCtx = GetEvaluatorContext(context);
			var res = engine.DotNetRuntime.LoadField(evalCtx.EvaluationContext, evalCtx.Frame, GetDotNetValue(obj), field, evalCtx.CancellationToken);
			return GetValueThrow(res);
		}

		public override void StoreField(object context, DmdFieldInfo field, object obj, object value) {
			var evalCtx = GetEvaluatorContext(context);
			var errorMessage = engine.DotNetRuntime.StoreField(evalCtx.EvaluationContext, evalCtx.Frame, GetDotNetValue(obj), field, value, evalCtx.CancellationToken);
			if (errorMessage != null)
				throw new DmdEvaluatorException(errorMessage);
		}
	}

	sealed class DmdEvaluatorException : Exception {
		public DmdEvaluatorException(string message) : base(message) { }
	}
}
