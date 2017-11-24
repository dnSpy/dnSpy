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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters.CSharp {
	struct CSharpStackFrameFormatter {
		readonly ITextColorWriter output;
		readonly DbgEvaluationContext context;
		readonly LanguageFormatter languageFormatter;
		readonly DbgStackFrameFormatterOptions options;
		readonly ValueFormatterOptions valueOptions;
		readonly CultureInfo cultureInfo;
		/*readonly*/ CancellationToken cancellationToken;

		const string Keyword_this = "this";
		const string Keyword_params = "params";
		const string Keyword_get = "get";
		const string Keyword_set = "set";
		const string Keyword_add = "add";
		const string Keyword_remove = "remove";
		const string Keyword_out = "out";
		const string Keyword_ref = "ref";
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

		static readonly Dictionary<string, string[]> nameToOperatorName = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "op_Addition", "operator +".Split(' ') },
			{ "op_BitwiseAnd", "operator &".Split(' ') },
			{ "op_BitwiseOr", "operator |".Split(' ') },
			{ "op_Decrement", "operator --".Split(' ') },
			{ "op_Division", "operator /".Split(' ') },
			{ "op_Equality", "operator ==".Split(' ') },
			{ "op_ExclusiveOr", "operator ^".Split(' ') },
			{ "op_Explicit", "explicit operator".Split(' ') },
			{ "op_False", "operator false".Split(' ') },
			{ "op_GreaterThan", "operator >".Split(' ') },
			{ "op_GreaterThanOrEqual", "operator >=".Split(' ') },
			{ "op_Implicit", "implicit operator".Split(' ') },
			{ "op_Increment", "operator ++".Split(' ') },
			{ "op_Inequality", "operator !=".Split(' ') },
			{ "op_LeftShift", "operator <<".Split(' ') },
			{ "op_LessThan", "operator <".Split(' ') },
			{ "op_LessThanOrEqual", "operator <=".Split(' ') },
			{ "op_LogicalNot", "operator !".Split(' ') },
			{ "op_Modulus", "operator %".Split(' ') },
			{ "op_Multiply", "operator *".Split(' ') },
			{ "op_OnesComplement", "operator ~".Split(' ') },
			{ "op_RightShift", "operator >>".Split(' ') },
			{ "op_Subtraction", "operator -".Split(' ') },
			{ "op_True", "operator true".Split(' ') },
			{ "op_UnaryNegation", "operator -".Split(' ') },
			{ "op_UnaryPlus", "operator +".Split(' ') },
		};

		public CSharpStackFrameFormatter(ITextColorWriter output, DbgEvaluationContext context, LanguageFormatter languageFormatter, DbgStackFrameFormatterOptions options, ValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) {
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
		void WriteIdentifier(string id, object color) => OutputWrite(CSharpTypeFormatter.GetFormattedIdentifier(id), color);

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
			new CSharpTypeFormatter(output, typeOptions, cultureInfo).Format(type, null);
		}

		void WriteToken(DmdMemberInfo member) {
			if (!Tokens)
				return;
			var primitiveFormatter = new CSharpPrimitiveValueFormatter(output, GetValueFormatterOptions() & ~ValueFormatterOptions.Decimal, cultureInfo);
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

				var param = i < parameters.Count ? parameters[i] : null;

				bool needSpace = false;
				if (showParameterTypes) {
					needSpace = true;

					if (param?.IsDefined("System.ParamArrayAttribute", false) == true) {
						OutputWrite(Keyword_params, BoxedTextColor.Keyword);
						WriteSpace();
					}
					var paramterType = parameterTypes[i];
					WriteRefIfByRef(param);
					if (paramterType.IsByRef)
						paramterType = paramterType.GetElementType();
					FormatType(paramterType);
				}
				if (showParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if ((object)param != null)
						WriteIdentifier(param.Name, BoxedTextColor.Parameter);
					else
						WriteIdentifier("A_" + (baseIndex + i).ToString(), BoxedTextColor.Parameter);
				}
				if (showParameterValues)
					needSpace = FormatValue(frame, (uint)(baseIndex + i), needSpace);
			}

			OutputWrite(closeParen, BoxedTextColor.Punctuation);
		}

		void WriteRefIfByRef(DmdParameterInfo param) {
			if ((object)param == null)
				return;
			var type = param.ParameterType;
			if (!type.IsByRef)
				return;
			if (!param.IsIn && param.IsOut) {
				OutputWrite(Keyword_out, BoxedTextColor.Keyword);
				WriteSpace();
			}
			else {
				OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
				WriteSpace();
			}
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

					var valueFormatter = new CSharpValueFormatter(output, context, frame, languageFormatter, valueOptions, cultureInfo, cancellationToken);
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
			//TODO:
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
			OutputWrite(keyword, BoxedTextColor.Keyword);
		}

		void Format(DbgStackFrame frame, DmdMethodBase method, DmdPropertyInfo property, AccessorKind accessorKind) {
			if (ReturnTypes) {
				FormatType(property.PropertyType);
				WriteSpace();
			}
			if (DeclaringTypes) {
				FormatType(property.DeclaringType);
				WritePeriod();
			}
			if (property.GetIndexParameters().Count != 0) {
				OutputWrite(Keyword_this, BoxedTextColor.Keyword);
				WriteToken(property);
				WriteMethodParameterListCore(frame, method, GetAllMethodParameterTypes(property.GetMethodSignature()), IndexerParenOpen, IndexerParenClose, showParameterTypes: true, showParameterNames: false, showParameterValues: false);
			}
			else {
				WriteIdentifier(property.Name, TypeFormatterUtils.GetColor(property));
				WriteToken(property);
			}
			WriteAccessor(accessorKind);
			WriteToken(method);
			WriteGenericArguments(method);
			WriteMethodParameterList(frame, method, MethodParenOpen, MethodParenClose);
			WriteOffset(frame);
		}

		void Format(DbgStackFrame frame, DmdMethodBase method, DmdEventInfo @event, AccessorKind accessorKind) {
			if (DeclaringTypes) {
				FormatType(@event.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(@event.Name, TypeFormatterUtils.GetColor(@event));
			WriteToken(@event);
			WriteAccessor(accessorKind);
			WriteToken(method);
			WriteGenericArguments(method);
			WriteMethodParameterList(frame, method, MethodParenOpen, MethodParenClose);
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
			bool isExplicitOrImplicit = operatorInfo != null && (operatorInfo[0] == "explicit" || operatorInfo[0] == "implicit");

			if (!isExplicitOrImplicit) {
				if (ReturnTypes && !(method is DmdConstructorInfo)) {
					FormatType(sig.ReturnType);
					WriteSpace();
				}
			}

			if (DeclaringTypes) {
				FormatType(method.DeclaringType);
				WritePeriod();
			}
			if (method is DmdConstructorInfo)
				WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(method.DeclaringType.Name), TypeFormatterUtils.GetColor(method, canBeModule: false));
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
			WriteMethodParameterList(frame, method, MethodParenOpen, MethodParenClose);
			WriteOffset(frame);
		}

		static string[] TryGetOperatorInfo(string name) {
			nameToOperatorName.TryGetValue(name, out var list);
			return list;
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'a' <= s[0] && s[0] <= 'z' ? BoxedTextColor.Keyword : BoxedTextColor.Operator);

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
