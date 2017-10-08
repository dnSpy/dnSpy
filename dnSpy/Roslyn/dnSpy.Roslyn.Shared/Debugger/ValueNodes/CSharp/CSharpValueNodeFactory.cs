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

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes.CSharp {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.CSharp)]
	sealed class CSharpValueNodeFactory : LanguageValueNodeFactory {
		const Formatters.TypeFormatterOptions typeFormatterOptions = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;

		protected override DbgDotNetValueNodeProviderFactory CreateValueNodeProviderFactory() => new CSharpValueNodeProviderFactory(this);

		protected override bool IsIdentifierPartCharacter(char c) => UnicodeCharacterUtilities.IsIdentifierPartCharacter(c);

		void AddCastBegin(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
			sb.Append("((");
			new Formatters.CSharp.CSharpTypeFormatter(new StringBuilderTextColorOutput(sb), typeFormatterOptions, null).Format(castType, null);
			sb.Append(')');
		}

		void AddCastEnd(StringBuilder sb, DmdType castType) {
			if ((object)castType == null)
				return;
			sb.Append(')');
		}

		public override string GetFieldExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			var sb = Formatters.ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return Formatters.ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetPropertyExpression(string baseExpression, string name, DmdType castType, bool addParens) {
			var sb = Formatters.ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('.');
			sb.Append(name);
			return Formatters.ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int index, DmdType castType, bool addParens) {
			var sb = Formatters.ObjectCache.AllocStringBuilder();
			AddCastBegin(sb, castType);
			AddParens(sb, baseExpression, addParens);
			AddCastEnd(sb, castType);
			sb.Append('[');
			sb.Append(index.ToString());
			sb.Append(']');
			return Formatters.ObjectCache.FreeAndToString(ref sb);
		}

		public override string GetExpression(string baseExpression, int[] indexes, DmdType castType, bool addParens) {
			var sb = Formatters.ObjectCache.AllocStringBuilder();
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
			return Formatters.ObjectCache.FreeAndToString(ref sb);
		}

		public override string EscapeIdentifier(string identifier) => Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(identifier);

		protected override void FormatReturnValueMethodName(ITextColorWriter output, DmdMethodBase method, DmdPropertyInfo property) {
			const Formatters.TypeFormatterOptions options = Formatters.TypeFormatterOptions.IntrinsicTypeKeywords | Formatters.TypeFormatterOptions.Namespaces;
			var formatter = new Formatters.CSharp.CSharpTypeFormatter(output, options, null);
			formatter.Format(method.DeclaringType, null);
			output.Write(BoxedTextColor.Operator, ".");
			if ((object)property != null) {
				output.Write(MemberUtils.GetColor(property), Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(property.Name));
				output.Write(BoxedTextColor.Operator, ".");
				output.Write(BoxedTextColor.Keyword, "get");
			}
			else {
				var methodColor = MemberUtils.GetColor(method, canBeModule: false);
				if (TryGetMethodName(method.Name, out var containingMethodName, out var localFunctionName)) {
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(containingMethodName));
					output.Write(BoxedTextColor.Operator, ".");
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(localFunctionName));
				}
				else
					output.Write(methodColor, Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(method.Name));
			}
		}

		static bool TryGetMethodName(string name, out string containingMethodName, out string localFunctionName) {
			// Some local function metadata names (real names: Method2(), Method3()) (Roslyn: GeneratedNames.MakeLocalFunctionName())
			//
			//		<Method1>g__Method20_0
			//		<Method1>g__Method30_1
			//		<Method2>g__Method21_0
			//		<Method2>g__Method31_1
			// later C# compiler version
			//		<Method1>g__Method2|0_0
			//
			//	<XXX> = XXX = containing method
			//	'g' = GeneratedNameKind.LocalFunction
			//	0_0 = methodOrdinal '_' entityOrdinal
			//	Method2, Method3 = names of local funcs
			//
			// Since a method can end in a digit and method ordinal is a number, we have to guess where
			// the name ends.
			//
			// This has been fixed, see https://github.com/dotnet/roslyn/pull/21848

			containingMethodName = null;
			localFunctionName = null;

			if (name.Length == 0 || name[0] != '<')
				return false;
			int index = name.IndexOf('>');
			if (index < 0)
				return false;
			containingMethodName = name.Substring(1, index - 1);
			if (containingMethodName.Length == 0)
				return false;
			index++;
			const char GeneratedNameKind_LocalFunction = 'g';
			if (NextChar(name, ref index) != GeneratedNameKind_LocalFunction)
				return false;
			if (NextChar(name, ref index) != '_')
				return false;
			if (NextChar(name, ref index) != '_')
				return false;

			// If it's a later C# compiler version, we can easily find the real name
			int sepIndex = name.IndexOf('|', index);
			if (sepIndex >= 0) {
				if (sepIndex != index) {
					localFunctionName = name.Substring(index, sepIndex - index);
					return true;
				}
				return false;
			}

			int endIndex = name.IndexOf('_', index);
			if (endIndex < 0)
				endIndex = name.Length;
			if (char.IsDigit(name[endIndex - 1]))
				endIndex--;
			if (index != endIndex) {
				localFunctionName = name.Substring(index, endIndex - index);
				return true;
			}

			return false;
		}

		static char NextChar(string s, ref int index) {
			if (index >= s.Length)
				return (char)0;
			return s[index++];
		}
	}
}
