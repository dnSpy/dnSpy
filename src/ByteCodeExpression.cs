using System;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class ByteCodeExpression
	{
		ControlFlow.BasicBlock basicBlock;
		
		OpCode opCode;
		object operand;
		List<ByteCodeExpression> arguments = new List<ByteCodeExpression>();
		bool returnsValue;
		Cecil.TypeReference type;
		
		ByteCodeExpression branchTarget;
		List<ByteCodeExpression> branchesHere = new List<ByteCodeExpression>();
		
		bool isSSASR = false;
		
		public Decompiler.ControlFlow.BasicBlock BasicBlock {
			get { return basicBlock; }
			set {
				basicBlock = value;
				foreach (ByteCodeExpression argument in arguments) {
					argument.BasicBlock = value;
				}
			}
		}
		
		public OpCode OpCode {
			get { return opCode; }
			set { opCode = value; }
		}
		
		public object Operand {
			get { return operand; }
			set { operand = value; }
		}
		
		public List<ByteCodeExpression> Arguments {
			get { return arguments; }
		}
		
		public bool ReturnsValue {
			get { return returnsValue; }
			set { returnsValue = value; }
		}
		
		public TypeReference Type {
			get { return type; }
			set { type = value; }
		}
		
		/// <summary> Single static assignment; single read </summary>
		public bool IsSSASR {
			get { return isSSASR; }
			set { isSSASR = value; }
		}
		
		public ByteCodeExpression BranchTarget {
			get { return branchTarget; }
			set { branchTarget = value; }
		}
		
		public List<ByteCodeExpression> BranchesHere {
			get { return branchesHere; }
		}
		
		public bool IsBranchTarget {
			get { return BranchesHere.Count > 0; }
		}
		
		public static ByteCodeExpression Ldloc(string name)
		{
			return new ByteCodeExpression(OpCodes.Ldloc, new VariableDefinition(name, null), true);
		}
		
		public static ByteCodeExpression Stloc(string name)
		{
			return new ByteCodeExpression(OpCodes.Stloc, new VariableDefinition(name, null), false);
		}
		
		public ByteCodeExpression(OpCode opCode, object operand, bool returnsValue)
		{
			this.opCode = opCode;
			this.operand = operand;
			this.returnsValue = returnsValue;
		}
		
		public ByteCodeExpression(ByteCode byteCode)
		{
			this.OpCode = byteCode.OpCode;
			this.Operand = byteCode.Operand;
			foreach(CilStackSlot arg in byteCode.StackBefore.PeekCount(byteCode.PopCount)) {
				string name = string.Format("expr{0:X2}", arg.AllocadedBy.Offset);
				this.Arguments.Add(Ldloc(name));
			}
			this.ReturnsValue = byteCode.PushCount > 0;
			this.Type = byteCode.Type;
		}
		
		public override string ToString()
		{
			return string.Format("[ByteCodeExpression OpCode={0}]", this.opCode);
		}
		
		public ByteCodeExpression Clone()
		{
			ByteCodeExpression clone = (ByteCodeExpression)this.MemberwiseClone();
			clone.branchTarget = null;
			clone.branchesHere = new List<ByteCodeExpression>();
			return clone;
		}
		
		public bool IsConstant()
		{
			if (!IsOpCodeConstant()) return false;
			foreach(ByteCodeExpression arg in this.Arguments) {
				if (!arg.IsConstant()) return false;
			}
			return true;
		}
		
		bool IsOpCodeConstant()
		{
			switch(this.OpCode.Code) {
				case Code.Ldarg: return true;
				default: return false;
			}
		}
	}
}
