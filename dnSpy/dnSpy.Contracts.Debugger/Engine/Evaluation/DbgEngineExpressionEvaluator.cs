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

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Expression evaluator
	/// </summary>
	public abstract class DbgEngineExpressionEvaluator {
		/// <summary>
		/// Creates evaluator state used to cache data that is needed to evaluate an expression
		/// </summary>
		/// <returns></returns>
		public abstract object CreateExpressionEvaluatorState();

		/// <summary>
		/// Evaluates an expression
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <param name="state">State created by <see cref="CreateExpressionEvaluatorState"/> or null to store the state in <paramref name="evalInfo"/>'s context</param>
		/// <returns></returns>
		public abstract DbgEngineEvaluationResult Evaluate(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object state);

		/// <summary>
		/// Assigns the value of an expression to another expression
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngineEEAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options);
	}

	/// <summary>
	/// Evaluation result
	/// </summary>
	public readonly struct DbgEngineEvaluationResult {
		/// <summary>
		/// Gets the value or null if there was an error
		/// </summary>
		public DbgEngineValue Value { get; }

		/// <summary>
		/// Gets the format specifiers, if any
		/// </summary>
		public ReadOnlyCollection<string> FormatSpecifiers { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgEvaluationResultFlags Flags { get; }

		/// <summary>
		/// Gets the error or null if none
		/// </summary>
		public string Error { get; }

		static readonly ReadOnlyCollection<string> emptyFormatSpecifiers = new ReadOnlyCollection<string>(Array.Empty<string>());

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="flags">Flags</param>
		public DbgEngineEvaluationResult(DbgEngineValue value, ReadOnlyCollection<string> formatSpecifiers, DbgEvaluationResultFlags flags) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			FormatSpecifiers = formatSpecifiers ?? emptyFormatSpecifiers;
			Flags = flags;
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		/// <param name="flags">Flags</param>
		public DbgEngineEvaluationResult(string error, DbgEvaluationResultFlags flags = 0) {
			Value = null;
			FormatSpecifiers = emptyFormatSpecifiers;
			Flags = flags;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}

	/// <summary>
	/// Expression evaluator assignment result
	/// </summary>
	public readonly struct DbgEngineEEAssignmentResult {
		/// <summary>
		/// Error message or null
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgEEAssignmentResultFlags Flags { get; }

		/// <summary>
		/// true if the error is from the compiler and no debuggee code was executed
		/// </summary>
		public bool IsCompilerError => (Flags & DbgEEAssignmentResultFlags.CompilerError) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Result flags</param>
		/// <param name="error">Error message or null</param>
		public DbgEngineEEAssignmentResult(DbgEEAssignmentResultFlags flags, string error) {
			Flags = flags;
			Error = error;
		}
	}
}
