// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;

using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	public enum SpecialOpCode
	{
		/// <summary>
		/// No special op code: SsaInstruction has a normal IL instruction
		/// </summary>
		None,
		/// <summary>
		/// Φ function: chooses the appropriate variable based on which CFG edge was used to enter this block
		/// </summary>
		Phi,
		/// <summary>
		/// Variable is read from before passing it by ref.
		/// This instruction constructs a managed reference to the variable.
		/// </summary>
		PrepareByRefCall,
		/// <summary>
		/// This instruction constructs a managed reference to the variable.
		/// The variable is not really read from.
		/// </summary>
		PrepareByOutCall,
		/// <summary>
		/// This instruction constructs a managed reference to the variable.
		/// The reference is used for a field access on a value type.
		/// </summary>
		PrepareForFieldAccess,
		/// <summary>
		/// Variable is written to after passing it by ref or out.
		/// </summary>
		WriteAfterByRefOrOutCall,
		/// <summary>
		/// Variable is not initialized.
		/// </summary>
		Uninitialized,
		/// <summary>
		/// Value is passed in as parameter
		/// </summary>
		Parameter,
		/// <summary>
		/// Value is a caught exception.
		/// TypeOperand is set to the exception type.
		/// </summary>
		Exception,
		/// <summary>
		/// Initialize a value type. Unlike the real initobj instruction, this one does not take an address
		/// but assigns to the target variable.
		/// TypeOperand is set to the type being created.
		/// </summary>
		InitObj
	}
	
	public sealed class SsaInstruction
	{
		public readonly SsaBlock ParentBlock;
		public readonly SpecialOpCode SpecialOpCode;
		
		/// <summary>
		/// The original IL instruction.
		/// May be null for "invented" instructions (SpecialOpCode != None).
		/// </summary>
		public readonly Instruction Instruction;
		
		/// <summary>
		/// Prefixes in front of the IL instruction.
		/// </summary>
		public readonly Instruction[] Prefixes;
		
		/// <summary>
		/// Gets the type operand. This is used only in combination with some special opcodes.
		/// </summary>
		public readonly TypeReference TypeOperand;
		
		public SsaVariable Target;
		public SsaVariable[] Operands;
		
		static readonly SsaVariable[] emptyVariableArray = {};
		static readonly Instruction[] emptyInstructionArray = {};
		
		public SsaInstruction(SsaBlock parentBlock, Instruction instruction, SsaVariable target, SsaVariable[] operands,
		                      Instruction[] prefixes = null, SpecialOpCode specialOpCode = SpecialOpCode.None,
		                      TypeReference typeOperand = null)
		{
			this.ParentBlock = parentBlock;
			this.Instruction = instruction;
			this.Prefixes = prefixes ?? emptyInstructionArray;
			this.Target = target;
			this.Operands = operands ?? emptyVariableArray;
			this.SpecialOpCode = specialOpCode;
			this.TypeOperand = typeOperand;
			Debug.Assert((typeOperand != null) == (specialOpCode == SpecialOpCode.Exception || specialOpCode == SpecialOpCode.InitObj));
		}
		
		/// <summary>
		/// Gets whether this instruction is a simple assignment from one variable to another.
		/// </summary>
		public bool IsMoveInstruction {
			get {
				return Target != null && Operands.Length == 1 && Instruction != null && OpCodeInfo.Get(Instruction.OpCode).IsMoveInstruction;
			}
		}
		
		public void ReplaceVariableInOperands(SsaVariable oldVar, SsaVariable newVar)
		{
			for (int i = 0; i < this.Operands.Length; i++) {
				if (this.Operands[i] == oldVar)
					this.Operands[i] = newVar;
			}
		}
		
		public override string ToString()
		{
			StringWriter w = new StringWriter();
			WriteTo(w);
			return w.ToString();
		}
		
		public void WriteTo(TextWriter writer)
		{
			foreach (Instruction prefix in this.Prefixes) {
				Disassembler.DisassemblerHelpers.WriteTo(prefix, new PlainTextOutput(writer));
				writer.WriteLine();
			}
			if (Instruction != null && Instruction.Offset >= 0) {
				writer.Write(CecilExtensions.OffsetToString(Instruction.Offset));
				writer.Write(": ");
			}
			if (Target != null) {
				writer.Write(Target.ToString());
				writer.Write(" = ");
			}
			if (IsMoveInstruction) {
				writer.Write(Operands[0].ToString());
				if (Instruction != null) {
					writer.Write(" (" + Instruction.OpCode.Name + ")");
				}
			} else {
				if (Instruction == null) {
					writer.Write(SpecialOpCode.ToString());
				} else {
					writer.Write(Instruction.OpCode.Name);
					if(null != Instruction.Operand) {
						writer.Write(' ');
						Disassembler.DisassemblerHelpers.WriteOperand(new PlainTextOutput(writer), Instruction.Operand);
						writer.Write(' ');
					}
				}
				if (TypeOperand != null) {
					writer.Write(' ');
					writer.Write(TypeOperand.ToString());
					writer.Write(' ');
				}
				if (Operands.Length > 0) {
					writer.Write('(');
					for (int i = 0; i < Operands.Length; i++) {
						if (i > 0)
							writer.Write(", ");
						writer.Write(Operands[i].ToString());
					}
					writer.Write(')');
				}
			}
		}
	}
}
