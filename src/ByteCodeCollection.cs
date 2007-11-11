using System;
using System.Collections;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class ByteCodeCollection: IEnumerable<ByteCode>
	{
		List<ByteCode> byteCodes = new List<ByteCode>();
		
		public IEnumerator<ByteCode> GetEnumerator()
		{
			return byteCodes.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return byteCodes.GetEnumerator();
		}
		
		public int Count {
			get {
				return byteCodes.Count;
			}
		}
		
		public ByteCode this[int index] {
			get {
				return byteCodes[index];
			}
		}
		
		public ByteCode GetByOffset(int offset)
		{
			foreach(ByteCode byteCode in this) {
				if (byteCode.Offset == offset) {
					return byteCode;
				}
			}
			throw new Exception("Not found");
		}
		
		public ByteCodeCollection(InstructionCollection instCol)
		{
			foreach(Instruction inst in instCol) {
				byteCodes.Add(new ByteCode(inst));
			}
			foreach(ByteCode byteCode in this) {
				if (byteCode.CanBranch) {
					byteCode.Operand = GetByOffset(((Instruction)byteCode.Operand).Offset);
				}
			}
			for(int i = 0; i < byteCodes.Count - 1; i++) {
				this[i].Next = this[i + 1];
			}
		}
	}
}
