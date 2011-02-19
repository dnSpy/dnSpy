// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace Decompiler.Transforms
{
	/// <summary>
	/// Converts "new Action(obj, ldftn(func))" into "new Action(obj.func)".
	/// For anonymous methods, creates an AnonymousMethodExpression.
	/// </summary>
	public class DelegateConstruction : ContextTrackingVisitor
	{
		internal sealed class Annotation
		{
			/// <summary>
			/// ldftn or ldvirtftn?
			/// </summary>
			public readonly bool IsVirtual;
			
			public Annotation(bool isVirtual)
			{
				this.IsVirtual = isVirtual;
			}
		}
		
		public DelegateConstruction(DecompilerContext context) : base(context)
		{
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (objectCreateExpression.Arguments.Count() == 2) {
				Expression obj = objectCreateExpression.Arguments.First();
				Expression func = objectCreateExpression.Arguments.Last();
				Annotation annotation = func.Annotation<Annotation>();
				if (annotation != null) {
					IdentifierExpression methodIdent = (IdentifierExpression)((InvocationExpression)func).Arguments.Single();
					MethodReference method = methodIdent.Annotation<MethodReference>();
					if (method != null) {
						if (HandleAnonymousMethod(objectCreateExpression, obj, method))
							return null;
						// Perform the transformation to "new Action(obj.func)".
						obj.Remove();
						methodIdent.Remove();
						if (!annotation.IsVirtual && obj is ThisReferenceExpression) {
							// maybe it's getting the pointer of a base method?
							if (method.DeclaringType != context.CurrentType) {
								obj = new BaseReferenceExpression();
							}
						}
						if (!annotation.IsVirtual && obj is NullReferenceExpression && !method.HasThis) {
							// We're loading a static method.
							// However it is possible to load extension methods with an instance, so we compare the number of arguments:
							bool isExtensionMethod = false;
							TypeReference delegateType = objectCreateExpression.Type.Annotation<TypeReference>();
							if (delegateType != null) {
								TypeDefinition delegateTypeDef = delegateType.Resolve();
								if (delegateTypeDef != null) {
									MethodDefinition invokeMethod = delegateTypeDef.Methods.FirstOrDefault(m => m.Name == "Invoke");
									if (invokeMethod != null) {
										isExtensionMethod = (invokeMethod.Parameters.Count + 1 == method.Parameters.Count);
									}
								}
							}
							if (!isExtensionMethod) {
								obj = new TypeReferenceExpression { Type = AstBuilder.ConvertType(method.DeclaringType) };
							}
						}
						// now transform the identifier into a member reference
						MemberReferenceExpression mre = new MemberReferenceExpression();
						mre.Target = obj;
						mre.MemberName = methodIdent.Identifier;
						methodIdent.TypeArguments.MoveTo(mre.TypeArguments);
						mre.AddAnnotation(method);
						objectCreateExpression.Arguments.Clear();
						objectCreateExpression.Arguments.Add(mre);
						return null;
					}
				}
			}
			return base.VisitObjectCreateExpression(objectCreateExpression, data);
		}
		
		bool HandleAnonymousMethod(ObjectCreateExpression objectCreateExpression, Expression target, MethodReference methodRef)
		{
			// Anonymous methods are defined in the same assembly, so there's no need to Resolve().
			MethodDefinition method = methodRef as MethodDefinition;
			if (method == null || !method.Name.StartsWith("<", StringComparison.Ordinal))
				return false;
			if (!(method.IsCompilerGenerated() || IsPotentialClosure(method.DeclaringType)))
				return false;
			
			// Decompile the anonymous method:
			
			DecompilerContext subContext = context.Clone();
			subContext.CurrentMethod = method;
			BlockStatement body = AstMethodBodyBuilder.CreateMethodBody(method, subContext);
			TransformationPipeline.RunTransformationsUntil(body, v => v is DelegateConstruction, subContext);
			body.AcceptVisitor(this, null);
			
			AnonymousMethodExpression ame = new AnonymousMethodExpression();
			bool isLambda = false;
			if (method.Parameters.All(p => string.IsNullOrEmpty(p.Name))) {
				ame.HasParameterList = false;
			} else {
				ame.HasParameterList = true;
				ame.Parameters.AddRange(AstBuilder.MakeParameters(method.Parameters));
				if (ame.Parameters.All(p => p.ParameterModifier == ParameterModifier.None)) {
					isLambda = (body.Statements.Count == 1 && body.Statements.Single() is ReturnStatement);
				}
			}
			
			// Replace all occurrences of 'this' in the method body with the delegate's target:
			foreach (AstNode node in body.Descendants) {
				if (node is ThisReferenceExpression)
					node.ReplaceWith(target.Clone());
				
			}
			if (isLambda) {
				LambdaExpression lambda = new LambdaExpression();
				ame.Parameters.MoveTo(lambda.Parameters);
				Expression returnExpr = ((ReturnStatement)body.Statements.Single()).Expression;
				returnExpr.Remove();
				lambda.Body = returnExpr;
				objectCreateExpression.ReplaceWith(lambda);
			} else {
				ame.Body = body;
				objectCreateExpression.ReplaceWith(ame);
			}
			return true;
		}
		
		bool IsPotentialClosure(TypeDefinition potentialDisplayClass)
		{
			if (potentialDisplayClass == null || !potentialDisplayClass.IsCompilerGenerated())
				return false;
			// check that methodContainingType is within containingType
			while (potentialDisplayClass != context.CurrentType) {
				potentialDisplayClass = potentialDisplayClass.DeclaringType;
				if (potentialDisplayClass == null)
					return false;
			}
			return true;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			base.VisitBlockStatement(blockStatement, data);
			foreach (VariableDeclarationStatement stmt in blockStatement.Statements.OfType<VariableDeclarationStatement>()) {
				if (stmt.Variables.Count() != 1)
					continue;
				var variable = stmt.Variables.Single();
				TypeDefinition type = stmt.Type.Annotation<TypeDefinition>();
				if (!IsPotentialClosure(type))
					continue;
				ObjectCreateExpression oce = variable.Initializer as ObjectCreateExpression;
				if (oce == null || oce.Type.Annotation<TypeReference>() != type || oce.Arguments.Any() || !oce.Initializer.IsNull)
					continue;
				// Looks like we found a display class creation. Now let's verify that the variable is used only for field accesses:
				bool ok = true;
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name) {
						if (!(identExpr.Parent is MemberReferenceExpression && identExpr.Parent.Annotation<FieldReference>() != null))
							ok = false;
					}
				}
				if (!ok)
					continue;
				Dictionary<string, Expression> dict = new Dictionary<string, Expression>();
				// Delete the variable declaration statement:
				AstNode cur;
				AstNode next = stmt.NextSibling;
				stmt.Remove();
				for (cur = next; cur != null; cur = next) {
					next = cur.NextSibling;
					
					// Delete any following statements as long as they assign simple variables to the display class:
					// Test for the pattern:
					// "variableName.MemberName = right;"
					ExpressionStatement es = cur as ExpressionStatement;
					if (es == null)
						break;
					AssignmentExpression ae = es.Expression as AssignmentExpression;
					if (ae == null || ae.Operator != AssignmentOperatorType.Assign)
						break;
					MemberReferenceExpression left = ae.Left as MemberReferenceExpression;
					if (left == null || !IsParameter(ae.Right))
						break;
					if (!(left.Target is IdentifierExpression) || (left.Target as IdentifierExpression).Identifier != variable.Name)
						break;
					dict[left.MemberName] = ae.Right;
					es.Remove();
				}
				
				// Now create variables for all fields of the display class (except for those that we already handled)
				foreach (FieldDefinition field in type.Fields) {
					if (dict.ContainsKey(field.Name))
						continue;
					VariableDeclarationStatement newVarDecl = new VariableDeclarationStatement();
					newVarDecl.Type = AstBuilder.ConvertType(field.FieldType, field);
					newVarDecl.Variables.Add(new VariableInitializer(field.Name));
					blockStatement.InsertChildBefore(cur, newVarDecl, BlockStatement.StatementRole);
					dict[field.Name] = new IdentifierExpression(field.Name);
				}
				
				// Now figure out where the closure was accessed and use the simpler replacement expression there:
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name) {
						MemberReferenceExpression mre = (MemberReferenceExpression)identExpr.Parent;
						Expression replacement;
						if (dict.TryGetValue(mre.MemberName, out replacement)) {
							mre.ReplaceWith(replacement.Clone());
						}
					}
				}
			}
			return null;
		}
		
		bool IsParameter(Expression expr)
		{
			if (expr is ThisReferenceExpression)
				return true;
			IdentifierExpression ident = expr as IdentifierExpression;
			return ident != null && ident.Annotation<ParameterReference>() != null;
		}
	}
}
