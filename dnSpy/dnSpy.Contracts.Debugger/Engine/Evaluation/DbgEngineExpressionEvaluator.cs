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

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Expression evaluator
	/// </summary>
	public abstract class DbgEngineExpressionEvaluator {
		/// <summary>
		/// Evaluates an expression. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Evaluates an expression
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineEvaluationResult> callback, CancellationToken cancellationToken);

		/// <summary>
		/// Assigns the value of an expression to another expression. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineEEAssignmentResult Assign(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Assigns the value of an expression to another expression
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Assign(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEngineEEAssignmentResult> callback, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Evaluation result
	/// </summary>
	public struct DbgEngineEvaluationResult {
		/// <summary>
		/// Gets the thread or null if there was an error
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Gets the value or null if there was an error
		/// </summary>
		public DbgEngineValue Value { get; }

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
		/// <param name="thread">Thread</param>
		/// <param name="value">Value</param>
		/// <param name="flags">Flags</param>
		public DbgEngineEvaluationResult(DbgThread thread, DbgEngineValue value, DbgEvaluationResultFlags flags) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Flags = flags;
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		/// <param name="flags">Flags</param>
		public DbgEngineEvaluationResult(string error, DbgEvaluationResultFlags flags = 0) {
			Thread = null;
			Value = null;
			Flags = flags;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}

	/// <summary>
	/// Expression evaluator assignment result
	/// </summary>
	public struct DbgEngineEEAssignmentResult {
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
