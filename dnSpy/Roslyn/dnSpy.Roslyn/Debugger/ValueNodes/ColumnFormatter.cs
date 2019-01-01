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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	abstract class ColumnFormatter {
		public virtual bool FormatName(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) => false;
		public virtual bool FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterOptions options, CultureInfo cultureInfo) => false;
		public virtual bool FormatExpectedType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) => false;
		public virtual bool FormatActualType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgDotNetFormatter formatter, DbgValueFormatterTypeOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) => false;
	}
}
