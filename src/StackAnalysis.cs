using System;
using System.Collections.Generic;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	// Imutable
	public struct CilStackSlot {
		ByteCode allocadedBy;
		Cecil.TypeReference type;
		
		public ByteCode AllocadedBy {
			get { return allocadedBy; }
		}
		
		public Cecil.TypeReference Type {
			get { return type; }
		}
		
		public CilStackSlot(ByteCode allocadedBy, Cecil.TypeReference type)
		{
			this.allocadedBy = allocadedBy;
			this.type = type;
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			if (allocadedBy != null) hashCode ^= allocadedBy.GetHashCode(); 
			if (type != null) hashCode ^= type.GetHashCode(); 
			return hashCode;
		}
		
		public override bool Equals(object obj)
		{
			if (!(obj is CilStackSlot)) return false; 
			CilStackSlot myCilStackSlot = (CilStackSlot)obj;
			return object.Equals(this.allocadedBy, myCilStackSlot.allocadedBy) && object.Equals(this.type, myCilStackSlot.type);
		}
		
		public override string ToString()
		{
			if (allocadedBy == null) {
				return "<??>";
			} else {
				return string.Format("expr{0:X2}", this.allocadedBy.Offset);
			}
		}
	}
	
	/// <remarks> The tail of the list is the top of the stack </remarks>
	public class CilStack: List<CilStackSlot> {
		public static CilStack Empty = new CilStack();
		
		public CilStack Clone()
		{
			return new CilStack(this);
		}
		
		public CilStackSlot[] PopCount(int count)
		{
			CilStackSlot[] poped = this.GetRange(this.Count - count, count).ToArray();
			this.RemoveRange(this.Count - count, count);
			return poped;
		}
		
		public void Push(CilStackSlot slot)
		{
			this.Add(slot);
		}
		
		public CilStackSlot Peek(int depth)
		{
			return this[this.Count - depth];
		}
		
		public CilStack(): base()
		{
			
		}
		
		public CilStack(IEnumerable<CilStackSlot> slotEnum): base(slotEnum)
		{
			
		}
		
		public override string ToString()
		{
			string ret = "Stack: {";
			bool first = true;
			foreach(CilStackSlot slot in this) {
				if (!first) ret += ", ";
				ret += slot.ToString();
				first = false;
			}
			ret += "}";
			return ret;
		}
	}
	
	public partial class StackAnalysis {
		MethodDefinition methodDef;
		Dictionary<ByteCode, CilStack> stackBefore = new Dictionary<ByteCode, CilStack>();
		Dictionary<ByteCode, CilStack> stackAfter = new Dictionary<ByteCode, CilStack>();
		Dictionary<ByteCode, List<ByteCode>> branchTargetOf = new Dictionary<ByteCode, List<ByteCode>>();
		
		public Dictionary<ByteCode, CilStack> StackBefore {
			get { return stackBefore; }
		}
		
		public Dictionary<ByteCode, CilStack> StackAfter {
			get { return stackAfter; }
		}
		
		public Dictionary<ByteCode, List<ByteCode>> BranchTargetOf {
			get { return branchTargetOf; }
		}
		
		public Cecil.TypeReference GetTypeOf(ByteCode byteCode)
		{
			if (byteCode.PushCount == 0) {
				return TypeVoid;
			} else {
				return StackAfter[byteCode].Peek(1).Type;
			}
		}
		
		public StackAnalysis(MethodDefinition methodDef, ByteCodeCollection byteCodeCol) {
			this.methodDef = methodDef;
			
			foreach(ByteCode byteCode in byteCodeCol) {
				stackBefore[byteCode] = null;
				stackAfter[byteCode] = null;
				branchTargetOf[byteCode] = new List<ByteCode>();
			}
			
			foreach(ByteCode byteCode in byteCodeCol) {
				if (byteCode.CanBranch) {
					branchTargetOf[byteCode.BranchTarget].Add(byteCode);
				}
			}
			
			if (byteCodeCol.Count > 0) {
				ByteCode firstInst = byteCodeCol[0];
				stackBefore[firstInst] = CilStack.Empty;
				ProcessByteCodeRec(firstInst);
			}
		}
		
		void ProcessByteCodeRec(ByteCode byteCode)
		{
			stackAfter[byteCode] = ChangeStack(stackBefore[byteCode], byteCode);
			
			switch(byteCode.OpCode.FlowControl) {
				case FlowControl.Branch:
					CopyStack(byteCode, byteCode.BranchTarget);
					break;
				case FlowControl.Cond_Branch:
					CopyStack(byteCode, byteCode.Next);
					CopyStack(byteCode, byteCode.BranchTarget);
					break;
				case FlowControl.Next:
				case FlowControl.Call:
					CopyStack(byteCode, byteCode.Next);
					break;
				case FlowControl.Return:
					if (stackAfter[byteCode].Count > 0) throw new Exception("Non-empty stack at the end");
					break;
				default: throw new NotImplementedException();
			}
		}
		
		CilStack ChangeStack(CilStack oldStack, ByteCode byteCode)
		{
			CilStack newStack = oldStack.Clone();
			CilStackSlot[] popedSlots = newStack.PopCount(byteCode.PopCount);
			List<Cecil.TypeReference> typeArgs = new List<Cecil.TypeReference>();
			foreach(CilStackSlot slot in popedSlots) {
				typeArgs.Add(slot.Type);
			}
			for (int i = 0; i < byteCode.PushCount; i++) {
				newStack.Push(new CilStackSlot(byteCode, GetType(methodDef, byteCode, typeArgs.ToArray())));
			}
			return newStack;
		}
		
		void CopyStack(ByteCode byteCodeFrom, ByteCode byteCodeTo)
		{
			CilStack mergedStack;
			if (!Merge(stackAfter[byteCodeFrom], stackBefore[byteCodeTo], out mergedStack)) {
				stackBefore[byteCodeTo] = mergedStack;
				ProcessByteCodeRec(byteCodeTo);
			}
		}
		
		bool Merge(CilStack stack1, CilStack stack2, out CilStack merged)
		{
			// Both null
			if (stack1 == null && stack2 == null) {
				throw new Exception("Both stacks are null");
			}
			// One of stacks null, one is not
			if (stack1 == null || stack2 == null) {
				merged = stack1 ?? stack2;
				return false;
			}
			// Both are non-null
			if (stack1.Count != stack2.Count) {
				throw new Exception("Stack merge error: different sizes");
			}
			
			bool same = true;
			int count = stack1.Count;
			merged = stack1.Clone();
			for (int i = 0; i < count; i++) {
				if (!stack1[i].Equals(stack2[i])) {
					merged[i] = new CilStackSlot(null, null); // Merge slots
					same = false;
				}
			}
			return same;
		}
	}
}
