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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Disassembly;
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	static class FormatterTextKindExtensions {
		public const FormatterTextKind UnknownSymbol = (FormatterTextKind)(-1);
	}

	static class BlockFactory {
		const string LABEL_PREFIX = "LBL_";
		const string FUNC_PREFIX = "FNC_";

		// The values are sorted, eg. call has higher prio than branch
		enum TargetKind {
			// If it's probably code (any instruction after an unconditional branch, ret, etc)
			Unknown,
			// RIP-relative address referenced this location
			Data,
			// start of a known block
			BlockStart,
			// branch target
			Branch,
			// call target
			Call,
		}

		readonly struct BlockInfo {
			public readonly TargetKind TargetKind;
			public readonly NativeCodeBlockKind Kind;
			public readonly ulong Address;
			public readonly string? Comment;
			public readonly List<X86InstructionInfo> Instructions;
			public BlockInfo(TargetKind targetKind, NativeCodeBlockKind kind, ulong address, string? comment) {
				TargetKind = targetKind;
				Kind = kind;
				Address = address;
				Comment = comment;
				Instructions = new List<X86InstructionInfo>();
			}
		}

		static void Add(Dictionary<ulong, TargetKind> targets, ulong address, TargetKind kind) {
			if (!targets.TryGetValue(address, out var existingKind) || existingKind < kind)
				targets[address] = kind;
		}

		static ArraySegment<byte> GetBytes(ArraySegment<byte> code, ulong address, ref Instruction instr) {
			int index = (int)(instr.IP - address);
			return new ArraySegment<byte>(code.Array!, code.Offset + index, instr.Length);
		}

		static string GetLabel(int index) => LABEL_PREFIX + index.ToString();
		static string GetFunc(int index) => FUNC_PREFIX + index.ToString();

		public static Block[] Create(int bitness, NativeCodeBlock[] blocks) {
			var targets = new Dictionary<ulong, TargetKind>();
			var instrInfo = new List<(Instruction instruction, int block, ArraySegment<byte> code)>();
			for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++) {
				var block = blocks[blockIndex];

				var reader = new ByteArrayCodeReader(block.Code);
				var decoder = Decoder.Create(bitness, reader);
				decoder.IP = block.Address;
				while (reader.CanReadByte) {
					decoder.Decode(out var instr);
					instrInfo.Add((instr, blockIndex, GetBytes(block.Code, block.Address, ref instr)));

					switch (instr.FlowControl) {
					case FlowControl.Next:
					case FlowControl.Interrupt:
						break;

					case FlowControl.UnconditionalBranch:
						Add(targets, instr.NextIP, TargetKind.Unknown);
						if (instr.Op0Kind == OpKind.NearBranch16 || instr.Op0Kind == OpKind.NearBranch32 || instr.Op0Kind == OpKind.NearBranch64)
							Add(targets, instr.NearBranchTarget, TargetKind.Branch);
						break;

					case FlowControl.ConditionalBranch:
					case FlowControl.XbeginXabortXend:
						if (instr.Op0Kind == OpKind.NearBranch16 || instr.Op0Kind == OpKind.NearBranch32 || instr.Op0Kind == OpKind.NearBranch64)
							Add(targets, instr.NearBranchTarget, TargetKind.Branch);
						break;

					case FlowControl.Call:
						if (instr.Op0Kind == OpKind.NearBranch16 || instr.Op0Kind == OpKind.NearBranch32 || instr.Op0Kind == OpKind.NearBranch64)
							Add(targets, instr.NearBranchTarget, TargetKind.Call);
						break;

					case FlowControl.IndirectBranch:
						Add(targets, instr.NextIP, TargetKind.Unknown);
						// Unknown target
						break;

					case FlowControl.IndirectCall:
						// Unknown target
						break;

					case FlowControl.Return:
					case FlowControl.Exception:
						Add(targets, instr.NextIP, TargetKind.Unknown);
						break;

					default:
						Debug.Fail($"Unknown flow control: {instr.FlowControl}");
						break;
					}

					var baseReg = instr.MemoryBase;
					if (baseReg == Register.RIP || baseReg == Register.EIP) {
						int opCount = instr.OpCount;
						for (int i = 0; i < opCount; i++) {
							if (instr.GetOpKind(i) == OpKind.Memory) {
								if (IsCodeAddress(blocks, instr.IPRelativeMemoryAddress))
									Add(targets, instr.IPRelativeMemoryAddress, TargetKind.Branch);
								break;
							}
						}
					}
					else if (instr.MemoryDisplSize >= 2) {
						ulong displ;
						switch (instr.MemoryDisplSize) {
						case 2:
						case 4: displ = instr.MemoryDisplacement; break;
						case 8: displ = instr.MemoryDisplacement64; break;
						default:
							Debug.Fail($"Unknown mem displ size: {instr.MemoryDisplSize}");
							goto case 8;
						}
						if (IsCodeAddress(blocks, displ))
							Add(targets, displ, TargetKind.Branch);
					}
				}
			}
			foreach (var block in blocks) {
				if (targets.TryGetValue(block.Address, out var origKind)) {
					if (origKind < TargetKind.BlockStart && origKind != TargetKind.Unknown)
						targets[block.Address] = TargetKind.BlockStart;
				}
				else
					targets.Add(block.Address, TargetKind.Unknown);
			}

			var newBlocks = new List<BlockInfo>();
			BlockInfo currentBlock = default;
			int labelIndex = 0, methodIndex = 0;
			for (int i = 0; i < instrInfo.Count; i++) {
				var info = instrInfo[i];
				ref var instr = ref info.instruction;
				if (targets.TryGetValue(instr.IP, out var targetKind)) {
					var origBlock = blocks[info.block];
					currentBlock = new BlockInfo(targetKind, origBlock.Kind, instr.IP, origBlock.Address == instr.IP ? origBlock.Comment : null);
					newBlocks.Add(currentBlock);
				}
				// The addr of each block is always in the dictionary so currentBlock is initialized
				Debug2.Assert(!(currentBlock.Instructions is null));
				currentBlock.Instructions.Add(new X86InstructionInfo(info.code, instr));
			}

			newBlocks.Sort((a, b) => a.Address.CompareTo(b.Address));

			var x86Blocks = new Block[newBlocks.Count];
			for (int i = 0; i < newBlocks.Count; i++) {
				var block = newBlocks[i];

				var instructions = block.Instructions;
				var x86Instructions = new X86InstructionInfo[instructions.Count];
				for (int j = 0; j < instructions.Count; j++)
					x86Instructions[j] = instructions[j];

				string? label;
				FormatterTextKind labelKind;
				switch (block.TargetKind) {
				case TargetKind.Unknown:
					label = null;
					labelKind = default;
					break;

				case TargetKind.Data:
					label = GetLabel(labelIndex++);
					labelKind = FormatterTextKind.Data;
					break;

				case TargetKind.BlockStart:
				case TargetKind.Branch:
					label = GetLabel(labelIndex++);
					labelKind = FormatterTextKind.Label;
					break;

				case TargetKind.Call:
					label = GetFunc(methodIndex++);
					labelKind = FormatterTextKind.Function;
					break;

				default:
					Debug.Fail($"Unknown target kind: {block.TargetKind}");
					goto case TargetKind.Unknown;
				}

				x86Blocks[i] = new Block(block.Kind, block.Address, block.Comment, label, labelKind, x86Instructions);
			}
			return x86Blocks;
		}

		static bool IsCodeAddress(NativeCodeBlock[] blocks, ulong address) {
			foreach (var block in blocks) {
				if (address >= block.Address && address < block.Address + (uint)block.Code.Count)
					return true;
			}
			return false;
		}
	}
}
