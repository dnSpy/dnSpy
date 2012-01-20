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
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Converts "new Action(obj, ldftn(func))" into "new Action(obj.func)".
	/// For anonymous methods, creates an AnonymousMethodExpression.
	/// Also gets rid of any "Display Classes" left over after inlining an anonymous method.
	/// </summary>
	public class DelegateConstruction : ContextTrackingVisitor<object>
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
		
		List<string> currentlyUsedVariableNames = new List<string>();
		
		public DelegateConstruction(DecompilerContext context) : base(context)
		{
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (objectCreateExpression.Arguments.Count == 2) {
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
							if (method.DeclaringType.GetElementType() != context.CurrentType) {
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
		
		internal static bool IsAnonymousMethod(DecompilerContext context, MethodDefinition method)
		{
			if (method == null || !(method.HasGeneratedName() || method.Name.Contains("$")))
				return false;
			if (!(method.IsCompilerGenerated() || IsPotentialClosure(context, method.DeclaringType)))
				return false;
			return true;
		}
		
		bool HandleAnonymousMethod(ObjectCreateExpression objectCreateExpression, Expression target, MethodReference methodRef)
		{
			if (!context.Settings.AnonymousMethods)
				return false; // anonymous method decompilation is disabled
			if (target != null && !(target is IdentifierExpression || target is ThisReferenceExpression || target is NullReferenceExpression))
				return false; // don't copy arbitrary expressions, deal with identifiers only
			
			// Anonymous methods are defined in the same assembly
			MethodDefinition method = methodRef.ResolveWithinSameModule();
			if (!IsAnonymousMethod(context, method))
				return false;
			
			// Create AnonymousMethodExpression and prepare parameters
			AnonymousMethodExpression ame = new AnonymousMethodExpression();
			ame.CopyAnnotationsFrom(objectCreateExpression); // copy ILRanges etc.
			ame.RemoveAnnotations<MethodReference>(); // remove reference to delegate ctor
			ame.AddAnnotation(method); // add reference to anonymous method
			ame.Parameters.AddRange(AstBuilder.MakeParameters(method, isLambda: true));
			ame.HasParameterList = true;
			
			// rename variables so that they don't conflict with the parameters:
			foreach (ParameterDeclaration pd in ame.Parameters) {
				EnsureVariableNameIsAvailable(objectCreateExpression, pd.Name);
			}
			
			// Decompile the anonymous method:
			
			DecompilerContext subContext = context.Clone();
			subContext.CurrentMethod = method;
			subContext.ReservedVariableNames.AddRange(currentlyUsedVariableNames);
			BlockStatement body = AstMethodBodyBuilder.CreateMethodBody(method, subContext, ame.Parameters);
			TransformationPipeline.RunTransformationsUntil(body, v => v is DelegateConstruction, subContext);
			body.AcceptVisitor(this, null);
			
			
			bool isLambda = false;
			if (ame.Parameters.All(p => p.ParameterModifier == ParameterModifier.None)) {
				isLambda = (body.Statements.Count == 1 && body.Statements.Single() is ReturnStatement);
			}
			// Remove the parameter list from an AnonymousMethodExpression if the original method had no names,
			// and the parameters are not used in the method body
			if (!isLambda && method.Parameters.All(p => string.IsNullOrEmpty(p.Name))) {
				var parameterReferencingIdentifiers =
					from ident in body.Descendants.OfType<IdentifierExpression>()
					let v = ident.Annotation<ILVariable>()
					where v != null && v.IsParameter && method.Parameters.Contains(v.OriginalParameter)
					select ident;
				if (!parameterReferencingIdentifiers.Any()) {
					ame.Parameters.Clear();
					ame.HasParameterList = false;
				}
			}
			
			// Replace all occurrences of 'this' in the method body with the delegate's target:
			foreach (AstNode node in body.Descendants) {
				if (node is ThisReferenceExpression)
					node.ReplaceWith(target.Clone());
				
			}
			if (isLambda) {
				LambdaExpression lambda = new LambdaExpression();
				lambda.CopyAnnotationsFrom(ame);
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
		
		internal static bool IsPotentialClosure(DecompilerContext context, TypeDefinition potentialDisplayClass)
		{
			if (potentialDisplayClass == null || !potentialDisplayClass.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return false;
			// check that methodContainingType is within containingType
			while (potentialDisplayClass != context.CurrentType) {
				potentialDisplayClass = potentialDisplayClass.DeclaringType;
				if (potentialDisplayClass == null)
					return false;
			}
			return true;
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (context.Settings.ExpressionTrees && ExpressionTreeConverter.CouldBeExpressionTree(invocationExpression)) {
				Expression converted = ExpressionTreeConverter.TryConvert(context, invocationExpression);
				if (converted != null) {
					invocationExpression.ReplaceWith(converted);
					return converted.AcceptVisitor(this, data);
				}
			}
			return base.VisitInvocationExpression(invocationExpression, data);
		}
		
		#region Track current variables
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(methodDeclaration.Parameters.Select(p => p.Name));
				return base.VisitMethodDeclaration(methodDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}
		
		public override object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(operatorDeclaration.Parameters.Select(p => p.Name));
				return base.VisitOperatorDeclaration(operatorDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(constructorDeclaration.Parameters.Select(p => p.Name));
				return base.VisitConstructorDeclaration(constructorDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}
		
		public override object VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			Debug.Assert(currentlyUsedVariableNames.Count == 0);
			try {
				currentlyUsedVariableNames.AddRange(indexerDeclaration.Parameters.Select(p => p.Name));
				return base.VisitIndexerDeclaration(indexerDeclaration, data);
			} finally {
				currentlyUsedVariableNames.Clear();
			}
		}
		
		public override object VisitAccessor(Accessor accessor, object data)
		{
			try {
				currentlyUsedVariableNames.Add("value");
				return base.VisitAccessor(accessor, data);
			} finally {
				currentlyUsedVariableNames.RemoveAt(currentlyUsedVariableNames.Count - 1);
			}
		}
		
		public override object VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			foreach (VariableInitializer v in variableDeclarationStatement.Variables)
				currentlyUsedVariableNames.Add(v.Name);
			return base.VisitVariableDeclarationStatement(variableDeclarationStatement, data);
		}
		
		public override object VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			foreach (VariableInitializer v in fixedStatement.Variables)
				currentlyUsedVariableNames.Add(v.Name);
			return base.VisitFixedStatement(fixedStatement, data);
		}
		#endregion
		
		static readonly ExpressionStatement displayClassAssignmentPattern =
			new ExpressionStatement(new AssignmentExpression(
				new NamedNode("variable", new IdentifierExpression(Pattern.AnyString)),
				new ObjectCreateExpression { Type = new AnyNode("type") }
			));
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			int numberOfVariablesOutsideBlock = currentlyUsedVariableNames.Count;
			base.VisitBlockStatement(blockStatement, data);
			foreach (ExpressionStatement stmt in blockStatement.Statements.OfType<ExpressionStatement>().ToArray()) {
				Match displayClassAssignmentMatch = displayClassAssignmentPattern.Match(stmt);
				if (!displayClassAssignmentMatch.Success)
					continue;
				
				ILVariable variable = displayClassAssignmentMatch.Get<AstNode>("variable").Single().Annotation<ILVariable>();
				if (variable == null)
					continue;
				TypeDefinition type = variable.Type.ResolveWithinSameModule();
				if (!IsPotentialClosure(context, type))
					continue;
				if (displayClassAssignmentMatch.Get<AstType>("type").Single().Annotation<TypeReference>().ResolveWithinSameModule() != type)
					continue;
				
				// Looks like we found a display class creation. Now let's verify that the variable is used only for field accesses:
				bool ok = true;
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name && identExpr != displayClassAssignmentMatch.Get("variable").Single()) {
						if (!(identExpr.Parent is MemberReferenceExpression && identExpr.Parent.Annotation<FieldReference>() != null))
							ok = false;
					}
				}
				if (!ok)
					continue;
				Dictionary<FieldReference, AstNode> dict = new Dictionary<FieldReference, AstNode>();
				
				// Delete the variable declaration statement:
				VariableDeclarationStatement displayClassVarDecl = PatternStatementTransform.FindVariableDeclaration(stmt, variable.Name);
				if (displayClassVarDecl != null)
					displayClassVarDecl.Remove();
				
				// Delete the assignment statement:
				AstNode cur = stmt.NextSibling;
				stmt.Remove();
				
				// Delete any following statements as long as they assign parameters to the display class
				BlockStatement rootBlock = blockStatement.Ancestors.OfType<BlockStatement>().LastOrDefault() ?? blockStatement;
				List<ILVariable> parameterOccurrances = rootBlock.Descendants.OfType<IdentifierExpression>()
					.Select(n => n.Annotation<ILVariable>()).Where(p => p != null && p.IsParameter).ToList();
				AstNode next;
				for (; cur != null; cur = next) {
					next = cur.NextSibling;
					
					// Test for the pattern:
					// "variableName.MemberName = right;"
					ExpressionStatement closureFieldAssignmentPattern = new ExpressionStatement(
						new AssignmentExpression(
							new NamedNode("left", new MemberReferenceExpression { 
							              	Target = new IdentifierExpression(variable.Name),
							              	MemberName = Pattern.AnyString
							              }),
							new AnyNode("right")
						)
					);
					Match m = closureFieldAssignmentPattern.Match(cur);
					if (m.Success) {
						FieldDefinition fieldDef = m.Get<MemberReferenceExpression>("left").Single().Annotation<FieldReference>().ResolveWithinSameModule();
						AstNode right = m.Get<AstNode>("right").Single();
						bool isParameter = false;
						bool isDisplayClassParentPointerAssignment = false;
						if (right is ThisReferenceExpression) {
							isParameter = true;
						} else if (right is IdentifierExpression) {
							// handle parameters only if the whole method contains no other occurrence except for 'right'
							ILVariable v = right.Annotation<ILVariable>();
							isParameter = v.IsParameter && parameterOccurrances.Count(c => c == v) == 1;
							if (!isParameter && IsPotentialClosure(context, v.Type.ResolveWithinSameModule())) {
								// parent display class within the same method
								// (closure2.localsX = closure1;)
								isDisplayClassParentPointerAssignment = true;
							}
						} else if (right is MemberReferenceExpression) {
							// copy of parent display class reference from an outer lambda
							// closure2.localsX = this.localsY
							MemberReferenceExpression mre = m.Get<MemberReferenceExpression>("right").Single();
							do {
								// descend into the targets of the mre as long as the field types are closures
								FieldDefinition fieldDef2 = mre.Annotation<FieldReference>().ResolveWithinSameModule();
								if (fieldDef2 == null || !IsPotentialClosure(context, fieldDef2.FieldType.ResolveWithinSameModule())) {
									break;
								}
								// if we finally get to a this reference, it's copying a display class parent pointer
								if (mre.Target is ThisReferenceExpression) {
									isDisplayClassParentPointerAssignment = true;
								}
								mre = mre.Target as MemberReferenceExpression;
							} while (mre != null);
						}
						if (isParameter || isDisplayClassParentPointerAssignment) {
							dict[fieldDef] = right;
							cur.Remove();
						} else {
							break;
						}
					} else {
						break;
					}
				}
				
				// Now create variables for all fields of the display class (except for those that we already handled as parameters)
				List<Tuple<AstType, ILVariable>> variablesToDeclare = new List<Tuple<AstType, ILVariable>>();
				foreach (FieldDefinition field in type.Fields) {
					if (field.IsStatic)
						continue; // skip static fields
					if (dict.ContainsKey(field)) // skip field if it already was handled as parameter
						continue;
					string capturedVariableName = field.Name;
					if (capturedVariableName.StartsWith("$VB$Local_", StringComparison.Ordinal) && capturedVariableName.Length > 10)
						capturedVariableName = capturedVariableName.Substring(10);
					EnsureVariableNameIsAvailable(blockStatement, capturedVariableName);
					currentlyUsedVariableNames.Add(capturedVariableName);
					ILVariable ilVar = new ILVariable
					{
						IsGenerated = true,
						Name = capturedVariableName,
						Type = field.FieldType,
					};
					variablesToDeclare.Add(Tuple.Create(AstBuilder.ConvertType(field.FieldType, field), ilVar));
					dict[field] = new IdentifierExpression(capturedVariableName).WithAnnotation(ilVar);
				}
				
				// Now figure out where the closure was accessed and use the simpler replacement expression there:
				foreach (var identExpr in blockStatement.Descendants.OfType<IdentifierExpression>()) {
					if (identExpr.Identifier == variable.Name) {
						MemberReferenceExpression mre = (MemberReferenceExpression)identExpr.Parent;
						AstNode replacement;
						if (dict.TryGetValue(mre.Annotation<FieldReference>().ResolveWithinSameModule(), out replacement)) {
							mre.ReplaceWith(replacement.Clone());
						}
					}
				}
				// Now insert the variable declarations (we can do this after the replacements only so that the scope detection works):
				Statement insertionPoint = blockStatement.Statements.FirstOrDefault();
				foreach (var tuple in variablesToDeclare) {
					var newVarDecl = new VariableDeclarationStatement(tuple.Item1, tuple.Item2.Name);
					newVarDecl.Variables.Single().AddAnnotation(new CapturedVariableAnnotation());
					newVarDecl.Variables.Single().AddAnnotation(tuple.Item2);
					blockStatement.Statements.InsertBefore(insertionPoint, newVarDecl);
				}
			}
			currentlyUsedVariableNames.RemoveRange(numberOfVariablesOutsideBlock, currentlyUsedVariableNames.Count - numberOfVariablesOutsideBlock);
			return null;
		}

		void EnsureVariableNameIsAvailable(AstNode currentNode, string name)
		{
			int pos = currentlyUsedVariableNames.IndexOf(name);
			if (pos < 0) {
				// name is still available
				return;
			}
			// Naming conflict. Let's rename the existing variable so that the field keeps the name from metadata.
			NameVariables nv = new NameVariables();
			// Add currently used variable and parameter names
			foreach (string nameInUse in currentlyUsedVariableNames)
				nv.AddExistingName(nameInUse);
			// variables declared in child nodes of this block
			foreach (VariableInitializer vi in currentNode.Descendants.OfType<VariableInitializer>())
				nv.AddExistingName(vi.Name);
			// parameters in child lambdas
			foreach (ParameterDeclaration pd in currentNode.Descendants.OfType<ParameterDeclaration>())
				nv.AddExistingName(pd.Name);
			
			string newName = nv.GetAlternativeName(name);
			currentlyUsedVariableNames[pos] = newName;
			
			// find top-most block
			AstNode topMostBlock = currentNode.Ancestors.OfType<BlockStatement>().LastOrDefault() ?? currentNode;
			
			// rename identifiers
			foreach (IdentifierExpression ident in topMostBlock.Descendants.OfType<IdentifierExpression>()) {
				if (ident.Identifier == name) {
					ident.Identifier = newName;
					ILVariable v = ident.Annotation<ILVariable>();
					if (v != null)
						v.Name = newName;
				}
			}
			// rename variable declarations
			foreach (VariableInitializer vi in topMostBlock.Descendants.OfType<VariableInitializer>()) {
				if (vi.Name == name) {
					vi.Name = newName;
					ILVariable v = vi.Annotation<ILVariable>();
					if (v != null)
						v.Name = newName;
				}
			}
		}
	}
}
