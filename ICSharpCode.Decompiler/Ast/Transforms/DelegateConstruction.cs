// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;
using Mono.Cecil;

namespace Decompiler.Transforms
{
	/// <summary>
	/// Converts "new Action(obj, ldftn(func))" into "new Action(obj.func)".
	/// For anonymous methods, creates an AnonymousMethodExpression.
	/// Also gets rid of any "Display Classes" left over after inlining an anonymous method.
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
		
		internal sealed class CapturedVariableAnnotation
		{
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
				Dictionary<FieldReference, AstNode> dict = new Dictionary<FieldReference, AstNode>();
				// Delete the variable declaration statement:
				AstNode cur = stmt.NextSibling;
				stmt.Remove();
				if (blockStatement.Parent.NodeType == NodeType.Member || blockStatement.Parent is Accessor) {
					// Delete any following statements as long as they assign parameters to the display class
					// Do parameter handling only for closures created in the top scope (direct child of method/accessor)
					List<ParameterReference> parameterOccurrances = blockStatement.Descendants.OfType<IdentifierExpression>()
						.Select(n => n.Annotation<ParameterReference>()).Where(p => p != null).ToList();
					AstNode next;
					for (; cur != null; cur = next) {
						next = cur.NextSibling;
						
						// Test for the pattern:
						// "variableName.MemberName = right;"
						ExpressionStatement closureFieldAssignmentPattern = new ExpressionStatement(
							new AssignmentExpression(
								new NamedNode("left", new MemberReferenceExpression { Target = new IdentifierExpression(variable.Name) }).ToExpression(),
								new AnyNode("right").ToExpression()
							)
						);
						Match m = closureFieldAssignmentPattern.Match(cur);
						if (m != null) {
							AstNode right = m.Get("right").Single();
							bool isParameter = false;
							if (right is ThisReferenceExpression) {
								isParameter = true;
							} else if (right is IdentifierExpression) {
								// handle parameters only if the whole method contains no other occurrance except for 'right'
								ParameterReference param = right.Annotation<ParameterReference>();
								isParameter = parameterOccurrances.Count(c => c == param) == 1;
							}
							if (isParameter) {
								dict[m.Get<MemberReferenceExpression>("left").Single().Annotation<FieldReference>()] = right;
								cur.Remove();
							} else {
								break;
							}
						} else {
							break;
						}
					}
				}
				
				// Now create variables for all fields of the display class (except for those that we already handled as parameters)
				List<Tuple<AstType, string>> variablesToDeclare = new List<Tuple<AstType, string>>();
				foreach (FieldDefinition field in type.Fields) {
					if (dict.ContainsKey(field))
						continue;
					variablesToDeclare.Add(Tuple.Create(AstBuilder.ConvertType(field.FieldType, field), field.Name));
					dict[field] = new IdentifierExpression(field.Name);
				}
				
				// Now figure out where the closure was accessed and use the simpler replacement expression there:
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name) {
						MemberReferenceExpression mre = (MemberReferenceExpression)identExpr.Parent;
						AstNode replacement;
						if (dict.TryGetValue(mre.Annotation<FieldReference>(), out replacement)) {
							mre.ReplaceWith(replacement.Clone());
						}
					}
				}
				// Now insert the variable declarations (we can do this after the replacements only so that the scope detection works):
				foreach (var tuple in variablesToDeclare) {
					var newVarDecl = DeclareVariableInSmallestScope.DeclareVariable(blockStatement, tuple.Item1, tuple.Item2, allowPassIntoLoops: false);
					if (newVarDecl != null)
						newVarDecl.Variables.Single().AddAnnotation(new CapturedVariableAnnotation());
				}
			}
			return null;
		}
	}
}
