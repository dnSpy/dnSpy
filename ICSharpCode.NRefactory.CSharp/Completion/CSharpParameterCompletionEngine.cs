// 
// CSharpParameterCompletionEngine.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Completion;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CSharpParameterCompletionEngine : CSharpCompletionEngineBase
	{
		internal IParameterCompletionDataFactory factory;
		
		public CSharpParameterCompletionEngine(IDocument document, IParameterCompletionDataFactory factory, IProjectContent content, CSharpTypeResolveContext ctx, CompilationUnit unit, CSharpParsedFile parsedFile) : base (content, ctx, unit, parsedFile)
		{
			if (document == null) {
				throw new ArgumentNullException("document");
			}
			if (factory == null) {
				throw new ArgumentNullException("factory");
			}
			this.document = document;
			this.factory = factory;
		}

		public ExpressionResult GetIndexerBeforeCursor()
		{
			CompilationUnit baseUnit;
			if (currentMember == null && currentType == null) { 
				return null;
			}
			if (Unit == null) {
				return null;
			}
			baseUnit = ParseStub("x] = a[1");
			
			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt(location, n => n is IndexerExpression); 
			AstNode expr;
			if (mref is IndexerExpression) {
				expr = ((IndexerExpression)mref).Target;
			} else {
				return null;
			}
			
			return new ExpressionResult((AstNode)expr, baseUnit);
		}
		
		public ExpressionResult GetConstructorInitializerBeforeCursor()
		{
			CompilationUnit baseUnit;
			if (currentMember == null && currentType == null) { 
				return null;
			}
			if (Unit == null) {
				return null;
			}
			baseUnit = ParseStub("a) {}", false);
			
			var expr = baseUnit.GetNodeAt <ConstructorInitializer>(location); 
			if (expr == null) {
				return null;
			}
			return new ExpressionResult((AstNode)expr, baseUnit);
		}
		
		public ExpressionResult GetTypeBeforeCursor()
		{
			CompilationUnit baseUnit;
			if (currentMember == null && currentType == null) { 
				return null;
			}
			if (Unit == null) {
				return null;
			}
			baseUnit = ParseStub("x> a");
			
			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var expr = baseUnit.GetNodeAt<AstType>(location.Line, location.Column + 1);
			if (expr == null)
				return null;
			// '>' position
			return new ExpressionResult((AstNode)expr, baseUnit);
		}

		IEnumerable<IMethod> CollectMethods(AstNode resolvedNode, MethodGroupResolveResult resolveResult)
		{
			//			var lookup = new MemberLookup (ctx.CurrentTypeDefinition, Compilation.MainAssembly);
			bool onlyStatic = false;
			if (resolvedNode is IdentifierExpression && currentMember != null && currentMember.IsStatic) {
				onlyStatic = true;
			}
			
			foreach (var method in resolveResult.Methods) {
				if (method.IsConstructor) {
					continue;
				}
				//				if (!lookup.IsAccessible (member, true))
				//					continue;
				if (onlyStatic && !method.IsStatic) {
					continue;
				}
				yield return method;	
			}
				
			foreach (var extMethods in resolveResult.GetExtensionMethods ()) {
				foreach (var method in extMethods) {
					yield return method;
				}
			}
		}
		
		public IParameterDataProvider GetParameterDataProvider(int offset, char completionChar)
		{
			if (offset <= 0) {
				return null;
			}
			if (completionChar != '(' && completionChar != '<' && completionChar != '[' && completionChar != ',') {
				return null;
			}
			
			SetOffset(offset);
			if (IsInsideCommentStringOrDirective()) {
				return null;
			}

			ResolveResult resolveResult;
			switch (completionChar) {
				case '(':
					var invoke = GetInvocationBeforeCursor(true) ?? GetConstructorInitializerBeforeCursor();
					if (invoke == null) {
						return null;
					}
					if (invoke.Node is ConstructorInitializer) {
						var init = (ConstructorInitializer)invoke.Node;
						if (init.ConstructorInitializerType == ConstructorInitializerType.This) {
							return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), ctx.CurrentTypeDefinition);
						} else {
							var baseType = ctx.CurrentTypeDefinition.DirectBaseTypes.FirstOrDefault(bt => bt.Kind != TypeKind.Interface);
							if (baseType == null) {
								return null;
							}
							return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), baseType);
						}
					}
					if (invoke.Node is ObjectCreateExpression) {
						var createType = ResolveExpression(((ObjectCreateExpression)invoke.Node).Type, invoke.Unit);
						if (createType.Item1.Type.Kind == TypeKind.Unknown)
							return null;
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), createType.Item1.Type);
					}
				
					if (invoke.Node is ICSharpCode.NRefactory.CSharp.Attribute) {
						var attribute = ResolveExpression(invoke);
						if (attribute == null || attribute.Item1 == null) {
							return null;
						}
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), attribute.Item1.Type);
					}
					var invocationExpression = ResolveExpression(invoke);
					if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError) {
						return null;
					}
					resolveResult = invocationExpression.Item1;
					if (resolveResult is MethodGroupResolveResult) {
						return factory.CreateMethodDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectMethods(invoke.Node, resolveResult as MethodGroupResolveResult));
					}
					if (resolveResult is MemberResolveResult) {
						var mr = resolveResult as MemberResolveResult;
						if (mr.Member is IMethod) {
							return factory.CreateMethodDataProvider(document.GetOffset(invoke.Node.StartLocation), new [] { (IMethod)mr.Member });
						}
					}
				
					if (resolveResult.Type.Kind == TypeKind.Delegate) {
						return factory.CreateDelegateDataProvider(document.GetOffset(invoke.Node.StartLocation), resolveResult.Type);
					}
				
