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

using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;
using Iced.Intel;

namespace dnSpy.Debugger.AntiAntiDebug {
	abstract class PatcherX86 {
		protected readonly DbgProcess process;
		protected readonly DbgHookedNativeFunctionProvider functionProvider;
		ulong nextBranchTargetId;

		protected PatcherX86(DbgNativeFunctionHookContext context) {
			process = context.Process;
			functionProvider = context.FunctionProvider;
			nextBranchTargetId = ulong.MaxValue;
		}

		protected Instruction AddTargetId(Instruction instruction) {
			if (process.Bitness == 64)
				instruction.IP = nextBranchTargetId;
			else
				instruction.IP32 = (uint)nextBranchTargetId;
			nextBranchTargetId--;
			return instruction;
		}

		protected sealed class CodeWriterImpl : CodeWriter {
			readonly DbgHookedNativeFunction function;
			public CodeWriterImpl(DbgHookedNativeFunction function) => this.function = function;
			public override void WriteByte(byte value) => function.WriteByte(value);
		}
	}
}
