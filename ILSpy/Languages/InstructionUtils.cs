/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy
{
	static class InstructionUtils
	{
		public static void AddOpCode(IList<short> instrs, Code code)
		{
			if ((uint)code <= 0xFF)
				instrs.Add((byte)code);
			else if (((uint)code >> 8) == 0xFE) {
				instrs.Add((byte)((uint)code >> 8));
				instrs.Add(unchecked((byte)code));
			}
			else if (code == Code.UNKNOWN1)
				instrs.AddUnknownByte();
			else if (code == Code.UNKNOWN2)
				instrs.AddUnknownInt16();
			else
				throw new InvalidOperationException();
		}

		static void AddUnknownByte(this IList<short> instrs)
		{
			instrs.Add(-1);
		}

		static void AddUnknownInt16(this IList<short> instrs)
		{
			instrs.Add(-1);
			instrs.Add(-1);
		}

		static void AddUnknownInt32(this IList<short> instrs)
		{
			instrs.Add(-1);
			instrs.Add(-1);
			instrs.Add(-1);
			instrs.Add(-1);
		}

		static void AddUnknownInt64(this IList<short> instrs)
		{
			instrs.AddUnknownInt32();
			instrs.AddUnknownInt32();
		}

		static void AddInt16(this IList<short> instrs, short val)
		{
			instrs.Add(unchecked((byte)val));
			instrs.Add(unchecked((byte)(val >> 8)));
		}

		static void AddInt32(this IList<short> instrs, int val)
		{
			instrs.Add(unchecked((byte)val));
			instrs.Add(unchecked((byte)(val >> 8)));
			instrs.Add(unchecked((byte)(val >> 16)));
			instrs.Add(unchecked((byte)(val >> 24)));
		}

		static void AddInt64(this IList<short> instrs, long val)
		{
			instrs.Add(unchecked((byte)val));
			instrs.Add(unchecked((byte)(val >> 8)));
			instrs.Add(unchecked((byte)(val >> 16)));
			instrs.Add(unchecked((byte)(val >> 24)));
			instrs.Add(unchecked((byte)(val >> 32)));
			instrs.Add(unchecked((byte)(val >> 40)));
			instrs.Add(unchecked((byte)(val >> 48)));
			instrs.Add(unchecked((byte)(val >> 56)));
		}

		static void AddSingle(this IList<short> instrs, float val)
		{
			foreach (var b in BitConverter.GetBytes(val))
				instrs.Add(b);
		}

		static void AddDouble(this IList<short> instrs, double val)
		{
			foreach (var b in BitConverter.GetBytes(val))
				instrs.Add(b);
		}

		static void AddToken(this IList<short> instrs, ModuleDefMD module, uint token)
		{
			if (module == null || module.ResolveToken(token) == null)
				instrs.AddUnknownInt32();
			else
				instrs.AddInt32(unchecked((int)token));
		}

		public static void AddOperand(IList<short> instrs, ModuleDefMD module, uint offset, OpCode opCode, object operand)
		{
			Instruction target;
			IVariable variable;
			switch (opCode.OperandType) {
			case OperandType.InlineBrTarget:
				target = operand as Instruction;
				if (target == null)
					instrs.AddUnknownInt32();
				else
					instrs.AddInt32(unchecked((int)target.Offset - (int)(offset + 4)));
				break;

			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
			case OperandType.InlineType:
				var tok = operand as ITokenOperand;
				instrs.AddToken(module, tok == null ? 0 : tok.MDToken.Raw);
				break;

			case OperandType.InlineSig:
				var msig = operand as MethodSig;
				instrs.AddToken(module, msig == null ? 0 : msig.OriginalToken);
				break;

			case OperandType.InlineString:
				instrs.AddUnknownInt32();
				break;

			case OperandType.InlineI:
				if (operand is int)
					instrs.AddInt32((int)operand);
				else
					instrs.AddUnknownInt32();
				break;

			case OperandType.InlineI8:
				if (operand is long)
					instrs.AddInt64((long)operand);
				else
					instrs.AddUnknownInt64();
				break;

			case OperandType.InlineR:
				if (operand is double)
					instrs.AddDouble((double)operand);
				else
					instrs.AddUnknownInt64();
				break;

			case OperandType.ShortInlineR:
				if (operand is float)
					instrs.AddSingle((float)operand);
				else
					instrs.AddUnknownInt32();
				break;

			case OperandType.InlineSwitch:
				var targets = operand as IList<Instruction>;
				if (targets == null)
					instrs.AddUnknownInt32();
				else {
					uint offsetAfter = offset + 4 + (uint)targets.Count * 4;
					instrs.AddInt32(targets.Count);
					foreach (var instr in targets) {
						if (instr == null)
							instrs.AddUnknownInt32();
						else
							instrs.AddInt32(unchecked((int)instr.Offset - (int)offsetAfter));
					}
				}
				break;

			case OperandType.InlineVar:
				variable = operand as IVariable;
				if (variable == null)
					instrs.AddUnknownInt16();
				else if (ushort.MinValue <= variable.Index && variable.Index <= ushort.MaxValue)
					instrs.AddInt16(unchecked((short)variable.Index));
				else
					instrs.AddUnknownInt16();
				break;

			case OperandType.ShortInlineVar:
				variable = operand as IVariable;
				if (variable == null)
					instrs.AddUnknownByte();
				else if (byte.MinValue <= variable.Index && variable.Index <= byte.MaxValue)
					instrs.Add((byte)variable.Index);
				else
					instrs.AddUnknownByte();
				break;

			case OperandType.ShortInlineBrTarget:
				target = operand as Instruction;
				if (target == null)
					instrs.AddUnknownByte();
				else {
					int displ = unchecked((int)target.Offset - (int)(offset + 1));
					if (sbyte.MinValue <= displ && displ <= sbyte.MaxValue)
						instrs.Add((short)(displ & 0xFF));
					else
						instrs.AddUnknownByte();
				}
				break;

			case OperandType.ShortInlineI:
				if (operand is sbyte)
					instrs.Add((short)((sbyte)operand & 0xFF));
				else if (operand is byte)
					instrs.Add((short)((byte)operand & 0xFF));
				else
					instrs.AddUnknownByte();
				break;

			case OperandType.InlineNone:
			case OperandType.InlinePhi:
				break;

			default: throw new InvalidOperationException();
			}
		}
	}
}
