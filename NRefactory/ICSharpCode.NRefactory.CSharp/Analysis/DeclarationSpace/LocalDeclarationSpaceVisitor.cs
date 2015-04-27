//
// LocalDeclarationSpaceVisitor.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	public class LocalDeclarationSpaceVisitor : DepthFirstAstVisitor
	{
		LocalDeclarationSpace currentDeclarationSpace;
		Dictionary<AstNode, LocalDeclarationSpace> nodeDeclarationSpaces = new Dictionary<AstNode, LocalDeclarationSpace>();
		
		public LocalDeclarationSpace GetDeclarationSpace(AstNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			while (node != null) {
				LocalDeclarationSpace declarationSpace;
				if (nodeDeclarationSpaces.TryGetValue(node, out declarationSpace))
					return declarationSpace;
				node = node.Parent;
			}
			return null;
		}

		#region Visitor

		void AddDeclaration(string name, AstNode node)
		{
			if (currentDeclarationSpace != null)
				currentDeclarationSpace.AddDeclaration(name, node);
		}

		public override void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			AddDeclaration(variableInitializer.Name, variableInitializer);
			base.VisitVariableInitializer(variableInitializer);
		}

		public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			AddDeclaration(parameterDeclaration.Name, parameterDeclaration);
			base.VisitParameterDeclaration(parameterDeclaration);
		}

		void VisitNewDeclarationSpace(AstNode node)
		{
			var oldDeclarationSpace = currentDeclarationSpace;
			currentDeclarationSpace = new LocalDeclarationSpace();
			if (oldDeclarationSpace != null)
				oldDeclarationSpace.AddChildSpace(currentDeclarationSpace);

			VisitChildren(node);

			nodeDeclarationSpaces.Add(node, currentDeclarationSpace);
			currentDeclarationSpace = oldDeclarationSpace;
		}

		#region Declaration space creating nodes
		
		public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			VisitNewDeclarationSpace(methodDeclaration);
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			VisitNewDeclarationSpace(blockStatement);
		}

		public override void VisitSwitchStatement(SwitchStatement switchStatement)
		{
			VisitNewDeclarationSpace(switchStatement);
		}

		public override void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			AddDeclaration(foreachStatement.VariableName, foreachStatement);
			VisitNewDeclarationSpace(foreachStatement);
		}
		
		public override void VisitForStatement(ForStatement forStatement)
		{
			VisitNewDeclarationSpace(forStatement);
		}

		public override void VisitUsingStatement(UsingStatement usingStatement)
		{
			VisitNewDeclarationSpace(usingStatement);
		}

		public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			VisitNewDeclarationSpace(lambdaExpression);
		}

		public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			VisitNewDeclarationSpace(anonymousMethodExpression);
		}

		public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			AddDeclaration(eventDeclaration.Name, eventDeclaration);
		}

		public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			VisitNewDeclarationSpace(eventDeclaration);
		}

		#endregion
		#endregion
	}
}
