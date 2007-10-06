//
// MethodBody.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

	using Mono.Cecil;

	public sealed class MethodBody : IVariableDefinitionProvider, IScopeProvider, ICodeVisitable {

		MethodDefinition m_method;
		int m_maxStack;
		int m_codeSize;
		bool m_initLocals;
		int m_localVarToken;

		InstructionCollection m_instructions;
		ExceptionHandlerCollection m_exceptions;
		VariableDefinitionCollection m_variables;
		ScopeCollection m_scopes;

		private CilWorker m_cilWorker;

		public MethodDefinition Method {
			get { return m_method; }
		}

		public int MaxStack {
			get { return m_maxStack; }
			set { m_maxStack = value; }
		}

		public int CodeSize {
			get { return m_codeSize; }
			set { m_codeSize = value; }
		}

		public bool InitLocals {
			get { return m_initLocals; }
			set { m_initLocals = value; }
		}

		internal int LocalVarToken {
			get { return m_localVarToken; }
			set { m_localVarToken = value; }
		}

		public CilWorker CilWorker {
			get {
				if (m_cilWorker == null)
					m_cilWorker = new CilWorker (this);
				return m_cilWorker;
			}
			set { m_cilWorker = value; }
		}

		public InstructionCollection Instructions {
			get { return m_instructions; }
		}

		public ExceptionHandlerCollection ExceptionHandlers {
			get { return m_exceptions; }
		}

		public VariableDefinitionCollection Variables {
			get { return m_variables; }
		}

		public ScopeCollection Scopes {
			get { return m_scopes; }
		}

		public MethodBody (MethodDefinition meth)
		{
			m_method = meth;
			m_instructions = new InstructionCollection (this);
			m_exceptions = new ExceptionHandlerCollection (this);
			m_variables = new VariableDefinitionCollection (this);
			m_scopes = new ScopeCollection (this);
		}

		internal static Instruction GetInstruction (MethodBody oldBody, MethodBody newBody, Instruction i)
		{
			int pos = oldBody.Instructions.IndexOf (i);
			if (pos > -1 && pos < newBody.Instructions.Count)
				return newBody.Instructions [pos];

			return newBody.Instructions.Outside;
		}

		internal static MethodBody Clone (MethodBody body, MethodDefinition parent, ImportContext context)
		{
			MethodBody nb = new MethodBody (parent);
			nb.MaxStack = body.MaxStack;
			nb.InitLocals = body.InitLocals;
			nb.CodeSize = body.CodeSize;

			foreach (VariableDefinition var in body.Variables)
				nb.Variables.Add (new VariableDefinition (
					context.Import (var.VariableType)));

			foreach (Instruction instr in body.Instructions) {
				Instruction ni = new Instruction (instr.OpCode);

				switch (instr.OpCode.OperandType) {
				case OperandType.InlineParam :
				case OperandType.ShortInlineParam :
					if (instr.Operand == body.Method.This)
						ni.Operand = nb.Method.This;
					else {
						int param = body.Method.Parameters.IndexOf ((ParameterDefinition) instr.Operand);
						ni.Operand = parent.Parameters [param];
					}
					break;
				case OperandType.InlineVar :
				case OperandType.ShortInlineVar :
					int var = body.Variables.IndexOf ((VariableDefinition) instr.Operand);
					ni.Operand = nb.Variables [var];
					break;
				case OperandType.InlineField :
					ni.Operand = context.Import ((FieldReference) instr.Operand);
					break;
				case OperandType.InlineMethod :
					ni.Operand = context.Import ((MethodReference) instr.Operand);
					break;
				case OperandType.InlineType :
					ni.Operand = context.Import ((TypeReference) instr.Operand);
					break;
				case OperandType.InlineTok :
					if (instr.Operand is TypeReference)
						ni.Operand = context.Import ((TypeReference) instr.Operand);
					else if (instr.Operand is FieldReference)
						ni.Operand = context.Import ((FieldReference) instr.Operand);
					else if (instr.Operand is MethodReference)
						ni.Operand = context.Import ((MethodReference) instr.Operand);
					break;
				case OperandType.ShortInlineBrTarget :
				case OperandType.InlineBrTarget :
					break;
				default :
					ni.Operand = instr.Operand;
					break;
				}

				nb.Instructions.Add (ni);
			}

			for (int i = 0; i < body.Instructions.Count; i++) {
				Instruction instr = nb.Instructions [i];
				if (instr.OpCode.OperandType != OperandType.ShortInlineBrTarget &&
					instr.OpCode.OperandType != OperandType.InlineBrTarget)
					continue;

				instr.Operand = GetInstruction (body, nb, (Instruction) body.Instructions [i].Operand);
			}

			foreach (ExceptionHandler eh in body.ExceptionHandlers) {
				ExceptionHandler neh = new ExceptionHandler (eh.Type);
				neh.TryStart = GetInstruction (body, nb, eh.TryStart);
				neh.TryEnd = GetInstruction (body, nb, eh.TryEnd);
				neh.HandlerStart = GetInstruction (body, nb, eh.HandlerStart);
				neh.HandlerEnd = GetInstruction (body, nb, eh.HandlerEnd);

				switch (eh.Type) {
				case ExceptionHandlerType.Catch :
					neh.CatchType = context.Import (eh.CatchType);
					break;
				case ExceptionHandlerType.Filter :
					neh.FilterStart = GetInstruction (body, nb, eh.FilterStart);
					neh.FilterEnd = GetInstruction (body, nb, eh.FilterEnd);
					break;
				}

				nb.ExceptionHandlers.Add (neh);
			}

			return nb;
		}

		public void Simplify ()
		{
			foreach (Instruction i in this.Instructions) {
				if (i.OpCode.OpCodeType != OpCodeType.Macro)
					continue;

				switch (i.OpCode.Code) {
				case Code.Ldarg_0 :
					Simplify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 0));
					break;
				case Code.Ldarg_1 :
					Simplify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 1));
					break;
				case Code.Ldarg_2 :
					Simplify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 2));
					break;
				case Code.Ldarg_3 :
					Simplify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 3));
					break;
				case Code.Ldloc_0 :
					Simplify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 0));
					break;
				case Code.Ldloc_1 :
					Simplify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 1));
					break;
				case Code.Ldloc_2 :
					Simplify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 2));
					break;
				case Code.Ldloc_3 :
					Simplify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 3));
					break;
				case Code.Stloc_0 :
					Simplify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 0));
					break;
				case Code.Stloc_1 :
					Simplify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 1));
					break;
				case Code.Stloc_2 :
					Simplify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 2));
					break;
				case Code.Stloc_3 :
					Simplify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 3));
					break;
				case Code.Ldarg_S :
					i.OpCode = OpCodes.Ldarg;
					break;
				case Code.Ldarga_S :
					i.OpCode = OpCodes.Ldarga;
					break;
				case Code.Starg_S :
					i.OpCode = OpCodes.Starg;
					break;
				case Code.Ldloc_S :
					i.OpCode = OpCodes.Ldloc;
					break;
				case Code.Ldloca_S :
					i.OpCode = OpCodes.Ldloca;
					break;
				case Code.Stloc_S :
					i.OpCode = OpCodes.Stloc;
					break;
				case Code.Ldc_I4_M1 :
					Simplify (i, OpCodes.Ldc_I4, -1);
					break;
				case Code.Ldc_I4_0 :
					Simplify (i, OpCodes.Ldc_I4, 0);
					break;
				case Code.Ldc_I4_1 :
					Simplify (i, OpCodes.Ldc_I4, 1);
					break;
				case Code.Ldc_I4_2 :
					Simplify (i, OpCodes.Ldc_I4, 2);
					break;
				case Code.Ldc_I4_3 :
					Simplify (i, OpCodes.Ldc_I4, 3);
					break;
				case Code.Ldc_I4_4 :
					Simplify (i, OpCodes.Ldc_I4, 4);
					break;
				case Code.Ldc_I4_5 :
					Simplify (i, OpCodes.Ldc_I4, 5);
					break;
				case Code.Ldc_I4_6 :
					Simplify (i, OpCodes.Ldc_I4, 6);
					break;
				case Code.Ldc_I4_7 :
					Simplify (i, OpCodes.Ldc_I4, 7);
					break;
				case Code.Ldc_I4_8 :
					Simplify (i, OpCodes.Ldc_I4, 8);
					break;
				case Code.Ldc_I4_S :
					i.OpCode = OpCodes.Ldc_I4;
					i.Operand = (int) (sbyte) i.Operand;
					break;
				case Code.Br_S :
					i.OpCode = OpCodes.Br;
					break;
				case Code.Brfalse_S :
					i.OpCode = OpCodes.Brfalse;
					break;
				case Code.Brtrue_S :
					i.OpCode = OpCodes.Brtrue;
					break;
				case Code.Beq_S :
					i.OpCode = OpCodes.Beq;
					break;
				case Code.Bge_S :
					i.OpCode = OpCodes.Bge;
					break;
				case Code.Bgt_S :
					i.OpCode = OpCodes.Bgt;
					break;
				case Code.Ble_S :
					i.OpCode = OpCodes.Ble;
					break;
				case Code.Blt_S :
					i.OpCode = OpCodes.Blt;
					break;
				case Code.Bne_Un_S :
					i.OpCode = OpCodes.Bne_Un;
					break;
				case Code.Bge_Un_S :
					i.OpCode = OpCodes.Bge_Un;
					break;
				case Code.Bgt_Un_S :
					i.OpCode = OpCodes.Bgt_Un;
					break;
				case Code.Ble_Un_S :
					i.OpCode = OpCodes.Ble_Un;
					break;
				case Code.Blt_Un_S :
					i.OpCode = OpCodes.Blt_Un;
					break;
				case Code.Leave_S :
					i.OpCode = OpCodes.Leave;
					break;
				}
			}
		}

		static void Simplify (Instruction i, OpCode op, object operand)
		{
			i.OpCode = op;
			i.Operand = operand;
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitMethodBody (this);
			m_variables.Accept (visitor);
			m_instructions.Accept (visitor);
			m_exceptions.Accept (visitor);
			m_scopes.Accept (visitor);

			visitor.TerminateMethodBody (this);
		}
	}
}
