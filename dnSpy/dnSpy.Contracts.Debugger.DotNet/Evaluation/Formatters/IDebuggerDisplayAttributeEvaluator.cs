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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

	// The formatters need to evaluate expressions from DebuggerDisplayAttributes but they can't do it
	// without some help. It's .NET specific and I don't want to add a new public API on ExpressionEvaluator
	// so this 'internal' API is used. The language formatters are exported and don't have access to the IL
	// interpreter used by the .NET language code in dnSpy.Debugger.DotNet. They can get access to this
	// interface by using the extension method below.

	public interface IDebuggerDisplayAttributeEvaluator {
		DbgDotNetEvalResult Evaluate(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object state, CancellationToken cancellationToken);
	}

	public static class IDebuggerDisplayAttributeEvaluatorUtils {
		public static void Initialize(DbgEvaluationContext context, IDebuggerDisplayAttributeEvaluator evaluator) =>
			context.GetOrCreateData(() => evaluator);

		public static IDebuggerDisplayAttributeEvaluator GetDebuggerDisplayAttributeEvaluator(this DbgEvaluationContext context) =>
			context.GetData<IDebuggerDisplayAttributeEvaluator>();
	}

	public struct DbgDotNetEvalResult {
		public DbgDotNetValue Value { get; }
		public string Error { get; }
		public DbgDotNetEvalResult(string error) {
			Value = null;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
		public DbgDotNetEvalResult(DbgDotNetValue value) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Error = null;
		}
	}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
