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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code.FilterExpressionEvaluator {
	/// <summary>
	/// Evaluates breakpoint filter expressions. Use <see cref="ExportDbgFilterExpressionEvaluatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgFilterExpressionEvaluator {
		/// <summary>
		/// Checks if <paramref name="expr"/> is a valid expression. Returns null if it's a valid expression,
		/// otherwise an error string is returned.
		/// </summary>
		/// <param name="expr">Filter expression</param>
		/// <returns></returns>
		public abstract string? IsValidExpression(string expr);

		/// <summary>
		/// Evaluates <paramref name="expr"/> and returns the result of the expression.
		/// </summary>
		/// <param name="expr">Filter expression</param>
		/// <param name="variableProvider">Provides the values of the variables that can be used by the expression</param>
		/// <returns></returns>
		public abstract DbgFilterExpressionEvaluatorResult Evaluate(string expr, DbgFilterEEVariableProvider variableProvider);

		/// <summary>
		/// Parses <paramref name="expr"/> and writes text and color to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="expr">Expression</param>
		public abstract void Write(IDbgTextWriter output, string expr);
	}

	/// <summary>
	/// Result of evaluating a filter expression
	/// </summary>
	public readonly struct DbgFilterExpressionEvaluatorResult {
		/// <summary>
		/// Result if <see cref="HasError"/> is false
		/// </summary>
		public bool Result { get; }

		/// <summary>
		/// Error message if <see cref="HasError"/> is true
		/// </summary>
		public string? Error { get; }

		/// <summary>
		/// true if there was an error
		/// </summary>
		public bool HasError => Error is not null;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error</param>
		public DbgFilterExpressionEvaluatorResult(string error) {
			Result = false;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="result">Result of evaluated expression</param>
		public DbgFilterExpressionEvaluatorResult(bool result) {
			Result = result;
			Error = null;
		}
	}

	/// <summary>Metadata</summary>
	public interface IDbgFilterExpressionEvaluatorMetadata {
		/// <summary>See <see cref="ExportDbgFilterExpressionEvaluatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgFilterExpressionEvaluator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgFilterExpressionEvaluatorAttribute : ExportAttribute, IDbgFilterExpressionEvaluatorMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgFilterExpressionEvaluatorAttribute(double order = double.MaxValue)
			: base(typeof(DbgFilterExpressionEvaluator)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
