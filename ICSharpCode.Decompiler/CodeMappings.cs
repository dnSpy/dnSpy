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
using System.Linq;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	/// <summary> Maps method's source code to IL </summary>
	public class MethodDebugSymbols
	{
		public MethodDefinition CecilMethod { get; set; }
		public List<ILVariable> LocalVariables { get; set; }
		public List<SequencePoint> SequencePoints { get; set; }
		public TextLocation StartLocation { get; set; }
		public TextLocation EndLocation { get; set; }
		
		public MethodDebugSymbols(MethodDefinition methodDef)
		{
			this.CecilMethod = methodDef;
			this.LocalVariables = new List<ILVariable>();
			this.SequencePoints = new List<SequencePoint>();
		}
	}
	
	public class SequencePoint
	{
		public ILRange[] ILRanges { get; set; }
		public TextLocation StartLocation { get; set; }
		public TextLocation EndLocation { get; set; }
		public int ILOffset { get { return this.ILRanges[0].From; } }
		
		public override string ToString()
		{
			return string.Join(" ", this.ILRanges) + " " + this.StartLocation + "-" + this.EndLocation;
		}
	}
}
