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
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
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
		static readonly DbgDotNetText emptyPropertyName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.InstanceProperty, EmptyPropertyName));

		DebugViewNoResultsValueNode(string expression, string emptyMessage) {
			Expression = expression;
			noResultsName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, emptyMessage));
		}

		public static DebugViewNoResultsValueNode TryCreate(DbgEvaluationInfo evalInfo, string expression, DbgDotNetValueResult valueResult) {
			DbgDotNetValueResult getterResult = default;
			try {
				if (!valueResult.ValueIsException)
					return null;
				var appDomain = valueResult.Value.Type.AppDomain;
				var emptyProperty = valueResult.Value.Type.GetProperty(EmptyPropertyName, DmdSignatureCallingConvention.HasThis | DmdSignatureCallingConvention.Property, 0, appDomain.System_String, Array.Empty<DmdType>(), throwOnError: false);
				var emptyGetter = emptyProperty?.GetGetMethod(DmdGetAccessorOptions.All);
				if ((object)emptyGetter == null)
					return null;

				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				getterResult = runtime.Call(evalInfo, valueResult.Value, emptyGetter, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
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

		public override bool FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			noResultsName.WriteTo(output);
			return true;
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => 0;
		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) => Array.Empty<DbgDotNetValueNode>();
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
