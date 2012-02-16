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

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CSharpParameterCompletionEngine : CSharpCompletionEngineBase
	{
		internal IParameterCompletionDataFactory factory;
		
		public CSharpParameterCompletionEngine (IDocument document, IParameterCompletionDataFactory factory, IProjectContent content, CSharpTypeResolveContext ctx, CompilationUnit unit, CSharpParsedFile parsedFile) : base (content, ctx, unit, parsedFile)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (factory == null)
				throw new ArgumentNullException ("factory");
			this.document = document;
			this.factory = factory;
		}

		public Tuple<CSharpParsedFile, AstNode, CompilationUnit> GetIndexerBeforeCursor ()
		{
			CompilationUnit baseUnit;
			if (currentMember == null && currentType == null) 
				return null;
			if (Unit == null)
				return null;
			baseUnit = ParseStub ("x] = a[1");
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var mref = baseUnit.GetNodeAt (location, n => n is IndexerExpression); 
			AstNode expr;
			if (mref is IndexerExpression) {
				expr = ((IndexerExpression)mref).Target;
			} else {
				return null;
			}
			
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			if (member == null || member2 == null)
				return null;
			member2.Remove ();
			member.ReplaceWith (member2);
			var tsvisitor = new TypeSystemConvertVisitor (CSharpParsedFile.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, (AstNode)expr, Unit);
		}
		
		public Tuple<CSharpParsedFile, AstNode, CompilationUnit> GetTypeBeforeCursor ()
		{
			CompilationUnit baseUnit;
			if (currentMember == null && currentType == null) 
				return null;
			if (Unit == null)
				return null;
			baseUnit = ParseStub ("x> a");
			
			var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			var expr = baseUnit.GetNodeAt<AstType> (location.Line, location.Column + 1); // '>' position
			var member = Unit.GetNodeAt<AttributedNode> (memberLocation);
			var member2 = baseUnit.GetNodeAt<AttributedNode> (memberLocation);
			if (member == null || member2 == null)
				return null;
			member2.Remove ();
			member.ReplaceWith (member2);
			var tsvisitor = new TypeSystemConvertVisitor (CSharpParsedFile.FileName);
			Unit.AcceptVisitor (tsvisitor, null);
			return Tuple.Create (tsvisitor.ParsedFile, (AstNode)expr, Unit);
		}
		
		public IParameterDataProvider GetParameterDataProvider (int offset, char completionChar)
		{
			if (offset <= 0)
				return null;
			if (completionChar != '(' && completionChar != '<' && completionChar != '[' && completionChar != ',')
				return null;
			
			SetOffset (offset);
			if (IsInsideCommentOrString ())
				return null;
			
			
			ResolveResult resolveResult;
			switch (completionChar) {
			case '(':
				var invoke = GetInvocationBeforeCursor (true) ?? GetIndexerBeforeCursor ();
				if (invoke == null)
					return null;
				if (invoke.Item2 is ObjectCreateExpression) {
					var createType = ResolveExpression (invoke.Item1, ((ObjectCreateExpression)invoke.Item2).Type, invoke.Item3);
					return factory.CreateConstructorProvider (createType.Item1.Type);
				}
				
				if (invoke.Item2 is ICSharpCode.NRefactory.CSharp.Attribute) {
					var attribute = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
					if (attribute == null || attribute.Item1 == null)
						return null;
					return factory.CreateConstructorProvider (attribute.Item1.Type);
				}
				var invocationExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError)
					return null;
				resolveResult = invocationExpression.Item1;
				if (resolveResult is MethodGroupResolveResult)
					return factory.CreateMethodDataProvider (resolveResult as MethodGroupResolveResult);
				if (resolveResult is MemberResolveResult) {
					var mr = resolveResult as MemberResolveResult;
					if (mr.Member is IMethod)
						return factory.CreateMethodDataProvider ((IMethod)mr.Member);
				}
				
				if (resolveResult.Type.Kind == TypeKind.Delegate)
					return factory.CreateDelegateDataProvider (resolveResult.Type);
				
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
				invoke = GetInvocationBeforeCursor (true) ?? GetIndexerBeforeCursor ();
				if (invoke == null) {
					invoke = GetTypeBeforeCursor ();
					if (invoke !=null) {
						var typeExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
						if (typeExpression == null || typeExpression.Item1 == null || typeExpression.Item1.IsError)
							return null;
						
						return factory.CreateTypeParameterDataProvider (CollectAllTypes (typeExpression.Item1.Type));
					}
					return null;
				}
				if (invoke.Item2 is ObjectCreateExpression) {
					var createType = ResolveExpression (invoke.Item1, ((ObjectCreateExpression)invoke.Item2).Type, invoke.Item3);
					return factory.CreateConstructorProvider (createType.Item1.Type);
				}
				
				if (invoke.Item2 is ICSharpCode.NRefactory.CSharp.Attribute) {
					var attribute = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
					if (attribute == null || attribute.Item1 == null)
						return null;
					return factory.CreateConstructorProvider (attribute.Item1.Type);
				}
				
				invocationExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				
				if (invocationExpression == null || invocationExpression.Item1 == null || invocationExpression.Item1.IsError)
					return null;
				
				resolveResult = invocationExpression.Item1;
				if (resolveResult is MethodGroupResolveResult)
					return factory.CreateMethodDataProvider (resolveResult as MethodGroupResolveResult);
				if (resolveResult is MemberResolveResult) {
					if (resolveResult.Type.Kind == TypeKind.Delegate)
						return factory.CreateDelegateDataProvider (resolveResult.Type);
					var mr = resolveResult as MemberResolveResult;
					if (mr.Member is IMethod)
						return factory.CreateMethodDataProvider ((IMethod)mr.Member);
				}
				if (resolveResult != null)
					return factory.CreateIndexerParameterDataProvider (resolveResult.Type, invoke.Item2);
				break;
			case '<':
				invoke = GetTypeBeforeCursor ();
				if (invoke == null)
					return null;
				var tExpr = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				if (tExpr == null || tExpr.Item1 == null || tExpr.Item1.IsError)
					return null;
				
				return factory.CreateTypeParameterDataProvider (CollectAllTypes (tExpr.Item1.Type));
			case '[':
				invoke = GetIndexerBeforeCursor ();
				if (invoke == null)
					return null;
				var indexerExpression = ResolveExpression (invoke.Item1, invoke.Item2, invoke.Item3);
				if (indexerExpression == null || indexerExpression.Item1 == null || indexerExpression.Item1.IsError)
					return null;
				return factory.CreateIndexerParameterDataProvider (indexerExpression.Item1.Type, invoke.Item2);
			}
			return null;
		}
		
		IEnumerable<IType> CollectAllTypes (IType baseType)
		{
			var state = GetState ();
			for (var n = state.CurrentUsingScope; n != null; n = n.Parent) {
				foreach (var u in n.Usings) {
					foreach (var type in u.Types) {
						if (type.TypeParameterCount > 0 && type.Name == baseType.Name)
							yield return type;
					}
				}
				
				foreach (var type in n.Namespace.Types) {
					if (type.TypeParameterCount > 0 && type.Name == baseType.Name)
						yield return type;
				}
			}
		}
		
		List<string> GetUsedNamespaces ()
		{
			var scope = CSharpParsedFile.GetUsingScope (location);
			var result = new List<string> ();
			var resolver = new CSharpResolver (ctx);
			while (scope != null) {
				result.Add (scope.NamespaceName);
				
				foreach (var u in scope.Usings) {
					var ns = u.ResolveNamespace (resolver);
					if (ns == null)
						continue;
					result.Add (ns.FullName);
				}
				scope = scope.Parent;
			}
			return result;
		}
		
		public int GetCurrentParameterIndex (int triggerOffset)
		{
			SetOffset (triggerOffset);
			var text = GetMemberTextToCaret ();
			if (text.Item1.EndsWith ("(") || text.Item1.EndsWith ("<")) 
				return 0;
			var parameter = new Stack<int> ();
			
			bool inSingleComment = false, inString = false, inVerbatimString = false, inChar = false, inMultiLineComment = false;
			
			for (int i = 0; i < text.Item1.Length; i++) {
				char ch = text.Item1 [i];
				char nextCh = i + 1 < text.Item1.Length ? text.Item1 [i + 1] : '\0';
				
				switch (ch) {
				case '(':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					parameter.Push (0);
					break;
				case ')':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (parameter.Count > 0)
						parameter.Pop ();
					break;
				case '<':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					parameter.Push (0);
					break;
				case '>':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (parameter.Count > 0)
						parameter.Pop ();
					break;
				case ',':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
					if (parameter.Count > 0)
						parameter.Push (parameter.Pop () + 1);
					break;
				case '/':
					if (inString || inChar || inVerbatimString)
						break;
					if (nextCh == '/') {
						i++;
						inSingleComment = true;
					}
					if (nextCh == '*')
						inMultiLineComment = true;
					break;
				case '*':
					if (inString || inChar || inVerbatimString || inSingleComment)
						break;
					if (nextCh == '/') {
						i++;
						inMultiLineComment = false;
					}
					break;
				case '@':
					if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
						break;
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
					if (inString || inChar)
						i++;
					break;
				case '"':
					if (inSingleComment || inMultiLineComment || inChar)
						break;
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
					if (inSingleComment || inMultiLineComment || inString || inVerbatimString)
						break;
					inChar = !inChar;
					break;
				}
			}
			if (parameter.Count == 0)
				return -1;
			return parameter.Pop () + 1;
		}
		
		/*
		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = textEditorData.Caret.Offset - 1;
			var parsedDocument = Document.ParsedDocument;
			if (parsedDocument == null)
				return false;
			IMember mem = currentMember;
			if (mem == null || (mem is IType))
				return false;
			int startPos = textEditorData.LocationToOffset (mem.Region.BeginLine, mem.Region.BeginColumn);
			int parenDepth = 0;
			int chevronDepth = 0;
			while (cpos > startPos) {
				char c = textEditorData.GetCharAt (cpos);
				if (c == ')')
					parenDepth++;
				if (c == '>')
					chevronDepth++;
				if (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<') {
					int p = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, cpos + 1, startPos);
					if (p != -1) {
						cpos++;
						return true;
					} else {
						return false;
					}
				}
				if (c == '(')
					parenDepth--;
				if (c == '<')
					chevronDepth--;
				cpos--;
			}
			return false;
		}*/
	}
}

