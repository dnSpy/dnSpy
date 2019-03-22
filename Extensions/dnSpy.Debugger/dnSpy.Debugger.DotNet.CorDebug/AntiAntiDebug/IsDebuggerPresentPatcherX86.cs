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

using System.IO;
using dnSpy.Contracts.Debugger.AntiAntiDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using Iced.Intel;
using II = Iced.Intel;

namespace dnSpy.Debugger.DotNet.CorDebug.AntiAntiDebug {
	sealed class IsDebuggerPresentPatcherX86 : PatcherX86 {
		readonly string clrFilename;

		public IsDebuggerPresentPatcherX86(DbgNativeFunctionHookContext context, DbgCorDebugInternalRuntime runtime)
			: base(context) => clrFilename = runtime.ClrFilename;

		public bool TryPatchX86(out string errorMessage) {
			var function = functionProvider.GetFunction(IsDebuggerPresentConstants.DllName, IsDebuggerPresentConstants.FuncName);
			var clrDllName = Path.GetFileName(clrFilename);
			if (!functionProvider.TryGetModuleAddress(clrDllName, out var clrAddress, out var clrEndAddress)) {
				errorMessage = $"Couldn't get the address of {clrDllName}";
				return false;
			}

			/*
				Generate the following code:

				cmp dword ptr [esp],start_addr
				jb short return_0
				cmp dword ptr [esp],end_addr_inclusive
				ja short return_0
				; called by the CLR
				jmp orig_func
			return_0:
				xor eax,eax
				ret
			*/

			var instructions = new InstructionList();
			var return_0_instr = AddTargetId(Instruction.Create(II.Code.Xor_r32_rm32, Register.EAX, Register.EAX));

			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm32, new MemoryOperand(Register.ESP), (uint)clrAddress));
			instructions.Add(Instruction.CreateBranch(II.Code.Jb_rel8_32, return_0_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm32, new MemoryOperand(Register.ESP), (uint)clrEndAddress - 1));
			instructions.Add(Instruction.CreateBranch(II.Code.Ja_rel8_32, return_0_instr.IP));
			instructions.Add(Instruction.CreateBranch(II.Code.Jmp_rel32_32, function.NewFunctionAddress));
			//return_0:
			instructions.Add(return_0_instr);
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
			var clrDllName = Path.GetFileName(clrFilename);
			if (!functionProvider.TryGetModuleAddress(clrDllName, out var clrAddress, out var clrEndAddress)) {
				errorMessage = $"Couldn't get the address of {clrDllName}";
				return false;
			}

			/*
				Generate the following code:

				push rcx	; The original API doesn't modify it, so don't destroy the original value
				mov rcx,[rsp+8]
				mov rax,start_addr
				cmp rcx,rax
				jb short return_0
				mov rax,end_addr_inclusive
				cmp rcx,rax
				ja short return_0
				; called by the CLR
				mov rax,orig_func
				pop rcx
				jmp rax
			return_0:
				xor eax,eax
				pop rcx
				ret
			*/

			var instructions = new InstructionList();
			var return_0_instr = AddTargetId(Instruction.Create(II.Code.Xor_r32_rm32, Register.EAX, Register.EAX));

			instructions.Add(Instruction.Create(II.Code.Push_r64, Register.RCX));
			instructions.Add(Instruction.Create(II.Code.Mov_r64_rm64, Register.RCX, new MemoryOperand(Register.RSP, 8)));
			instructions.Add(Instruction.Create(II.Code.Mov_r64_imm64, Register.RAX, clrAddress));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm64_r64, Register.RCX, Register.RAX));
			instructions.Add(Instruction.CreateBranch(II.Code.Jb_rel8_64, return_0_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Mov_r64_imm64, Register.RAX, clrEndAddress - 1));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm64_r64, Register.RCX, Register.RAX));
			instructions.Add(Instruction.CreateBranch(II.Code.Ja_rel8_64, return_0_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Mov_r64_imm64, Register.RAX, function.NewFunctionAddress));
			instructions.Add(Instruction.Create(II.Code.Pop_r64, Register.RCX));
			instructions.Add(Instruction.Create(II.Code.Jmp_rm64, Register.RAX));
			//return_0:
			instructions.Add(return_0_instr);
			instructions.Add(Instruction.Create(II.Code.Pop_r64, Register.RCX));
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
