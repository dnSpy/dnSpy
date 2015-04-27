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
		
		public CSharpParameterCompletionEngine(IDocument document, ICompletionContextProvider completionContextProvider, IParameterCompletionDataFactory factory, IProjectContent content, CSharpTypeResolveContext ctx) : base (content, completionContextProvider, ctx)
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
			SyntaxTree baseUnit;
			if (currentMember == null && currentType == null) { 
				return null;
			}
			baseUnit = ParseStub("x]");
			
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
			SyntaxTree baseUnit;
			if (currentMember == null && currentType == null) { 
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
			SyntaxTree baseUnit;
			if (currentMember == null && currentType == null) { 
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

		public ExpressionResult GetMethodTypeArgumentInvocationBeforeCursor()
		{
			SyntaxTree baseUnit;
			if (currentMember == null && currentType == null) { 
				return null;
			}
			baseUnit = ParseStub("x>.A ()");
			
			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var expr = baseUnit.GetNodeAt<MemberReferenceExpression>(location.Line, location.Column + 1);
			if (expr == null)
				return null;
			return new ExpressionResult((AstNode)expr, baseUnit);
		}



		IEnumerable<IMethod> CollectMethods(AstNode resolvedNode, MethodGroupResolveResult resolveResult)
		{
			var lookup = new MemberLookup(ctx.CurrentTypeDefinition, Compilation.MainAssembly);
			bool onlyStatic = false;
			if (resolvedNode is IdentifierExpression && currentMember != null && currentMember.IsStatic || resolveResult.TargetResult is TypeResolveResult) {
				onlyStatic = true;
			}
			var methods = new List<IMethod>();
			foreach (var method in resolveResult.Methods) {
				if (method.IsConstructor) {
					continue;
				}
				if (!lookup.IsAccessible (method, true))
					continue;
				if (onlyStatic && !method.IsStatic) {
					continue;
				}
				if (method.IsShadowing) {
					for (int j = 0; j < methods.Count; j++) {
						if (ParameterListComparer.Instance.Equals(methods[j].Parameters, method.Parameters)) {
							methods.RemoveAt (j);
							j--;
						}
					}
				}
				methods.Add (method);
			}
			foreach (var m in methods) {
				yield return m;
			}
			foreach (var extMethods in resolveResult.GetEligibleExtensionMethods (true)) {
				foreach (var method in extMethods) {
					if (methods.Contains (method))
						continue;
					yield return new ReducedExtensionMethod (method);
				}
			}
		}

		IEnumerable<IProperty> GetAccessibleIndexers(IType type)
		{
			var lookup = new MemberLookup(ctx.CurrentTypeDefinition, Compilation.MainAssembly);
			var properties = new List<IProperty>();
			foreach (var property in type.GetProperties ()) {
				if (!property.IsIndexer)
					continue;
				if (!lookup.IsAccessible (property, true))
					continue;
				if (property.IsShadowing) {
					for (int j = 0; j < properties.Count; j++) {
						if (ParameterListComparer.Instance.Equals(properties[j].Parameters, property.Parameters)) {
							properties.RemoveAt (j);
							j--;
						}
					}
				}

				properties.Add (property);
			}
			return properties;
		}
		
		public IParameterDataProvider GetParameterDataProvider(int offset, char completionChar)
		{
			//Ignoring completionChar == '\0' because it usually means moving with arrow keys, tab or enter
			//we don't want to trigger on those events but it probably should be handled somewhere else
			//since our job is to resolve method and not to decide when to display tooltip or not
			if (offset <= 0 || completionChar == '\0') {
				return null;
			}
			SetOffset (offset);
			int startOffset;
			string text;
			if (currentMember == null && currentType == null) {
				//In case of attributes parse all file
				startOffset = 0;
				text = document.Text;
			} else {
				var memberText = GetMemberTextToCaret ();
				text = memberText.Item1;
				startOffset = document.GetOffset (memberText.Item2);
			}

			var parenStack = new Stack<int> ();
			var chevronStack = new Stack<int> ();
			var squareStack = new Stack<int> ();
			var bracketStack = new Stack<int> ();

			var lex = new MiniLexer (text);
			bool failed = lex.Parse ((ch, off) => {
				if (lex.IsInString || lex.IsInChar || lex.IsInVerbatimString || lex.IsInSingleComment || lex.IsInMultiLineComment || lex.IsInPreprocessorDirective)
					return false;
				switch (ch) {
				case '(':
					parenStack.Push (startOffset + off);
					break;
				case ')':
					if (parenStack.Count == 0) {
						return true;
					}
					parenStack.Pop ();
					break;
				case '<':
					chevronStack.Push (startOffset + off);
					break;
				case '>':
					//Don't abort if we don't have macthing '<' for '>' it could be if (i > 0) Foo($
					if (chevronStack.Count == 0) {
						return false;
					}
					chevronStack.Pop ();
					break;
				case '[':
					squareStack.Push (startOffset + off);
					break;
				case ']':
					if (squareStack.Count == 0) {
						return true;
					}
					squareStack.Pop ();
					break;
				case '{':
					bracketStack.Push (startOffset + off);
					break;
				case '}':
					if (bracketStack.Count == 0) {
						return true;
					}
					bracketStack.Pop ();
					break;
				}
				return false;
			});
			if (failed)
				return null;
			int result = -1;
			if (parenStack.Count > 0)
				result = parenStack.Pop ();
			if (squareStack.Count > 0)
				result = Math.Max (result, squareStack.Pop ());
			if (chevronStack.Count > 0)
				result = Math.Max (result, chevronStack.Pop ());

			//If we are inside { bracket we don't want to display anything
			if (bracketStack.Count > 0 && bracketStack.Pop () > result)
				return null;
			if (result == -1)
				return null;
			SetOffset (result + 1);
			ResolveResult resolveResult;
			switch (document.GetCharAt (result)) {
				case '(':
					var invoke = GetInvocationBeforeCursor(true) ?? GetConstructorInitializerBeforeCursor();
					if (invoke == null) {
						return null;
					}
					if (invoke.Node is ConstructorInitializer) {
						var init = (ConstructorInitializer)invoke.Node;
						if (init.ConstructorInitializerType == ConstructorInitializerType.This) {
							return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), ctx.CurrentTypeDefinition, init);
						} else {
							var baseType = ctx.CurrentTypeDefinition.DirectBaseTypes.FirstOrDefault(bt => bt.Kind != TypeKind.Interface);
							if (baseType == null) {
								return null;
							}
							return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), baseType);
						}
					}
					if (invoke.Node is ObjectCreateExpression) {
						var createType = ResolveExpression(((ObjectCreateExpression)invoke.Node).Type);
						if (createType.Result.Type.Kind == TypeKind.Unknown)
							return null;
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), createType.Result.Type);
					}
					
					if (invoke.Node is ICSharpCode.NRefactory.CSharp.Attribute) {
						var attribute = ResolveExpression(invoke);
						if (attribute == null || attribute.Result == null) {
							return null;
						}
						return factory.CreateConstructorProvider(document.GetOffset(invoke.Node.StartLocation), attribute.Result.Type);
					}
					var invocationExpression = ResolveExpression(invoke);
					if (invocationExpression == null || invocationExpression.Result == null || invocationExpression.Result.IsError) {
						return null;
					}
					resolveResult = invocationExpression.Result;
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
				case '<':
					invoke = GetMethodTypeArgumentInvocationBeforeCursor();
					if (invoke != null) {
						var tExpr2 = ResolveExpression(invoke);
						if (tExpr2 != null && tExpr2.Result is MethodGroupResolveResult && !tExpr2.Result.IsError) {
							return factory.CreateTypeParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectMethods(invoke.Node, tExpr2.Result as MethodGroupResolveResult));
						}
					}
					invoke = GetTypeBeforeCursor();
					if (invoke == null || invoke.Node.StartLocation.IsEmpty) {
						return null;
					}
					var tExpr = ResolveExpression(invoke);
					if (tExpr == null || tExpr.Result == null || tExpr.Result.IsError) {
						return null;
					}

					return factory.CreateTypeParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), CollectAllTypes(tExpr.Result.Type));
				case '[':
					invoke = GetIndexerBeforeCursor();
					if (invoke == null) {
						return null;
					}
					if (invoke.Node is ArrayCreateExpression) {
						return null;
					}
					var indexerExpression = ResolveExpression(invoke);
					if (indexerExpression == null || indexerExpression.Result == null || indexerExpression.Result.IsError) {
						return null;
					}
					return factory.CreateIndexerParameterDataProvider(document.GetOffset(invoke.Node.StartLocation), indexerExpression.Result.Type, GetAccessibleIndexers (indexerExpression.Result.Type), invoke.Node);
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
			var scope = ctx.CurrentUsingScope;
			var result = new List<string>();
			while (scope != null) {
				result.Add(scope.Namespace.FullName);
				
				foreach (var ns in scope.Usings) {
					result.Add(ns.FullName);
				}
				scope = scope.Parent;
			}
			return result;
		}
		
		
	}
}

