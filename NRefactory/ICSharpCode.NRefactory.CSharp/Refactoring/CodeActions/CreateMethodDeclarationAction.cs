// 
// CreateMethodDeclarationAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create method", Description = "Creates a method declaration out of an invocation.")]
	public class CreateMethodDeclarationAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var identifier = context.GetNode<IdentifierExpression>();
			if (identifier != null && !(identifier.Parent is InvocationExpression && ((InvocationExpression)identifier.Parent).Target == identifier))
				return GetActionsFromIdentifier(context, identifier);
			
			var memberReference = context.GetNode<MemberReferenceExpression>();
			if (memberReference != null && !(memberReference.Parent is InvocationExpression && ((InvocationExpression)memberReference.Parent).Target == memberReference))
				return GetActionsFromMemberReferenceExpression(context, memberReference);

			var invocation = context.GetNode<InvocationExpression>();
			if (invocation != null)
				return GetActionsFromInvocation(context, invocation);
			return Enumerable.Empty<CodeAction>();
		}

		IEnumerable<CodeAction> GetActionsFromMemberReferenceExpression(RefactoringContext context, MemberReferenceExpression invocation)
		{
			if (!(context.Resolve(invocation).IsError)) 
					yield break;

			var methodName = invocation.MemberName;
			var guessedType = CreateFieldAction.GuessType(context, invocation);
			if (guessedType.Kind != TypeKind.Delegate)
					yield break;
			var invocationMethod = guessedType.GetDelegateInvokeMethod();
			var state = context.GetResolverStateBefore(invocation);
			if (state.CurrentTypeDefinition == null)
				yield break;
			ResolveResult targetResolveResult = context.Resolve(invocation.Target);
			bool createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());

			bool isStatic;
			if (createInOtherType) {
				if (targetResolveResult.Type.GetDefinition() == null || targetResolveResult.Type.GetDefinition().Region.IsEmpty)
					yield break;
				isStatic = targetResolveResult is TypeResolveResult;
				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface || targetResolveResult.Type.Kind == TypeKind.Enum)
					yield break;
			} else {
				if (state.CurrentMember == null)
					yield break;
				isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
			}

//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic)) { 
//				yield break;
//			}

			yield return CreateAction(
				context, 
				methodName, 
				context.CreateShortType(invocationMethod.ReturnType),
				invocationMethod.Parameters.Select(parameter => new ParameterDeclaration(context.CreateShortType(parameter.Type), parameter.Name) { 
					ParameterModifier = GetModifiers(parameter)
				}),
				createInOtherType,
				isStatic,
				targetResolveResult);
		}
		
		IEnumerable<CodeAction> GetActionsFromIdentifier(RefactoringContext context, IdentifierExpression identifier)
		{
			if (!(context.Resolve(identifier).IsError))
				yield break;
			var methodName = identifier.Identifier;
			var guessedType = CreateFieldAction.GuessType(context, identifier);
			if (guessedType.Kind != TypeKind.Delegate)
				yield break;
			var invocationMethod = guessedType.GetDelegateInvokeMethod();
			if (invocationMethod == null)
				yield break;
			var state = context.GetResolverStateBefore(identifier);
			if (state.CurrentMember == null || state.CurrentTypeDefinition == null)
				yield break;
			bool isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;

			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic))
				yield break;

			yield return CreateAction(
				context, 
				methodName, 
				context.CreateShortType(invocationMethod.ReturnType),
				invocationMethod.Parameters.Select(parameter => new ParameterDeclaration(context.CreateShortType(parameter.Type), parameter.Name) { 
					ParameterModifier = GetModifiers(parameter)
				}),
				false,
				isStatic,
				null);
		}

		IEnumerable<CodeAction> GetActionsFromInvocation(RefactoringContext context, InvocationExpression invocation)
		{
			if (!(context.Resolve(invocation.Target).IsError)) 
				yield break;

			var methodName = GetMethodName(invocation);
			if (methodName == null)
				yield break;
			var state = context.GetResolverStateBefore(invocation);
			if (state.CurrentMember == null || state.CurrentTypeDefinition == null)
				yield break;
			var guessedType = invocation.Parent is ExpressionStatement ? new PrimitiveType("void") : CreateFieldAction.GuessAstType(context, invocation);

			bool createInOtherType = false;
			ResolveResult targetResolveResult = null;
			if (invocation.Target is MemberReferenceExpression) {
				targetResolveResult = context.Resolve(((MemberReferenceExpression)invocation.Target).Target);
				createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());
			}

			bool isStatic;
			if (createInOtherType) {
				if (targetResolveResult.Type.GetDefinition() == null || targetResolveResult.Type.GetDefinition().Region.IsEmpty)
					yield break;
				isStatic = targetResolveResult is TypeResolveResult;
				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface || targetResolveResult.Type.Kind == TypeKind.Enum)
					yield break;
			} else {
				isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
			}

