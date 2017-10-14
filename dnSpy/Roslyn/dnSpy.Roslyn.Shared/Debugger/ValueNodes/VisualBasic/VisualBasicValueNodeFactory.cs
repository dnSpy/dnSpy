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

using System.Text;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes.VisualBasic {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.VisualBasic)]
	sealed class VisualBasicValueNodeFactory : LanguageValueNodeFactory {
		const Formatters.TypeFormatterOptions typeFormatterOptions = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;

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
			new Formatters.VisualBasic.VisualBasicTypeFormatter(new StringBuilderTextColorOutput(sb), typeFormatterOptions, null).Format(castType, null);
			sb.Append(')');
		}

		public override string GetFieldExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetPropertyExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			var sb = ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int index, DmdType castType, bool addParens) {
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

		public override string EscapeIdentifier(string identifier) => Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(identifier);

		protected override void FormatReturnValueMethodName(ITextColorWriter output, DmdMethodBase method, DmdPropertyInfo property) {
			const Formatters.TypeFormatterOptions options = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;
			var formatter = new Formatters.VisualBasic.VisualBasicTypeFormatter(output, options, null);
			formatter.Format(method.DeclaringType, null);
			output.Write(BoxedTextColor.Operator, ".");
			if ((object)property != null) {
				output.Write(MemberUtils.GetColor(property), Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(property.Name));
				output.Write(BoxedTextColor.Operator, ".");
				output.Write(BoxedTextColor.Keyword, "Get");
			}
			else
				output.Write(MemberUtils.GetColor(method, canBeModule: true), Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(method.Name));
		}
	}
}
