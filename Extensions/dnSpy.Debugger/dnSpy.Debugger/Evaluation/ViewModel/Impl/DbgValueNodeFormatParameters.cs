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
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Text;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class DbgValueNodeFormatParameters : IDbgValueNodeFormatParameters {
		IDbgTextWriter? IDbgValueNodeFormatParameters.NameOutput => nameOutput;
		IDbgTextWriter? IDbgValueNodeFormatParameters.ValueOutput => valueOutput;
		IDbgTextWriter? IDbgValueNodeFormatParameters.ExpectedTypeOutput => expectedTypeOutput;
		IDbgTextWriter? IDbgValueNodeFormatParameters.ActualTypeOutput => actualTypeOutput;
		IDbgTextWriter? nameOutput, valueOutput, expectedTypeOutput, actualTypeOutput;

		DbgValueFormatterTypeOptions IDbgValueNodeFormatParameters.ExpectedTypeFormatterOptions => ValueFormatterTypeOptions;
		DbgValueFormatterTypeOptions IDbgValueNodeFormatParameters.ActualTypeFormatterOptions => ValueFormatterTypeOptions;

		public ClassifiedTextWriter NameOutput { get; }
		public ClassifiedTextWriter ValueOutput { get; }
		public ClassifiedTextWriter ExpectedTypeOutput { get; }
		public ClassifiedTextWriter ActualTypeOutput { get; }
		public DbgValueFormatterOptions NameFormatterOptions { get; set; }
		public DbgValueFormatterOptions ValueFormatterOptions { get; set; }
		public DbgValueFormatterOptions TypeFormatterOptions { get; set; }
		public DbgValueFormatterTypeOptions ValueFormatterTypeOptions { get; set; }

		public DbgValueNodeFormatParameters() {
			NameOutput = new ClassifiedTextWriter();
			ValueOutput = new ClassifiedTextWriter();
			ExpectedTypeOutput = new ClassifiedTextWriter();
			ActualTypeOutput = new ClassifiedTextWriter();
		}

		public void Initialize(bool formatName, bool formatValue, bool formatType) {
			nameOutput = formatName ? NameOutput : null;
			valueOutput = formatValue ? ValueOutput : null;
			if (formatType) {
				expectedTypeOutput = ExpectedTypeOutput;
				actualTypeOutput = ActualTypeOutput;
			}
			else {
				expectedTypeOutput = null;
				actualTypeOutput = null;
			}
		}
	}
}
