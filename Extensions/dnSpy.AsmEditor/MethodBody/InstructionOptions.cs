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

using System.Collections.Generic;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class InstructionOptions {
		public Code Code;
		public object Operand;
		public SequencePoint SequencePoint;//TODO: Use this

		public InstructionOptions() {
		}

		public InstructionOptions(Dictionary<object, object> ops, Instruction instr) {
			Code = instr.OpCode.Code;
			Operand = BodyUtils.ToOperandVM(ops, instr.Operand);
			SequencePoint = instr.SequencePoint;
		}

		public Instruction CopyTo(Dictionary<object, object> ops, Instruction instr) {
			instr.OpCode = Code.ToOpCode();
			instr.Operand = BodyUtils.ToOperandModel(ops, Operand);
			instr.SequencePoint = SequencePoint;
			return instr;
		}

		public Instruction Create(Dictionary<object, object> ops) => CopyTo(ops, new Instruction());
	}
}
