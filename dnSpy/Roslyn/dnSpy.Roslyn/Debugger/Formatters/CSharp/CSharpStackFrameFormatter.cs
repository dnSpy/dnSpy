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

namespace dnSpy.Roslyn.Debugger.Formatters.CSharp {
	readonly struct CSharpStackFrameFormatter {
		readonly IDbgTextWriter output;
		readonly DbgEvaluationInfo evalInfo;
		readonly LanguageFormatter languageFormatter;
		readonly DbgStackFrameFormatterOptions options;
		readonly ValueFormatterOptions valueOptions;
		readonly CultureInfo cultureInfo;

		const string Keyword_this = "this";
		const string Keyword_params = "params";
		const string Keyword_get = "get";
		const string Keyword_set = "set";
		const string Keyword_add = "add";
		const string Keyword_remove = "remove";
		const string Keyword_out = "out";
		const string Keyword_ref = "ref";
		const string Keyword_in = "in";
		const string Keyword_readonly = "readonly";
		const string GenericsParenOpen = "<";
		const string GenericsParenClose = ">";
		const string IndexerParenOpen = "[";
		const string IndexerParenClose = "]";
		const string MethodParenOpen = "(";
		const string MethodParenClose = ")";
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

		public CSharpStackFrameFormatter(IDbgTextWriter output, DbgEvaluationInfo evalInfo, LanguageFormatter languageFormatter, DbgStackFrameFormatterOptions options, ValueFormatterOptions valueOptions, CultureInfo cultureInfo) {
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
		void WriteIdentifier(string id, DbgTextColor color) => OutputWrite(CSharpTypeFormatter.GetFormattedIdentifier(id), color);

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

		void FormatReturnType(DmdType type, bool isReadOnly) {
			if (type.IsByRef && isReadOnly) {
				type = type.GetElementType();
				OutputWrite(Keyword_ref, DbgTextColor.Keyword);
				WriteSpace();
				OutputWrite(Keyword_readonly, DbgTextColor.Keyword);
				WriteSpace();
			}
			FormatType(type);
		}

		void FormatType(DmdType type) {
			var typeOptions = GetValueFormatterOptions().ToTypeFormatterOptions(showArrayValueSizes: false);
			new CSharpTypeFormatter(output, typeOptions, cultureInfo).Format(type, null);
		}

		void WriteToken(DmdMemberInfo member) {
			if (!Tokens)
				return;
			var primitiveFormatter = new CSharpPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);
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
			return sig != null && (sig.GetParameterTypes().Count > 0 || sig.GetVarArgsParameterTypes().Count > 0);
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
			if ((object)method == null)
				OutputWrite("???", DbgTextColor.Error);
			else {
				var propInfo = TypeFormatterUtils.TryGetProperty(method);
				if (propInfo.kind != AccessorKind.None) {
					Format(method, propInfo.property, propInfo.kind);
					return;
				}

				var eventInfo = TypeFormatterUtils.TryGetEvent(method);
				if (eventInfo.kind != AccessorKind.None) {
					Format(method, eventInfo.@event, eventInfo.kind);
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

				var param = i < parameters.Count ? parameters[i] : null;

				bool needSpace = false;
				if (showParameterTypes) {
					needSpace = true;

					if (param?.IsDefined("System.ParamArrayAttribute", false) == true) {
						OutputWrite(Keyword_params, DbgTextColor.Keyword);
						WriteSpace();
					}
					var parameterType = parameterTypes[i];
					WriteRefIfByRef(param);
					if (parameterType.IsByRef)
						parameterType = parameterType.GetElementType();
					FormatType(parameterType);
				}
				if (showParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if (!string.IsNullOrEmpty(param?.Name))
						WriteIdentifier(param.Name, DbgTextColor.Parameter);
					else
						WriteIdentifier("A_" + (baseIndex + i).ToString(), DbgTextColor.Parameter);
				}
				if (showParameterValues)
					needSpace = FormatValue((uint)(baseIndex + i), needSpace);
			}

			OutputWrite(closeParen, DbgTextColor.Punctuation);
		}

		void WriteRefIfByRef(DmdParameterInfo param) {
			if ((object)param == null)
				return;
			var type = param.ParameterType;
			if (!type.IsByRef)
				return;
			if (!param.IsIn && param.IsOut) {
				OutputWrite(Keyword_out, DbgTextColor.Keyword);
				WriteSpace();
			}
			else if (!param.IsIn && !param.IsOut && TypeFormatterUtils.IsReadOnlyParameter(param)) {
				OutputWrite(Keyword_in, DbgTextColor.Keyword);
				WriteSpace();
			}
			else {
				OutputWrite(Keyword_ref, DbgTextColor.Keyword);
				WriteSpace();
			}
		}

		bool FormatValue(uint index, bool needSpace) {
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			DbgDotNetValueResult parameterValue = default;
			DbgDotNetValue dereferencedValue = null;
			try {
				parameterValue = runtime.GetParameterValue(evalInfo, index);
				if (parameterValue.IsNormalResult) {
					if (needSpace) {
						WriteSpace();
						OutputWrite("=", DbgTextColor.Operator);
						WriteSpace();
					}
					needSpace = true;

					var valueFormatter = new CSharpValueFormatter(output, evalInfo, languageFormatter, valueOptions, cultureInfo);
					var value = parameterValue.Value;
					if (value.Type.IsByRef)
						value = dereferencedValue = value.LoadIndirect().Value;
					if (value == null)
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
			var primitiveFormatter = new CSharpPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);

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

			if (loc != null) {
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
			WritePeriod();
			OutputWrite(keyword, DbgTextColor.Keyword);
		}

		void Format(DmdMethodBase method, DmdPropertyInfo property, AccessorKind accessorKind) {
			if (ReturnTypes) {
				FormatReturnType(property.PropertyType, TypeFormatterUtils.IsReadOnlyProperty(property));
				WriteSpace();
			}
			if (DeclaringTypes) {
				FormatType(property.DeclaringType);
				WritePeriod();
			}
			if (property.GetIndexParameters().Count != 0) {
				OutputWrite(Keyword_this, DbgTextColor.Keyword);
				WriteToken(property);
				WriteMethodParameterListCore(method, GetAllMethodParameterTypes(property.GetMethodSignature()), IndexerParenOpen, IndexerParenClose, showParameterTypes: true, showParameterNames: false, showParameterValues: false);
			}
			else {
				WriteIdentifier(property.Name, TypeFormatterUtils.GetColor(property));
				WriteToken(property);
			}
			WriteAccessor(accessorKind);
			WriteToken(method);
			WriteGenericArguments(method);
			WriteMethodParameterList(method, MethodParenOpen, MethodParenClose);
			WriteOffset();
		}

		void Format(DmdMethodBase method, DmdEventInfo @event, AccessorKind accessorKind) {
			if (DeclaringTypes) {
				FormatType(@event.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(@event.Name, TypeFormatterUtils.GetColor(@event));
			WriteToken(@event);
			WriteAccessor(accessorKind);
			WriteToken(method);
			WriteGenericArguments(method);
			WriteMethodParameterList(method, MethodParenOpen, MethodParenClose);
			WriteOffset();
		}

		void Format(DmdMethodBase method) {
			if (StateMachineUtils.TryGetKickoffMethod(method, out var kickoffMethod))
				method = kickoffMethod;

			var sig = method.GetMethodSignature();

			string[] operatorInfo;
			if (method is DmdConstructorInfo)
				operatorInfo = null;
			else
				operatorInfo = Operators.TryGetOperatorInfo(method.Name);
			bool isExplicitOrImplicit = operatorInfo != null && (operatorInfo[0] == "explicit" || operatorInfo[0] == "implicit");

			if (!isExplicitOrImplicit) {
				if (ReturnTypes && !(method is DmdConstructorInfo)) {
					FormatReturnType(sig.ReturnType, TypeFormatterUtils.IsReadOnlyMethod(method));
					WriteSpace();
				}
			}

			if (DeclaringTypes) {
				FormatType(method.DeclaringType);
				WritePeriod();
			}
			if (method is DmdConstructorInfo)
				WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(method.DeclaringType.MetadataName), TypeFormatterUtils.GetColor(method, canBeModule: false));
			else {
				if (TypeFormatterUtils.TryGetMethodName(method.Name, out var containingMethodName, out var localFunctionName)) {
					var methodColor = TypeFormatterUtils.GetColor(method, canBeModule: false);
					WriteIdentifier(containingMethodName, methodColor);
					WritePeriod();
					WriteIdentifier(localFunctionName, methodColor);
				}
				else
					WriteMethodName(method, method.Name, operatorInfo);
			}
			if (isExplicitOrImplicit) {
				WriteToken(method);
				WriteSpace();
				FormatType(sig.ReturnType);
			}
			else
				WriteToken(method);

			WriteGenericArguments(method);
			WriteMethodParameterList(method, MethodParenOpen, MethodParenClose);
			WriteOffset();
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'a' <= s[0] && s[0] <= 'z' ? DbgTextColor.Keyword : DbgTextColor.Operator);

		void WriteMethodName(DmdMethodBase method, string name, string[] operatorInfo) {
			if (operatorInfo != null) {
				for (int i = 0; i < operatorInfo.Length; i++) {
					if (i > 0)
						WriteSpace();
					var s = operatorInfo[i];
					WriteOperatorInfoString(s);
				}
			}
			else
				WriteIdentifier(name, TypeFormatterUtils.GetColor(method, canBeModule: false));
		}
	}
}
