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
		/// Creates evaluator state used to cache data that is needed to evaluate an expression
		/// </summary>
		/// <returns></returns>
		public abstract object CreateExpressionEvaluatorState();

		/// <summary>
		/// Evaluates an expression. The returned <see cref="DbgValue"/> is automatically closed when its runtime continues.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="options">Options</param>
		/// <param name="state">State created by <see cref="CreateExpressionEvaluatorState"/> or null to store the state in <paramref name="evalInfo"/>'s context</param>
		/// <returns></returns>
		public abstract DbgEvaluationResult Evaluate(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object state);

		/// <summary>
		/// Assigns the value of an expression to another expression
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Target expression (lhs)</param>
		/// <param name="valueExpression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEEAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options);
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
		/// Fail if the expression causes side effects, eg. method calls
		/// </summary>
		NoSideEffects				= 0x00000002,

		/// <summary>
		/// Don't allow function evaluations (calling code in debugged process)
		/// </summary>
		NoFuncEval					= 0x00000004,

		/// <summary>
		/// Don't create a name/expression (only used by value nodes)
		/// </summary>
		NoName						= 0x00000008,

		/// <summary>
		/// Use only the locals that exist in the metadata. Don't show captured variables, show their display class variables instead
		/// </summary>
		RawLocals					= 0x00000010,
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
		/// The expression causes side effects
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

		/// <summary>
		/// The value is a thrown exception
		/// </summary>
		ThrownException				= 0x00000008,
	}

	/// <summary>
	/// Evaluation result
	/// </summary>
	public readonly struct DbgEvaluationResult {
		/// <summary>
		/// Gets the value or null if there was an error
		/// </summary>
		public DbgValue Value { get; }

		/// <summary>
		/// Gets the format specifiers
		/// </summary>
		public ReadOnlyCollection<string> FormatSpecifiers { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgEvaluationResultFlags Flags { get; }

		/// <summary>
		/// true if <see cref="Value"/> is a thrown exception
		/// </summary>
		public bool IsThrownException => (Flags & DbgEvaluationResultFlags.ThrownException) != 0;

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
		public DbgEvaluationResult(DbgValue value, ReadOnlyCollection<string> formatSpecifiers, DbgEvaluationResultFlags flags) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			FormatSpecifiers = formatSpecifiers ?? emptyFormatSpecifiers;
			Flags = flags;
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="flags">Flags</param>
		public DbgEvaluationResult(string error, ReadOnlyCollection<string> formatSpecifiers = null, DbgEvaluationResultFlags flags = 0) {
			Value = null;
			FormatSpecifiers = formatSpecifiers ?? emptyFormatSpecifiers;
			Flags = flags;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}

	/// <summary>
	/// Assignment result flags
	/// </summary>
	[Flags]
	public enum DbgEEAssignmentResultFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// The error is from the compiler and no debuggee code was executed
		/// </summary>
		CompilerError		= 0x00000001,

		/// <summary>
		/// Code in the debuggee was executed
		/// </summary>
		ExecutedCode		= 0x00000002,
	}

	/// <summary>
	/// Expression evaluator assignment result
	/// </summary>
	public readonly struct DbgEEAssignmentResult {
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
		public DbgEEAssignmentResult(DbgEEAssignmentResultFlags flags, string error) {
			Flags = flags;
			Error = error;
		}
	}
}