//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic)) { 
//				yield break;
//			}


			yield return CreateAction(
				context, 
				methodName, 
				guessedType,
				GenerateParameters(context, invocation.Arguments),
				createInOtherType,
				isStatic,
				targetResolveResult);
		}

		static ParameterModifier GetModifiers(IParameter parameter)
		{
			if (parameter.IsOut)
				return ParameterModifier.Out;
			if (parameter.IsRef)
				return ParameterModifier.Ref;
			if (parameter.IsParams)
				return ParameterModifier.Params;
			return ParameterModifier.None;
		}

		static CodeAction CreateAction(RefactoringContext context, string methodName, AstType returnType, IEnumerable<ParameterDeclaration> parameters, bool createInOtherType, bool isStatic, ResolveResult targetResolveResult)
		{
			return new CodeAction(context.TranslateString("Create method"), script => {
				var decl = new MethodDeclaration() {
					ReturnType = returnType,
					Name = methodName,
					Body = new BlockStatement() {
						new ThrowStatement(new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))
					}
				};
				decl.Parameters.AddRange(parameters);
				
				if (isStatic)
					decl.Modifiers |= Modifiers.Static;
				
				if (createInOtherType) {
					if (targetResolveResult.Type.Kind == TypeKind.Interface) {
						decl.Body = null;
						decl.Modifiers = Modifiers.None;
					} else {
						decl.Modifiers |= Modifiers.Public;
					}

					script.InsertWithCursor(context.TranslateString("Create method"), targetResolveResult.Type.GetDefinition(), decl);
					return;
				}

				script.InsertWithCursor(context.TranslateString("Create method"), Script.InsertPosition.Before, decl);
			});
		}

		public static IEnumerable<ParameterDeclaration> GenerateParameters(RefactoringContext context, IEnumerable<Expression> arguments)
		{
			var nameCounter = new Dictionary<string, int>();
			foreach (var argument in arguments) {
				var direction = ParameterModifier.None;
				AstNode node;
				if (argument is DirectionExpression) {
					var de = (DirectionExpression)argument;
					direction = de.FieldDirection == FieldDirection.Out ? ParameterModifier.Out : ParameterModifier.Ref;
					node = de.Expression;
				} else {
					node = argument;
				}

				var resolveResult = context.Resolve(node);
				string name = CreateBaseName(argument, resolveResult.Type);
				if (!nameCounter.ContainsKey(name)) {
					nameCounter [name] = 1;
				} else {
					nameCounter [name]++;
					name += nameCounter [name].ToString();
				}
				var type = resolveResult.Type.Kind == TypeKind.Unknown ? new PrimitiveType("object") : context.CreateShortType(resolveResult.Type);

				yield return new ParameterDeclaration(type, name) { ParameterModifier = direction};
			}
		}

		static string CreateBaseNameFromString(string str)
		{
			if (string.IsNullOrEmpty(str)) {
				return "empty";
			}
			var sb = new StringBuilder();
			bool firstLetter = true, wordStart = false;
			foreach (char ch in str) {
				if (char.IsWhiteSpace(ch)) {
					wordStart = true;
					continue;
				}
				if (!char.IsLetter(ch))
					continue;
				if (firstLetter) {
					sb.Append(char.ToLower(ch));
					firstLetter = false;
					continue;
				}
				if (wordStart) {
					sb.Append(char.ToUpper(ch));
					wordStart = false;
					continue;
				}
				sb.Append(ch);
			}
			return sb.Length == 0 ? "str" : sb.ToString();
		}

		public static string CreateBaseName(AstNode node, IType type)
		{
			string name = null;
			if (node is DirectionExpression)
				node = ((DirectionExpression)node).Expression;
			if (node is IdentifierExpression) {
				name = ((IdentifierExpression)node).Identifier;
			} else if (node is MemberReferenceExpression) {
				name = ((MemberReferenceExpression)node).MemberName;
			} else if (node is PrimitiveExpression) {
				var pe = (PrimitiveExpression)node;
				if (pe.Value is string) {
					name = CreateBaseNameFromString(pe.Value.ToString());
				} else {
					return char.ToLower(type.Name [0]).ToString();
				}
			} else {
				if (type.Kind == TypeKind.Unknown)
					return "par";
				name = GuessNameFromType(type);
			}

			name = char.ToLower(name [0]) + name.Substring(1);
			return name;
		}

		static string GuessNameFromType(IType returnType)
		{
			switch (returnType.ReflectionName) {
				case "System.Byte":
				case "System.SByte":
					return "b";
				
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":
					return "i";
				
				case "System.Boolean":
					return "b";
				
				case "System.DateTime":
					return "date";
				
				case "System.Char":
					return "ch";
				case "System.Double":
				case "System.Decimal":
					return "d";
				case "System.Single":
					return "f";
				case "System.String":
					return "str";
				
				case "System.Exception":
					return "e";
				case "System.Object":
					return "obj";
				case "System.Func":
					return "func";
				case "System.Action":
					return "action";
			}
			return returnType.Name;
		}
		
		string GetMethodName(InvocationExpression invocation)
		{
			if (invocation.Target is IdentifierExpression)
				return ((IdentifierExpression)invocation.Target).Identifier;
			if (invocation.Target is MemberReferenceExpression)
				return ((MemberReferenceExpression)invocation.Target).MemberName;

			return null;
		}
	}
}
