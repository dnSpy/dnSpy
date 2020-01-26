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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Debugger;
using Iced.Intel;
using II = Iced.Intel;

namespace dnSpy.Debugger.AntiAntiDebug {
	struct ApiPatcherX86 {
		readonly ProcessMemoryBlockAllocator processMemoryBlockAllocator;
		readonly bool is64;
		readonly ulong funcAddress;
		readonly ulong moduleAddress;
		readonly ulong moduleEndAddress;
		readonly CodeReaderImpl codeReader;

		// jmp near ptr target
		const int PATCH_SIZE_X86 = 5;

		// jmp near ptr target
		const int PATCH_SIZE_X64_SHORT = 5;
		// mov rax,target; jmp rax
		const int PATCH_SIZE_X64_LONG = 10 + 2;

		public ApiPatcherX86(ProcessMemoryBlockAllocator processMemoryBlockAllocator, bool is64, ulong funcAddress, ulong moduleAddress, ulong moduleEndAddress) {
			this.processMemoryBlockAllocator = processMemoryBlockAllocator ?? throw new ArgumentNullException(nameof(processMemoryBlockAllocator));
			this.is64 = is64;
			this.funcAddress = funcAddress;
			this.moduleAddress = moduleAddress;
			this.moduleEndAddress = moduleEndAddress;
			codeReader = new CodeReaderImpl();
		}

		sealed class CodeReaderImpl : CodeReader {
			readonly byte[] data;
			int dataIndex;

			public CodeReaderImpl() => data = new byte[0x100];

			public void Initialize(DbgProcess process, ulong address) {
				process.ReadMemory(address, data);
				dataIndex = 0;
			}

			public override int ReadByte() {
				if (dataIndex >= data.Length)
					return -1;
				return data[dataIndex++];
			}
		}

		void Initialize(Decoder decoder, ulong address) {
			codeReader.Initialize(processMemoryBlockAllocator.Process, address);
			decoder.IP = address;
		}

		void GetJmpTarget(ref Instruction instr, out ulong jmpTarget, out bool jmpTargetIsIndirect) {
			jmpTarget = 0;
			jmpTargetIsIndirect = false;

			switch (instr.Code) {
			case II.Code.Jmp_rm32:
			case II.Code.Jmp_rm64:
				if (instr.Op0Kind != OpKind.Memory)
					return;
				if (instr.IsIPRelativeMemoryOperand) {
					jmpTarget = instr.IPRelativeMemoryAddress;
					jmpTargetIsIndirect = true;
					return;
				}
				if (instr.MemoryBase == Register.None && instr.MemoryIndex == Register.None) {
					if (is64)
						jmpTarget = (ulong)(int)instr.MemoryDisplacement;
					else
						jmpTarget = instr.MemoryDisplacement;
					jmpTargetIsIndirect = true;
					return;
				}
				break;

			case II.Code.Jmp_rel8_32:
			case II.Code.Jmp_rel8_64:
			case II.Code.Jmp_rel32_32:
			case II.Code.Jmp_rel32_64:
				jmpTarget = instr.NearBranchTarget;
				jmpTargetIsIndirect = false;
				return;

			default:
				break;
			}
		}

		static bool IsWrite(OpAccess access) {
			switch (access) {
			case OpAccess.None:
			case OpAccess.Read:
			case OpAccess.CondRead:
			case OpAccess.NoMemAccess:
				return false;

			case OpAccess.Write:
			case OpAccess.CondWrite:
			case OpAccess.ReadWrite:
			case OpAccess.ReadCondWrite:
				return true;

			default:
				Debug.Fail($"Unknown access: {access}");
				return true;
			}
		}

		static bool ModifiesRegistersOrMemory(in InstructionInfo info) {
			if (info.IsStackInstruction)
				return true;

			foreach (var regInfo in info.GetUsedRegisters()) {
				if (IsWrite(regInfo.Access))
					return true;
			}

			foreach (var memInfo in info.GetUsedMemory()) {
				if (IsWrite(memInfo.Access))
					return true;
			}

			return false;
		}

