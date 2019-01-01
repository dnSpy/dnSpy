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

using System.Collections.ObjectModel;
using System.Globalization;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes {
	/// <summary>
	/// A .NET value node
	/// </summary>
	public abstract class DbgDotNetValueNode : DbgObject {
		/// <summary>
		/// Gets the expected type or null
		/// </summary>
		public abstract DmdType ExpectedType { get; }

		/// <summary>
		/// Gets the actual type or null
		/// </summary>
		public abstract DmdType ActualType { get; }

		/// <summary>
		/// Gets the error message or null
		/// </summary>
		public abstract string ErrorMessage { get; }

		/// <summary>
		/// Gets the value or null
		/// </summary>
		public abstract DbgDotNetValue Value { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		public abstract DbgDotNetText Name { get; }

		/// <summary>
		/// Gets the expression
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
		/// Gets the format specifiers or null
		/// </summary>
		public abstract ReadOnlyCollection<string> FormatSpecifiers { get; }

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
		/// Creates new children
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Index of first child</param>
		/// <param name="count">Max number of children to return</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options);

		/// <summary>
		/// Formats the name. Returns false if nothing was written to <paramref name="output"/>
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="formatter">Formatter</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <returns></returns>
		public virtual bool FormatName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) => false;

		/// <summary>
		/// Formats the value column. Returns false if nothing was written to <paramref name="output"/>
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="formatter">Formatter</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <returns></returns>
		public virtual bool FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) => false;

		/// <summary>
		/// Formats the expected type. Returns false if nothing was written to <paramref name="output"/>
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="formatter">Formatter</param>
		/// <param name="options">Options</param>
		/// <param name="valueOptions">Value options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <returns></returns>
		public virtual bool FormatExpectedType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) => false;

		/// <summary>
		/// Formats the actual type. Returns false if nothing was written to <paramref name="output"/>
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="formatter">Formatter</param>
		/// <param name="options">Options</param>
		/// <param name="valueOptions">Value options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <returns></returns>
		public virtual bool FormatActualType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) => false;
	}
}
