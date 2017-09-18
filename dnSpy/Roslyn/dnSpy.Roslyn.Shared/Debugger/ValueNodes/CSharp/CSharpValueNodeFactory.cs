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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes.CSharp {
	[ExportDbgDotNetValueNodeFactory(DbgDotNetLanguageGuids.CSharp)]
	sealed class CSharpValueNodeFactory : LanguageValueNodeFactory {
		public CSharpValueNodeFactory() : base(new CSharpValueNodeProviderFactory()) { }

		public override string GetExpression(string baseExpression, DmdFieldInfo field) {
			//TODO: Add parens, casts, use -> if pointer etc...
			return baseExpression + "." + field.Name;
		}

		public override string GetExpression(string baseExpression, DmdPropertyInfo property) {
			return baseExpression + "." + property.Name;
		}

		public override string GetExpression(string baseExpression, string name) {
			return baseExpression + "." + name;
		}

		public override string GetExpression(string baseExpression, int index) {
			return baseExpression + "[" + index.ToString() + "]";
		}

		public override string GetExpression(string baseExpression, int[] indexes) {
			var sb = Formatters.ValueFormatterObjectCache.AllocStringBuilder();
			sb.Append(baseExpression);
			sb.Append('[');
			for (int i = 0; i < indexes.Length; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(indexes[i].ToString());
			}
			sb.Append(']');
			return Formatters.ValueFormatterObjectCache.FreeAndToString(ref sb);
		}

		public override string EscapeIdentifier(string identifier) => Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(identifier);
	}
}
