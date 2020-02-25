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

using System;
using dnSpy.Contracts.MVVM;
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	sealed class DisasmBooleanSetting : ViewModelBase {
		readonly StringOutput output;
		readonly Func<bool> getValue;
		readonly Action<bool> setValue;
		readonly Formatter formatter;
		readonly Instruction instruction;

		public bool Value {
			get => getValue();
			set {
				bool currentValue = getValue();
				if (value != currentValue) {
					setValue(value);
					OnPropertyChanged(nameof(Value));
				}
			}
		}

		public string Disassembly {
			get {
				formatter.Format(instruction, output);
				return output.ToStringAndReset();
			}
		}

		public DisasmBooleanSetting(StringOutput output, Func<bool> getValue, Action<bool> setValue, Formatter formatter, in Instruction instruction) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
			this.setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			this.instruction = instruction;
		}

		internal void RaiseDisassemblyChanged() => OnPropertyChanged(nameof(Disassembly));
	}
}
