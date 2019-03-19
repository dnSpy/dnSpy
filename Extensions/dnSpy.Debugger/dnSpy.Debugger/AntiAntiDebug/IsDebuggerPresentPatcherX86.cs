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

using dnSpy.Contracts.Debugger.AntiAntiDebug;
using Iced.Intel;
using II = Iced.Intel;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class IsDebuggerPresentPatcherX86 : PatcherX86 {
		public IsDebuggerPresentPatcherX86(DbgNativeFunctionHookContext context) : base(context) { }

		public bool TryPatchX86(out string errorMessage) {
			var function = functionProvider.GetFunction(IsDebuggerPresentConstants.DllName, IsDebuggerPresentConstants.FuncName);

			/*
				Generate the following code:

				xor eax,eax
				ret
			*/

			var instructions = new InstructionList();
			instructions.Add(Instruction.Create(II.Code.Xor_r32_rm32, Register.EAX, Register.EAX));
			instructions.Add(Instruction.Create(II.Code.Retnd));

			var block = new InstructionBlock(new CodeWriterImpl(function), instructions, function.NewCodeAddress);
			if (!BlockEncoder.TryEncode(process.Bitness, block, out var encErrMsg)) {
				errorMessage = $"Failed to encode: {encErrMsg}";
				return false;
			}

			errorMessage = null;
			return true;
		}

		public bool TryPatchX64(out string errorMessage) {
			var function = functionProvider.GetFunction(IsDebuggerPresentConstants.DllName, IsDebuggerPresentConstants.FuncName);

			/*
				Generate the following code:

				xor eax,eax
				ret
			*/

			var instructions = new InstructionList();
			instructions.Add(Instruction.Create(II.Code.Xor_r32_rm32, Register.EAX, Register.EAX));
			instructions.Add(Instruction.Create(II.Code.Retnq));

			var block = new InstructionBlock(new CodeWriterImpl(function), instructions, function.NewCodeAddress);
			if (!BlockEncoder.TryEncode(process.Bitness, block, out var encErrMsg)) {
				errorMessage = $"Failed to encode: {encErrMsg}";
				return false;
			}

			errorMessage = null;
			return true;
		}
	}
}
