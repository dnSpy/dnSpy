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

using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Properties;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class ReturnValueColumnFormatter : ColumnFormatter {
		readonly LanguageValueNodeFactory owner;
		readonly DmdMethodBase method;

		public ReturnValueColumnFormatter(LanguageValueNodeFactory owner, DmdMethodBase method) {
			this.owner = owner;
			this.method = method;
		}

		public override bool FormatName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			var formatString = dnSpy_Roslyn_Resources.LocalsWindow_MethodOrProperty_Returned;
			const string pattern = "{0}";
			int index = formatString.IndexOf(pattern);
			Debug.Assert(index >= 0);
			if (index < 0) {
				formatString = "{0} returned";
				index = formatString.IndexOf(pattern);
			}

			if (index != 0)
				output.Write(DbgTextColor.Text, formatString.Substring(0, index));
			owner.FormatReturnValueMethodName(evalInfo, output, options, cultureInfo, method);
			if (index + pattern.Length != formatString.Length)
				output.Write(DbgTextColor.Text, formatString.Substring(index + pattern.Length));
			return true;
		}
	}
}
