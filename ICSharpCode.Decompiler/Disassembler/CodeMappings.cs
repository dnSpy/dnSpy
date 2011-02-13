// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.Disassembler
{
	public class ILCodeMapping
	{
		public int SourceCodeLine { get; set; }
		
		public Instruction ILInstruction { get; set; }
	}
	
	public class MethodMapping
	{
		public string TypeName { get; set; }
		
		public int MetadataToken { get; set; }
		
		public List<ILCodeMapping> MethodCodeMappings { get; set; }
		
		/// <summary>
		/// Finds the IL instruction given a source code line number.
		/// </summary>
		/// <param name="sourceCodeLine">Source code line number.</param>
		/// <returns>IL Instruction or null, if the instruction was not found.</returns>
		public Instruction FindByLine(int sourceCodeLine)
		{
			if (sourceCodeLine <= 0)
				throw new ArgumentException("The source line must be greater thatn 0.");
			
			if (MethodCodeMappings == null || MethodCodeMappings.Count == 0)
				return null;
			
			foreach (var codeMapping in MethodCodeMappings) {
				if (codeMapping.SourceCodeLine == sourceCodeLine)
					return codeMapping.ILInstruction;
			}
			
			return null;
		}
		
		/// <summary>
		/// Finds the source code line given an IL instruction offset.
		/// </summary>
		/// <param name="instruction">IL Instruction offset.</param>
		/// <returns>Source code line, if it is found, -1 otherwise.</returns>
		public int FindByInstruction(int instructionOffset)
		{
			if (instructionOffset <= 0)
				throw new ArgumentNullException("The instruction offset cannot be lower than 0.");
			
			if (MethodCodeMappings == null || MethodCodeMappings.Count == 0)
				return -1;
			
			foreach (var codeMapping in MethodCodeMappings) {
				if (codeMapping.ILInstruction.Offset == instructionOffset)
					return codeMapping.SourceCodeLine;
			}
			
			return -1;
		}
	}
}
