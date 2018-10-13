/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

namespace dnSpy.Disassembly {
	static class FormatterOutputTextKindExtensions {
		public const FormatterOutputTextKind UnknownSymbol = (FormatterOutputTextKind)(-1);
		public const FormatterOutputTextKind Data = (FormatterOutputTextKind)(-2);
		public const FormatterOutputTextKind Label = (FormatterOutputTextKind)(-3);
		public const FormatterOutputTextKind Function = (FormatterOutputTextKind)(-4);
	}

	static class X86BlockFactory {
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
			public readonly string Comment;
			public readonly List<X86InstructionInfo> Instructions;
			public BlockInfo(TargetKind targetKind, NativeCodeBlockKind kind, ulong address, string comment) {
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

		static byte[] GetBytes(byte[] code, ulong address, ref Instruction instr) {
			int index = (int)(instr.IP64 - address);
			var instrBytes = new byte[instr.ByteLength];
			Array.Copy(code, index, instrBytes, 0, instrBytes.Length);
			return instrBytes;
		}

		static string GetLabel(int index) => LABEL_PREFIX + index.ToString();
		static string GetFunc(int index) => FUNC_PREFIX + index.ToString();

		public static X86Block[] Create(int bitness, NativeCodeBlock[] blocks) {
			var targets = new Dictionary<ulong, TargetKind>();
			var instrInfo = new List<(Instruction instruction, int block, byte[] code)>();
			for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++) {
				var block = blocks[blockIndex];

				var reader = new ByteArrayCodeReader(block.Code);
				var decoder = Decoder.Create(bitness, reader);
				decoder.InstructionPointer = block.Address;
				while (reader.CanReadByte) {
					decoder.Decode(out var instr);
					instrInfo.Add((instr, blockIndex, GetBytes(block.Code, block.Address, ref instr)));

					switch (instr.FlowControl) {
					case FlowControl.Next:
					case FlowControl.Interrupt:
						break;

					case FlowControl.UnconditionalBranch:
						Add(targets, instr.NextIP64, TargetKind.Unknown);
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
						Add(targets, instr.NextIP64, TargetKind.Unknown);
						// Unknown target
						break;

					case FlowControl.IndirectCall:
						// Unknown target
						break;

					case FlowControl.Return:
					case FlowControl.Exception:
						Add(targets, instr.NextIP64, TargetKind.Unknown);
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
								Add(targets, instr.IPRelativeMemoryAddress, TargetKind.Data);
								break;
							}
						}
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
				if (targets.TryGetValue(instr.IP64, out var targetKind)) {
					var origBlock = blocks[info.block];
					currentBlock = new BlockInfo(targetKind, origBlock.Kind, instr.IP64, origBlock.Address == instr.IP64 ? origBlock.Comment : null);
					newBlocks.Add(currentBlock);
				}
				currentBlock.Instructions.Add(new X86InstructionInfo(info.code, instr));
			}

			newBlocks.Sort((a, b) => a.Address.CompareTo(b.Address));

			var x86Blocks = new X86Block[newBlocks.Count];
			for (int i = 0; i < newBlocks.Count; i++) {
				var block = newBlocks[i];

				var instructions = block.Instructions;
				var x86Instructions = new X86InstructionInfo[instructions.Count];
				for (int j = 0; j < instructions.Count; j++) {
					var instr = instructions[j];
					x86Instructions[j] = new X86InstructionInfo(instr.Bytes, instr.Instruction);
				}

				string label;
				FormatterOutputTextKind labelKind;
				switch (block.TargetKind) {
				case TargetKind.Unknown:
					label = null;
					labelKind = default;
					break;

				case TargetKind.Data:
					label = GetLabel(labelIndex++);
					labelKind = FormatterOutputTextKindExtensions.Data;
					break;

				case TargetKind.BlockStart:
				case TargetKind.Branch:
					label = GetLabel(labelIndex++);
					labelKind = FormatterOutputTextKindExtensions.Label;
					break;

				case TargetKind.Call:
					label = GetFunc(methodIndex++);
					labelKind = FormatterOutputTextKindExtensions.Function;
					break;

				default:
					Debug.Fail($"Unknown target kind: {block.TargetKind}");
					goto case TargetKind.Unknown;
				}

				x86Blocks[i] = new X86Block(block.Kind, block.Address, block.Comment, label, labelKind, x86Instructions);
			}
			return x86Blocks;
		}
	}
}
