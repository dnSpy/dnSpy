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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// IL instruction
	/// </summary>
	public readonly struct DbgILInstruction {
		/// <summary>
		/// Gets the offset
		/// </summary>
		public uint Offset { get; }

		/// <summary>
		/// Gets the opcode, <c>0x00XX</c> or <c>0xFEXX</c>
		/// </summary>
		public ushort OpCode { get; }

		/// <summary>
		/// Gets the operand
		/// </summary>
		public uint Operand { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="offset">Offset of instruction</param>
		/// <param name="opCode">IL opcode</param>
		/// <param name="operand">Integer operand</param>
		public DbgILInstruction(uint offset, ushort opCode, uint operand) {
			Offset = offset;
			OpCode = opCode;
			Operand = operand;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() =>
			$"{Offset:X4}: 0x{(OpCode <= byte.MaxValue ? OpCode.ToString("X2") : OpCode.ToString("X4"))} 0x{Operand:X8}";
	}
}
