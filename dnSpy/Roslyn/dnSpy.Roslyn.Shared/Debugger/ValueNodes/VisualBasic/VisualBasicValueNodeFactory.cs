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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes.VisualBasic {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.VisualBasic)]
	sealed class VisualBasicValueNodeFactory : LanguageValueNodeFactory {
		protected override DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory() => new VisualBasicValueNodeProviderFactory(this);

		public override string GetFieldExpression(string baseExpression, string name) {
			//TODO: Add parens, casts, use -> if pointer etc...
			return baseExpression + "." + name;
		}

		public override string GetPropertyExpression(string baseExpression, string name) {
			return baseExpression + "." + name;
		}

		public override string GetExpression(string baseExpression, int index) {
			return baseExpression + "(" + index.ToString() + ")";
		}

		public override string GetExpression(string baseExpression, int[] indexes) {
			var sb = Formatters.ObjectCache.AllocStringBuilder();
			sb.Append(baseExpression);
			sb.Append('(');
			for (int i = 0; i < indexes.Length; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(indexes[i].ToString());
			}
			sb.Append(')');
			return Formatters.ObjectCache.FreeAndToString(ref sb);
		}

		public override string EscapeIdentifier(string identifier) => Formatters.VisualBasic.VisualBasicTypeFormatter.GetFormattedIdentifier(identifier);

		protected override void FormatReturnValueMethodName(ITextColorWriter output, DmdMethodBase method, DmdPropertyInfo property) {
			const Formatters.TypeFormatterOptions options = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;
			var formatter = new Formatters.VisualBasic.VisualBasicTypeFormatter(output, options);
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
