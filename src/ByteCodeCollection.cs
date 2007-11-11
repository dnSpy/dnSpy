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
		List<ByteCode> list = new List<ByteCode>();
		
		public IEnumerator<ByteCode> GetEnumerator()
		{
			return list.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		
		public int Count {
			get {
				return list.Count;
			}
		}
		
		public ByteCode this[int index] {
			get {
				return list[index];
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
		
		public int IndexOf(ByteCode byteCode)
		{
			return list.IndexOf(byteCode);
		}
		
		public void Remove(ByteCode byteCode)
		{
			int index = IndexOf(byteCode);
			list.RemoveAt(index);
			byteCode.Next = null;
			byteCode.Previous = null;
			UpdateNextPrevious();
		}
		
		public ByteCodeCollection(MethodDefinition methodDef)
		{
			foreach(Instruction inst in methodDef.Body.Instructions) {
				list.Add(new ByteCode(methodDef, inst));
			}
			foreach(ByteCode byteCode in this) {
				if (byteCode.CanBranch) {
					byteCode.Operand = GetByOffset(((Instruction)byteCode.Operand).Offset);
				}
			}
			UpdateNextPrevious();
		}
		
		void UpdateNextPrevious()
		{
			for(int i = 0; i < list.Count - 1; i++) {
				this[i].Next = this[i + 1];
				this[i + 1].Previous = this[i];
			}
		}
	}
}
