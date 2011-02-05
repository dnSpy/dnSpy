// Copyright (c) 2010 Daniel Grunwald
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
	public enum JumpType
	{
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
		/// Jump from one catch block to its sibling
		/// </summary>
		MutualProtection,
		/// <summary>
		/// non-determistic jump at end finally (to any of the potential leave targets)
		/// </summary>
		EndFinally
	}
	
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
