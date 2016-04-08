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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using dnlib.DotNet;
using dnSpy.Decompiler.Shared;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// If the first element of a constructor is a chained constructor call, convert it into a constructor initializer.
	/// </summary>
	public class ConvertConstructorCallIntoInitializer : DepthFirstAstVisitor<object, object>, IAstTransformPoolObject
	{
		DecompilerContext context;

		public ConvertConstructorCallIntoInitializer(DecompilerContext context)
		{
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
		}

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
				var ilRanges = stmt.GetAllRecursiveILRanges();
				// Add the initializer: (unless it is the default 'base()')
				if (!(ci.ConstructorInitializerType == ConstructorInitializerType.Base && ci.Arguments.Count == 0)) {
					constructorDeclaration.Initializer = ci.WithAnnotation(invocation.Annotation<IMethod>());
					ci.AddAnnotation(ilRanges);
				}
				else
					constructorDeclaration.Body.HiddenStart = NRefactoryExtensions.CreateHidden(!context.CalculateILRanges ? null : ILRange.OrderAndJoinList(ilRanges), constructorDeclaration.Body.HiddenStart);
				// Remove the statement:
				stmt.Remove();
			}
			return null;
		}
		
		static readonly ExpressionStatement fieldInitializerPattern = new ExpressionStatement {
			Expression = new AssignmentExpression {
				Left = new NamedNode("fieldAccess", new MemberReferenceExpression { 
				                     	Target = new ThisReferenceExpression(),
				                     	MemberName = Pattern.AnyString
				                     }),
				Operator = AssignmentOperatorType.Assign,
				Right = new AnyNode("initializer")
			}
		};
		
		static readonly AstNode thisCallPattern = new ExpressionStatement(new ThisReferenceExpression().Invoke(".ctor", new Repeat(new AnyNode())));
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			// Handle initializers on instance fields
			HandleInstanceFieldInitializers(typeDeclaration.Members);
			
			// Now convert base constructor calls to initializers:
			base.VisitTypeDeclaration(typeDeclaration, data);
			
			// Remove single empty constructor:
			RemoveSingleEmptyConstructor(typeDeclaration);
			
			// Handle initializers on static fields:
			HandleStaticFieldInitializers(typeDeclaration.Members);
			return null;
		}
		
		void HandleInstanceFieldInitializers(IEnumerable<AstNode> members)
		{
			var instanceCtors = members.OfType<ConstructorDeclaration>().Where(c => (c.Modifiers & Modifiers.Static) == 0).ToArray();
			var instanceCtorsNotChainingWithThis = instanceCtors.Where(ctor => !thisCallPattern.IsMatch(ctor.Body.Statements.FirstOrDefault())).ToArray();
			if (instanceCtorsNotChainingWithThis.Length > 0) {
				MethodDef ctorMethodDef = instanceCtorsNotChainingWithThis[0].Annotation<MethodDef>();
				if (ctorMethodDef != null && DnlibExtensions.IsValueType(ctorMethodDef.DeclaringType))
					return;
				
				// Recognize field initializers:
				// Convert first statement in all ctors (if all ctors have the same statement) into a field initializer.
				bool allSame;
				do {
					Match m = fieldInitializerPattern.Match(instanceCtorsNotChainingWithThis[0].Body.FirstOrDefault());
					if (!m.Success)
						break;
					
					FieldDef fieldDef = m.Get<AstNode>("fieldAccess").Single().Annotation<IField>().ResolveFieldWithinSameModule();
					if (fieldDef == null)
						break;
					AstNode fieldOrEventDecl = members.FirstOrDefault(f => f.Annotation<FieldDef>() == fieldDef);
					if (fieldOrEventDecl == null)
						break;
					Expression initializer = m.Get<Expression>("initializer").Single();
					// 'this'/'base' cannot be used in field initializers
					if (initializer.DescendantsAndSelf.Any(n => n is ThisReferenceExpression || n is BaseReferenceExpression))
						break;
					
					allSame = true;
					for (int i = 1; i < instanceCtorsNotChainingWithThis.Length; i++) {
						if (!instanceCtors[0].Body.First().IsMatch(instanceCtorsNotChainingWithThis[i].Body.FirstOrDefault())) {
							allSame = false;
							break;
						}
					}
					if (allSame) {
						var ctorIlRanges = new List<Tuple<MemberMapping, List<ILRange>>>(instanceCtorsNotChainingWithThis.Length);
						for (int i = 0; i < instanceCtorsNotChainingWithThis.Length; i++) {
							var ctor = instanceCtorsNotChainingWithThis[i];
							var stmt = ctor.Body.First();
							stmt.Remove();
							var mm = ctor.Annotation<MemberMapping>() ?? ctor.Body.Annotation<MemberMapping>();
							Debug.Assert(mm != null);
							if (mm != null)
								ctorIlRanges.Add(Tuple.Create(mm, stmt.GetAllRecursiveILRanges()));
						}
						var varInit = fieldOrEventDecl.GetChildrenByRole(Roles.Variable).Single();
						initializer.Remove();
						initializer.RemoveAllILRangesRecursive();
						varInit.Initializer = initializer;
						fieldOrEventDecl.AddAnnotation(ctorIlRanges);
					}
				} while (allSame);
			}
		}
		
		void RemoveSingleEmptyConstructor(TypeDeclaration typeDeclaration)
		{
			if (!context.Settings.RemoveEmptyDefaultConstructors || context.Settings.ForceShowAllMembers)
				return;
			var instanceCtors = typeDeclaration.Members.OfType<ConstructorDeclaration>().Where(c => (c.Modifiers & Modifiers.Static) == 0).ToArray();
			if (instanceCtors.Length == 1) {
				ConstructorDeclaration emptyCtor = new ConstructorDeclaration();
				emptyCtor.Modifiers = ((typeDeclaration.Modifiers & Modifiers.Abstract) == Modifiers.Abstract ? Modifiers.Protected : Modifiers.Public);
				emptyCtor.Body = new BlockStatement();
				if (emptyCtor.IsMatch(instanceCtors[0]))
					instanceCtors[0].Remove();
			}
		}
		
		void HandleStaticFieldInitializers(IEnumerable<AstNode> members)
		{
			// Convert static constructor into field initializers if the class is BeforeFieldInit
			var staticCtor = members.OfType<ConstructorDeclaration>().FirstOrDefault(c => (c.Modifiers & Modifiers.Static) == Modifiers.Static);
			if (staticCtor != null) {
				MethodDef ctorMethodDef = staticCtor.Annotation<MethodDef>();
				if (ctorMethodDef != null && ctorMethodDef.DeclaringType.IsBeforeFieldInit) {
					var mm = staticCtor.Annotation<MemberMapping>() ?? staticCtor.Body.Annotation<MemberMapping>();
					Debug.Assert(mm != null);
					while (true) {
						ExpressionStatement es = staticCtor.Body.Statements.FirstOrDefault() as ExpressionStatement;
						if (es == null)
							break;
						AssignmentExpression assignment = es.Expression as AssignmentExpression;
						if (assignment == null || assignment.Operator != AssignmentOperatorType.Assign)
							break;
						FieldDef fieldDef = assignment.Left.Annotation<IField>().ResolveFieldWithinSameModule();
						if (fieldDef == null || !fieldDef.IsStatic)
							break;
						FieldDeclaration fieldDecl = members.OfType<FieldDeclaration>().FirstOrDefault(f => f.Annotation<FieldDef>() == fieldDef);
						if (fieldDecl == null)
							break;
						var ilRanges = assignment.GetAllRecursiveILRanges();
						assignment.RemoveAllILRangesRecursive();
						var varInit = fieldDecl.Variables.Single();
						varInit.Initializer = assignment.Right.Detach();
						var ctorIlRanges = new List<Tuple<MemberMapping, List<ILRange>>>(1);
						if (mm != null)
							ctorIlRanges.Add(Tuple.Create(mm, ilRanges));
						fieldDecl.AddAnnotation(ctorIlRanges);
						es.Remove();
					}
					if (!context.Settings.ForceShowAllMembers && context.Settings.RemoveEmptyDefaultConstructors && staticCtor.Body.Statements.Count == 0)
						staticCtor.Remove();
				}
			}
		}
		
		void IAstTransform.Run(AstNode node)
		{
			// If we're viewing some set of members (fields are direct children of CompilationUnit),
			// we also need to handle those:
			HandleInstanceFieldInitializers(node.Children);
			HandleStaticFieldInitializers(node.Children);
			
			node.AcceptVisitor(this, null);
		}
	}
}
