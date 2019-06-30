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
	static class DbgValueFormatterOptionsExtensions {
		public static ValueFormatterOptions ToValueFormatterOptions(this DbgValueFormatterOptions options) {
			var res = ValueFormatterOptions.None;
			if ((options & DbgValueFormatterOptions.Edit) != 0)
				res |= ValueFormatterOptions.Edit;
			if ((options & DbgValueFormatterOptions.Decimal) != 0)
				res |= ValueFormatterOptions.Decimal;
			if ((options & DbgValueFormatterOptions.FuncEval) != 0)
				res |= ValueFormatterOptions.FuncEval;
			if ((options & DbgValueFormatterOptions.ToString) != 0)
				res |= ValueFormatterOptions.ToString;
			if ((options & DbgValueFormatterOptions.DigitSeparators) != 0)
				res |= ValueFormatterOptions.DigitSeparators;
			if ((options & DbgValueFormatterOptions.NoStringQuotes) != 0)
				res |= ValueFormatterOptions.NoStringQuotes;
			if ((options & DbgValueFormatterOptions.NoDebuggerDisplay) != 0)
				res |= ValueFormatterOptions.NoDebuggerDisplay;
			if ((options & DbgValueFormatterOptions.FullString) != 0)
				res |= ValueFormatterOptions.FullString;
			if ((options & DbgValueFormatterOptions.Namespaces) != 0)
				res |= ValueFormatterOptions.Namespaces;
			if ((options & DbgValueFormatterOptions.IntrinsicTypeKeywords) != 0)
				res |= ValueFormatterOptions.IntrinsicTypeKeywords;
			if ((options & DbgValueFormatterOptions.Tokens) != 0)
				res |= ValueFormatterOptions.Tokens;
			return res;
		}
	}
}
