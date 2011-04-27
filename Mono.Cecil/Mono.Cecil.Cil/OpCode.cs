//
// OpCode.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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
			get { return OpCodeNames.names [(int) Code]; }
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
			get { return op1 == 0xff ? op2 : (short) ((op1 << 8) | op2); }
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

		internal static readonly string [] names;

		static OpCodeNames ()
		{
			var table = new byte [] {
				3, 110, 111, 112,
				5, 98, 114, 101, 97, 107,
				7, 108, 100, 97, 114, 103, 46, 48,
				7, 108, 100, 97, 114, 103, 46, 49,
				7, 108, 100, 97, 114, 103, 46, 50,
				7, 108, 100, 97, 114, 103, 46, 51,
				7, 108, 100, 108, 111, 99, 46, 48,
				7, 108, 100, 108, 111, 99, 46, 49,
				7, 108, 100, 108, 111, 99, 46, 50,
				7, 108, 100, 108, 111, 99, 46, 51,
				7, 115, 116, 108, 111, 99, 46, 48,
				7, 115, 116, 108, 111, 99, 46, 49,
				7, 115, 116, 108, 111, 99, 46, 50,
				7, 115, 116, 108, 111, 99, 46, 51,
				7, 108, 100, 97, 114, 103, 46, 115,
				8, 108, 100, 97, 114, 103, 97, 46, 115,
				7, 115, 116, 97, 114, 103, 46, 115,
				7, 108, 100, 108, 111, 99, 46, 115,
				8, 108, 100, 108, 111, 99, 97, 46, 115,
				7, 115, 116, 108, 111, 99, 46, 115,
				6, 108, 100, 110, 117, 108, 108,
				9, 108, 100, 99, 46, 105, 52, 46, 109, 49,
				8, 108, 100, 99, 46, 105, 52, 46, 48,
				8, 108, 100, 99, 46, 105, 52, 46, 49,
				8, 108, 100, 99, 46, 105, 52, 46, 50,
				8, 108, 100, 99, 46, 105, 52, 46, 51,
				8, 108, 100, 99, 46, 105, 52, 46, 52,
				8, 108, 100, 99, 46, 105, 52, 46, 53,
				8, 108, 100, 99, 46, 105, 52, 46, 54,
				8, 108, 100, 99, 46, 105, 52, 46, 55,
				8, 108, 100, 99, 46, 105, 52, 46, 56,
				8, 108, 100, 99, 46, 105, 52, 46, 115,
				6, 108, 100, 99, 46, 105, 52,
				6, 108, 100, 99, 46, 105, 56,
				6, 108, 100, 99, 46, 114, 52,
				6, 108, 100, 99, 46, 114, 56,
				3, 100, 117, 112,
				3, 112, 111, 112,
				3, 106, 109, 112,
				4, 99, 97, 108, 108,
				5, 99, 97, 108, 108, 105,
				3, 114, 101, 116,
				4, 98, 114, 46, 115,
				9, 98, 114, 102, 97, 108, 115, 101, 46, 115,
				8, 98, 114, 116, 114, 117, 101, 46, 115,
				5, 98, 101, 113, 46, 115,
				5, 98, 103, 101, 46, 115,
				5, 98, 103, 116, 46, 115,
				5, 98, 108, 101, 46, 115,
				5, 98, 108, 116, 46, 115,
				8, 98, 110, 101, 46, 117, 110, 46, 115,
				8, 98, 103, 101, 46, 117, 110, 46, 115,
				8, 98, 103, 116, 46, 117, 110, 46, 115,
				8, 98, 108, 101, 46, 117, 110, 46, 115,
				8, 98, 108, 116, 46, 117, 110, 46, 115,
				2, 98, 114,
				7, 98, 114, 102, 97, 108, 115, 101,
				6, 98, 114, 116, 114, 117, 101,
				3, 98, 101, 113,
				3, 98, 103, 101,
				3, 98, 103, 116,
				3, 98, 108, 101,
				3, 98, 108, 116,
				6, 98, 110, 101, 46, 117, 110,
				6, 98, 103, 101, 46, 117, 110,
				6, 98, 103, 116, 46, 117, 110,
				6, 98, 108, 101, 46, 117, 110,
				6, 98, 108, 116, 46, 117, 110,
				6, 115, 119, 105, 116, 99, 104,
				8, 108, 100, 105, 110, 100, 46, 105, 49,
				8, 108, 100, 105, 110, 100, 46, 117, 49,
				8, 108, 100, 105, 110, 100, 46, 105, 50,
				8, 108, 100, 105, 110, 100, 46, 117, 50,
				8, 108, 100, 105, 110, 100, 46, 105, 52,
				8, 108, 100, 105, 110, 100, 46, 117, 52,
				8, 108, 100, 105, 110, 100, 46, 105, 56,
				7, 108, 100, 105, 110, 100, 46, 105,
				8, 108, 100, 105, 110, 100, 46, 114, 52,
				8, 108, 100, 105, 110, 100, 46, 114, 56,
				9, 108, 100, 105, 110, 100, 46, 114, 101, 102,
				9, 115, 116, 105, 110, 100, 46, 114, 101, 102,
				8, 115, 116, 105, 110, 100, 46, 105, 49,
				8, 115, 116, 105, 110, 100, 46, 105, 50,
				8, 115, 116, 105, 110, 100, 46, 105, 52,
				8, 115, 116, 105, 110, 100, 46, 105, 56,
				8, 115, 116, 105, 110, 100, 46, 114, 52,
				8, 115, 116, 105, 110, 100, 46, 114, 56,
				3, 97, 100, 100,
				3, 115, 117, 98,
				3, 109, 117, 108,
				3, 100, 105, 118,
				6, 100, 105, 118, 46, 117, 110,
				3, 114, 101, 109,
				6, 114, 101, 109, 46, 117, 110,
				3, 97, 110, 100,
				2, 111, 114,
				3, 120, 111, 114,
				3, 115, 104, 108,
				3, 115, 104, 114,
				6, 115, 104, 114, 46, 117, 110,
				3, 110, 101, 103,
				3, 110, 111, 116,
				7, 99, 111, 110, 118, 46, 105, 49,
				7, 99, 111, 110, 118, 46, 105, 50,
				7, 99, 111, 110, 118, 46, 105, 52,
				7, 99, 111, 110, 118, 46, 105, 56,
				7, 99, 111, 110, 118, 46, 114, 52,
				7, 99, 111, 110, 118, 46, 114, 56,
				7, 99, 111, 110, 118, 46, 117, 52,
				7, 99, 111, 110, 118, 46, 117, 56,
				8, 99, 97, 108, 108, 118, 105, 114, 116,
				5, 99, 112, 111, 98, 106,
				5, 108, 100, 111, 98, 106,
				5, 108, 100, 115, 116, 114,
				6, 110, 101, 119, 111, 98, 106,
				9, 99, 97, 115, 116, 99, 108, 97, 115, 115,
				6, 105, 115, 105, 110, 115, 116,
				9, 99, 111, 110, 118, 46, 114, 46, 117, 110,
				5, 117, 110, 98, 111, 120,
				5, 116, 104, 114, 111, 119,
				5, 108, 100, 102, 108, 100,
				6, 108, 100, 102, 108, 100, 97,
				5, 115, 116, 102, 108, 100,
				6, 108, 100, 115, 102, 108, 100,
				7, 108, 100, 115, 102, 108, 100, 97,
				6, 115, 116, 115, 102, 108, 100,
				5, 115, 116, 111, 98, 106,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 49, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 50, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 52, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 56, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 49, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 50, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 52, 46, 117, 110,
				14, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 56, 46, 117, 110,
				13, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 46, 117, 110,
				13, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 46, 117, 110,
				3, 98, 111, 120,
				6, 110, 101, 119, 97, 114, 114,
				5, 108, 100, 108, 101, 110,
				7, 108, 100, 101, 108, 101, 109, 97,
				9, 108, 100, 101, 108, 101, 109, 46, 105, 49,
				9, 108, 100, 101, 108, 101, 109, 46, 117, 49,
				9, 108, 100, 101, 108, 101, 109, 46, 105, 50,
				9, 108, 100, 101, 108, 101, 109, 46, 117, 50,
				9, 108, 100, 101, 108, 101, 109, 46, 105, 52,
				9, 108, 100, 101, 108, 101, 109, 46, 117, 52,
				9, 108, 100, 101, 108, 101, 109, 46, 105, 56,
				8, 108, 100, 101, 108, 101, 109, 46, 105,
				9, 108, 100, 101, 108, 101, 109, 46, 114, 52,
				9, 108, 100, 101, 108, 101, 109, 46, 114, 56,
				10, 108, 100, 101, 108, 101, 109, 46, 114, 101, 102,
				8, 115, 116, 101, 108, 101, 109, 46, 105,
				9, 115, 116, 101, 108, 101, 109, 46, 105, 49,
				9, 115, 116, 101, 108, 101, 109, 46, 105, 50,
				9, 115, 116, 101, 108, 101, 109, 46, 105, 52,
				9, 115, 116, 101, 108, 101, 109, 46, 105, 56,
				9, 115, 116, 101, 108, 101, 109, 46, 114, 52,
				9, 115, 116, 101, 108, 101, 109, 46, 114, 56,
				10, 115, 116, 101, 108, 101, 109, 46, 114, 101, 102,
				10, 108, 100, 101, 108, 101, 109, 46, 97, 110, 121,
				10, 115, 116, 101, 108, 101, 109, 46, 97, 110, 121,
				9, 117, 110, 98, 111, 120, 46, 97, 110, 121,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 49,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 49,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 50,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 50,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 52,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 52,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105, 56,
				11, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117, 56,
				9, 114, 101, 102, 97, 110, 121, 118, 97, 108,
				8, 99, 107, 102, 105, 110, 105, 116, 101,
				8, 109, 107, 114, 101, 102, 97, 110, 121,
				7, 108, 100, 116, 111, 107, 101, 110,
				7, 99, 111, 110, 118, 46, 117, 50,
				7, 99, 111, 110, 118, 46, 117, 49,
				6, 99, 111, 110, 118, 46, 105,
				10, 99, 111, 110, 118, 46, 111, 118, 102, 46, 105,
				10, 99, 111, 110, 118, 46, 111, 118, 102, 46, 117,
				7, 97, 100, 100, 46, 111, 118, 102,
				10, 97, 100, 100, 46, 111, 118, 102, 46, 117, 110,
				7, 109, 117, 108, 46, 111, 118, 102,
				10, 109, 117, 108, 46, 111, 118, 102, 46, 117, 110,
				7, 115, 117, 98, 46, 111, 118, 102,
				10, 115, 117, 98, 46, 111, 118, 102, 46, 117, 110,
				10, 101, 110, 100, 102, 105, 110, 97, 108, 108, 121,
				5, 108, 101, 97, 118, 101,
				7, 108, 101, 97, 118, 101, 46, 115,
				7, 115, 116, 105, 110, 100, 46, 105,
				6, 99, 111, 110, 118, 46, 117,
				7, 97, 114, 103, 108, 105, 115, 116,
				3, 99, 101, 113,
				3, 99, 103, 116,
				6, 99, 103, 116, 46, 117, 110,
				3, 99, 108, 116,
				6, 99, 108, 116, 46, 117, 110,
				5, 108, 100, 102, 116, 110,
				9, 108, 100, 118, 105, 114, 116, 102, 116, 110,
				5, 108, 100, 97, 114, 103,
				6, 108, 100, 97, 114, 103, 97,
				5, 115, 116, 97, 114, 103,
				5, 108, 100, 108, 111, 99,
				6, 108, 100, 108, 111, 99, 97,
				5, 115, 116, 108, 111, 99,
				8, 108, 111, 99, 97, 108, 108, 111, 99,
				9, 101, 110, 100, 102, 105, 108, 116, 101, 114,
				10, 117, 110, 97, 108, 105, 103, 110, 101, 100, 46,
				9, 118, 111, 108, 97, 116, 105, 108, 101, 46,
				5, 116, 97, 105, 108, 46,
				7, 105, 110, 105, 116, 111, 98, 106,
				12, 99, 111, 110, 115, 116, 114, 97, 105, 110, 101, 100, 46,
				5, 99, 112, 98, 108, 107,
				7, 105, 110, 105, 116, 98, 108, 107,
				3, 110, 111, 46,
				7, 114, 101, 116, 104, 114, 111, 119,
				6, 115, 105, 122, 101, 111, 102,
				10, 114, 101, 102, 97, 110, 121, 116, 121, 112, 101,
				9, 114, 101, 97, 100, 111, 110, 108, 121, 46,
			};

			names = new string [219];

			for (int i = 0, p = 0; i < names.Length; i++) {
				var buffer = new char [table [p++]];

				for (int j = 0; j < buffer.Length; j++)
					buffer [j] = (char) table [p++];

				names [i] = new string (buffer);
			}
		}
	}
}