		static bool IsClose64(ulong afterInstr, ulong target) {
			long diff = (long)(target - afterInstr);
			return int.MinValue <= diff && diff <= int.MaxValue;
		}

		bool TryGetPatchBlock(Decoder decoder, ulong startAddress, ulong newFunctionAddress, out uint patchSize, out ulong blockAddress, out InstructionList blockInstructions) {
			patchSize = 0;
			var instrInfoFactory = new InstructionInfoFactory();
			blockInstructions = new InstructionList();

			const int MAX_BLOCKS = 1000;
			for (int i = 0; i < MAX_BLOCKS; i++) {
				if (is64) {
					// Check if we can use 'jmp near ptr xxx' instead of 'mov rax,xxx; jmp rax'.
					// If we can use 'jmp near ptr xxx', we'll copy at least 5 bytes (PATCH_SIZE_X64_SHORT)
					// but it can be up to 15-1 extra bytes (1 extra instruction with max len that starts at
					// offset 4 = 5-1).
					ulong minAddr = newFunctionAddress + PATCH_SIZE_X64_SHORT;
					ulong maxAddr = newFunctionAddress + PATCH_SIZE_X64_SHORT - 1 + 15;
					if (IsClose64(startAddress + PATCH_SIZE_X64_SHORT, minAddr) && IsClose64(startAddress + PATCH_SIZE_X64_SHORT, maxAddr))
						patchSize = PATCH_SIZE_X64_SHORT;
					else
						patchSize = PATCH_SIZE_X64_LONG;
				}
				else
					patchSize = PATCH_SIZE_X86;

				Initialize(decoder, startAddress);
				blockInstructions.Clear();
				bool modifiesRegsOrMem = false;
				while (true) {
					decoder.Decode(out var instr);
					blockInstructions.Add(instr);
					var info = instrInfoFactory.GetInfo(instr);

					ulong jmpTarget;
					bool jmpTargetIsIndirect;
					// We can only follow the branch if this code did not modify any registers or memory
					// locations (eg. the stack). The handlers assume they have the exact same input as
					// the real function.
					modifiesRegsOrMem |= ModifiesRegistersOrMemory(info);
					if (modifiesRegsOrMem) {
						jmpTarget = 0;
						jmpTargetIsIndirect = false;
					}
					else
						GetJmpTarget(ref instr, out jmpTarget, out jmpTargetIsIndirect);

					bool canDecodeMore;
					switch (info.FlowControl) {
					case FlowControl.Next:
					case FlowControl.ConditionalBranch:
					case FlowControl.Call:
					case FlowControl.IndirectCall:
						canDecodeMore = true;
						break;

					case FlowControl.UnconditionalBranch:
					case FlowControl.IndirectBranch:
					case FlowControl.Return:
					case FlowControl.Interrupt:
					case FlowControl.XbeginXabortXend:
					case FlowControl.Exception:
						canDecodeMore = false;
						break;

					default:
						Debug.Fail($"Unknown flow control: {info.FlowControl}");
						canDecodeMore = false;
						break;
					}

					uint currentSize = (uint)decoder.IP - (uint)startAddress;
					if (currentSize >= patchSize && instr.Code != II.Code.INVALID) {
						blockAddress = startAddress;
						return true;
					}
					if (!canDecodeMore) {
						if (jmpTarget != 0) {
							if (!jmpTargetIsIndirect)
								startAddress = jmpTarget;
							else if (is64) {
								var ptrData = processMemoryBlockAllocator.Process.ReadMemory(jmpTarget, 8);
								startAddress = BitConverter.ToUInt64(ptrData, 0);
							}
							else {
								var ptrData = processMemoryBlockAllocator.Process.ReadMemory(jmpTarget, 4);
								startAddress = BitConverter.ToUInt32(ptrData, 0);
							}
							break;
						}
						blockAddress = 0;
						return false;
					}
				}
			}

			blockAddress = 0;
			return false;
		}

