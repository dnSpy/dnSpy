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
using System.Collections.Generic;
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters.VisualBasic {
	readonly struct VisualBasicStackFrameFormatter {
		readonly IDbgTextWriter output;
		readonly DbgEvaluationInfo evalInfo;
		readonly LanguageFormatter languageFormatter;
		readonly DbgStackFrameFormatterOptions options;
		readonly ValueFormatterOptions valueOptions;
		readonly CultureInfo cultureInfo;

		const string Keyword_As = "As";
		const string Keyword_get = "Get";
		const string Keyword_set = "Set";
		const string Keyword_add = "Add";
		const string Keyword_remove = "Remove";
		const string Keyword_ByRef = "ByRef";
		const string Keyword_params = "ParamArray";
		const string Keyword_New = "New";
		const string Keyword_Sub = "Sub";
		const string Keyword_Function = "Function";
		const string Keyword_ReadOnly = "ReadOnly";
		const string Keyword_Property = "Property";
		const string Keyword_Event = "Event";
		const string MethodParenOpen = "(";
		const string MethodParenClose = ")";
		const string GenericsParenOpen = "(";
		const string GenericsParenClose = ")";
		const string Keyword_Of = "Of";
		const string CommentBegin = "/*";
		const string CommentEnd = "*/";

		bool ModuleNames => (options & DbgStackFrameFormatterOptions.ModuleNames) != 0;
		bool ParameterTypes => (options & DbgStackFrameFormatterOptions.ParameterTypes) != 0;
		bool ParameterNames => (options & DbgStackFrameFormatterOptions.ParameterNames) != 0;
		bool ParameterValues => (options & DbgStackFrameFormatterOptions.ParameterValues) != 0;
		bool DeclaringTypes => (options & DbgStackFrameFormatterOptions.DeclaringTypes) != 0;
		bool ReturnTypes => (options & DbgStackFrameFormatterOptions.ReturnTypes) != 0;
		bool Namespaces => (options & DbgStackFrameFormatterOptions.Namespaces) != 0;
		bool IntrinsicTypeKeywords => (options & DbgStackFrameFormatterOptions.IntrinsicTypeKeywords) != 0;
		bool Decimal => (options & DbgStackFrameFormatterOptions.Decimal) != 0;
		bool Tokens => (options & DbgStackFrameFormatterOptions.Tokens) != 0;
		bool ShowIP => (options & DbgStackFrameFormatterOptions.IP) != 0;
		bool DigitSeparators => (options & DbgStackFrameFormatterOptions.DigitSeparators) != 0;
		bool FullString => (options & DbgStackFrameFormatterOptions.FullString) != 0;

		public VisualBasicStackFrameFormatter(IDbgTextWriter output, DbgEvaluationInfo evalInfo, LanguageFormatter languageFormatter, DbgStackFrameFormatterOptions options, ValueFormatterOptions valueOptions, CultureInfo? cultureInfo) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.evalInfo = evalInfo ?? throw new ArgumentNullException(nameof(evalInfo));
			this.languageFormatter = languageFormatter ?? throw new ArgumentNullException(nameof(languageFormatter));
			this.options = options;
			this.valueOptions = valueOptions;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
		}

		void OutputWrite(string s, DbgTextColor color) => output.Write(color, s);
		void WriteSpace() => OutputWrite(" ", DbgTextColor.Text);
		void WritePeriod() => OutputWrite(".", DbgTextColor.Operator);
		void WriteIdentifier(string id, DbgTextColor color) => OutputWrite(VisualBasicTypeFormatter.GetFormattedIdentifier(id), color);

		void WriteCommaSpace() {
			OutputWrite(",", DbgTextColor.Punctuation);
			WriteSpace();
		}

		ValueFormatterOptions GetValueFormatterOptions() {
			const ValueFormatterOptions Mask =
				ValueFormatterOptions.Decimal |
				ValueFormatterOptions.Namespaces |
				ValueFormatterOptions.IntrinsicTypeKeywords |
				ValueFormatterOptions.Tokens |
				ValueFormatterOptions.DigitSeparators;
			var res = valueOptions & ~Mask;
			if (Decimal)
				res |= ValueFormatterOptions.Decimal;
			if (Namespaces)
				res |= ValueFormatterOptions.Namespaces;
			if (IntrinsicTypeKeywords)
				res |= ValueFormatterOptions.IntrinsicTypeKeywords;
			if (Tokens)
				res |= ValueFormatterOptions.Tokens;
			if (DigitSeparators)
				res |= ValueFormatterOptions.DigitSeparators;
			if (FullString)
				res |= ValueFormatterOptions.FullString;
			return res;
		}

		void FormatType(DmdType type) {
			var typeOptions = GetValueFormatterOptions().ToTypeFormatterOptions(showArrayValueSizes: false);
			new VisualBasicTypeFormatter(output, typeOptions, cultureInfo).Format(type, null);
		}

		void WriteToken(DmdMemberInfo member) {
			if (!Tokens)
				return;
			var primitiveFormatter = new VisualBasicPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);
			var tokenString = primitiveFormatter.ToFormattedUInt32((uint)member.MetadataToken);
			OutputWrite(CommentBegin + tokenString + CommentEnd, DbgTextColor.Comment);
		}

		bool NeedThreadSwitch() {
			if (!ParameterValues)
				return false;
			if (evalInfo.Frame.IsClosed)
				return false;
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			if (runtime.Dispatcher.CheckAccess())
				return false;
			var sig = runtime.GetFrameMethod(evalInfo)?.GetMethodSignature();
			return sig is not null && (sig.GetParameterTypes().Count > 0 || sig.GetVarArgsParameterTypes().Count > 0);
		}

		public void Format() {
			// Minimize thread switches by switching to the debug engine thread
			if (NeedThreadSwitch())
				FormatInvoke();
			else
				FormatCore();
		}

		void FormatInvoke() {
			var @this = this;
			if (!evalInfo.Runtime.GetDotNetRuntime().Dispatcher.TryInvoke(() => @this.FormatCore())) {
				// process has exited
				OutputWrite("???", DbgTextColor.Error);
			}
		}

		void FormatCore() {
			if (ModuleNames) {
				OutputWrite(evalInfo.Frame.Module?.Name ?? "???", DbgTextColor.ModuleName);
				OutputWrite("!", DbgTextColor.Operator);
			}

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var method = runtime.GetFrameMethod(evalInfo);
			if (method is null)
				OutputWrite("???", DbgTextColor.Error);
			else {
				var propInfo = TypeFormatterUtils.TryGetProperty(method);
				if (propInfo.kind != AccessorKind.None) {
					Format(method, propInfo.property!, propInfo.kind);
					return;
				}

				var eventInfo = TypeFormatterUtils.TryGetEvent(method);
				if (eventInfo.kind != AccessorKind.None) {
					Format(method, eventInfo.@event!, eventInfo.kind);
					return;
				}

				Format(method);
			}
		}

		void WriteGenericArguments(DmdMethodBase method) {
			var genArgs = method.GetGenericArguments();
			if (genArgs.Count == 0)
				return;
			OutputWrite(GenericsParenOpen, DbgTextColor.Punctuation);
			OutputWrite(Keyword_Of, DbgTextColor.Keyword);
			WriteSpace();
			for (int i = 0; i < genArgs.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				FormatType(genArgs[i]);
			}
			OutputWrite(GenericsParenClose, DbgTextColor.Punctuation);
		}

		void WriteMethodParameterList(DmdMethodBase method, string openParen, string closeParen) =>
			WriteMethodParameterListCore(method, GetAllMethodParameterTypes(method.GetMethodSignature()), openParen, closeParen, ParameterTypes, ParameterNames, ParameterValues);

		void WriteMethodParameterListCore(DmdMethodBase method, IList<DmdType> parameterTypes, string openParen, string closeParen, bool showParameterTypes, bool showParameterNames, bool showParameterValues) {
			if (!showParameterTypes && !showParameterNames && !showParameterValues)
				return;

			OutputWrite(openParen, DbgTextColor.Punctuation);

			int baseIndex = method.IsStatic ? 0 : 1;
			var parameters = method.GetParameters();
			for (int i = 0; i < parameterTypes.Count; i++) {
				if (i > 0)
					WriteCommaSpace();

				var parameterType = parameterTypes[i];
				var param = i < parameters.Count ? parameters[i] : null;
				bool needSpace = false;
				if (showParameterNames || showParameterTypes) {
					if (parameterType.IsByRef) {
						parameterType = parameterType.GetElementType()!;
						OutputWrite(Keyword_ByRef, DbgTextColor.Keyword);
						WriteSpace();
					}

					if (param?.IsDefined("System.ParamArrayAttribute", false) == true) {
						OutputWrite(Keyword_params, DbgTextColor.Keyword);
						needSpace = true;
					}
				}

				if (showParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if (!string2.IsNullOrEmpty(param?.Name))
						WriteIdentifier(param.Name, DbgTextColor.Parameter);
					else
						WriteIdentifier("A_" + (baseIndex + i).ToString(), DbgTextColor.Parameter);
				}
				if (showParameterTypes) {
					if (showParameterNames) {
						WriteSpace();
						OutputWrite(Keyword_As, DbgTextColor.Keyword);
					}
					if (needSpace)
						WriteSpace();
					needSpace = true;

					FormatType(parameterType);
				}
				if (showParameterValues)
					needSpace = FormatValue((uint)(baseIndex + i), needSpace);
			}

			OutputWrite(closeParen, DbgTextColor.Punctuation);
		}

		bool FormatValue(uint index, bool needSpace) {
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			DbgDotNetValueResult parameterValue = default;
			DbgDotNetValue? dereferencedValue = null;
			try {
				parameterValue = runtime.GetParameterValue(evalInfo, index);
				if (parameterValue.IsNormalResult) {
					if (needSpace) {
						WriteSpace();
						OutputWrite("=", DbgTextColor.Operator);
						WriteSpace();
					}
					needSpace = true;

					var valueFormatter = new VisualBasicValueFormatter(output, evalInfo, languageFormatter, valueOptions, cultureInfo);
					var value = parameterValue.Value;
					if (value?.Type.IsByRef == true)
						value = dereferencedValue = value.LoadIndirect().Value;
					if (value is null)
						OutputWrite("???", DbgTextColor.Error);
					else
						valueFormatter.Format(value);
				}
			}
			finally {
				parameterValue.Value?.Dispose();
				dereferencedValue?.Dispose();
			}
			return needSpace;
		}

		static IList<DmdType> GetAllMethodParameterTypes(DmdMethodSignature sig) {
			if (sig.GetVarArgsParameterTypes().Count == 0)
				return sig.GetParameterTypes();
			var list = new List<DmdType>(sig.GetParameterTypes().Count + sig.GetVarArgsParameterTypes().Count);
			list.AddRange(sig.GetParameterTypes());
			list.AddRange(sig.GetVarArgsParameterTypes());
			return list;
		}

		void WriteOffset() {
			if (!ShowIP)
				return;
			WriteSpace();
			OutputWrite("(", DbgTextColor.Punctuation);
			OutputWrite("IL", DbgTextColor.Text);
			var primitiveFormatter = new VisualBasicPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);

			var loc = evalInfo.Frame.Location as IDbgDotNetCodeLocation;
			var ilOffsetMapping = loc?.ILOffsetMapping ?? DbgILOffsetMapping.Exact;
			switch (ilOffsetMapping) {
			case DbgILOffsetMapping.Prolog:
				OutputWrite("=", DbgTextColor.Operator);
				OutputWrite("prolog", DbgTextColor.Text);
				break;

			case DbgILOffsetMapping.Epilog:
				OutputWrite("=", DbgTextColor.Operator);
				OutputWrite("epilog", DbgTextColor.Text);
				break;

			case DbgILOffsetMapping.Exact:
			case DbgILOffsetMapping.Approximate:
				OutputWrite(ilOffsetMapping == DbgILOffsetMapping.Exact ? "=" : "≈", DbgTextColor.Operator);
				if (evalInfo.Frame.FunctionOffset <= ushort.MaxValue)
					primitiveFormatter.FormatUInt16((ushort)evalInfo.Frame.FunctionOffset);
				else
					primitiveFormatter.FormatUInt32(evalInfo.Frame.FunctionOffset);
				break;

			case DbgILOffsetMapping.Unknown:
			case DbgILOffsetMapping.NoInfo:
			case DbgILOffsetMapping.UnmappedAddress:
			default:
				OutputWrite("=", DbgTextColor.Operator);
				OutputWrite("???", DbgTextColor.Error);
				break;
			}

			if (loc is not null) {
				var addr = loc.NativeAddress;
				if (addr.Address != 0) {
					WriteCommaSpace();
					OutputWrite("Native", DbgTextColor.Text);
					OutputWrite("=", DbgTextColor.Operator);
					if (evalInfo.Runtime.Process.PointerSize == 4)
						primitiveFormatter.FormatUInt32((uint)addr.Address);
					else
						primitiveFormatter.FormatUInt64(addr.Address);
					long offs = (long)addr.Offset;
					if (offs < 0) {
						offs = -offs;
						OutputWrite("-", DbgTextColor.Operator);
					}
					else
						OutputWrite("+", DbgTextColor.Operator);
					primitiveFormatter.FormatFewDigits((ulong)offs);
				}
			}
			OutputWrite(")", DbgTextColor.Punctuation);
		}

		void WriteAccessor(AccessorKind accessorKind) {
			string keyword;
			switch (accessorKind) {
			case AccessorKind.None:
			default:
				return;

			case AccessorKind.Getter:
				keyword = Keyword_get;
				break;

			case AccessorKind.Setter:
				keyword = Keyword_set;
				break;

			case AccessorKind.Adder:
				keyword = Keyword_add;
				break;

			case AccessorKind.Remover:
				keyword = Keyword_remove;
				break;
			}
			OutputWrite(keyword, DbgTextColor.Keyword);
			WriteSpace();
		}

		void Format(DmdMethodBase method, DmdPropertyInfo property, AccessorKind accessorKind) {
			if (property.SetMethod is null) {
				OutputWrite(Keyword_ReadOnly, DbgTextColor.Keyword);
				WriteSpace();
			}

			OutputWrite(Keyword_Property, DbgTextColor.Keyword);
			WriteSpace();

			WriteAccessor(accessorKind);
			WriteToken(method);

			if (DeclaringTypes) {
				FormatType(property.DeclaringType!);
				WritePeriod();
			}

			WriteIdentifier(property.Name, TypeFormatterUtils.GetColor(property));
			WriteToken(property);
			WriteGenericArguments(method);
			WriteMethodParameterList(method, MethodParenOpen, MethodParenClose);
			WriteReturnType(method);
			WriteOffset();
		}

		void Format(DmdMethodBase method, DmdEventInfo @event, AccessorKind accessorKind) {
			OutputWrite(Keyword_Event, DbgTextColor.Keyword);
			WriteSpace();

			WriteAccessor(accessorKind);
			WriteToken(method);

			if (DeclaringTypes) {
				FormatType(@event.DeclaringType!);
				WritePeriod();
			}
			WriteIdentifier(@event.Name, TypeFormatterUtils.GetColor(@event));
			WriteToken(@event);
			WriteSpace();
			OutputWrite(Keyword_As, DbgTextColor.Keyword);
			WriteSpace();
			FormatType(@event.EventHandlerType);
			WriteOffset();
		}

		void Format(DmdMethodBase method) {
			if (StateMachineUtils.TryGetKickoffMethod(method, out var kickoffMethod))
				method = kickoffMethod;

			var sig = method.GetMethodSignature();

			string[]? operatorInfo;
			if (method is DmdConstructorInfo)
				operatorInfo = null;
			else
				operatorInfo = Operators.TryGetOperatorInfo(method.Name);

			if (operatorInfo is not null) {
				for (int i = 0; i < operatorInfo.Length - 1; i++) {
					WriteOperatorInfoString(operatorInfo[i]);
					WriteSpace();
				}
			}
			else {
				bool isSub = method.GetMethodSignature().ReturnType == method.AppDomain.System_Void;
				OutputWrite(isSub ? Keyword_Sub : Keyword_Function, DbgTextColor.Keyword);
				WriteSpace();
			}

			if (DeclaringTypes) {
				FormatType(method.DeclaringType!);
				WritePeriod();
			}
			if (method is DmdConstructorInfo)
				OutputWrite(Keyword_New, DbgTextColor.Keyword);
			else {
				if (TypeFormatterUtils.TryGetMethodName(method.Name, out var containingMethodName, out var localFunctionName)) {
					var methodColor = TypeFormatterUtils.GetColor(method, canBeModule: true);
					WriteIdentifier(containingMethodName, methodColor);
					WritePeriod();
					WriteIdentifier(localFunctionName, methodColor);
				}
				else
					WriteMethodName(method, method.Name, operatorInfo);
			}
			WriteToken(method);

			WriteGenericArguments(method);
			WriteMethodParameterList(method, MethodParenOpen, MethodParenClose);
			WriteReturnType(method);
			WriteOffset();
		}

		void WriteReturnType(DmdMethodBase method) {
			if (!ReturnTypes)
				return;

			var sig = method.GetMethodSignature();
			if (sig.ReturnType == method.AppDomain.System_Void)
				return;

			WriteSpace();
			OutputWrite(Keyword_As, DbgTextColor.Keyword);
			WriteSpace();
			FormatType(sig.ReturnType);
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'A' <= s[0] && s[0] <= 'Z' ? DbgTextColor.Keyword : DbgTextColor.Operator);

		void WriteMethodName(DmdMethodBase method, string name, string[]? operatorInfo) {
			if (operatorInfo is not null)
				WriteOperatorInfoString(operatorInfo[operatorInfo.Length - 1]);
			else
				WriteIdentifier(name, TypeFormatterUtils.GetColor(method, canBeModule: true));
		}
	}
}
