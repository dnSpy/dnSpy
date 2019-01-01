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

using System.Globalization;
using System.Text;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Debugger.Formatters;

namespace dnSpy.Roslyn.Debugger.ValueNodes.VisualBasic {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.VisualBasic)]
	sealed class VisualBasicValueNodeFactory : LanguageValueNodeFactory {
		internal const TypeFormatterOptions TypeFormatterOptions = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;
		const string GenericsParenOpen = "(";
		const string GenericsParenClose = ")";
		const string Keyword_Of = "Of";

		protected override bool SupportsModuleTypes => true;
		protected override DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory() => new VisualBasicValueNodeProviderFactory(this);
		protected override bool IsIdentifierPartCharacter(char c) => SyntaxFacts.IsIdentifierPartCharacter(c);

		void AddCastBegin(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
			sb.Append("CType(");
		}

		void AddCastEnd(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
			sb.Append(", ");
			new Formatters.VisualBasic.VisualBasicTypeFormatter(new DbgStringBuilderTextWriter(sb), TypeFormatterOptions, null).Format(castType, null);
			sb.Append(')');
		}

		public override string GetFieldExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetPropertyExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int index, DmdType castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('(');
			sb.Append(index.ToString());
			sb.Append(')');
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int[] indexes, DmdType castType, bool addParens) {
			baseExpression = RemoveFormatSpecifiers(baseExpression);
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('(');
			for (int i = 0; i < indexes.Length; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(indexes[i].ToString());
			}
			sb.Append(')');
			return ObjectCache.FreeAndToString(ref sb);
		}

		protected override string EscapeIdentifier(string identifier) => Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(identifier);

		protected override void FormatReturnValueMethodName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValueFormatterTypeOptions typeOptions, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, DmdMethodBase method, DmdPropertyInfo property) {
			var typeFormatter = new Formatters.VisualBasic.VisualBasicTypeFormatter(output, typeOptions.ToTypeFormatterOptions(), null);
			typeFormatter.Format(method.DeclaringType, null);
			var valueFormatter = new Formatters.VisualBasic.VisualBasicPrimitiveValueFormatter(output, valueOptions.ToValueFormatterOptions(), cultureInfo);
			output.Write(DbgTextColor.Operator, ".");
			if ((object)property != null) {
				output.Write(MemberUtils.GetColor(property), Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(property.Name));
				valueFormatter.WriteTokenComment(property.MetadataToken);
				output.Write(DbgTextColor.Operator, ".");
				output.Write(DbgTextColor.Keyword, "Get");
				valueFormatter.WriteTokenComment(method.MetadataToken);
			}
			else {
				var operatorInfo = Formatters.VisualBasic.Operators.TryGetOperatorInfo(method.Name);
				if (operatorInfo != null && method is DmdMethodInfo methodInfo) {
					for (int i = 0; i < operatorInfo.Length; i++) {
						if (i > 0)
							output.Write(DbgTextColor.Text, " ");
						var s = operatorInfo[i];
						output.Write('A' <= s[0] && s[0] <= 'Z' ? DbgTextColor.Keyword : DbgTextColor.Operator, s);
					}
					WriteGenericMethodArguments(output, method, typeFormatter);
				}
				else {
					output.Write(TypeFormatterUtils.GetColor(method, canBeModule: true), Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(method.Name));
					valueFormatter.WriteTokenComment(method.MetadataToken);
					WriteGenericMethodArguments(output, method, typeFormatter);
				}
			}
		}

		void WriteGenericMethodArguments(IDbgTextWriter output, DmdMethodBase method, Formatters.VisualBasic.VisualBasicTypeFormatter typeFormatter) {
			var genArgs = method.GetGenericArguments();
			if (genArgs.Count == 0)
				return;
			output.Write(DbgTextColor.Punctuation, GenericsParenOpen);
			output.Write(DbgTextColor.Keyword, Keyword_Of);
			output.Write(DbgTextColor.Text, " ");
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
