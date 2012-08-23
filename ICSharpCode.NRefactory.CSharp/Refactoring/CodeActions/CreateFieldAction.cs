// 
// CreateField.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create field", Description = "Creates a field for a undefined variable.")]
	public class CreateFieldAction : ICodeActionProvider
	{
		internal static bool IsInvocationTarget(AstNode node)
		{
			var invoke = node.Parent as InvocationExpression;
			return invoke != null && invoke.Target == node;
		}

		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var expr = context.GetNode(n => n is IdentifierExpression || n is MemberReferenceExpression) as Expression;
			if (expr == null)
				yield break;

			if (expr is MemberReferenceExpression && !(((MemberReferenceExpression)expr).Target is ThisReferenceExpression))
				yield break;

			var propertyName = CreatePropertyAction.GetPropertyName(expr);
			if (propertyName == null)
				yield break;

			if (IsInvocationTarget(expr))
				yield break;
			var statement = expr.GetParent<Statement>();
			if (statement == null)
				yield break;
			if (!(context.Resolve(expr).IsError))
				yield break;
			var guessedType = CreateFieldAction.GuessAstType(context, expr);
			if (guessedType == null)
				yield break;
			var state = context.GetResolverStateBefore(expr);
			if (state.CurrentMember == null || state.CurrentTypeDefinition == null)
				yield break;

			bool isStatic = state.CurrentMember.IsStatic | state.CurrentTypeDefinition.IsStatic;

//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(identifier.Identifier, AffectedEntity.Field, Modifiers.Private, isStatic)) { 
//				yield break;
//			}

			yield return new CodeAction(context.TranslateString("Create field"), script => {
				var decl = new FieldDeclaration() {
					ReturnType = guessedType,
					Variables = { new VariableInitializer(propertyName) }
				};
				if (isStatic)
					decl.Modifiers |= Modifiers.Static;
				script.InsertWithCursor(context.TranslateString("Create field"), Script.InsertPosition.Before, decl);
			});

		}

		#region Type guessing
		static int GetArgumentIndex(IEnumerable<Expression> arguments, AstNode parameter)
		{
			int argumentNumber = 0;
			foreach (var arg in arguments) {
				if (arg == parameter) {
					return argumentNumber;
				}
				argumentNumber++;
			}
			return -1;
		}

		static IEnumerable<IType> GetAllValidTypesFromInvokation(CSharpAstResolver resolver, InvocationExpression invoke, AstNode parameter)
		{
			int index = GetArgumentIndex(invoke.Arguments, parameter);
			if (index < 0)
				yield break;
					
			var targetResult = resolver.Resolve(invoke.Target);
			if (targetResult is MethodGroupResolveResult) {
				foreach (var method in ((MethodGroupResolveResult)targetResult).Methods) {
					if (index < method.Parameters.Count) {
						yield return method.Parameters [index].Type;
					}
				}
			}
		}

		static IEnumerable<IType> GetAllValidTypesFromObjectCreation(CSharpAstResolver resolver, ObjectCreateExpression invoke, AstNode parameter)
		{
			int index = GetArgumentIndex(invoke.Arguments, parameter);
			if (index < 0)
				yield break;

			var targetResult = resolver.Resolve(invoke.Type);
			if (targetResult is TypeResolveResult) {
				var type = ((TypeResolveResult)targetResult).Type;
				if (type.Kind == TypeKind.Delegate && index == 0) {
					yield return type;
					yield break;
				}
				foreach (var constructor in type.GetConstructors ()) {
					if (index < constructor.Parameters.Count)
						yield return constructor.Parameters [index].Type;
				}
			}
		}

		static IType GetElementType(CSharpAstResolver resolver, IType type)
		{
			// TODO: A better get element type method.
			if (type.Kind == TypeKind.Array || type.Kind == TypeKind.Dynamic) {
				if (type.Kind == TypeKind.Array)
					return ((ArrayType)type).ElementType;
				return resolver.Compilation.FindType(KnownTypeCode.Object);
			}

			foreach (var method in type.GetMethods (m => m.Name == "GetEnumerator")) {
				var pr = method.ReturnType.GetProperties(p => p.Name == "Current").FirstOrDefault();
				if (pr != null)
					return pr.ReturnType;
			}

			return resolver.Compilation.FindType(KnownTypeCode.Object);
		}

		internal static IEnumerable<IType> GetValidTypes(CSharpAstResolver resolver, Expression expr)
		{
			if (expr.Parent is DirectionExpression) {
				var parent = expr.Parent.Parent;
				if (parent is InvocationExpression) {
					var invoke = (InvocationExpression)parent;
					return GetAllValidTypesFromInvokation(resolver, invoke, expr.Parent);
				}
			}

			if (expr.Parent is ArrayInitializerExpression) {
				var aex = expr.Parent as ArrayInitializerExpression;
				if (aex.IsSingleElement)
					aex = aex.Parent as ArrayInitializerExpression;
				var type = GetElementType(resolver, resolver.Resolve(aex.Parent).Type);
				if (type.Kind != TypeKind.Unknown)
					return new [] { type };
			}

			if (expr.Parent is ObjectCreateExpression) {
				var invoke = (ObjectCreateExpression)expr.Parent;
				return GetAllValidTypesFromObjectCreation(resolver, invoke, expr);
			}

			if (expr.Parent is ArrayCreateExpression) {
				var ace = (ArrayCreateExpression)expr.Parent;
				if (!ace.Type.IsNull) {
					return new [] { resolver.Resolve(ace.Type).Type };
				}
			}

			if (expr.Parent is InvocationExpression) {
				var parent = expr.Parent;
				if (parent is InvocationExpression) {
					var invoke = (InvocationExpression)parent;
					return GetAllValidTypesFromInvokation(resolver, invoke, expr);
				}
			}
			
			if (expr.Parent is VariableInitializer) {
				var initializer = (VariableInitializer)expr.Parent;
				var field = initializer.GetParent<FieldDeclaration>();
				if (field != null)
					return new [] { resolver.Resolve(field.ReturnType).Type };
				return new [] { resolver.Resolve(initializer).Type };
			}
			
			if (expr.Parent is CastExpression) {
				var cast = (CastExpression)expr.Parent;
				return new [] { resolver.Resolve(cast.Type).Type };
			}
			
			if (expr.Parent is AsExpression) {
				var cast = (AsExpression)expr.Parent;
				return new [] { resolver.Resolve(cast.Type).Type };
			}

			if (expr.Parent is AssignmentExpression) {
				var assign = (AssignmentExpression)expr.Parent;
				var other = assign.Left == expr ? assign.Right : assign.Left;
				return new [] { resolver.Resolve(other).Type };
			}

			if (expr.Parent is BinaryOperatorExpression) {
				var assign = (BinaryOperatorExpression)expr.Parent;
				var other = assign.Left == expr ? assign.Right : assign.Left;
				return new [] { resolver.Resolve(other).Type };
			}
			
			if (expr.Parent is ReturnStatement) {
				var state = resolver.GetResolverStateBefore(expr.Parent);
				if (state != null  && state.CurrentMember != null)
					return new [] { state.CurrentMember.ReturnType };
			}

			if (expr.Parent is YieldReturnStatement) {
				var state = resolver.GetResolverStateBefore(expr);
				if (state != null && (state.CurrentMember.ReturnType is ParameterizedType)) {
					var pt = (ParameterizedType)state.CurrentMember.ReturnType;
					if (pt.FullName == "System.Collections.Generic.IEnumerable") {
						return new [] { pt.TypeArguments.First() };
					}
				}
			}

			if (expr.Parent is UnaryOperatorExpression) {
				var uop = (UnaryOperatorExpression)expr.Parent;
				switch (uop.Operator) {
					case UnaryOperatorType.Not:
						return new [] { resolver.Compilation.FindType(KnownTypeCode.Boolean) };
					case UnaryOperatorType.Minus:
					case UnaryOperatorType.Plus:
					case UnaryOperatorType.Increment:
					case UnaryOperatorType.Decrement:
					case UnaryOperatorType.PostIncrement:
					case UnaryOperatorType.PostDecrement:
						return new [] { resolver.Compilation.FindType(KnownTypeCode.Int32) };
				}
			}
			return Enumerable.Empty<IType>();
		}
		static readonly IType[] emptyTypes = new IType[0];
		internal static AstType GuessAstType(RefactoringContext context, Expression expr)
		{
			var type = GetValidTypes(context.Resolver, expr).ToArray();
			var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.ImprovedReturnAllResults;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);
			if (inferedType.Kind == TypeKind.Unknown)
				return new PrimitiveType("object");
			return context.CreateShortType(inferedType);
		}

		internal static IType GuessType(RefactoringContext context, Expression expr)
		{
			var type = GetValidTypes(context.Resolver, expr).ToArray();
			var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.ImprovedReturnAllResults;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);
			return inferedType;
		}
		#endregion
	}
}

