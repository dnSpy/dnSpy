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

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Describes the type of a control flow egde.
	/// </summary>
	public enum JumpType
	{
		/// <summary>
		/// A regular control flow edge.
		/// </summary>
		Normal,
		/// <summary>
		/// Jump to exception handler (an exception occurred)
		/// </summary>
		JumpToExceptionHandler,
		/// <summary>
		/// Jump from try block to leave target:
		/// This is not a real jump, as the finally handler is executed first!
		/// </summary>
		LeaveTry,
		/// <summary>
		/// Jump at endfinally (to any of the potential leave targets).
		/// For any leave-instruction, control flow enters the finally block - the edge to the leave target (LeaveTry) is not a real control flow edge.
		/// EndFinally edges are inserted at the end of the finally block, jumping to any of the targets of the leave instruction.
		/// This edge type is only used when copying of finally blocks is disabled (with copying, a normal deterministic edge is used at each copy of the endfinally node).
		/// </summary>
		EndFinally
	}
	
	/// <summary>
	/// Represents an edge in the control flow graph, pointing from Source to Target.
	/// </summary>
	public sealed class ControlFlowEdge
	{
		public readonly ControlFlowNode Source;
		public readonly ControlFlowNode Target;
		public readonly JumpType Type;
		
		public ControlFlowEdge(ControlFlowNode source, ControlFlowNode target, JumpType type)
		{
			this.Source = source;
			this.Target = target;
			this.Type = type;
		}
		
		public override string ToString()
		{
			switch (Type) {
				case JumpType.Normal:
					return "#" + Target.BlockIndex;
				case JumpType.JumpToExceptionHandler:
					return "e:#" + Target.BlockIndex;
				default:
					return Type.ToString() + ":#" + Target.BlockIndex;
			}
		}
	}
}
