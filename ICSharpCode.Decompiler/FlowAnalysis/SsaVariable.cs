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
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Represents a variable used with the SsaInstruction register-based instructions.
	/// Despite what the name suggests, the variable is not necessarily in single-assignment form - take a look at "bool IsSingleAssignment".
	/// </summary>
	public sealed class SsaVariable
	{
		public int OriginalVariableIndex;
		public readonly string Name;
		public readonly bool IsStackLocation;
		
		public readonly ParameterDefinition Parameter;
		public readonly VariableDefinition Variable;
		
		public SsaVariable(ParameterDefinition p)
		{
			this.Name = string.IsNullOrEmpty(p.Name) ? "param" + p.Index : p.Name;
			this.Parameter = p;
		}
		
		public SsaVariable(VariableDefinition v)
		{
			this.Name = string.IsNullOrEmpty(v.Name) ? "V_" + v.Index : v.Name;
			this.Variable = v;
		}
		
		public SsaVariable(int stackLocation)
		{
			this.Name = "stack" + stackLocation;
			this.IsStackLocation = true;
		}
		
		public SsaVariable(SsaVariable original, string newName)
		{
			this.Name = newName;
			this.IsStackLocation = original.IsStackLocation;
			this.OriginalVariableIndex = original.OriginalVariableIndex;
			this.Parameter = original.Parameter;
			this.Variable = original.Variable;
		}
		
		public override string ToString()
		{
			return Name;
		}
		
		/// <summary>
		/// Gets whether this variable has only a single assignment.
		/// This field is initialized in TransformToSsa step.
		/// </summary>
		/// <remarks>Not all variables can be transformed to single assignment form: variables that have their address taken
		/// cannot be represented in SSA (although SimplifyByRefCalls will get rid of the address-taking instruction in almost all cases)</remarks>
		public bool IsSingleAssignment;
		
		/// <summary>
		/// Gets the instruction defining the variable.
		/// This field is initialized in TransformToSsa step. It is only set for variables with a single assignment.
		/// </summary>
		public SsaInstruction Definition;
		
		/// <summary>
		/// Gets the places where a variable is used.
		/// If a single instruction reads a variable 2 times (e.g. adding to itself), then it must be included 2 times in this list!
		/// </summary>
		public List<SsaInstruction> Usage;
	}
}
