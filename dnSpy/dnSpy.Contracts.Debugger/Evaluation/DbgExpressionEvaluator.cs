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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Expression evaluator
	/// </summary>
	public abstract class DbgExpressionEvaluator {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Evaluates an expression. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEvaluationResult Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options);

		/// <summary>
		/// Evaluates an expression
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		public abstract void Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEvaluationResult> callback);

		/// <summary>
		/// Assigns the value of an expression to another expression. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEEAssignmentResult Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options);

		/// <summary>
		/// Assigns the value of an expression to another expression
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		public abstract void Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEEAssignmentResult> callback);
	}

	/// <summary>
	/// Evaluation options
	/// </summary>
	[Flags]
	public enum DbgEvaluationOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Set if it's an expression, false if it's a statement
		/// </summary>
		Expression					= 0x00000001,

		/// <summary>
		/// Fail if the expression has side effects, eg. method calls
		/// </summary>
		NoSideEffects				= 0x00000002,

		/// <summary>
		/// Don't allow function evaluations (calling code in debugged process)
		/// </summary>
		NoFuncEval					= 0x00000004,
	}

	/// <summary>
	/// Evaluation result flags
	/// </summary>
	[Flags]
	public enum DbgEvaluationResultFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// The expression has side effects
		/// </summary>
		SideEffects					= 0x00000001,

		/// <summary>
		/// The result is read-only
		/// </summary>
		ReadOnly					= 0x00000002,

		/// <summary>
		/// It's a boolean expression
		/// </summary>
		BooleanExpression			= 0x00000004,
	}

	/// <summary>
	/// Evaluation result
	/// </summary>
	public struct DbgEvaluationResult {
		/// <summary>
		/// Gets the value or null if there was an error
		/// </summary>
		public DbgValue Value { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgEvaluationResultFlags Flags { get; }

		/// <summary>
		/// Gets the error or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="flags">Flags</param>
		public DbgEvaluationResult(DbgValue value, DbgEvaluationResultFlags flags) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Flags = flags;
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		public DbgEvaluationResult(string error) {
			Value = null;
			Flags = 0;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}

	/// <summary>
	/// Expression evaluator assignment result
	/// </summary>
	public struct DbgEEAssignmentResult {
		/// <summary>
		/// Error message or null
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message or null</param>
		public DbgEEAssignmentResult(string error) => Error = error;
	}
}
