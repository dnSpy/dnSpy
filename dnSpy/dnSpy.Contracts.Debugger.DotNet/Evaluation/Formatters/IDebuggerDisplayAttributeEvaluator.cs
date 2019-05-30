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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

	// The formatters need to evaluate expressions from DebuggerDisplayAttributes but they can't do it
	// without some help. It's .NET specific and I don't want to add a new public API on ExpressionEvaluator
	// so this 'internal' API is used. The language formatters are exported and don't have access to the IL
	// interpreter used by the .NET language code in dnSpy.Debugger.DotNet. They can get access to this
	// interface by using the extension method below.

	public interface IDebuggerDisplayAttributeEvaluator {
		DbgDotNetEvalResult Evaluate(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object? state);
	}

	public static class IDebuggerDisplayAttributeEvaluatorUtils {
		public static void Initialize(DbgEvaluationContext context, IDebuggerDisplayAttributeEvaluator evaluator) =>
			context.GetOrCreateData(() => evaluator);

		public static IDebuggerDisplayAttributeEvaluator GetDebuggerDisplayAttributeEvaluator(this DbgEvaluationContext context) =>
			context.GetData<IDebuggerDisplayAttributeEvaluator>();
	}

	public readonly struct DbgDotNetEvalResult {
		public DbgDotNetValue? Value { get; }
		public ReadOnlyCollection<string> FormatSpecifiers { get; }
		public DbgEvaluationResultFlags Flags { get; }
		public bool IsThrownException => (Flags & DbgEvaluationResultFlags.ThrownException) != 0;
		public string? Error { get; }
		static readonly ReadOnlyCollection<string> emptyFormatSpecifiers = new ReadOnlyCollection<string>(Array.Empty<string>());
		public DbgDotNetEvalResult(string error, ReadOnlyCollection<string>? formatSpecifiers = null, DbgEvaluationResultFlags flags = 0) {
			Value = null;
			FormatSpecifiers = formatSpecifiers ?? emptyFormatSpecifiers;
			Flags = flags;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
		public DbgDotNetEvalResult(DbgDotNetValue value, ReadOnlyCollection<string>? formatSpecifiers, DbgEvaluationResultFlags flags) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			FormatSpecifiers = formatSpecifiers ?? emptyFormatSpecifiers;
			Flags = flags;
			Error = null;
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
