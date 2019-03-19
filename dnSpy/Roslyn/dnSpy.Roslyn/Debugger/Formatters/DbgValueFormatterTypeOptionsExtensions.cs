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
	static class DbgValueFormatterTypeOptionsExtensions {
		public static TypeFormatterOptions ToTypeFormatterOptions(this DbgValueFormatterTypeOptions options) {
			var res = TypeFormatterOptions.None;
			if ((options & DbgValueFormatterTypeOptions.Decimal) != 0)
				res |= TypeFormatterOptions.UseDecimal;
			if ((options & DbgValueFormatterTypeOptions.DigitSeparators) != 0)
				res |= TypeFormatterOptions.DigitSeparators;
			if ((options & DbgValueFormatterTypeOptions.IntrinsicTypeKeywords) != 0)
				res |= TypeFormatterOptions.IntrinsicTypeKeywords;
			if ((options & DbgValueFormatterTypeOptions.Tokens) != 0)
				res |= TypeFormatterOptions.Tokens;
			if ((options & DbgValueFormatterTypeOptions.Namespaces) != 0)
				res |= TypeFormatterOptions.Namespaces;
			return res;
		}
	}
}
