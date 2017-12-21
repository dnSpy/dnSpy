/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Provides code ranges for .NET steppers
	/// </summary>
	public abstract class DbgDotNetCodeRangeService {
		/// <summary>
		/// The offset is in an epilog
		/// </summary>
		public const uint EPILOG = 0xFFFFFFFF;

		/// <summary>
		/// The offset is in the prolog
		/// </summary>
		public const uint PROLOG = 0xFFFFFFFE;

		/// <summary>
		/// Gets code ranges
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">Offset of IL instruction, or one of <see cref="PROLOG"/>, <see cref="EPILOG"/></param>
		/// <param name="options">Options</param>
		/// <param name="callback">Gets called when the lookup is complete</param>
		public abstract void GetCodeRanges(DbgModule module, uint token, uint offset, GetCodeRangesOptions options, Action<GetCodeRangeResult> callback);
	}

	/// <summary>
	/// Get code ranges options
	/// </summary>
	[Flags]
	public enum GetCodeRangesOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Gets the instructions of the statements
		/// </summary>
		Instructions			= 0x00000001,
	}

	/// <summary>
	/// Contains the code ranges of the requested statement
	/// </summary>
	public readonly struct GetCodeRangeResult {
		/// <summary>
		/// Code ranges of the statement if <see cref="Success"/> is true
		/// </summary>
		public DbgCodeRange[] StatementRanges { get; }

		/// <summary>
		/// Gets the instructions if <see cref="GetCodeRangesOptions.Instructions"/> option was used
		/// </summary>
		public DbgILInstruction[][] StatementInstructions { get; }

		/// <summary>
		/// true if the code ranges were found
		/// </summary>
		public bool Success { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="success">true if successful</param>
		/// <param name="statementRanges">Code ranges of the statement</param>
		/// <param name="statementInstructions">Statement instructions</param>
		public GetCodeRangeResult(bool success, DbgCodeRange[] statementRanges, DbgILInstruction[][] statementInstructions) {
			Success = success;
			StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
			StatementInstructions = statementInstructions ?? throw new ArgumentNullException(nameof(statementInstructions));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="statementRanges">Code ranges of the statement</param>
		/// <param name="statementInstructions">Statement instructions</param>
		public GetCodeRangeResult(DbgCodeRange[] statementRanges, DbgILInstruction[][] statementInstructions) {
			Success = true;
			StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
			StatementInstructions = statementInstructions ?? throw new ArgumentNullException(nameof(statementInstructions));
		}
	}

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
