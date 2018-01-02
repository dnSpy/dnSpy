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

using System.Globalization;
using System.Text;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Debugger.Formatters;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes.CSharp {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.CSharp)]
	sealed class CSharpValueNodeFactory : LanguageValueNodeFactory {
		internal const TypeFormatterOptions TypeFormatterOptions = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;

		protected override bool SupportsModuleTypes => false;
		protected override DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory() => new CSharpValueNodeProviderFactory(this);
		protected override bool IsIdentifierPartCharacter(char c) => UnicodeCharacterUtilities.IsIdentifierPartCharacter(c);

		void AddCastBegin(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
			sb.Append("((");
			new Formatters.CSharp.CSharpTypeFormatter(new StringBuilderTextColorOutput(sb), TypeFormatterOptions, null).Format(castType, null);
			sb.Append(')');
		}

		void AddCastEnd(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
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
			sb.Append('[');
			sb.Append(index.ToString());
			sb.Append(']');
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int[] indexes, DmdType castType, bool addParens) {
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

		protected override void FormatReturnValueMethodName(DbgEvaluationInfo evalInfo, ITextColorWriter output, DbgValueFormatterTypeOptions typeOptions, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, DmdMethodBase method, DmdPropertyInfo property) {
			var typeFormatter = new Formatters.CSharp.CSharpTypeFormatter(output, typeOptions.ToTypeFormatterOptions(), null);
			typeFormatter.Format(method.DeclaringType, null);
			var valueFormatter = new Formatters.CSharp.CSharpPrimitiveValueFormatter(output, valueOptions.ToValueFormatterOptions(), cultureInfo);
			output.Write(BoxedTextColor.Operator, ".");
			if ((object)property != null) {
				output.Write(MemberUtils.GetColor(property), Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(property.Name));
				valueFormatter.WriteTokenComment(property.MetadataToken);
				output.Write(BoxedTextColor.Operator, ".");
				output.Write(BoxedTextColor.Keyword, "get");
				valueFormatter.WriteTokenComment(method.MetadataToken);
			}
			else {
				var methodColor = TypeFormatterUtils.GetColor(method, canBeModule: false);
				if (TypeFormatterUtils.TryGetMethodName(method.Name, out var containingMethodName, out var localFunctionName)) {
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(containingMethodName));
					output.Write(BoxedTextColor.Operator, ".");
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(localFunctionName));
					valueFormatter.WriteTokenComment(method.MetadataToken);
				}
				else {
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(method.Name));
					valueFormatter.WriteTokenComment(method.MetadataToken);
				}
			}
		}
	}
}
