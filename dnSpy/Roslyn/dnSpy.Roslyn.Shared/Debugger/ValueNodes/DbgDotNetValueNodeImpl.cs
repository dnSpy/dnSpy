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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Debugger.Formatters;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
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
		public override ReadOnlyCollection<string> FormatSpecifiers { get; }
		public override bool? HasChildren => childNodeProvider?.HasChildren ?? false;

		readonly LanguageValueNodeFactory valueNodeFactory;
		readonly DbgDotNetValueNodeProvider childNodeProvider;
		readonly DbgDotNetValueNodeInfo nodeInfo;
		readonly DbgDotNetText valueText;

		public DbgDotNetValueNodeImpl(LanguageValueNodeFactory valueNodeFactory, DbgDotNetValueNodeProvider childNodeProvider, in DbgDotNetText name, DbgDotNetValueNodeInfo nodeInfo, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, DmdType actualType, string errorMessage, in DbgDotNetText valueText, ReadOnlyCollection<string> formatSpecifiers) {
			if (name.Parts == null)
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
			FormatSpecifiers = formatSpecifiers;
		}

		public override bool FormatName(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			if (Value == null)
				return false;
			if ((options & DbgValueFormatterOptions.NoDebuggerDisplay) != 0)
				return false;
			var languageFormatter = formatter as LanguageFormatter;
			Debug.Assert(languageFormatter != null);
			if (languageFormatter == null)
				return false;
			var displayAttrFormatter = new DebuggerDisplayAttributeFormatter(context, frame, languageFormatter, output, options, cultureInfo, cancellationToken);
			return displayAttrFormatter.FormatName(Value);
		}

		public override bool FormatValue(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			if (valueText.Parts != null) {
				valueText.WriteTo(output);
				return true;
			}
			return false;
		}

		public override bool FormatActualType(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) =>
			FormatDebuggerDisplayAttributeType(context, frame, output, formatter, valueOptions, cultureInfo, cancellationToken);

		public override bool FormatExpectedType(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) =>
			FormatDebuggerDisplayAttributeType(context, frame, output, formatter, valueOptions, cultureInfo, cancellationToken);

		bool FormatDebuggerDisplayAttributeType(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			if (Value == null)
				return false;
			if ((options & DbgValueFormatterOptions.NoDebuggerDisplay) != 0)
				return false;
			var languageFormatter = formatter as LanguageFormatter;
			Debug.Assert(languageFormatter != null);
			if (languageFormatter == null)
				return false;
			var displayAttrFormatter = new DebuggerDisplayAttributeFormatter(context, frame, languageFormatter, output, options, cultureInfo, cancellationToken);
			return displayAttrFormatter.FormatType(Value);
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) =>
			childNodeProvider?.GetChildCount(context, frame, cancellationToken) ?? 0;

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			if (childNodeProvider == null)
				return Array.Empty<DbgDotNetValueNode>();
			return childNodeProvider.GetChildren(valueNodeFactory, context, frame, index, count, options, cancellationToken);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			Value?.Dispose();
			nodeInfo?.Dispose();
			childNodeProvider?.Dispose();
		}
	}
}
