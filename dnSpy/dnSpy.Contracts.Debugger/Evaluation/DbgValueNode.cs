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
using System.Globalization;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// A value shown in a treeview (eg. in locals window)
	/// </summary>
	public abstract class DbgValueNode : DbgObject {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// true if this is an error value node
		/// </summary>
		public bool HasError => !(ErrorMessage is null);

		/// <summary>
		/// Gets the error message or null
		/// </summary>
		public abstract string? ErrorMessage { get; }

		/// <summary>
		/// true if <see cref="Value"/> is not null
		/// </summary>
		public bool HasValue => !(Value is null);

		/// <summary>
		/// Gets the value or null if there's none
		/// </summary>
		public abstract DbgValue? Value { get; }

		/// <summary>
		/// true if it's a node that has an <see cref="Expression"/> can be evaluated, false if it's a non-value node, eg. 'Type variables', 'Raw View', etc.
		/// </summary>
		public abstract bool CanEvaluateExpression { get; }

		/// <summary>
		/// Gets the expression that is used when adding an expression to the watch window or
		/// when assigning a new value to the source. See also <see cref="CanEvaluateExpression"/>.
		/// </summary>
		public abstract string Expression { get; }

		/// <summary>
		/// Image name, see <see cref="PredefinedDbgValueNodeImageNames"/>
		/// </summary>
		public abstract string ImageName { get; }

		/// <summary>
		/// true if this is a read-only value
		/// </summary>
		public abstract bool IsReadOnly { get; }

		/// <summary>
		/// true if the expression causes side effects
		/// </summary>
		public abstract bool CausesSideEffects { get; }

		/// <summary>
		/// Returns true if it has children, false if it has no children and null if it's unknown (eg. it's too expensive to calculate it now).
		/// UI code can use this property to decide if it shows the treeview node expander ("|>").
		/// </summary>
		public abstract bool? HasChildren { get; }

		/// <summary>
		/// Number of children. This property is called as late as possible and can be lazily initialized.
		/// It's assumed to be 0 if <see cref="HasChildren"/> is false.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		public abstract ulong GetChildCount(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Creates new children. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Index of first child</param>
		/// <param name="count">Max number of children to return</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options);

		/// <summary>
		/// Formats the name
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="options">Formatter options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public void FormatName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo = null) =>
			Format(evalInfo, new DbgValueNodeFormatParameters {
				NameOutput = output ?? throw new ArgumentNullException(nameof(output)),
				NameFormatterOptions = options,
			}, cultureInfo);

		/// <summary>
		/// Formats the value
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="options">Formatter options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public void FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) =>
			Format(evalInfo, new DbgValueNodeFormatParameters {
				ValueOutput = output ?? throw new ArgumentNullException(nameof(output)),
				ValueFormatterOptions = options,
			}, cultureInfo);

		/// <summary>
		/// Formats the expected type ("field" type)
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="options">Formatter options</param>
		/// <param name="valueOptions">Value options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public void FormatExpectedType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo) =>
			Format(evalInfo, new DbgValueNodeFormatParameters {
				ExpectedTypeOutput = output ?? throw new ArgumentNullException(nameof(output)),
				ExpectedTypeFormatterOptions = options,
				TypeFormatterOptions = valueOptions,
			}, cultureInfo);

		/// <summary>
		/// Formats the actual type (value type)
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="options">Formatter options</param>
		/// <param name="valueOptions">Value options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public void FormatActualType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo) =>
			Format(evalInfo, new DbgValueNodeFormatParameters {
				ActualTypeOutput = output ?? throw new ArgumentNullException(nameof(output)),
				ActualTypeFormatterOptions = options,
				TypeFormatterOptions = valueOptions,
			}, cultureInfo);

		/// <summary>
		/// Formats the name, value, and type
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public abstract void Format(DbgEvaluationInfo evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options);
	}

	/// <summary>
	/// Assignment result
	/// </summary>
	public readonly struct DbgValueNodeAssignmentResult {
		/// <summary>
		/// Gets the error message or null if none
		/// </summary>
		public string? Error { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgEEAssignmentResultFlags Flags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Result flags</param>
		/// <param name="error">Error message or null if none</param>
		public DbgValueNodeAssignmentResult(DbgEEAssignmentResultFlags flags, string? error) {
			Flags = flags;
			Error = error;
		}
	}

	/// <summary>
	/// Parameters when formatting a <see cref="DbgValueNode"/>
	/// </summary>
	public interface IDbgValueNodeFormatParameters {
		/// <summary>
		/// Used when writing the name or null if it's not written
		/// </summary>
		IDbgTextWriter? NameOutput { get; }

		/// <summary>
		/// Used when writing the value or null if it's not written
		/// </summary>
		IDbgTextWriter? ValueOutput { get; }

		/// <summary>
		/// Used when writing the expected type ("field" type) or null if it's not written
		/// </summary>
		IDbgTextWriter? ExpectedTypeOutput { get; }

		/// <summary>
		/// Used when writing the actual type (value type) or null if it's not written
		/// </summary>
		IDbgTextWriter? ActualTypeOutput { get; }

		/// <summary>
		/// Name formatter options
		/// </summary>
		DbgValueFormatterOptions NameFormatterOptions { get; }

		/// <summary>
		/// Value formatter options
		/// </summary>
		DbgValueFormatterOptions ValueFormatterOptions { get; }

		/// <summary>
		/// Type formatter options
		/// </summary>
		DbgValueFormatterOptions TypeFormatterOptions { get; }

		/// <summary>
		/// Expected type formatter options
		/// </summary>
		DbgValueFormatterTypeOptions ExpectedTypeFormatterOptions { get; }

		/// <summary>
		/// Actual type formatter options
		/// </summary>
		DbgValueFormatterTypeOptions ActualTypeFormatterOptions { get; }
	}

	sealed class DbgValueNodeFormatParameters : IDbgValueNodeFormatParameters {
		public IDbgTextWriter? NameOutput { get; set; }
		public IDbgTextWriter? ValueOutput { get; set; }
		public IDbgTextWriter? ExpectedTypeOutput { get; set; }
		public IDbgTextWriter? ActualTypeOutput { get; set; }
		public DbgValueFormatterOptions NameFormatterOptions { get; set; }
		public DbgValueFormatterOptions ValueFormatterOptions { get; set; }
		public DbgValueFormatterOptions TypeFormatterOptions { get; set; }
		public DbgValueFormatterTypeOptions ExpectedTypeFormatterOptions { get; set; }
		public DbgValueFormatterTypeOptions ActualTypeFormatterOptions { get; set; }
	}
}
