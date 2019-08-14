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
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DmdEvaluatorImpl : DmdEvaluator {
		readonly DbgEngineImpl engine;

		public DmdEvaluatorImpl(DbgEngineImpl engine) =>
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

		DbgEvaluationInfo GetEvaluationInfo(object? context) {
			const string errorMessage = nameof(context) + " must not be null and must be a " + nameof(DbgEvaluationInfo);
			if (context is null)
				throw new ArgumentNullException(nameof(context), errorMessage);
			var evalInfo = context as DbgEvaluationInfo;
			if (evalInfo is null)
				throw new ArgumentException(errorMessage, nameof(context));
			return evalInfo;
		}

		DbgDotNetValue? GetDotNetValue(object? obj) {
			if (obj is null)
				return null;
			var dnValue = obj as DbgDotNetValue;
			if (dnValue is null)
				throw new ArgumentException("Value must be a " + nameof(DbgDotNetValue));
			return dnValue;
		}

		object GetValueThrow(DbgDotNetValueResult result) {
			if (!(result.ErrorMessage is null))
				throw new DmdEvaluatorException(result.ErrorMessage);
			Debug2.Assert(!(result.Value is null));
			if (result.ValueIsException) {
				var msg = "An exception was thrown: " + result.Value.Type.FullName;
				result.Value.Dispose();
				throw new DmdEvaluatorException(msg);
			}
			return result.Value;
		}

		public override object? CreateInstance(object? context, DmdConstructorInfo ctor, object?[] arguments) {
			var evalInfo = GetEvaluationInfo(context);
			var res = engine.DotNetRuntime.CreateInstance(evalInfo, ctor, arguments, DbgDotNetInvokeOptions.None);
			return GetValueThrow(res);
		}

		public override object? Invoke(object? context, DmdMethodBase method, object? obj, object?[] arguments) {
			var evalInfo = GetEvaluationInfo(context);
			var res = engine.DotNetRuntime.Call(evalInfo, GetDotNetValue(obj), method, arguments, DbgDotNetInvokeOptions.None);
			return GetValueThrow(res);
		}

		public override object? LoadField(object? context, DmdFieldInfo field, object? obj) {
			var evalInfo = GetEvaluationInfo(context);
			var res = engine.DotNetRuntime.LoadField(evalInfo, GetDotNetValue(obj), field);
			return GetValueThrow(res);
		}

		public override void StoreField(object? context, DmdFieldInfo field, object? obj, object? value) {
			var evalInfo = GetEvaluationInfo(context);
			var errorMessage = engine.DotNetRuntime.StoreField(evalInfo, GetDotNetValue(obj), field, value);
			if (!(errorMessage is null))
				throw new DmdEvaluatorException(errorMessage);
		}
	}

	sealed class DmdEvaluatorException : Exception {
		public DmdEvaluatorException(string message) : base(message) { }
	}
}
