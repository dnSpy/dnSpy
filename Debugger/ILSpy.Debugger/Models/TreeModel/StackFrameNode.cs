// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System.Collections.Generic;
using Debugger;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal class StackFrameNode: TreeNode
	{
		StackFrame stackFrame;
		
		public StackFrame StackFrame {
			get { return stackFrame; }
		}
		
		public StackFrameNode(StackFrame stackFrame)
		{
			this.stackFrame = stackFrame;
			
			this.Name = stackFrame.MethodInfo.Name;
			this.ChildNodes = LazyGetChildNodes();
		}
		
		IEnumerable<TreeNode> LazyGetChildNodes()
		{
			foreach(DebugParameterInfo par in stackFrame.MethodInfo.GetParameters()) {
				var image = ExpressionNode.GetImageForParameter();
				var expression = new ExpressionNode(image, par.Name, par.GetExpression());
				yield return expression;
			}
			var ip = this.StackFrame.IP;
			if (ip.IsValid) {
				foreach (DebugLocalVariableInfo locVar in stackFrame.MethodInfo.GetLocalVariables(ip.Offset)) {
					var image = ExpressionNode.GetImageForLocalVariable();
					var expression = new ExpressionNode(image, locVar.Name, locVar.GetExpression());
					yield return expression;
				}
			}
			if (stackFrame.Thread.CurrentException != null) {
				yield return new ExpressionNode(null, "__exception", new IdentifierExpression("__exception"));
			}
		}
	}
}
