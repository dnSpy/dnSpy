// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace Decompiler.Transforms
{
	public class ConvertConstructorCallIntoInitializer : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			ExpressionStatement stmt = constructorDeclaration.Body.Statements.FirstOrDefault() as ExpressionStatement;
			if (stmt == null)
				return null;
			InvocationExpression invocation = stmt.Expression as InvocationExpression;
			if (invocation == null)
				return null;
			MemberReferenceExpression mre = invocation.Target as MemberReferenceExpression;
			if (mre != null && mre.MemberName == ".ctor") {
				ConstructorInitializer ci = new ConstructorInitializer();
				if (mre.Target is ThisReferenceExpression)
					ci.ConstructorInitializerType = ConstructorInitializerType.This;
				else if (mre.Target is BaseReferenceExpression)
					ci.ConstructorInitializerType = ConstructorInitializerType.Base;
				else
					return null;
				// Move arguments from invocation to initializer:
				invocation.Arguments.MoveTo(ci.Arguments);
				// Add the initializer:
				constructorDeclaration.Initializer = ci.WithAnnotation(invocation.Annotation<MethodReference>());
				// Remove the statement:
				stmt.Remove();
			}
			return null;
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			base.VisitTypeDeclaration(typeDeclaration, data);
			// Remove single empty constructor:
			var ctors = typeDeclaration.Members.OfType<ConstructorDeclaration>().Where(c => (c.Modifiers & Modifiers.Static) == 0).ToArray();
			if (ctors.Length == 1 && ctors[0].Body.Children.Count() == 0
			    && ctors[0].Initializer.ConstructorInitializerType == ConstructorInitializerType.Base
			    && ctors[0].Initializer.Arguments.Count() == 0
			    && ctors[0].Modifiers == ((typeDeclaration.Modifiers & Modifiers.Abstract) == Modifiers.Abstract ? Modifiers.Protected : Modifiers.Public))
			{
				ctors[0].Remove();
			}
			return null;
		}
		
		void IAstTransform.Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}
