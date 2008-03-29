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
		
		public ByteCodeCollection(MethodDefinition methodDef)
		{
			foreach(Instruction inst in methodDef.Body.Instructions) {
				list.Add(new ByteCode(methodDef, inst));
			}
			foreach(ByteCode byteCode in this) {
				if (byteCode.CanBranch) {
					if (byteCode.Operand is Instruction[]) {
						List<ByteCode> operands = new List<ByteCode>();
						foreach(Instruction inst in (Instruction[])byteCode.Operand) {
							ByteCode target = GetByOffset(inst.Offset);
							operands.Add(target);
							target.BranchesHere.Add(byteCode);
						}
						byteCode.Operand = operands.ToArray();
					} else {
						ByteCode target = GetByOffset(((Instruction)byteCode.Operand).Offset);
						byteCode.Operand = target;
						target.BranchesHere.Add(byteCode);
					}
				}
			}
			UpdateNextPrevious();
			UpdateStackAnalysis(methodDef);
		}
		
		void UpdateNextPrevious()
		{
			for(int i = 0; i < list.Count - 1; i++) {
				this[i].Next = this[i + 1];
				this[i + 1].Previous = this[i];
			}
		}
		
		void UpdateStackAnalysis(MethodDefinition methodDef)
		{
			if (this.Count > 0) {
				this[0].MergeStackBeforeWith(CilStack.Empty);
			}
			foreach(ExceptionHandler handler in methodDef.Body.ExceptionHandlers) {
				ByteCode byteCode = this.GetByOffset(handler.HandlerStart.Offset);
				CilStack expetionStack = new CilStack();
				// TODO: Allocated by what?
				expetionStack.Add(new CilStackSlot(byteCode, ByteCode.TypeException));
				byteCode.MergeStackBeforeWith(expetionStack);
			}
		}
	}
}
