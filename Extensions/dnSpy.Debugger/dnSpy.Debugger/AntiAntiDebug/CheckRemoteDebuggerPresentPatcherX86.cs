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

using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Debugger.AntiAntiDebug;
using Iced.Intel;
using II = Iced.Intel;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class CheckRemoteDebuggerPresentPatcherX86 : PatcherX86 {
		readonly int pid;

		public CheckRemoteDebuggerPresentPatcherX86(DbgNativeFunctionHookContext context) : base(context) => pid = context.Process.Id;

		public bool TryPatchX86([NotNullWhen(false)] out string? errorMessage) {
			var function = functionProvider.GetFunction(CheckRemoteDebuggerPresentConstants.DllName, CheckRemoteDebuggerPresentConstants.FuncName);
			if (!functionProvider.TryGetFunction("kernel32.dll", "GetProcessId", out var addrGetProcessId)) {
				errorMessage = "Couldn't get address of GetProcessId";
				return false;
			}

			/*
				Generate the following code:

				mov eax,[esp+4]
				test eax,eax
				jz jmp_orig_func
				cmp dword ptr [esp+8],0
				je jmp_orig_func
				cmp eax,-1
				je fix
				push eax
				call GetProcessId
				cmp eax,pid
				jne jmp_orig_func
			fix:
				mov eax,[esp+8]
				and dword ptr [eax],0
				mov eax,1
				ret 8
			jmp_orig_func:
				jmp orig_func
			*/

			var instructions = new InstructionList();
			var fix_instr = AddTargetId(Instruction.Create(II.Code.Mov_r32_rm32, Register.EAX, new MemoryOperand(Register.ESP, 8)));
			var jmp_orig_func_instr = AddTargetId(Instruction.CreateBranch(II.Code.Jmp_rel32_32, function.NewFunctionAddress));

			instructions.Add(Instruction.Create(II.Code.Mov_r32_rm32, Register.EAX, new MemoryOperand(Register.ESP, 4)));
			instructions.Add(Instruction.Create(II.Code.Test_rm32_r32, Register.EAX, Register.EAX));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_32, jmp_orig_func_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm8, new MemoryOperand(Register.ESP, 8), 0));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_32, jmp_orig_func_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm8, Register.EAX, -1));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_32, fix_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Push_r32, Register.EAX));
			instructions.Add(Instruction.CreateBranch(II.Code.Call_rel32_32, addrGetProcessId));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm32, Register.EAX, pid));
			instructions.Add(Instruction.CreateBranch(II.Code.Jne_rel8_32, jmp_orig_func_instr.IP));
			instructions.Add(fix_instr);
			instructions.Add(Instruction.Create(II.Code.And_rm32_imm8, new MemoryOperand(Register.EAX), 0));
			instructions.Add(Instruction.Create(II.Code.Mov_r32_imm32, Register.EAX, 1));
			instructions.Add(Instruction.Create(II.Code.Retnd_imm16, 8));
			instructions.Add(jmp_orig_func_instr);

			var block = new InstructionBlock(new CodeWriterImpl(function), instructions, function.NewCodeAddress);
			if (!BlockEncoder.TryEncode(process.Bitness, block, out var encErrMsg, out _)) {
				errorMessage = $"Failed to encode: {encErrMsg}";
				return false;
			}

			errorMessage = null;
			return true;
		}

		public bool TryPatchX64([NotNullWhen(false)] out string? errorMessage) {
			var function = functionProvider.GetFunction(CheckRemoteDebuggerPresentConstants.DllName, CheckRemoteDebuggerPresentConstants.FuncName);
			if (!functionProvider.TryGetFunction("kernel32.dll", "GetProcessId", out var addrGetProcessId)) {
				errorMessage = "Couldn't get address of GetProcessId";
				return false;
			}

			/*
				Generate the following code:

				test rcx,rcx
				jz jmp_orig_func
				test rdx,rdx
				jz jmp_orig_func
				cmp rcx,-1
				je fix
				mov [rsp+8],rcx
				push rdx
				sub rsp,20h
				call GetProcessId
				add rsp,20h
				pop rdx
				mov rcx,[rsp+8]
				cmp eax,pid
				jne jmp_orig_func
			fix:
				and dword ptr [rdx],0
				mov eax,1
				ret
			jmp_orig_func:
				jmp orig_func
			*/

			var instructions = new InstructionList();
			var fix_instr = AddTargetId(Instruction.Create(II.Code.And_rm32_imm8, new MemoryOperand(Register.RDX), 0));
			var jmp_orig_func_instr = AddTargetId(Instruction.CreateBranch(II.Code.Jmp_rel32_64, function.NewFunctionAddress));

			instructions.Add(Instruction.Create(II.Code.Test_rm64_r64, Register.RCX, Register.RCX));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_64, jmp_orig_func_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Test_rm64_r64, Register.RDX, Register.RDX));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_64, jmp_orig_func_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm64_imm8, Register.RCX, -1));
			instructions.Add(Instruction.CreateBranch(II.Code.Je_rel8_64, fix_instr.IP));
			instructions.Add(Instruction.Create(II.Code.Mov_rm64_r64, new MemoryOperand(Register.RSP, 8), Register.RCX));
			instructions.Add(Instruction.Create(II.Code.Push_r64, Register.RDX));
			instructions.Add(Instruction.Create(II.Code.Sub_rm64_imm8, Register.RSP, 0x20));
			instructions.Add(Instruction.CreateBranch(II.Code.Call_rel32_64, addrGetProcessId));
			instructions.Add(Instruction.Create(II.Code.Add_rm64_imm8, Register.RSP, 0x20));
			instructions.Add(Instruction.Create(II.Code.Pop_r64, Register.RDX));
			instructions.Add(Instruction.Create(II.Code.Mov_r64_rm64, Register.RCX, new MemoryOperand(Register.RSP, 8)));
			instructions.Add(Instruction.Create(II.Code.Cmp_rm32_imm32, Register.EAX, pid));
			instructions.Add(Instruction.CreateBranch(II.Code.Jne_rel8_64, jmp_orig_func_instr.IP));
			instructions.Add(fix_instr);
			instructions.Add(Instruction.Create(II.Code.Mov_r32_imm32, Register.EAX, 1));
			instructions.Add(Instruction.Create(II.Code.Retnq));
			instructions.Add(jmp_orig_func_instr);

			var block = new InstructionBlock(new CodeWriterImpl(function), instructions, function.NewCodeAddress);
			if (!BlockEncoder.TryEncode(process.Bitness, block, out var encErrMsg, out _)) {
				errorMessage = $"Failed to encode: {encErrMsg}";
				return false;
			}

			errorMessage = null;
			return true;
		}
	}
}
