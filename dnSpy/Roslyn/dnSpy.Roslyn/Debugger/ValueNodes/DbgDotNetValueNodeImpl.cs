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
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Debugger.Formatters;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class DbgDotNetValueNodeImpl : DbgDotNetValueNode {
		public override DmdType ExpectedType { get; }
		public override DmdType ActualType { get; }
		public override string ErrorMessage { get; }
		public override DbgDotNetValue Value { get; }
		public override DbgDotNetText Name { get; }
		public override string Expression { get; }
		public override string ImageName { get; }
		public override bool IsReadOnly { get; }
		public override bool CausesSideEffects { get; }
		public override ReadOnlyCollection<string> FormatSpecifiers => formatSpecifiers;
		public override bool? HasChildren => childNodeProvider?.HasChildren ?? false;

		readonly LanguageValueNodeFactory valueNodeFactory;
		readonly DbgDotNetValueNodeProvider childNodeProvider;
		readonly DbgDotNetValueNodeInfo nodeInfo;
		readonly DbgDotNetText valueText;
		ReadOnlyCollection<string> formatSpecifiers;
		readonly ColumnFormatter columnFormatter;

		public DbgDotNetValueNodeImpl(LanguageValueNodeFactory valueNodeFactory, DbgDotNetValueNodeProvider childNodeProvider, DbgDotNetText name, DbgDotNetValueNodeInfo nodeInfo, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, DmdType actualType, string errorMessage, DbgDotNetText valueText, ReadOnlyCollection<string> formatSpecifiers, ColumnFormatter columnFormatter) {
			if (name.Parts == null && columnFormatter == null)
				throw new ArgumentException();
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.childNodeProvider = childNodeProvider;
			this.nodeInfo = nodeInfo;
			Name = name;
			Value = nodeInfo?.DisplayValue;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
			IsReadOnly = isReadOnly;
			CausesSideEffects = causesSideEffects;
			ExpectedType = expectedType;
			ActualType = actualType;
			ErrorMessage = errorMessage;
			this.valueText = valueText;
			this.formatSpecifiers = formatSpecifiers;
			this.columnFormatter = columnFormatter;
		}

		internal void SetFormatSpecifiers(ReadOnlyCollection<string> formatSpecifiers) => this.formatSpecifiers = formatSpecifiers;

		public override bool FormatName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			if (columnFormatter?.FormatName(evalInfo, output, formatter, options, cultureInfo) == true)
				return true;
			if (Value == null)
				return false;
			if ((options & DbgValueFormatterOptions.NoDebuggerDisplay) != 0)
				return false;
			var languageFormatter = formatter as LanguageFormatter;
			Debug.Assert(languageFormatter != null);
			if (languageFormatter == null)
				return false;
			var displayAttrFormatter = new DebuggerDisplayAttributeFormatter(evalInfo, languageFormatter, output, options, cultureInfo);
			return displayAttrFormatter.FormatName(Value);
		}

		public override bool FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			if (columnFormatter?.FormatValue(evalInfo, output, formatter, options, cultureInfo) == true)
				return true;

			if (valueText.Parts != null) {
				valueText.WriteTo(output);
				return true;
			}
			return false;
		}

		public override bool FormatActualType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) =>
			columnFormatter?.FormatActualType(evalInfo, output, formatter, options, valueOptions, cultureInfo) ??
			FormatDebuggerDisplayAttributeType(evalInfo, output, formatter, valueOptions, cultureInfo);

		public override bool FormatExpectedType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) =>
			columnFormatter?.FormatExpectedType(evalInfo, output, formatter, options, valueOptions, cultureInfo) ??
			FormatDebuggerDisplayAttributeType(evalInfo, output, formatter, valueOptions, cultureInfo);

		bool FormatDebuggerDisplayAttributeType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			if (Value == null)
				return false;
			if ((options & DbgValueFormatterOptions.NoDebuggerDisplay) != 0)
				return false;
			var languageFormatter = formatter as LanguageFormatter;
			Debug.Assert(languageFormatter != null);
			if (languageFormatter == null)
				return false;
			var displayAttrFormatter = new DebuggerDisplayAttributeFormatter(evalInfo, languageFormatter, output, options, cultureInfo);
			return displayAttrFormatter.FormatType(Value);
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) =>
			childNodeProvider?.GetChildCount(evalInfo) ?? 0;

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			if (childNodeProvider == null)
				return Array.Empty<DbgDotNetValueNode>();
			return childNodeProvider.GetChildren(valueNodeFactory, evalInfo, index, count, options, FormatSpecifiers);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			Value?.Dispose();
			nodeInfo?.Dispose();
			childNodeProvider?.Dispose();
		}
	}
}
