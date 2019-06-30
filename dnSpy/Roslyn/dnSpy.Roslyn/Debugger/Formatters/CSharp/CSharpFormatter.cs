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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters.CSharp {
	[ExportDbgDotNetFormatter(DbgDotNetLanguageGuids.CSharp)]
	sealed class CSharpFormatter : LanguageFormatter {
		public override void FormatType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DmdType type, DbgDotNetValue? value, DbgValueFormatterTypeOptions options, CultureInfo? cultureInfo) =>
			new CSharpTypeFormatter(output, options.ToTypeFormatterOptions(), cultureInfo).Format(type, value);

		public override void FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetValue value, DbgValueFormatterOptions options, CultureInfo? cultureInfo) =>
			new CSharpValueFormatter(output, evalInfo, this, options.ToValueFormatterOptions(), cultureInfo).Format(value);

		public override void FormatFrame(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo) =>
			new CSharpStackFrameFormatter(output, evalInfo, this, options, valueOptions.ToValueFormatterOptions(), cultureInfo).Format();
	}
}
