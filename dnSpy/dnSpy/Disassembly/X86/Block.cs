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
using dnSpy.Contracts.Disassembly;
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	readonly struct Block {
		public NativeCodeBlockKind Kind { get; }
		public ulong Address { get; }
		public string Comment { get; }
		public string Label { get; }
		public FormatterOutputTextKind LabelKind { get; }
		public X86InstructionInfo[] Instructions { get; }

		public Block(NativeCodeBlockKind kind, ulong address, string comment, string label, FormatterOutputTextKind labelKind, X86InstructionInfo[] instructions) {
			Kind = kind;
			Address = address;
			Comment = comment;
			Label = label;
			LabelKind = labelKind;
			Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
		}
	}

	struct X86InstructionInfo {
		public ArraySegment<byte> Code;
		public Instruction Instruction;
		public X86InstructionInfo(ArraySegment<byte> code, in Instruction instruction) {
			Code = code;
			Instruction = instruction;
		}
	}
}
