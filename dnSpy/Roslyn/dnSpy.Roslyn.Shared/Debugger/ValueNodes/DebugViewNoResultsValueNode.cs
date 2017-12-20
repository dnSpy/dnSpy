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

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class DebugViewNoResultsValueNode : DbgDotNetValueNode {
		public override DmdType ExpectedType => null;
		public override DmdType ActualType => null;
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value => null;
		public override DbgDotNetText Name => emptyPropertyName;
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.Property;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override ReadOnlyCollection<string> FormatSpecifiers => null;
		public override bool? HasChildren => false;

		const string EmptyPropertyName = "Empty";
		readonly DbgDotNetText noResultsName;
		static readonly DbgDotNetText emptyPropertyName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.InstanceProperty, EmptyPropertyName));

		DebugViewNoResultsValueNode(string expression, string emptyMessage) {
			Expression = expression;
			noResultsName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, emptyMessage));
		}

		public static DebugViewNoResultsValueNode TryCreate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgDotNetValueResult valueResult, CancellationToken cancellationToken) {
			DbgDotNetValueResult getterResult = default;
			try {
				if (!valueResult.ValueIsException)
					return null;
				var appDomain = valueResult.Value.Type.AppDomain;
				var emptyProperty = valueResult.Value.Type.GetProperty(EmptyPropertyName, DmdSignatureCallingConvention.HasThis | DmdSignatureCallingConvention.Property, 0, appDomain.System_String, Array.Empty<DmdType>(), throwOnError: false);
				var emptyGetter = emptyProperty?.GetGetMethod(DmdGetAccessorOptions.All);
				if ((object)emptyGetter == null)
					return null;

				var runtime = context.Runtime.GetDotNetRuntime();
				getterResult = runtime.Call(context, frame, valueResult.Value, emptyGetter, Array.Empty<object>(), DbgDotNetInvokeOptions.None, cancellationToken);
				if (!getterResult.IsNormalResult)
					return null;
				var rawValue = getterResult.Value.GetRawValue();
				if (!rawValue.HasRawValue || rawValue.ValueType != DbgSimpleValueType.StringUtf16 || !(rawValue.RawValue is string emptyMessage))
					return null;
				return new DebugViewNoResultsValueNode(expression, emptyMessage);
			}
			finally {
				getterResult.Value?.Dispose();
			}
		}

		public override bool FormatValue(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			noResultsName.WriteTo(output);
			return true;
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) => 0;
		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgDotNetValueNode>();
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
