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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters.VisualBasic {
	struct VisualBasicStackFrameFormatter {
		readonly ITextColorWriter output;
		readonly DbgEvaluationContext context;
		readonly LanguageFormatter languageFormatter;
		readonly DbgStackFrameFormatterOptions options;
		readonly ValueFormatterOptions valueOptions;
		readonly CultureInfo cultureInfo;
		/*readonly*/ CancellationToken cancellationToken;

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

		static readonly Dictionary<string, string[]> nameToOperatorName = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "op_UnaryPlus", "Operator +".Split(' ') },
			{ "op_UnaryNegation", "Operator -".Split(' ') },
			{ "op_False", "Operator IsFalse".Split(' ') },
			{ "op_True", "Operator IsTrue".Split(' ') },
			{ "op_OnesComplement", "Operator Not".Split(' ') },
			{ "op_Addition", "Operator +".Split(' ') },
			{ "op_Subtraction", "Operator -".Split(' ') },
			{ "op_Multiply", "Operator *".Split(' ') },
			{ "op_Division", "Operator /".Split(' ') },
			{ "op_IntegerDivision", @"Operator \".Split(' ') },
			{ "op_Concatenate", "Operator &".Split(' ') },
			{ "op_Exponent", "Operator ^".Split(' ') },
			{ "op_RightShift", "Operator >>".Split(' ') },
			{ "op_LeftShift", "Operator <<".Split(' ') },
			{ "op_Equality", "Operator =".Split(' ') },
			{ "op_Inequality", "Operator <>".Split(' ') },
			{ "op_GreaterThan", "Operator >".Split(' ') },
			{ "op_GreaterThanOrEqual", "Operator >=".Split(' ') },
			{ "op_LessThan", "Operator <".Split(' ') },
			{ "op_LessThanOrEqual", "Operator <=".Split(' ') },
			{ "op_BitwiseAnd", "Operator And".Split(' ') },
			{ "op_Like", "Operator Like".Split(' ') },
			{ "op_Modulus", "Operator Mod".Split(' ') },
			{ "op_BitwiseOr", "Operator Or".Split(' ') },
			{ "op_ExclusiveOr", "Operator Xor".Split(' ') },
			{ "op_Implicit", "Widening Operator CType".Split(' ') },
			{ "op_Explicit", "Narrowing Operator CType".Split(' ') },
		};

		public VisualBasicStackFrameFormatter(ITextColorWriter output, DbgEvaluationContext context, LanguageFormatter languageFormatter, DbgStackFrameFormatterOptions options, ValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.context = context ?? throw new ArgumentNullException(nameof(context));
			this.languageFormatter = languageFormatter ?? throw new ArgumentNullException(nameof(languageFormatter));
			this.options = options;
			this.valueOptions = valueOptions;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			this.cancellationToken = cancellationToken;
		}

		void OutputWrite(string s, object color) => output.Write(color, s);
		void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);
		void WritePeriod() => OutputWrite(".", BoxedTextColor.Operator);
		void WriteIdentifier(string id, object color) => OutputWrite(VisualBasicTypeFormatter.GetFormattedIdentifier(id), color);

		void WriteCommaSpace() {
			OutputWrite(",", BoxedTextColor.Punctuation);
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
			OutputWrite(CommentBegin + tokenString + CommentEnd, BoxedTextColor.Comment);
		}

		bool NeedThreadSwitch(DbgStackFrame frame) {
			if (!ParameterValues)
				return false;
			if (frame.IsClosed)
				return false;
			var runtime = frame.Runtime.GetDotNetRuntime();
			if (runtime.Dispatcher.CheckAccess())
				return false;
			var sig = runtime.GetFrameMethod(context, frame, cancellationToken)?.GetMethodSignature();
			return sig != null && (sig.GetParameterTypes().Count > 0 || sig.GetVarArgsParameterTypes().Count > 0);
		}

		public void Format(DbgStackFrame frame) {
			// Minimize thread switches by switching to the debug engine thread
			if (NeedThreadSwitch(frame))
				FormatInvoke(frame);
			else
				FormatCore(frame);
		}

		void FormatInvoke(DbgStackFrame frame2) {
			var @this = this;
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => @this.FormatCore(frame2));
		}

		void FormatCore(DbgStackFrame frame) {
			if (ModuleNames) {
				OutputWrite(frame.Module?.Name ?? "???", BoxedTextColor.AssemblyModule);
				OutputWrite("!", BoxedTextColor.Operator);
			}

			var runtime = context.Runtime.GetDotNetRuntime();
			var method = runtime.GetFrameMethod(context, frame, cancellationToken);
			if ((object)method == null)
				OutputWrite("???", BoxedTextColor.Error);
			else {
				var propInfo = TypeFormatterUtils.TryGetProperty(method);
				if (propInfo.kind != AccessorKind.None) {
					Format(frame, method, propInfo.property, propInfo.kind);
					return;
				}

				var eventInfo = TypeFormatterUtils.TryGetEvent(method);
				if (eventInfo.kind != AccessorKind.None) {
					Format(frame, method, eventInfo.@event, eventInfo.kind);
					return;
				}

				Format(frame, method);
			}
		}

		void WriteGenericArguments(DmdMethodBase method) {
			var genArgs = method.GetGenericArguments();
			if (genArgs.Count == 0)
				return;
			OutputWrite(GenericsParenOpen, BoxedTextColor.Punctuation);
			OutputWrite(Keyword_Of, BoxedTextColor.Keyword);
			WriteSpace();
			for (int i = 0; i < genArgs.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				FormatType(genArgs[i]);
			}
			OutputWrite(GenericsParenClose, BoxedTextColor.Punctuation);
		}

		void WriteMethodParameterList(DbgStackFrame frame, DmdMethodBase method, string openParen, string closeParen) =>
			WriteMethodParameterListCore(frame, method, GetAllMethodParameterTypes(method.GetMethodSignature()), openParen, closeParen, ParameterTypes, ParameterNames, ParameterValues);

		void WriteMethodParameterListCore(DbgStackFrame frame, DmdMethodBase method, IList<DmdType> parameterTypes, string openParen, string closeParen, bool showParameterTypes, bool showParameterNames, bool showParameterValues) {
			if (!showParameterTypes && !showParameterNames && !showParameterValues)
				return;

			OutputWrite(openParen, BoxedTextColor.Punctuation);

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
						parameterType = parameterType.GetElementType();
						OutputWrite(Keyword_ByRef, BoxedTextColor.Keyword);
						WriteSpace();
					}

					if (param?.IsDefined("System.ParamArrayAttribute", false) == true) {
						OutputWrite(Keyword_params, BoxedTextColor.Keyword);
						needSpace = true;
					}
				}

				if (showParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if (!string.IsNullOrEmpty(param?.Name))
						WriteIdentifier(param.Name, BoxedTextColor.Parameter);
					else
						WriteIdentifier("A_" + (baseIndex + i).ToString(), BoxedTextColor.Parameter);
				}
				if (showParameterTypes) {
					if (showParameterNames) {
						WriteSpace();
						OutputWrite(Keyword_As, BoxedTextColor.Keyword);
					}
					if (needSpace)
						WriteSpace();
					needSpace = true;

					FormatType(parameterType);
				}
				if (showParameterValues)
					needSpace = FormatValue(frame, (uint)(baseIndex + i), needSpace);
			}

			OutputWrite(closeParen, BoxedTextColor.Punctuation);
		}

		bool FormatValue(DbgStackFrame frame, uint index, bool needSpace) {
			var runtime = context.Runtime.GetDotNetRuntime();
			DbgDotNetValueResult parameterValue = default;
			DbgDotNetValue dereferencedValue = null;
			try {
				parameterValue = runtime.GetParameterValue(context, frame, index, cancellationToken);
				if (parameterValue.IsNormalResult) {
					if (needSpace) {
						WriteSpace();
						OutputWrite("=", BoxedTextColor.Operator);
						WriteSpace();
					}
					needSpace = true;

					var valueFormatter = new VisualBasicValueFormatter(output, context, frame, languageFormatter, valueOptions, cultureInfo, cancellationToken);
					var value = parameterValue.Value;
					if (value.Type.IsByRef)
						value = dereferencedValue = value.LoadIndirect();
					if (value == null)
						OutputWrite("???", BoxedTextColor.Error);
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

		void WriteOffset(DbgStackFrame frame) {
			if (!ShowIP)
				return;
			WriteSpace();
			OutputWrite("(", BoxedTextColor.Punctuation);
			OutputWrite("IL", BoxedTextColor.Text);
			OutputWrite("=", BoxedTextColor.Operator);
			var primitiveFormatter = new VisualBasicPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);
			if (frame.FunctionOffset <= ushort.MaxValue)
				primitiveFormatter.FormatUInt16((ushort)frame.FunctionOffset);
			else
				primitiveFormatter.FormatUInt32(frame.FunctionOffset);

			if (frame.Location is IDbgDotNetCodeLocation loc) {
				var addr = loc.NativeAddress;
				if (addr.Address != 0) {
					WriteCommaSpace();
					OutputWrite("Native", BoxedTextColor.Text);
					OutputWrite("=", BoxedTextColor.Operator);
					if (frame.Runtime.Process.PointerSize == 4)
						primitiveFormatter.FormatUInt32((uint)addr.Address);
					else
						primitiveFormatter.FormatUInt64(addr.Address);
					long offs = (long)addr.Offset;
					if (offs < 0) {
						offs = -offs;
						OutputWrite("-", BoxedTextColor.Operator);
					}
					else
						OutputWrite("+", BoxedTextColor.Operator);
					primitiveFormatter.FormatFewDigits((ulong)offs);
				}
			}
			OutputWrite(")", BoxedTextColor.Punctuation);
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
			OutputWrite(keyword, BoxedTextColor.Keyword);
			WriteSpace();
		}

		void Format(DbgStackFrame frame, DmdMethodBase method, DmdPropertyInfo property, AccessorKind accessorKind) {
			if ((object)property.SetMethod == null) {
				OutputWrite(Keyword_ReadOnly, BoxedTextColor.Keyword);
				WriteSpace();
			}

			OutputWrite(Keyword_Property, BoxedTextColor.Keyword);
			WriteSpace();

			WriteAccessor(accessorKind);
			WriteToken(method);

			if (DeclaringTypes) {
				FormatType(property.DeclaringType);
				WritePeriod();
			}

			WriteIdentifier(property.Name, TypeFormatterUtils.GetColor(property));
			WriteToken(property);
			WriteGenericArguments(method);
			WriteMethodParameterList(frame, method, MethodParenOpen, MethodParenClose);
			WriteReturnType(method);
			WriteOffset(frame);
		}

		void Format(DbgStackFrame frame, DmdMethodBase method, DmdEventInfo @event, AccessorKind accessorKind) {
			OutputWrite(Keyword_Event, BoxedTextColor.Keyword);
			WriteSpace();

			WriteAccessor(accessorKind);
			WriteToken(method);

			if (DeclaringTypes) {
				FormatType(@event.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(@event.Name, TypeFormatterUtils.GetColor(@event));
			WriteToken(@event);
			WriteSpace();
			OutputWrite(Keyword_As, BoxedTextColor.Keyword);
			WriteSpace();
			FormatType(@event.EventHandlerType);
			WriteOffset(frame);
		}

		void Format(DbgStackFrame frame, DmdMethodBase method) {
			if (StateMachineUtils.TryGetKickoffMethod(method, out var kickoffMethod))
				method = kickoffMethod;

			var sig = method.GetMethodSignature();

			string[] operatorInfo;
			if (method is DmdConstructorInfo)
				operatorInfo = null;
			else
				operatorInfo = TryGetOperatorInfo(method.Name);

			if (operatorInfo != null) {
				for (int i = 0; i < operatorInfo.Length - 1; i++) {
					WriteOperatorInfoString(operatorInfo[i]);
					WriteSpace();
				}
			}
			else {
				bool isSub = method.GetMethodSignature().ReturnType == method.AppDomain.System_Void;
				OutputWrite(isSub ? Keyword_Sub : Keyword_Function, BoxedTextColor.Keyword);
				WriteSpace();
			}

			if (DeclaringTypes) {
				FormatType(method.DeclaringType);
				WritePeriod();
			}
			if (method is DmdConstructorInfo)
				OutputWrite(Keyword_New, BoxedTextColor.Keyword);
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
			WriteMethodParameterList(frame, method, MethodParenOpen, MethodParenClose);
			WriteReturnType(method);
			WriteOffset(frame);
		}

		static string[] TryGetOperatorInfo(string name) {
			nameToOperatorName.TryGetValue(name, out var list);
			return list;
		}

		void WriteReturnType(DmdMethodBase method) {
			if (!ReturnTypes)
				return;

			var sig = method.GetMethodSignature();
			if (sig.ReturnType == method.AppDomain.System_Void)
				return;

			WriteSpace();
			OutputWrite(Keyword_As, BoxedTextColor.Keyword);
			WriteSpace();
			FormatType(sig.ReturnType);
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'A' <= s[0] && s[0] <= 'Z' ? BoxedTextColor.Keyword : BoxedTextColor.Operator);

		void WriteMethodName(DmdMethodBase method, string name, string[] operatorInfo) {
			if (operatorInfo != null)
				WriteOperatorInfoString(operatorInfo[operatorInfo.Length - 1]);
			else
				WriteIdentifier(name, TypeFormatterUtils.GetColor(method, canBeModule: true));
		}
	}
}
