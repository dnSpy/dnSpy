//
// TypeGuessing.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public static class TypeGuessing
	{
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

		static IEnumerable<IType> GetAllValidTypesFromInvocation(CSharpAstResolver resolver, InvocationExpression invoke, AstNode parameter)
		{
			int index = GetArgumentIndex(invoke.Arguments, parameter);
			if (index < 0)
				yield break;

			var targetResult = resolver.Resolve(invoke.Target) as MethodGroupResolveResult;
			if (targetResult != null) {
				foreach (var method in targetResult.Methods) {
					if (index < method.Parameters.Count) {
						if (method.Parameters [index].IsParams) {
							var arrayType = method.Parameters [index].Type as ArrayType;
							if (arrayType != null)
								yield return arrayType.ElementType;
						}

						yield return method.Parameters [index].Type;
					}
				}
				foreach (var extMethods in targetResult.GetExtensionMethods ()) {
					foreach (var extMethod in extMethods) {
						IType[] inferredTypes;
						var m = extMethod;
						if (CSharpResolver.IsEligibleExtensionMethod(targetResult.TargetType, extMethod, true, out inferredTypes)) {
							if (inferredTypes != null)
								m = extMethod.Specialize(new TypeParameterSubstitution(null, inferredTypes));
						}

						int correctedIndex = index + 1;
						if (correctedIndex < m.Parameters.Count) {
							if (m.Parameters [correctedIndex].IsParams) {
								var arrayType = m.Parameters [correctedIndex].Type as ArrayType;
								if (arrayType != null)
									yield return arrayType.ElementType;
							}
							yield return m.Parameters [correctedIndex].Type;
						}
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

		public static IType GetElementType(CSharpAstResolver resolver, IType type)
		{
			// TODO: A better get element type method.
			if (type.Kind == TypeKind.Array || type.Kind == TypeKind.Dynamic) {
				if (type.Kind == TypeKind.Array)
					return ((ArrayType)type).ElementType;
				return resolver.Compilation.FindType(KnownTypeCode.Object);
			}


			foreach (var method in type.GetMethods (m => m.Name == "GetEnumerator")) {
				IType returnType = null;
				foreach (var prop in method.ReturnType.GetProperties(p => p.Name == "Current")) {
					if (returnType != null && prop.ReturnType.IsKnownType (KnownTypeCode.Object))
						continue;
					returnType = prop.ReturnType;
				}
				if (returnType != null)
					return returnType;
			}

			return resolver.Compilation.FindType(KnownTypeCode.Object);
		}

		static IEnumerable<IType> GuessFromConstructorInitializer(CSharpAstResolver resolver, AstNode expr)
		{
			var init = expr.Parent as ConstructorInitializer;
			var rr = resolver.Resolve(expr.Parent);
			int index = GetArgumentIndex(init.Arguments, expr);
			if (index >= 0) {
				foreach (var constructor in rr.Type.GetConstructors()) {
					if (index < constructor.Parameters.Count) {
						yield return constructor.Parameters[index].Type;
					}
				}
			}
		}

		public static IEnumerable<IType> GetValidTypes(CSharpAstResolver resolver, AstNode expr)
		{
			if (expr.Role == Roles.Condition) {
				return new [] { resolver.Compilation.FindType (KnownTypeCode.Boolean) };
			}

			var mref = expr as MemberReferenceExpression;
			if (mref != null) {
				// case: guess enum when trying to access not existent enum member
				var rr = resolver.Resolve(mref.Target);
				if (!rr.IsError && rr.Type.Kind == TypeKind.Enum)
					return new [] { rr.Type };
			}

			if (expr.Parent is ParenthesizedExpression || expr.Parent is NamedArgumentExpression) {
				return GetValidTypes(resolver, expr.Parent);
			}
			if (expr.Parent is DirectionExpression) {
				var parent = expr.Parent.Parent;
				if (parent is InvocationExpression) {
					var invoke = (InvocationExpression)parent;
					return GetAllValidTypesFromInvocation(resolver, invoke, expr.Parent);
				}
			}

			if (expr.Parent is ArrayInitializerExpression) {
				if (expr is NamedExpression)
					return new [] { resolver.Resolve(((NamedExpression)expr).Expression).Type };

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
					return GetAllValidTypesFromInvocation(resolver, invoke, expr);
				}
			}

			if (expr.Parent is VariableInitializer) {
				var initializer = (VariableInitializer)expr.Parent;
				var field = initializer.GetParent<FieldDeclaration>();
				if (field != null) {
					var rr = resolver.Resolve(field.ReturnType);
					if (!rr.IsError)
						return new [] { rr.Type };
				}
				var varStmt = initializer.GetParent<VariableDeclarationStatement>();
				if (varStmt != null) {
					var rr = resolver.Resolve(varStmt.Type);
					if (!rr.IsError)
						return new [] { rr.Type };
				}
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
				var parent = expr.Ancestors.FirstOrDefault(n => n is EntityDeclaration || n is AnonymousMethodExpression|| n is LambdaExpression);
				if (parent != null) {
					var rr = resolver.Resolve(parent);
					if (!rr.IsError)
						return new [] { rr.Type };
				}
				var e = parent as EntityDeclaration;
				if (e != null) {
					var rt = resolver.Resolve(e.ReturnType);
					if (!rt.IsError)
						return new [] { rt.Type };
				}
			}

			if (expr.Parent is YieldReturnStatement) {
				ParameterizedType pt = null;
				var parent = expr.Ancestors.FirstOrDefault(n => n is EntityDeclaration || n is AnonymousMethodExpression|| n is LambdaExpression);
				if (parent != null) {
					var rr = resolver.Resolve(parent);
					if (!rr.IsError)
						pt = rr.Type as ParameterizedType;
				}
				var e = parent as EntityDeclaration;
				if (e != null) {
					var rt = resolver.Resolve(e.ReturnType);
					if (!rt.IsError)
						pt = rt.Type as ParameterizedType;
				}
				if (pt != null) {
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

			if (expr.Parent is ConstructorInitializer)
				return GuessFromConstructorInitializer(resolver, expr);

			if (expr.Parent is NamedExpression) {
				var rr = resolver.Resolve(expr.Parent);
				if (!rr.IsError) {
					return new [] { rr.Type };
				}
			}

			return Enumerable.Empty<IType>();
		}
		static readonly IType[] emptyTypes = new IType[0];
		public static AstType GuessAstType(RefactoringContext context, AstNode expr)
		{
			var type = GetValidTypes(context.Resolver, expr).ToArray();
			var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.Improved;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);
			if (inferedType.Kind == TypeKind.Unknown)
				return new PrimitiveType("object");
			return context.CreateShortType(inferedType);
		}

		public static IType GuessType(BaseRefactoringContext context, AstNode expr)
		{
			if (expr is SimpleType && expr.Role == Roles.TypeArgument) {
				if (expr.Parent is MemberReferenceExpression || expr.Parent is IdentifierExpression) {
					var rr = context.Resolve (expr.Parent);
					var argumentNumber = expr.Parent.GetChildrenByRole (Roles.TypeArgument).TakeWhile (c => c != expr).Count ();

					var mgrr = rr as MethodGroupResolveResult;
					if (mgrr != null && mgrr.Methods.Any () && mgrr.Methods.First ().TypeArguments.Count > argumentNumber)
						return mgrr.Methods.First ().TypeParameters[argumentNumber]; 
				} else if (expr.Parent is MemberType || expr.Parent is SimpleType) {
					var rr = context.Resolve (expr.Parent);
					var argumentNumber = expr.Parent.GetChildrenByRole (Roles.TypeArgument).TakeWhile (c => c != expr).Count ();
					var mgrr = rr as TypeResolveResult;
					if (mgrr != null &&  mgrr.Type.TypeParameterCount > argumentNumber) {
						return mgrr.Type.GetDefinition ().TypeParameters[argumentNumber]; 
					}
				}
			}

			var type = GetValidTypes(context.Resolver, expr).ToArray();
			var typeInference = new TypeInference(context.Compilation);
			typeInference.Algorithm = TypeInferenceAlgorithm.Improved;
			var inferedType = typeInference.FindTypeInBounds(type, emptyTypes);
			return inferedType;
		}
	}
}

