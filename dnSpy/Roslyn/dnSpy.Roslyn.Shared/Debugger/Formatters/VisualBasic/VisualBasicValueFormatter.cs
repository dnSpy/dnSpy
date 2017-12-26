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
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters.VisualBasic {
	struct VisualBasicValueFormatter {
		readonly ITextColorWriter output;
		readonly DbgEvaluationInfo evalInfo;
		readonly LanguageFormatter languageFormatter;
		readonly ValueFormatterOptions options;
		readonly CultureInfo cultureInfo;
		const int MAX_RECURSION = 200;
		int recursionCounter;

		const string TypeNameOpenParen = "{";
		const string TypeNameCloseParen = "}";
		const string TupleTypeOpenParen = "(";
		const string TupleTypeCloseParen = ")";
		const string KeyValuePairTypeOpenParen = "[";
		const string KeyValuePairTypeCloseParen = "]";

		bool FuncEval => (options & ValueFormatterOptions.FuncEval) != 0;
		bool UseToString => (options & ValueFormatterOptions.ToString) != 0;
		bool NoDebuggerDisplay => (options & ValueFormatterOptions.NoDebuggerDisplay) != 0;

		public VisualBasicValueFormatter(ITextColorWriter output, DbgEvaluationInfo evalInfo, LanguageFormatter languageFormatter, ValueFormatterOptions options, CultureInfo cultureInfo) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.evalInfo = evalInfo ?? throw new ArgumentNullException(nameof(evalInfo));
			this.languageFormatter = languageFormatter ?? throw new ArgumentNullException(nameof(languageFormatter));
			this.options = options;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			recursionCounter = 0;
		}

		void OutputWrite(string s, object color) => output.Write(color, s);

		void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);

		public void Format(DbgDotNetValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				if (recursionCounter++ >= MAX_RECURSION) {
					OutputWrite("???", BoxedTextColor.Error);
					return;
				}

				if (TrySimpleFormat(value))
					return;
				var type = value.Type;
				int tupleArity = TypeFormatterUtils.GetTupleArity(type);
				if (tupleArity > 0 && TryFormatTuple(value, tupleArity))
					return;
				if (KeyValuePairTypeUtils.IsKeyValuePair(type) && TryFormatKeyValuePair(value))
					return;
				if (TryFormatWithDebuggerAttributes(value))
					return;
				if (TryFormatToString(value))
					return;
				FormatTypeName(value);
			}
			finally {
				recursionCounter--;
			}
		}

		void FormatTypeName(DbgDotNetValue value) {
			OutputWrite(TypeNameOpenParen, BoxedTextColor.Error);
			new VisualBasicTypeFormatter(output, options.ToTypeFormatterOptions(showArrayValueSizes: true), cultureInfo).Format(value.Type, value);
			OutputWrite(TypeNameCloseParen, BoxedTextColor.Error);
		}

		bool TryFormatTuple(DbgDotNetValue value, int tupleArity) {
			Debug.Assert(TypeFormatterUtils.GetTupleArity(value.Type) == tupleArity && tupleArity > 0);
			OutputWrite(TupleTypeOpenParen, BoxedTextColor.Punctuation);

			var values = ObjectCache.AllocDotNetValueList();
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			int index = 0;
			foreach (var info in TupleTypeUtils.GetTupleFields(value.Type, tupleArity)) {
				if (index++ > 0) {
					OutputWrite(",", BoxedTextColor.Punctuation);
					WriteSpace();
				}
				if (info.tupleIndex < 0) {
					OutputWrite("???", BoxedTextColor.Error);
					break;
				}
				else {
					var objValue = value;
					DbgDotNetValueResult valueResult = default;
					try {
						foreach (var field in info.fields) {
							valueResult = runtime.LoadField(evalInfo, objValue, field);
							if (valueResult.Value != null)
								values.Add(valueResult.Value);
							if (valueResult.HasError || valueResult.ValueIsException) {
								objValue = null;
								break;
							}
							objValue = valueResult.Value;
						}
						valueResult = default;
						if (objValue == null) {
							OutputWrite("???", BoxedTextColor.Error);
							break;
						}
						Format(objValue);
					}
					finally {
						valueResult.Value?.Dispose();
						foreach (var v in values)
							v?.Dispose();
						values.Clear();
					}
				}
			}
			ObjectCache.Free(ref values);

			OutputWrite(TupleTypeCloseParen, BoxedTextColor.Punctuation);
			return true;
		}

		bool TryFormatKeyValuePair(DbgDotNetValue value) {
			var info = KeyValuePairTypeUtils.TryGetFields(value.Type);
			if ((object)info.keyField == null)
				return false;
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			DbgDotNetValueResult keyResult = default, valueResult = default;
			try {
				keyResult = runtime.LoadField(evalInfo, value, info.keyField);
				if (keyResult.ErrorMessage != null || keyResult.ValueIsException)
					return false;
				valueResult = runtime.LoadField(evalInfo, value, info.valueField);
				if (valueResult.ErrorMessage != null || valueResult.ValueIsException)
					return false;

				OutputWrite(KeyValuePairTypeOpenParen, BoxedTextColor.Punctuation);
				Format(keyResult.Value);
				OutputWrite(",", BoxedTextColor.Punctuation);
				WriteSpace();
				Format(valueResult.Value);
				OutputWrite(KeyValuePairTypeCloseParen, BoxedTextColor.Punctuation);
				return true;
			}
			finally {
				keyResult.Value?.Dispose();
				valueResult.Value?.Dispose();
			}
		}

		bool TryFormatWithDebuggerAttributes(DbgDotNetValue value) {
			if (!FuncEval || NoDebuggerDisplay)
				return false;
			return new DebuggerDisplayAttributeFormatter(evalInfo, languageFormatter, output, options.ToDbgValueFormatterOptions(), cultureInfo).FormatValue(value);
		}

		bool TryFormatToString(DbgDotNetValue value) {
			if (!FuncEval || !UseToString)
				return false;
			var s = new ToStringFormatter(evalInfo).GetToStringValue(value);
			if (s == null)
				return false;
			OutputWrite(TypeNameOpenParen + s + TypeNameCloseParen, BoxedTextColor.ToStringEval);
			return true;
		}

		bool TrySimpleFormat(DbgDotNetValue value) {
			var rawValue = value.GetRawValue();
			if (rawValue.ValueType == DbgSimpleValueType.Void) {
				Debug.Assert(value.Type == value.Type.AppDomain.System_Void);
				OutputWrite(dnSpy_Roslyn_Shared_Resources.DebuggerExpressionHasNoValue, BoxedTextColor.Text);
				return true;
			}

			return new VisualBasicPrimitiveValueFormatter(output, options, cultureInfo).TryFormat(value.Type, rawValue);
		}
	}
}
