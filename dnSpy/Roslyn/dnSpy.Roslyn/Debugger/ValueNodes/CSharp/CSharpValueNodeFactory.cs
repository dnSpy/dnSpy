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

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Debugger.Formatters;

namespace dnSpy.Roslyn.Debugger.ValueNodes.CSharp {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.CSharp)]
	sealed class CSharpValueNodeFactory : LanguageValueNodeFactory {
		internal const TypeFormatterOptions TypeFormatterOptions = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;
		const string Keyword_this = "this";
		const string Keyword_params = "params";
		const string Keyword_out = "out";
		const string Keyword_ref = "ref";
		const string Keyword_in = "in";
		const string GenericsParenOpen = "<";
		const string GenericsParenClose = ">";
		const string IndexerParenOpen = "[";
		const string IndexerParenClose = "]";

		protected override bool SupportsModuleTypes => false;
		protected override DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory() => new CSharpValueNodeProviderFactory(this);
		protected override bool IsIdentifierPartCharacter(char c) => Utilities.UnicodeCharacterUtilities.IsIdentifierPartCharacter(c);

		void AddCastBegin(StringBuilder sb, DmdType? castType) {
			if (castType is null)
				return;
			sb.Append("((");
			new Formatters.CSharp.CSharpTypeFormatter(new DbgStringBuilderTextWriter(sb), TypeFormatterOptions, null).Format(castType, null);
			sb.Append(')');
		}

		void AddCastEnd(StringBuilder sb, DmdType? castType) {
			if (castType is null)
				return;
			sb.Append(')');
		}

		public override string GetFieldExpression(string baseExpression, string name, DmdType? castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetPropertyExpression(string baseExpression, string name, DmdType? castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int index, DmdType? castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('[');
			sb.Append(index.ToString());
			sb.Append(']');
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int[] indexes, DmdType? castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('[');
			for (int i = 0; i < indexes.Length; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(indexes[i].ToString());
			}
			sb.Append(']');
			return ObjectCache.FreeAndToString(ref sb);
		}

		protected override string EscapeIdentifier(string identifier) => Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(identifier);

		protected override void FormatReturnValueMethodName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterTypeOptions typeOptions, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo, DmdMethodBase method, DmdPropertyInfo? property) {
			var typeFormatter = new Formatters.CSharp.CSharpTypeFormatter(output, typeOptions.ToTypeFormatterOptions(), null);
			typeFormatter.Format(method.DeclaringType!, null);
			var valueFormatter = new Formatters.CSharp.CSharpPrimitiveValueFormatter(output, valueOptions.ToValueFormatterOptions(), cultureInfo);
			output.Write(DbgTextColor.Operator, ".");
			if (property is not null) {
				if (property.GetIndexParameters().Count != 0) {
					output.Write(DbgTextColor.Keyword, Keyword_this);
					WriteMethodParameterList(output, method, typeFormatter, GetAllMethodParameterTypes(property.GetMethodSignature()), IndexerParenOpen, IndexerParenClose);
				}
				else
					output.Write(MemberUtils.GetColor(property), Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(property.Name));
				valueFormatter.WriteTokenComment(property.MetadataToken);
				output.Write(DbgTextColor.Operator, ".");
				output.Write(DbgTextColor.Keyword, "get");
				valueFormatter.WriteTokenComment(method.MetadataToken);
			}
			else {
				var methodColor = TypeFormatterUtils.GetColor(method, canBeModule: false);
				if (TypeFormatterUtils.TryGetMethodName(method.Name, out var containingMethodName, out var localFunctionName)) {
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(containingMethodName));
					output.Write(DbgTextColor.Operator, ".");
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(localFunctionName));
					valueFormatter.WriteTokenComment(method.MetadataToken);
					WriteGenericMethodArguments(output, method, typeFormatter);
				}
				else {
					var operatorInfo = Formatters.CSharp.Operators.TryGetOperatorInfo(method.Name);
					if (operatorInfo is not null && method is DmdMethodInfo methodInfo) {
						bool isExplicitOrImplicit = operatorInfo[0] == "explicit" || operatorInfo[0] == "implicit";

						for (int i = 0; i < operatorInfo.Length; i++) {
							if (i > 0)
								output.Write(DbgTextColor.Text, " ");
							var s = operatorInfo[i];
							output.Write('a' <= s[0] && s[0] <= 'z' ? DbgTextColor.Keyword : DbgTextColor.Operator, s);
						}

						valueFormatter.WriteTokenComment(method.MetadataToken);
						WriteGenericMethodArguments(output, method, typeFormatter);
						if (isExplicitOrImplicit) {
							output.Write(DbgTextColor.Text, " ");
							typeFormatter.Format(methodInfo.ReturnType, null);
						}
					}
					else {
						output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(method.Name));
						valueFormatter.WriteTokenComment(method.MetadataToken);
						WriteGenericMethodArguments(output, method, typeFormatter);
					}
				}
			}
		}

		static IList<DmdType> GetAllMethodParameterTypes(DmdMethodSignature sig) {
			if (sig.GetVarArgsParameterTypes().Count == 0)
				return sig.GetParameterTypes();
			var list = new List<DmdType>(sig.GetParameterTypes().Count + sig.GetVarArgsParameterTypes().Count);
			list.AddRange(sig.GetParameterTypes());
			list.AddRange(sig.GetVarArgsParameterTypes());
			return list;
		}

		void WriteMethodParameterList(IDbgTextWriter output, DmdMethodBase method, Formatters.CSharp.CSharpTypeFormatter typeFormatter, IList<DmdType> parameterTypes, string openParen, string closeParen) {
			output.Write(DbgTextColor.Punctuation, openParen);

			int baseIndex = method.IsStatic ? 0 : 1;
			var parameters = method.GetParameters();
			for (int i = 0; i < parameterTypes.Count; i++) {
				if (i > 0) {
					output.Write(DbgTextColor.Punctuation, ",");
					output.Write(DbgTextColor.Text, " ");
				}

				var param = i < parameters.Count ? parameters[i] : null;
				if (param?.IsDefined("System.ParamArrayAttribute", false) == true) {
					output.Write(DbgTextColor.Keyword, Keyword_params);
					output.Write(DbgTextColor.Text, " ");
				}
				var parameterType = parameterTypes[i];
				WriteRefIfByRef(output, param);
				if (parameterType.IsByRef)
					parameterType = parameterType.GetElementType()!;
				typeFormatter.Format(parameterType, null);
			}

			output.Write(DbgTextColor.Punctuation, closeParen);
		}

		void WriteRefIfByRef(IDbgTextWriter output, DmdParameterInfo? param) {
			if (param is null)
				return;
			var type = param.ParameterType;
			if (!type.IsByRef)
				return;
			if (!param.IsIn && param.IsOut) {
				output.Write(DbgTextColor.Keyword, Keyword_out);
				output.Write(DbgTextColor.Text, " ");
			}
			else if (!param.IsIn && !param.IsOut && TypeFormatterUtils.IsReadOnlyParameter(param)) {
				output.Write(DbgTextColor.Keyword, Keyword_in);
				output.Write(DbgTextColor.Text, " ");
			}
			else {
				output.Write(DbgTextColor.Keyword, Keyword_ref);
				output.Write(DbgTextColor.Text, " ");
			}
		}

		void WriteGenericMethodArguments(IDbgTextWriter output, DmdMethodBase method, Formatters.CSharp.CSharpTypeFormatter typeFormatter) {
			var genArgs = method.GetGenericArguments();
			if (genArgs.Count == 0)
				return;
			output.Write(DbgTextColor.Punctuation, GenericsParenOpen);
			for (int i = 0; i < genArgs.Count; i++) {
				if (i > 0) {
					output.Write(DbgTextColor.Punctuation, ",");
					output.Write(DbgTextColor.Text, " ");
				}
				typeFormatter.Format(genArgs[i], null);
			}
			output.Write(DbgTextColor.Punctuation, GenericsParenClose);
		}
	}
}