		sealed class ArrayCodeWriterImpl : CodeWriter {
			public readonly byte[] Data;
			public int Index;
			public ArrayCodeWriterImpl(int size) => Data = new byte[size];
			public override void WriteByte(byte value) {
				if (Index < Data.Length)
					Data[Index] = value;
				Index++;
			}
		}

		public PatchAPIResult Patch() {
			var decoder = Decoder.Create(is64 ? 64 : 32, codeReader);

			var memBlock = processMemoryBlockAllocator.Allocate(moduleAddress, moduleEndAddress);
			ulong newFunctionAddress = memBlock.CurrentAddress;
			if (!TryGetPatchBlock(decoder, funcAddress, newFunctionAddress, out uint patchSize, out var blockAddress, out var blockInstructions))
				return new PatchAPIResult("Couldn't find a block to patch");

			if (!TryCopyOriginalBlock(memBlock, blockInstructions, out var errorMessage))
				return new PatchAPIResult(errorMessage);
			ulong newCodeAddress = memBlock.CurrentAddress;

			var instructions = new InstructionList();
			if (is64) {
				if (patchSize == PATCH_SIZE_X64_LONG) {
					instructions.Add(Instruction.Create(II.Code.Mov_r64_imm64, Register.RAX, newCodeAddress));
					instructions.Add(Instruction.Create(II.Code.Jmp_rm64, Register.RAX));
				}
				else {
					Debug.Assert(patchSize == PATCH_SIZE_X64_SHORT);
					instructions.Add(Instruction.CreateBranch(II.Code.Jmp_rel32_64, newCodeAddress));
				}
			}
			else {
				Debug.Assert(patchSize == PATCH_SIZE_X86);
				instructions.Add(Instruction.CreateBranch(II.Code.Jmp_rel32_32, newCodeAddress));
			}
			var arrayCodeWriter = new ArrayCodeWriterImpl((int)patchSize);
			var block = new InstructionBlock(arrayCodeWriter, instructions, blockAddress);
			if (!BlockEncoder.TryEncode(is64 ? 64 : 32, block, out errorMessage, out _, options: BlockEncoderOptions.DontFixBranches))
				return new PatchAPIResult(errorMessage);

			Debug.Assert((uint)arrayCodeWriter.Index == patchSize);
			if ((uint)arrayCodeWriter.Index != patchSize)
				return new PatchAPIResult("Internal x86/x64 patcher error");

			var simplePatch = new SimpleAPIPatch(blockAddress, arrayCodeWriter.Data);
			return new PatchAPIResult(memBlock, newFunctionAddress, simplePatch);
		}

		sealed class ProcessMemoryBlockCodeWriter : CodeWriter {
			readonly ProcessMemoryBlock memBlock;
			public ProcessMemoryBlockCodeWriter(ProcessMemoryBlock memBlock) => this.memBlock = memBlock;
			public override void WriteByte(byte value) => memBlock.WriteByte(value);
		}

		bool TryCopyOriginalBlock(ProcessMemoryBlock memBlock, InstructionList blockInstructions, [NotNullWhen(false)] out string? errorMessage) {
			Debug.Assert(blockInstructions.Count != 0);
			var targetAddr = blockInstructions[blockInstructions.Count - 1].NextIP;
			blockInstructions.Add(Instruction.CreateBranch(is64 ? II.Code.Jmp_rel32_64 : II.Code.Jmp_rel32_32, targetAddr));
			var codeWriter = new ProcessMemoryBlockCodeWriter(memBlock);
			var block = new InstructionBlock(codeWriter, blockInstructions, memBlock.CurrentAddress);
			return BlockEncoder.TryEncode(is64 ? 64 : 32, block, out errorMessage, out _);
		}
	}
}
