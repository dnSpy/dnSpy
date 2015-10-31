/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ICSharpCode.Decompiler.Disassembler;

namespace dnSpy {
	sealed class ModifiedInstructionBytesReader : IInstructionBytesReader {
		readonly ITokenResolver resolver;
		readonly IList<Instruction> instrs;
		int instrIndex;
		readonly List<short> instrBytes = new List<short>(10);
		int byteIndex;

		public ModifiedInstructionBytesReader(MethodDef method) {
			this.resolver = method.Module;
			this.instrs = method.Body.Instructions;
		}

		public int ReadByte() {
			if (byteIndex >= instrBytes.Count)
				InitializeNextInstruction();
			return instrBytes[byteIndex++];
		}

		void InitializeNextInstruction() {
			if (instrIndex >= instrs.Count)
				throw new InvalidOperationException();
			var instr = instrs[instrIndex++];

			byteIndex = 0;
			instrBytes.Clear();

			InstructionUtils.AddOpCode(instrBytes, instr.OpCode.Code);
			InstructionUtils.AddOperand(instrBytes, resolver, instr.Offset + (uint)instr.OpCode.Size, instr.OpCode, instr.Operand);
		}

		public void SetInstruction(int index, uint offset) {
			instrIndex = index;
			InitializeNextInstruction();
		}

		public void Dispose() {
		}
	}
}
