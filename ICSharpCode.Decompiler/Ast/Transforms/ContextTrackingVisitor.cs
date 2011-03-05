// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Base class for AST visitors that need the current type/method context info.
	/// </summary>
	public abstract class ContextTrackingVisitor : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		protected readonly DecompilerContext context;
		
		protected ContextTrackingVisitor(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			TypeDefinition oldType = context.CurrentType;
			try {
				context.CurrentType = typeDeclaration.Annotation<TypeDefinition>();
				return base.VisitTypeDeclaration(typeDeclaration, data);
			} finally {
				context.CurrentType = oldType;
			}
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = methodDeclaration.Annotation<MethodDefinition>();
				return base.VisitMethodDeclaration(methodDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			Debug.Assert(context.CurrentMethod == null);
			try {
				context.CurrentMethod = constructorDeclaration.Annotation<MethodDefinition>();
				return base.VisitConstructorDeclaration(constructorDeclaration, data);
			} finally {
				context.CurrentMethod = null;
			}
		}
		
		public override object VisitAccessor(Accessor accessor, object data)
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