//				
//				if (result.ExpressionContext == ExpressionContext.BaseConstructorCall) {
//					if (resolveResult is ThisResolveResult)
//						return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as ThisResolveResult);
//					if (resolveResult is BaseResolveResult)
//						return new NRefactoryParameterDataProvider (textEditorData, resolver, resolveResult as BaseResolveResult);
//				}
//				IType resolvedType = resolver.SearchType (resolveResult.ResolvedType);
//				if (resolvedType != null && resolvedType.ClassType == ClassType.Delegate) {
//					return new NRefactoryParameterDataProvider (textEditorData, result.Expression, resolvedType);
//				}
					break;
				case ',':
					invoke = GetInvocationBeforeCursor(true) ?? GetIndexerBeforeCursor();
					if (invoke == null) {
						invoke = GetTypeBeforeCursor();
						if (invoke != null) {
							if (GetCurrentParameterIndex(document.GetOffset(invoke.Node.StartLocation), offset) < 0)
								return null;
							var typeExpression = ResolveExpression(invoke);
							if (typeExpression == null || typeExpression.Item1 == null || typeExpression.Item1.IsError) {
								return null;
							}
						
							return factory.CreateTypeParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectAllTypes(typeExpression.Item1.Type));
						}
						return null;
					}
					if (GetCurrentParameterIndex(document.GetOffset(invoke.Node.StartLocation), offset) < 0)
						return null;
					if (invoke.Node is ObjectCreateExpression) {
						var createType = ResolveExpression(((ObjectCreateExpression)invoke.Node).Type, invoke.Unit);
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), createType.Item1.Type);
					}
				
					if (invoke.Node is ICSharpCode.NRefactory.CSharp.Attribute) {
						var attribute = ResolveExpression(invoke);
						if (attribute == null || attribute.Item1 == null) {
							return null;
						}
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), attribute.Item1.Type);
					}
				
					invocationExpression = ResolveExpression(invoke);
				
					if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError) {
						return null;
					}
				
					resolveResult = invocationExpression.Item1;
					if (resolveResult is MethodGroupResolveResult) {
						return factory.CreateMethodDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectMethods(invoke.Node, resolveResult as MethodGroupResolveResult));
					}
					if (resolveResult is MemberResolveResult) {
						if (resolveResult.Type.Kind == TypeKind.Delegate) {
							return factory.CreateDelegateDataProvider(document.GetOffset(invoke.Node.StartLocation), resolveResult.Type);
						}
						var mr = resolveResult as MemberResolveResult;
						if (mr.Member is IMethod) {
							return factory.CreateMethodDataProvider(document.GetOffset(invoke.Node.StartLocation), new [] { (IMethod)mr.Member });
						}
					}
					if (resolveResult != null) {
						return factory.CreateIndexerParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), resolveResult.Type, invoke.Node);
					}
					break;
				case '<':
					invoke = GetTypeBeforeCursor();
					if (invoke == null) {
						return null;
					}
					var tExpr = ResolveExpression(invoke);
					if (tExpr == null || tExpr.Item1 == null || tExpr.Item1.IsError) {
						return null;
					}
				
					return factory.CreateTypeParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectAllTypes(tExpr.Item1.Type));
				case '[':
					invoke = GetIndexerBeforeCursor();
					if (invoke == null) {
						return null;
					}
					var indexerExpression = ResolveExpression(invoke);
					if (indexerExpression == null || indexerExpression.Item1 == null || indexerExpression.Item1.IsError) {
						return null;
					}
					return factory.CreateIndexerParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), indexerExpression.Item1.Type, invoke.Node);
			}
			return null;
		}
		
		IEnumerable<IType> CollectAllTypes(IType baseType)
		{
			var state = GetState();
			for (var n = state.CurrentUsingScope; n != null; n = n.Parent) {
				foreach (var u in n.Usings) {
					foreach (var type in u.Types) {
						if (type.TypeParameterCount > 0 && type.Name == baseType.Name) {
							yield return type;
						}
					}
				}
				
				foreach (var type in n.Namespace.Types) {
					if (type.TypeParameterCount > 0 && type.Name == baseType.Name) {
						yield return type;
					}
				}
			}
		}
		
		List<string> GetUsedNamespaces()
		{
			var scope = CSharpParsedFile.GetUsingScope(location);
			var result = new List<string>();
			var resolver = new CSharpResolver(ctx);
			while (scope != null) {
				result.Add(scope.NamespaceName);
				
				foreach (var u in scope.Usings) {
					var ns = u.ResolveNamespace(resolver);
					if (ns == null) {
						continue;
					}
					result.Add(ns.FullName);
				}
				scope = scope.Parent;
			}
			return result;
		}
		
		public int GetCurrentParameterIndex(int triggerOffset, int endOffset)
		{
			char lastChar = document.GetCharAt(endOffset - 1);
			if (lastChar == '(' || lastChar == '<') { 
				return 0;
			}
			var parameter = new Stack<int>();
			var bracketStack = new Stack<Stack<int>>();
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			for (int i = triggerOffset; i < endOffset; i++) {
				char ch = document.GetCharAt(i);
				char nextCh = i + 1 < document.TextLength ? document.GetCharAt(i + 1) : '\0';
				switch (ch) {
					case '{':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						bracketStack.Push(parameter);
						parameter = new Stack<int>();
						break;
					case '[':
					case '(':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push(0);
						break;
					case '}':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (bracketStack.Count > 0) {
							parameter = bracketStack.Pop();
						} else {
							return -1;
						}
						break;
					case ']':
					case ')':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop();
						} else {
							return -1;
						}
						break;
					case '<':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						parameter.Push(0);
						break;
					case '>':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Pop();
						}
						break;
					case ',':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (parameter.Count > 0) {
							parameter.Push(parameter.Pop() + 1);
						}
						break;
					case '/':
						if (inString || inChar || inVerbatimString) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inSingleComment = true;
						}
						if (nextCh == '*') {
							inMultiLineComment = true;
						}
						break;
					case '*':
						if (inString || inChar || inVerbatimString || inSingleComment) {
							break;
						}
						if (nextCh == '/') {
							i++;
							inMultiLineComment = false;
						}
						break;
					case '@':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment) {
							break;
						}
						if (nextCh == '"') {
							i++;
							inVerbatimString = true;
						}
						break;
					case '\n':
					case '\r':
						inSingleComment = false;
						inString = false;
						inChar = false;
						break;
					case '\\':
						if (inString || inChar) {
							i++;
						}
						break;
					case '"':
						if (inSingleComment || inMultiLineComment || inChar) {
							break;
						}
						if (inVerbatimString) {
							if (nextCh == '"') {
								i++;
								break;
							}
							inVerbatimString = false;
							break;
						}
						inString = !inString;
						break;
					case '\'':
						if (inSingleComment || inMultiLineComment || inString || inVerbatimString) {
							break;
						}
						inChar = !inChar;
						break;
				}
			}
			if (parameter.Count == 0 || bracketStack.Count > 0) {
				return -1;
			}
			return parameter.Pop() + 1;
		}
	}
}

