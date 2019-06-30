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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class ValueFormatterOptionsExtensions {
		public static DbgValueFormatterOptions ToDbgValueFormatterOptions(this ValueFormatterOptions options) {
			var res = DbgValueFormatterOptions.None;
			if ((options & ValueFormatterOptions.Edit) != 0)
				res |= DbgValueFormatterOptions.Edit;
			if ((options & ValueFormatterOptions.Decimal) != 0)
				res |= DbgValueFormatterOptions.Decimal;
			if ((options & ValueFormatterOptions.FuncEval) != 0)
				res |= DbgValueFormatterOptions.FuncEval;
			if ((options & ValueFormatterOptions.ToString) != 0)
				res |= DbgValueFormatterOptions.ToString;
			if ((options & ValueFormatterOptions.DigitSeparators) != 0)
				res |= DbgValueFormatterOptions.DigitSeparators;
			if ((options & ValueFormatterOptions.NoStringQuotes) != 0)
				res |= DbgValueFormatterOptions.NoStringQuotes;
			if ((options & ValueFormatterOptions.NoDebuggerDisplay) != 0)
				res |= DbgValueFormatterOptions.NoDebuggerDisplay;
			if ((options & ValueFormatterOptions.FullString) != 0)
				res |= DbgValueFormatterOptions.FullString;
			if ((options & ValueFormatterOptions.Namespaces) != 0)
				res |= DbgValueFormatterOptions.Namespaces;
			if ((options & ValueFormatterOptions.IntrinsicTypeKeywords) != 0)
				res |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
			if ((options & ValueFormatterOptions.Tokens) != 0)
				res |= DbgValueFormatterOptions.Tokens;
			return res;
		}

		public static TypeFormatterOptions ToTypeFormatterOptions(this ValueFormatterOptions options, bool showArrayValueSizes) {
			var res = TypeFormatterOptions.None;
			if ((options & ValueFormatterOptions.IntrinsicTypeKeywords) != 0)
				res |= TypeFormatterOptions.IntrinsicTypeKeywords;
			if ((options & ValueFormatterOptions.Tokens) != 0)
				res |= TypeFormatterOptions.Tokens;
			if ((options & ValueFormatterOptions.Namespaces) != 0)
				res |= TypeFormatterOptions.Namespaces;
			if (showArrayValueSizes)
				res |= TypeFormatterOptions.ShowArrayValueSizes;
			if ((options & ValueFormatterOptions.Decimal) != 0)
				res |= TypeFormatterOptions.UseDecimal;
			if ((options & ValueFormatterOptions.DigitSeparators) != 0)
				res |= TypeFormatterOptions.DigitSeparators;
			return res;
		}
	}
}
