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
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Base class for AST visitors that need the current type/method context info.
	/// </summary>
	public abstract class ContextTrackingVisitor<TResult> : DepthFirstAstVisitor<object, TResult>, IAstTransform
	{
		protected readonly DecompilerContext context;
		
		protected ContextTrackingVisitor(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public override TResult VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			TypeDefinition oldType = context.CurrentType;
			try {
				context.CurrentType = typeDeclaration.Annotation<TypeDefinition>();
				return base.VisitTypeDeclaration(typeDeclaration, data);
			} finally {
				context.CurrentType = oldType;
			}
		}
		
		public override TResult VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = methodDeclaration.Annotation<MethodDefinition>();
				return base.VisitMethodDeclaration(methodDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override TResult VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = constructorDeclaration.Annotation<MethodDefinition>();
				return base.VisitConstructorDeclaration(constructorDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override TResult VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = destructorDeclaration.Annotation<MethodDefinition>();
				return base.VisitDestructorDeclaration(destructorDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override TResult VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = operatorDeclaration.Annotation<MethodDefinition>();
				return base.VisitOperatorDeclaration(operatorDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override TResult VisitAccessor(Accessor accessor, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = accessor.Annotation<MethodDefinition>();
				return base.VisitAccessor(accessor, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		void IAstTransform.Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}
