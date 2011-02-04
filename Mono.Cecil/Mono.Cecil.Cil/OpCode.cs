//
// OpCode.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil.Cil {

	public enum FlowControl {
		Branch,
		Break,
		Call,
		Cond_Branch,
		Meta,
		Next,
		Phi,
		Return,
		Throw,
	}

	public enum OpCodeType {
		Annotation,
		Macro,
		Nternal,
		Objmodel,
		Prefix,
		Primitive,
	}

	public enum OperandType {
		InlineBrTarget,
		InlineField,
		InlineI,
		InlineI8,
		InlineMethod,
		InlineNone,
		InlinePhi,
		InlineR,
		InlineSig,
		InlineString,
		InlineSwitch,
		InlineTok,
		InlineType,
		InlineVar,
		InlineArg,
		ShortInlineBrTarget,
		ShortInlineI,
		ShortInlineR,
		ShortInlineVar,
		ShortInlineArg,
	}

	public enum StackBehaviour {
		Pop0,
		Pop1,
		Pop1_pop1,
		Popi,
		Popi_pop1,
		Popi_popi,
		Popi_popi8,
		Popi_popi_popi,
		Popi_popr4,
		Popi_popr8,
		Popref,
		Popref_pop1,
		Popref_popi,
		Popref_popi_popi,
		Popref_popi_popi8,
		Popref_popi_popr4,
		Popref_popi_popr8,
		Popref_popi_popref,
		PopAll,
		Push0,
		Push1,
		Push1_push1,
		Pushi,
		Pushi8,
		Pushr4,
		Pushr8,
		Pushref,
		Varpop,
		Varpush,
	}

	public struct OpCode {

		readonly byte op1;
		readonly byte op2;
		readonly byte code;
		readonly byte flow_control;
		readonly byte opcode_type;
		readonly byte operand_type;
		readonly byte stack_behavior_pop;
		readonly byte stack_behavior_push;

		public string Name {
			get { return OpCodeNames.names [op1 == 0xff ? op2 : op2 + 256]; }
		}

		public int Size {
			get { return op1 == 0xff ? 1 : 2; }
		}

		public byte Op1 {
			get { return op1; }
		}

		public byte Op2 {
			get { return op2; }
		}

		public short Value {
			get { return (short) ((op1 << 8) | op2); }
		}

		public Code Code {
			get { return (Code) code; }
		}

		public FlowControl FlowControl {
			get { return (FlowControl) flow_control; }
		}

		public OpCodeType OpCodeType {
			get { return (OpCodeType) opcode_type; }
		}

		public OperandType OperandType {
			get { return (OperandType) operand_type; }
		}

		public StackBehaviour StackBehaviourPop {
			get { return (StackBehaviour) stack_behavior_pop; }
		}

		public StackBehaviour StackBehaviourPush {
			get { return (StackBehaviour) stack_behavior_push; }
		}

		internal OpCode (int x, int y)
		{
			this.op1 = (byte) ((x >> 0) & 0xff);
			this.op2 = (byte) ((x >> 8) & 0xff);
			this.code = (byte) ((x >> 16) & 0xff);
			this.flow_control = (byte) ((x >> 24) & 0xff);

			this.opcode_type = (byte) ((y >> 0) & 0xff);
			this.operand_type = (byte) ((y >> 8) & 0xff);
			this.stack_behavior_pop = (byte) ((y >> 16) & 0xff);
			this.stack_behavior_push = (byte) ((y >> 24) & 0xff);

			if (op1 == 0xff)
				OpCodes.OneByteOpCode [op2] = this;
			else
				OpCodes.TwoBytesOpCode [op2] = this;
		}

		public override int GetHashCode ()
		{
			return Value;
		}

		public override bool Equals (object obj)
		{
			if (!(obj is OpCode))
				return false;

			var opcode = (OpCode) obj;
			return op1 == opcode.op1 && op2 == opcode.op2;
		}

		public bool Equals (OpCode opcode)
		{
			return op1 == opcode.op1 && op2 == opcode.op2;
		}

		public static bool operator == (OpCode one, OpCode other)
		{
			return one.op1 == other.op1 && one.op2 == other.op2;
		}

		public static bool operator != (OpCode one, OpCode other)
		{
			return one.op1 != other.op1 || one.op2 != other.op2;
		}

		public override string ToString ()
		{
			return Name;
		}
	}

	static class OpCodeNames {

		internal static readonly string [] names = {
			"nop",
			"break",
			"ldarg.0",
			"ldarg.1",
			"ldarg.2",
			"ldarg.3",
			"ldloc.0",
			"ldloc.1",
			"ldloc.2",
			"ldloc.3",
			"stloc.0",
			"stloc.1",
			"stloc.2",
			"stloc.3",
			"ldarg.s",
			"ldarga.s",
			"starg.s",
			"ldloc.s",
			"ldloca.s",
			"stloc.s",
			"ldnull",
			"ldc.i4.m1",
			"ldc.i4.0",
			"ldc.i4.1",
			"ldc.i4.2",
			"ldc.i4.3",
			"ldc.i4.4",
			"ldc.i4.5",
			"ldc.i4.6",
			"ldc.i4.7",
			"ldc.i4.8",
			"ldc.i4.s",
			"ldc.i4",
			"ldc.i8",
			"ldc.r4",
			"ldc.r8",
			null,
			"dup",
			"pop",
			"jmp",
			"call",
			"calli",
			"ret",
			"br.s",
			"brfalse.s",
			"brtrue.s",
			"beq.s",
			"bge.s",
			"bgt.s",
			"ble.s",
			"blt.s",
			"bne.un.s",
			"bge.un.s",
			"bgt.un.s",
			"ble.un.s",
			"blt.un.s",
			"br",
			"brfalse",
			"brtrue",
			"beq",
			"bge",
			"bgt",
			"ble",
			"blt",
			"bne.un",
			"bge.un",
			"bgt.un",
			"ble.un",
			"blt.un",
			"switch",
			"ldind.i1",
			"ldind.u1",
			"ldind.i2",
			"ldind.u2",
			"ldind.i4",
			"ldind.u4",
			"ldind.i8",
			"ldind.i",
			"ldind.r4",
			"ldind.r8",
			"ldind.ref",
			"stind.ref",
			"stind.i1",
			"stind.i2",
			"stind.i4",
			"stind.i8",
			"stind.r4",
			"stind.r8",
			"add",
			"sub",
			"mul",
			"div",
			"div.un",
			"rem",
			"rem.un",
			"and",
			"or",
			"xor",
			"shl",
			"shr",
			"shr.un",
			"neg",
			"not",
			"conv.i1",
			"conv.i2",
			"conv.i4",
			"conv.i8",
			"conv.r4",
			"conv.r8",
			"conv.u4",
			"conv.u8",
			"callvirt",
			"cpobj",
			"ldobj",
			"ldstr",
			"newobj",
			"castclass",
			"isinst",
			"conv.r.un",
			null,
			null,
			"unbox",
			"throw",
			"ldfld",
			"ldflda",
			"stfld",
			"ldsfld",
			"ldsflda",
			"stsfld",
			"stobj",
			"conv.ovf.i1.un",
			"conv.ovf.i2.un",
			"conv.ovf.i4.un",
			"conv.ovf.i8.un",
			"conv.ovf.u1.un",
			"conv.ovf.u2.un",
			"conv.ovf.u4.un",
			"conv.ovf.u8.un",
			"conv.ovf.i.un",
			"conv.ovf.u.un",
			"box",
			"newarr",
			"ldlen",
			"ldelema",
			"ldelem.i1",
			"ldelem.u1",
			"ldelem.i2",
			"ldelem.u2",
			"ldelem.i4",
			"ldelem.u4",
			"ldelem.i8",
			"ldelem.i",
			"ldelem.r4",
			"ldelem.r8",
			"ldelem.ref",
			"stelem.i",
			"stelem.i1",
			"stelem.i2",
			"stelem.i4",
			"stelem.i8",
			"stelem.r4",
			"stelem.r8",
			"stelem.ref",
			"ldelem.any",
			"stelem.any",
			"unbox.any",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			"conv.ovf.i1",
			"conv.ovf.u1",
			"conv.ovf.i2",
			"conv.ovf.u2",
			"conv.ovf.i4",
			"conv.ovf.u4",
			"conv.ovf.i8",
			"conv.ovf.u8",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			"refanyval",
			"ckfinite",
			null,
			null,
			"mkrefany",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			"ldtoken",
			"conv.u2",
			"conv.u1",
			"conv.i",
			"conv.ovf.i",
			"conv.ovf.u",
			"add.ovf",
			"add.ovf.un",
			"mul.ovf",
			"mul.ovf.un",
			"sub.ovf",
			"sub.ovf.un",
			"endfinally",
			"leave",
			"leave.s",
			"stind.i",
			"conv.u",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			"prefix7",
			"prefix6",
			"prefix5",
			"prefix4",
			"prefix3",
			"prefix2",
			"prefix1",
			"prefixref",
			"arglist",
			"ceq",
			"cgt",
			"cgt.un",
			"clt",
			"clt.un",
			"ldftn",
			"ldvirtftn",
			null,
			"ldarg",
			"ldarga",
			"starg",
			"ldloc",
			"ldloca",
			"stloc",
			"localloc",
			null,
			"endfilter",
			"unaligned.",
			"volatile.",
			"tail.",
			"initobj",
			"constrained.",
			"cpblk",
			"initblk",
			"no.",		// added by spouliot to match Cecil existing definitions
			"rethrow",
			null,
			"sizeof",
			"refanytype",
			"readonly.",	// added by spouliot to match Cecil existing definitions
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
		};
	}
}
