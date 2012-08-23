// 
// CSharpCompletionEngine.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public class CSharpCompletionEngine : CSharpCompletionEngineBase
	{
		internal ICompletionDataFactory factory;
		
		#region Additional input properties
		public CSharpFormattingOptions FormattingPolicy { get; set; }
		
		public string EolMarker { get; set; }
		
		public string IndentString { get; set; }
		#endregion
		
		#region Result properties
		public bool AutoCompleteEmptyMatch;
		public bool AutoSelect;
		public string DefaultCompletionString;
		public bool CloseOnSquareBrackets;
#endregion
		
		public CSharpCompletionEngine(IDocument document, ICompletionContextProvider completionContextProvider, ICompletionDataFactory factory, IProjectContent content, CSharpTypeResolveContext ctx) : base (content, completionContextProvider, ctx)
		{
			if (document == null) {
				throw new ArgumentNullException("document");
			}
			if (factory == null) {
				throw new ArgumentNullException("factory");
			}
			this.document = document;
			this.factory = factory;
			// Set defaults for additional input properties
			this.FormattingPolicy = FormattingOptionsFactory.CreateMono();
			this.EolMarker = Environment.NewLine;
			this.IndentString = "\t";
		}
		
		public bool TryGetCompletionWord(int offset, out int startPos, out int wordLength)
		{
			startPos = wordLength = 0;
			int pos = offset - 1;
			while (pos >= 0) {
				char c = document.GetCharAt(pos);
				if (!char.IsLetterOrDigit(c) && c != '_')
					break;
				pos--;
			}
			if (pos == -1)
				return false;
			
			pos++;
			startPos = pos;
			
			while (pos < document.TextLength) {
				char c = document.GetCharAt(pos);
				if (!char.IsLetterOrDigit(c) && c != '_')
					break;
				pos++;
			}
			wordLength = pos - startPos;
			return true;
		}
		
		public IEnumerable<ICompletionData> GetCompletionData(int offset, bool controlSpace)
		{
			this.AutoCompleteEmptyMatch = true;
			this.AutoSelect = true;
			this.DefaultCompletionString = null;
			SetOffset(offset);
			if (offset > 0) {
				char lastChar = document.GetCharAt(offset - 1);
				var result = MagicKeyCompletion(lastChar, controlSpace) ?? Enumerable.Empty<ICompletionData>();
				if (controlSpace && char.IsWhiteSpace(lastChar)) {
					offset -= 2;
					while (offset >= 0 && char.IsWhiteSpace (document.GetCharAt (offset))) {
						offset--;
					}
					if (offset > 0) {
						var nonWsResult = MagicKeyCompletion(
							document.GetCharAt(offset),
							controlSpace
						);
						if (nonWsResult != null) {
							var text = new HashSet<string>(result.Select(r => r.CompletionText));
							result = result.Concat(nonWsResult.Where(r => !text.Contains(r.CompletionText)));
						}
					}
				}
				
				return result;
			}
			return Enumerable.Empty<ICompletionData>();
		}
		
		IEnumerable<string> GenerateNameProposals(AstType type)
		{
			if (type is PrimitiveType) {
				var pt = (PrimitiveType)type;
				switch (pt.Keyword) {
					case "object":
						yield return "o";
						yield return "obj";
						break;
					case "bool":
						yield return "b";
						yield return "pred";
						break;
					case "double":
					case "float":
					case "decimal":
						yield return "d";
						yield return "f";
						yield return "m";
						break;
					default:
						yield return "i";
						yield return "j";
						yield return "k";
						break;
				}
				yield break;
			}
			string name;
			if (type is SimpleType) {
				name = ((SimpleType)type).Identifier;
			} else if (type is MemberType) {
				name = ((MemberType)type).MemberName;
			} else {
				yield break;
			}
			
			var names = WordParser.BreakWords(name);
			
			var possibleName = new StringBuilder();
			for (int i = 0; i < names.Count; i++) {
				possibleName.Length = 0;
				for (int j = i; j < names.Count; j++) {
					if (string.IsNullOrEmpty(names [j])) {
						continue;
					}
					if (j == i) { 
						names [j] = Char.ToLower(names [j] [0]) + names [j].Substring(1);
					}
					possibleName.Append(names [j]);
				}
				yield return possibleName.ToString();
			}
		}
		
		IEnumerable<ICompletionData> HandleMemberReferenceCompletion(ExpressionResult expr)
		{
			if (expr == null) 
				return null;
			
			// do not complete <number>. (but <number>.<number>.)
			if (expr.Node is PrimitiveExpression) {
				var pexpr = (PrimitiveExpression)expr.Node;
				if (!(pexpr.Value is string || pexpr.Value is char) && !pexpr.LiteralValue.Contains('.')) {
					return null;
				}
			}
			var resolveResult = ResolveExpression(expr);
			if (resolveResult == null) {
				return null;
			}
			if (expr.Node is AstType) {
				// need to look at paren.parent because of "catch (<Type>.A" expression
				if (expr.Node.Parent != null && expr.Node.Parent.Parent is CatchClause)
					return HandleCatchClauseType(expr);
				return CreateTypeAndNamespaceCompletionData(
					location,
					resolveResult.Item1,
					expr.Node,
					resolveResult.Item2
				);
			}
			
			
			return CreateCompletionData(
				location,
				resolveResult.Item1,
				expr.Node,
				resolveResult.Item2
			);
		}
		
		bool IsInPreprocessorDirective()
		{
			var text = GetMemberTextToCaret().Item1;
			var miniLexer = new MiniLexer(text);
			miniLexer.Parse();
			return miniLexer.IsInPreprocessorDirective;
		}
		
		IEnumerable<ICompletionData> HandleObjectInitializer(SyntaxTree unit, AstNode n)
		{
			var p = n.Parent;
			while (p != null && !(p is ObjectCreateExpression)) {
				p = p.Parent;
			}
			var parent = (ArrayInitializerExpression)n.Parent;
			if (parent.IsSingleElement)
				parent = (ArrayInitializerExpression)parent.Parent;
			if (p != null) {
				var contextList = new CompletionDataWrapper(this);
				var initializerResult = ResolveExpression(p);
				if (initializerResult != null && initializerResult.Item1.Type.Kind != TypeKind.Unknown) {
					// check 3 cases:
					// 1) New initalizer { xpr
					// 2) Object initializer { prop = val1, field = val2, xpr
					// 3) Array initializer { new Foo (), a, xpr
					// in case 1 all object/array initializer options should be given - in the others not.
					
					AstNode prev = null;
					if (parent.Elements.Count > 1) {
						prev = parent.Elements.First();
						if (prev is ArrayInitializerExpression && ((ArrayInitializerExpression)prev).IsSingleElement) 
							prev = ((ArrayInitializerExpression)prev).Elements.FirstOrDefault();
					}
					
					if (prev != null && !(prev is NamedExpression)) {
						AddContextCompletion(contextList, GetState(), n);
						// case 3)
						return contextList.Result;
					}
					
					foreach (var m in initializerResult.Item1.Type.GetMembers (m => m.IsPublic && (m.EntityType == EntityType.Property || m.EntityType == EntityType.Field))) {
						contextList.AddMember(m);
					}
					
					if (prev != null && (prev is NamedExpression)) {
						// case 2)
						return contextList.Result;
					}
					
					// case 1)
					
					// check if the object is a list, if not only provide object initalizers
					var list = typeof(System.Collections.IList).ToTypeReference().Resolve(Compilation);
					if (initializerResult.Item1.Type.Kind != TypeKind.Array && list != null) {
						var def = initializerResult.Item1.Type.GetDefinition(); 
						if (def != null && !def.IsDerivedFrom(list.GetDefinition()))
							return contextList.Result;
					}
					
					AddContextCompletion(contextList, GetState(), n);
					return contextList.Result;
				}
			}
			return null;
		}
		
		IEnumerable<ICompletionData> MagicKeyCompletion(char completionChar, bool controlSpace)
		{
			Tuple<ResolveResult, CSharpResolver> resolveResult;
			switch (completionChar) {
			// Magic key completion
				case ':':
				case '.':
					if (IsInsideCommentStringOrDirective()) {
						return Enumerable.Empty<ICompletionData>();
					}
					return HandleMemberReferenceCompletion(GetExpressionBeforeCursor());
				case '#':
					if (!IsInPreprocessorDirective())
						return null;
					return GetDirectiveCompletionData();
			// XML doc completion
				case '<':
					if (IsInsideDocComment()) {
						return GetXmlDocumentationCompletionData();
					}
					if (controlSpace) {
						return DefaultControlSpaceItems();
					}
					return null;
				case '>':
					if (!IsInsideDocComment()) {
						if (offset > 2 && document.GetCharAt(offset - 2) == '-' && !IsInsideCommentStringOrDirective()) {
							return HandleMemberReferenceCompletion(GetExpressionBeforeCursor());
						}
						return null;
					}
					string lineText = document.GetText(document.GetLineByNumber(location.Line));
					int startIndex = Math.Min(location.Column - 1, lineText.Length - 1);
					while (startIndex >= 0 && lineText [startIndex] != '<') {
						--startIndex;
						if (lineText [startIndex] == '/') {
							// already closed.
							startIndex = -1;
							break;
						}
					}
				
					if (startIndex >= 0) {
						int endIndex = startIndex;
						while (endIndex <= location.Column && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText [endIndex])) {
							endIndex++;
						}
						string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring(
						startIndex + 1,
						endIndex - startIndex - 2
						) : null;
						if (!string.IsNullOrEmpty(tag) && commentTags.IndexOf(tag) >= 0) {
							document.Insert(offset, "</" + tag + ">", AnchorMovementType.BeforeInsertion);
						}
					}
					return null;
				
			// Parameter completion
				case '(':
					if (IsInsideCommentStringOrDirective()) {
						return null;
					}
					var invoke = GetInvocationBeforeCursor(true);
					if (invoke == null) {
						if (controlSpace)
							return DefaultControlSpaceItems(invoke);
						return null;
					}
					if (invoke.Node is TypeOfExpression) {
						return CreateTypeList();
					}
					var invocationResult = ResolveExpression(invoke);
					if (invocationResult == null) {
						return null;
					}
					var methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
					if (methodGroup != null) {
						return CreateParameterCompletion(
						methodGroup,
						invocationResult.Item2,
						invoke.Node,
						invoke.Unit,
						0,
						controlSpace
						);
					}
				
					if (controlSpace) {
						return DefaultControlSpaceItems(invoke);
					}
					return null;
				case '=':
					return controlSpace ? DefaultControlSpaceItems() : null;
				case ',':
					int cpos2;
					if (!GetParameterCompletionCommandOffset(out cpos2)) { 
						return null;
					}
				//	completionContext = CompletionWidget.CreateCodeCompletionContext (cpos2);
				//	int currentParameter2 = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, completionContext) - 1;
				//				return CreateParameterCompletion (CreateResolver (), location, ExpressionContext.MethodBody, provider.Methods, currentParameter);	
					break;
				
			// Completion on space:
				case ' ':
					int tokenIndex = offset;
					string token = GetPreviousToken(ref tokenIndex, false);
					if (IsInsideCommentStringOrDirective()) {
						if (IsInPreprocessorDirective())
							return HandleKeywordCompletion(tokenIndex, token);
						return null;
					}
				// check propose name, for context <variable name> <ctrl+space> (but only in control space context)
				//IType isAsType = null;
					var isAsExpression = GetExpressionAt(offset);
					if (controlSpace && isAsExpression != null && isAsExpression.Node is VariableDeclarationStatement && token != "new") {
						var parent = isAsExpression.Node as VariableDeclarationStatement;
						var proposeNameList = new CompletionDataWrapper(this);
						if (parent.Variables.Count != 1)
							return DefaultControlSpaceItems(isAsExpression, controlSpace);
					
						foreach (var possibleName in GenerateNameProposals (parent.Type)) {
							if (possibleName.Length > 0) {
								proposeNameList.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
							}
						}
					
						AutoSelect = false;
						AutoCompleteEmptyMatch = false;
						return proposeNameList.Result;
					}
				//				int tokenIndex = offset;
				//				string token = GetPreviousToken (ref tokenIndex, false);
				//				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
				//					resolver = CreateResolver ();
				//					ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (document, resolver.Unit, Document.FileName, resolver.CallingType);
				//					IReturnType objectInitializer = ((ExpressionContext.TypeExpressionContext)exactContext).UnresolvedType;
				//					if (objectInitializer != null && objectInitializer.ArrayDimensions == 0 && objectInitializer.PointerNestingLevel == 0 && (token == "{" || token == ","))
				//						return CreateCtrlSpaceCompletionData (completionContext, result); 
				//				}
					if (token == "=") {
						int j = tokenIndex;
						string prevToken = GetPreviousToken(ref j, false);
						if (prevToken == "=" || prevToken == "+" || prevToken == "-") {
							token = prevToken + token;
							tokenIndex = j;
						}
					}
					switch (token) {
						case "(":
						case ",":
							int cpos;
							if (!GetParameterCompletionCommandOffset(out cpos)) { 
								break;
							}
							int currentParameter = GetCurrentParameterIndex(cpos - 1, this.offset) - 1;
							if (currentParameter < 0) {
								return null;
							}
							invoke = GetInvocationBeforeCursor(token == "(");
							if (invoke == null) {
								return null;
							}
							invocationResult = ResolveExpression(invoke);
							if (invocationResult == null) {
								return null;
							}
							methodGroup = invocationResult.Item1 as MethodGroupResolveResult;
							if (methodGroup != null) {
								return CreateParameterCompletion(
							methodGroup,
							invocationResult.Item2,
							invoke.Node,
							invoke.Unit,
							currentParameter,
							controlSpace);
							}
							return null;
						case "=":
						case "==":
							GetPreviousToken(ref tokenIndex, false);
							var expressionOrVariableDeclaration = GetExpressionAt(tokenIndex);
							if (expressionOrVariableDeclaration == null) {
								return null;
							}
					
							resolveResult = ResolveExpression(expressionOrVariableDeclaration);
					
							if (resolveResult == null) {
								return null;
							}
							if (resolveResult.Item1.Type.Kind == TypeKind.Enum) {
								var wrapper = new CompletionDataWrapper(this);
								AddContextCompletion(
							wrapper,
							resolveResult.Item2,
							expressionOrVariableDeclaration.Node);
								AddEnumMembers(wrapper, resolveResult.Item1.Type, resolveResult.Item2);
								AutoCompleteEmptyMatch = false;
								return wrapper.Result;
							}
					//				
					//					if (resolvedType.FullName == DomReturnType.Bool.FullName) {
					//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
					//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
					//						completionList.AutoCompleteEmptyMatch = false;
					//						cdc.Add ("true", "md-keyword");
					//						cdc.Add ("false", "md-keyword");
					//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
					//						return completionList;
					//					}
					//					if (resolvedType.ClassType == ClassType.Delegate && token == "=") {
					//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
					//						string parameterDefinition = AddDelegateHandlers (completionList, resolvedType);
					//						string varName = GetPreviousMemberReferenceExpression (tokenIndex);
					//						completionList.Add (new EventCreationCompletionData (document, varName, resolvedType, null, parameterDefinition, resolver.CallingMember, resolvedType));
					//						
					//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
					//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
					//						foreach (var data in completionList) {
					//							if (data is MemberCompletionData) 
					//								((MemberCompletionData)data).IsDelegateExpected = true;
					//						}
					//						return completionList;
					//					}
							return null;
						case "+=":
						case "-=":
							GetPreviousToken(ref tokenIndex, false);
					
							expressionOrVariableDeclaration = GetExpressionAt(tokenIndex);
							if (expressionOrVariableDeclaration == null) {
								return null;
							}
					
							resolveResult = ResolveExpression(expressionOrVariableDeclaration);
							if (resolveResult == null) {
								return null;
							}
					
					
							var mrr = resolveResult.Item1 as MemberResolveResult;
							if (mrr != null) {
								var evt = mrr.Member as IEvent;
								if (evt == null) {
									return null;
								}
								var delegateType = evt.ReturnType;
								if (delegateType.Kind != TypeKind.Delegate) {
									return null;
								}
						
								var wrapper = new CompletionDataWrapper(this);
								if (currentType != null) {
									//							bool includeProtected = DomType.IncludeProtected (dom, typeFromDatabase, resolver.CallingType);
									foreach (var method in ctx.CurrentTypeDefinition.Methods) {
										if (MatchDelegate(delegateType, method) /*&& method.IsAccessibleFrom (dom, resolver.CallingType, resolver.CallingMember, includeProtected) &&*/) {
											wrapper.AddMember(method);
											//									data.SetText (data.CompletionText + ";");
										}
									}
								}
								if (token == "+=") {
									string parameterDefinition = AddDelegateHandlers(
								wrapper,
								delegateType
									);
									string varName = GetPreviousMemberReferenceExpression(tokenIndex);
									wrapper.Result.Add(
								factory.CreateEventCreationCompletionData(
								varName,
								delegateType,
								evt,
								parameterDefinition,
								currentMember,
								currentType)
									);
								}
						
								return wrapper.Result;
							}
							return null;
						case ":":
							if (currentMember == null) {
								token = GetPreviousToken(ref tokenIndex, false);
								token = GetPreviousToken(ref tokenIndex, false);
								if (token == "enum")
									return HandleEnumContext();
								var wrapper = new CompletionDataWrapper(this);
						
								AddTypesAndNamespaces(
							wrapper,
							GetState(),
							null,
							t => currentType != null && !currentType.ReflectionName.Equals(t.ReflectionName) ? t : null
								);
								return wrapper.Result;
							}
							return null;
					}
				
					var keywordCompletion = HandleKeywordCompletion(tokenIndex, token);
					if (keywordCompletion == null && controlSpace) {
						goto default;
					}
					return keywordCompletion;
			// Automatic completion
				default:
					if (IsInsideCommentStringOrDirective()) {
						return null;
					}
					if (IsInLinqContext(offset)) {
						if (!controlSpace && !(char.IsLetter(completionChar) || completionChar == '_')) {
							return null;
						}
						tokenIndex = offset;
						token = GetPreviousToken(ref tokenIndex, false);
						// token last typed
						if (!char.IsWhiteSpace(completionChar) && !linqKeywords.Contains(token)) {
							token = GetPreviousToken(ref tokenIndex, false);
						}
						// token last typed
					
						if (linqKeywords.Contains(token)) {
							if (token == "from") {
								// after from no auto code completion.
								return null;
							}
							return DefaultControlSpaceItems();
						}
						var dataList = new CompletionDataWrapper(this);
						AddKeywords(dataList, linqKeywords);
						return dataList.Result;
					}
					if (currentType != null && currentType.Kind == TypeKind.Enum) {
						return HandleEnumContext();
					}
					var contextList = new CompletionDataWrapper(this);
					var identifierStart = GetExpressionAtCursor();
					if (identifierStart != null) {
						if (identifierStart.Node is TypeParameterDeclaration) {
							return null;
						}
					
						if (identifierStart.Node is MemberReferenceExpression) {
							return HandleMemberReferenceCompletion(
							new ExpressionResult(
							((MemberReferenceExpression)identifierStart.Node).Target,
							identifierStart.Unit
							)
							);
						}
					
						if (identifierStart.Node is Identifier) {
							// May happen in variable names
							return controlSpace ? DefaultControlSpaceItems(identifierStart) : null;
						}
						if (identifierStart.Node is VariableInitializer && location <= ((VariableInitializer)identifierStart.Node).NameToken.EndLocation) {
							return controlSpace ? HandleAccessorContext() ?? DefaultControlSpaceItems(identifierStart) : null;
						}
					
						if (identifierStart.Node is CatchClause) {
							if (((CatchClause)identifierStart.Node).VariableNameToken.Contains(location)) {
								return null;
							}
							return HandleCatchClauseType(identifierStart);
						}
					}
					if (!(char.IsLetter(completionChar) || completionChar == '_') && (!controlSpace || identifierStart == null || !(identifierStart.Node.Parent is ArrayInitializerExpression))) {
						return controlSpace ? HandleAccessorContext() ?? DefaultControlSpaceItems(identifierStart) : null;
					}
				
					char prevCh = offset > 2 ? document.GetCharAt(offset - 2) : ';';
					char nextCh = offset < document.TextLength ? document.GetCharAt(offset) : ' ';
					const string allowedChars = ";,.[](){}+-*/%^?:&|~!<>=";
					if (!Char.IsWhiteSpace(nextCh) && allowedChars.IndexOf(nextCh) < 0) {
						return null;
					}
					if (!(Char.IsWhiteSpace(prevCh) || allowedChars.IndexOf(prevCh) >= 0)) {
						return null;
					}
				
				// Do not pop up completion on identifier identifier (should be handled by keyword completion).
					tokenIndex = offset - 1;
					token = GetPreviousToken(ref tokenIndex, false);
					if (token == "class" || token == "interface" || token == "struct" || token == "enum" || token == "namespace") {
						// after these always follows a name
						return null;
					}
					var keywordresult = HandleKeywordCompletion(tokenIndex, token);
					if (keywordresult != null) {
						return keywordresult;
					}
				
					int prevTokenIndex = tokenIndex;
					var prevToken2 = GetPreviousToken(ref prevTokenIndex, false);
					if (prevToken2 == "delegate") {
						// after these always follows a name
						return null;
					}
				
					if (identifierStart == null && !string.IsNullOrEmpty(token) && !IsInsideCommentStringOrDirective() && (prevToken2 == ";" || prevToken2 == "{" || prevToken2 == "}")) {
						char last = token [token.Length - 1];
						if (char.IsLetterOrDigit(last) || last == '_' || token == ">") {
							return HandleKeywordCompletion(tokenIndex, token);
						}
					}
				
					if (identifierStart == null) {
						var accCtx = HandleAccessorContext();
						if (accCtx != null) {
							return accCtx;
						}
						return DefaultControlSpaceItems(null, controlSpace);
					}
					CSharpResolver csResolver;
					AstNode n = identifierStart.Node;
					if (n != null && n.Parent is AnonymousTypeCreateExpression) {
						AutoSelect = false;
					}
				
				// Handle foreach (type name _
					if (n is IdentifierExpression) {
						var prev = n.GetPrevNode() as ForeachStatement;
						if (prev != null && prev.InExpression.IsNull) {
							if (controlSpace) {
								contextList.AddCustom("in");
								return contextList.Result;
							}
							return null;
						}
					
						//						var astResolver = new CSharpAstResolver(
						//							GetState(),
						//							identifierStart.Unit,
						//							CSharpUnresolvedFile
						//						);
						//
						//						foreach (var type in CreateFieldAction.GetValidTypes(astResolver, (Expression)n)) {
						//							if (type.Kind == TypeKind.Delegate) {
						//								AddDelegateHandlers(contextList, type, false, false);
						//								AutoSelect = false;
						//								AutoCompleteEmptyMatch = false;
						//							}
						//						}
					}
				
				// Handle object/enumerable initialzer expressions: "new O () { P$"
					if (n is IdentifierExpression && n.Parent is ArrayInitializerExpression) {
						var result = HandleObjectInitializer(identifierStart.Unit, n);
						if (result != null)
							return result;
					}
				
					if (n != null && n.Parent is InvocationExpression) {
						var invokeParent = (InvocationExpression)n.Parent;
						var invokeResult = ResolveExpression(
						invokeParent.Target
						);
						var mgr = invokeResult != null ? invokeResult.Item1 as MethodGroupResolveResult : null;
						if (mgr != null) {
							int idx = 0;
							foreach (var arg in invokeParent.Arguments) {
								if (arg == n) {
									break;
								}
								idx++;
							}
						
							foreach (var method in mgr.Methods) {
								if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
									AutoSelect = false;
									AutoCompleteEmptyMatch = false;
								}
								foreach (var p in method.Parameters) {
									contextList.AddNamedParameterVariable(p);
								}
							}
							idx++;
							foreach (var list in mgr.GetEligibleExtensionMethods (true)) {
								foreach (var method in list) {
									if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
										AutoSelect = false;
										AutoCompleteEmptyMatch = false;
									}
								}
							}
						}
					}
				
					if (n != null && n.Parent is ObjectCreateExpression) {
						var invokeResult = ResolveExpression(n.Parent);
						var mgr = invokeResult != null ? invokeResult.Item1 as ResolveResult : null;
						if (mgr != null) {
							foreach (var constructor in mgr.Type.GetConstructors ()) {
								foreach (var p in constructor.Parameters) {
									contextList.AddVariable(p);
								}
							}
						}
					}
				
					if (n is IdentifierExpression) {
						var bop = n.Parent as BinaryOperatorExpression;
						Expression evaluationExpr = null;
					
						if (bop != null && bop.Right == n && (bop.Operator == BinaryOperatorType.Equality || bop.Operator == BinaryOperatorType.InEquality)) {
							evaluationExpr = bop.Left;
						}
						// check for compare to enum case 
						if (evaluationExpr != null) {
							resolveResult = ResolveExpression(evaluationExpr);
							if (resolveResult != null && resolveResult.Item1.Type.Kind == TypeKind.Enum) {
								var wrapper = new CompletionDataWrapper(this);
								AddContextCompletion(
								wrapper,
								resolveResult.Item2,
								evaluationExpr
								);
								AddEnumMembers(wrapper, resolveResult.Item1.Type, resolveResult.Item2);
								AutoCompleteEmptyMatch = false;
								return wrapper.Result;
							}
						}
					}
				
					if (n is Identifier && n.Parent is ForeachStatement) {
						if (controlSpace) {
							return DefaultControlSpaceItems();
						}
						return null;
					}
				
					if (n is ArrayInitializerExpression) {
						// check for new [] {...} expression -> no need to resolve the type there
						var parent = n.Parent as ArrayCreateExpression;
						if (parent != null && parent.Type.IsNull) {
							return DefaultControlSpaceItems();
						}
					
						var initalizerResult = ResolveExpression(n.Parent);
					
						var concreteNode = identifierStart.Unit.GetNodeAt<IdentifierExpression>(location);
						// check if we're on the right side of an initializer expression
						if (concreteNode != null && concreteNode.Parent != null && concreteNode.Parent.Parent != null && concreteNode.Identifier != "a" && concreteNode.Parent.Parent is NamedExpression) {
							return DefaultControlSpaceItems();
						}
						if (initalizerResult != null && initalizerResult.Item1.Type.Kind != TypeKind.Unknown) { 
						
							foreach (var property in initalizerResult.Item1.Type.GetProperties ()) {
								if (!property.IsPublic) {
									continue;
								}
								contextList.AddMember(property);
							}
							foreach (var field in initalizerResult.Item1.Type.GetFields ()) {       
								if (!field.IsPublic) {
									continue;
								}
								contextList.AddMember(field);
							}
							return contextList.Result;
						}
						return DefaultControlSpaceItems();
					}
					if (IsAttributeContext(n)) {
						// add attribute targets
						if (currentType == null) {
							contextList.AddCustom("assembly");
							contextList.AddCustom("module");
							contextList.AddCustom("type");
						} else {
							contextList.AddCustom("param");
							contextList.AddCustom("field");
							contextList.AddCustom("property");
							contextList.AddCustom("method");
							contextList.AddCustom("event");
						}
						contextList.AddCustom("return");
					}
					if (n is MemberType) {
						resolveResult = ResolveExpression(
						((MemberType)n).Target
						);
						return CreateTypeAndNamespaceCompletionData(
						location,
						resolveResult.Item1,
						((MemberType)n).Target,
						resolveResult.Item2
						);
					}
					if (n != null/* && !(identifierStart.Item2 is TypeDeclaration)*/) {
						csResolver = new CSharpResolver(ctx);
						var nodes = new List<AstNode>();
						nodes.Add(n);
						if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute) {
							nodes.Add(n.Parent);
						}
						var astResolver = CompletionContextProvider.GetResolver(csResolver, identifierStart.Unit);
						astResolver.ApplyNavigator(new NodeListResolveVisitorNavigator(nodes));
						try {
							csResolver = astResolver.GetResolverStateBefore(n);
						} catch (Exception) {
							csResolver = GetState();
						}
						// add attribute properties.
						if (n.Parent is ICSharpCode.NRefactory.CSharp.Attribute) {
							var resolved = astResolver.Resolve(n.Parent);
							if (resolved != null && resolved.Type != null) {
								foreach (var property in resolved.Type.GetProperties (p => p.Accessibility == Accessibility.Public)) {
									contextList.AddMember(property);
								}
								foreach (var field in resolved.Type.GetFields (p => p.Accessibility == Accessibility.Public)) {
									contextList.AddMember(field);
								}
							}
						}
					} else {
						csResolver = GetState();
					}
				// identifier has already started with the first letter
					offset--;
					AddContextCompletion(
					contextList,
					csResolver,
					identifierStart.Node
					);
					return contextList.Result;
			//				if (stub.Parent is BlockStatement)
				
			//				result = FindExpression (dom, completionContext, -1);
			//				if (result == null)
			//					return null;
			//				 else if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
			//					triggerWordLength = 1;
			//					bool autoSelect = true;
			//					IType returnType = null;
			//					if ((prevCh == ',' || prevCh == '(') && GetParameterCompletionCommandOffset (out cpos)) {
			//						ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
			//						NRefactoryParameterDataProvider dataProvider = ParameterCompletionCommand (ctx) as NRefactoryParameterDataProvider;
			//						if (dataProvider != null) {
			//							int i = dataProvider.GetCurrentParameterIndex (CompletionWidget, ctx) - 1;
			//							foreach (var method in dataProvider.Methods) {
			//								if (i < method.Parameters.Count) {
			//									returnType = dom.GetType (method.Parameters [i].ReturnType);
			//									autoSelect = returnType == null || returnType.ClassType != ClassType.Delegate;
			//									break;
			//								}
			//							}
			//						}
			//					}
			//					// Bug 677531 - Auto-complete doesn't always highlight generic parameter in method signature
			//					//if (result.ExpressionContext == ExpressionContext.TypeName)
			//					//	autoSelect = false;
			//					CompletionDataList dataList = CreateCtrlSpaceCompletionData (completionContext, result);
			//					AddEnumMembers (dataList, returnType);
			//					dataList.AutoSelect = autoSelect;
			//					return dataList;
			//				} else {
			//					result = FindExpression (dom, completionContext, 0);
			//					tokenIndex = offset;
			//					
			//					// check foreach case, unfortunately the expression finder is too dumb to handle full type names
			//					// should be overworked if the expression finder is replaced with a mcs ast based analyzer.
			//					var possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // starting letter
			//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varname
			//				
			//					// read return types to '(' token
			//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varType
			//					if (possibleForeachToken == ">") {
			//						while (possibleForeachToken != null && possibleForeachToken != "(") {
			//							possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
			//						}
			//					} else {
			//						possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // (
			//						if (possibleForeachToken == ".")
			//							while (possibleForeachToken != null && possibleForeachToken != "(")
			//								possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
			//					}
			//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // foreach
			//				
			//					if (possibleForeachToken == "foreach") {
			//						result.ExpressionContext = ExpressionContext.ForeachInToken;
			//					} else {
			//						return null;
			//						//								result.ExpressionContext = ExpressionContext.IdentifierExpected;
			//					}
			//					result.Expression = "";
			//					result.Region = DomRegion.Empty;
			//				
			//					return CreateCtrlSpaceCompletionData (completionContext, result);
			//				}
			//				break;
			}
			return null;
		}
		
		IEnumerable<ICompletionData> HandleCatchClauseType(ExpressionResult identifierStart)
		{
			Func<IType, IType> typePred = delegate (IType type) {
				if (type.GetAllBaseTypes().Any(t => t.ReflectionName == "System.Exception"))
					return type;
				return null;
			};
			if (identifierStart.Node is CatchClause) {
				var wrapper = new CompletionDataWrapper(this);
				AddTypesAndNamespaces(
					wrapper,
					GetState(),
					identifierStart.Node,
					typePred,
					m => false
				);
				return wrapper.Result;
			}
			
			var resolveResult = ResolveExpression(identifierStart);
			return CreateCompletionData(
				location,
				resolveResult.Item1,
				identifierStart.Node,
				resolveResult.Item2,
				typePred
			);
		}
		
		string[] validEnumBaseTypes = {
			"byte",
			"sbyte",
			"short",
			"int",
			"long",
			"ushort",
			"uint",
			"ulong"
		};
		
		IEnumerable<ICompletionData> HandleEnumContext()
		{
			var syntaxTree = ParseStub("a", false);
			if (syntaxTree == null) {
				return null;
			}
			
			var curType = syntaxTree.GetNodeAt<TypeDeclaration>(location);
			if (curType == null || curType.ClassType != ClassType.Enum) {
				syntaxTree = ParseStub("a {}", false);
				var node = syntaxTree.GetNodeAt<AstType>(location);
				if (node != null) {
					var wrapper = new CompletionDataWrapper(this);
					AddKeywords(wrapper, validEnumBaseTypes);
					return wrapper.Result;
				}
			}
			
			var member = syntaxTree.GetNodeAt<EnumMemberDeclaration>(location);
			if (member != null && member.NameToken.EndLocation < location) {
				return DefaultControlSpaceItems();
			}
			return null;
		}
		
		bool IsInLinqContext(int offset)
		{
			string token;
			while (null != (token = GetPreviousToken (ref offset, true)) && !IsInsideCommentStringOrDirective ()) {
				
				if (token == "from") {
					return !IsInsideCommentStringOrDirective(offset);
				}
				if (token == ";" || token == "{") {
					return false;
				}
			}
			return false;
		}
		
		IEnumerable<ICompletionData> HandleAccessorContext()
		{
			var unit = ParseStub("get; }", false);
			var node = unit.GetNodeAt(location, cn => !(cn is CSharpTokenNode));
			if (node is Accessor) {
				node = node.Parent;
			}
			var contextList = new CompletionDataWrapper(this);
			if (node is PropertyDeclaration) {
				contextList.AddCustom("get");
				contextList.AddCustom("set");
				AddKeywords(contextList, accessorModifierKeywords);
			} else if (node is CustomEventDeclaration) {
				contextList.AddCustom("add");
				contextList.AddCustom("remove");
			} else {
				return null;
			}
			
			return contextList.Result;
		}
		
		IEnumerable<ICompletionData> DefaultControlSpaceItems(ExpressionResult xp = null, bool controlSpace = true)
		{
			var wrapper = new CompletionDataWrapper(this);
			if (offset >= document.TextLength) {
				offset = document.TextLength - 1;
			}
			while (offset > 1 && char.IsWhiteSpace (document.GetCharAt (offset))) {
				offset--;
			}
			location = document.GetLocation(offset);
			
			if (xp == null) {
				xp = GetExpressionAtCursor();
			}
			AstNode node;
			SyntaxTree unit;
			Tuple<ResolveResult, CSharpResolver> rr;
			if (xp != null) {
				node = xp.Node;
				rr = ResolveExpression(node);
				unit = xp.Unit;
			} else {
				unit = ParseStub("foo", false);
				node = unit.GetNodeAt(
					location.Line,
					location.Column + 2,
					n => n is Expression || n is AstType
				);
				rr = ResolveExpression(node);
			}
			if (node is Identifier && node.Parent is ForeachStatement) {
				var foreachStmt = (ForeachStatement)node.Parent;
				foreach (var possibleName in GenerateNameProposals (foreachStmt.VariableType)) {
					if (possibleName.Length > 0) {
						wrapper.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
					}
				}
				
				AutoSelect = false;
				AutoCompleteEmptyMatch = false;
				return wrapper.Result;
			}
			
			if (node is Identifier && node.Parent is ParameterDeclaration) {
				if (!controlSpace) {
					return null;
				}
				// Try Parameter name case 
				var param = node.Parent as ParameterDeclaration;
				if (param != null) {
					foreach (var possibleName in GenerateNameProposals (param.Type)) {
						if (possibleName.Length > 0) {
							wrapper.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
						}
					}
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
					return wrapper.Result;
				}
			}
			/*			if (Unit != null && (node == null || node is TypeDeclaration)) {
				var constructor = Unit.GetNodeAt<ConstructorDeclaration>(
					location.Line,
					location.Column - 3
				);
				if (constructor != null && !constructor.ColonToken.IsNull && constructor.Initializer.IsNull) {
					wrapper.AddCustom("this");
					wrapper.AddCustom("base");
					return wrapper.Result;
				}
			}*/
			
			var initializer = node != null ? node.Parent as ArrayInitializerExpression : null;
			if (initializer != null) {
				var result = HandleObjectInitializer(unit, initializer);
				if (result != null)
					return result;
			}
			CSharpResolver csResolver = null;
			if (rr != null) {
				csResolver = rr.Item2;
			}
			if (csResolver == null) {
				if (node != null) {
					csResolver = GetState();
					//var astResolver = new CSharpAstResolver (csResolver, node, xp != null ? xp.Item1 : CSharpUnresolvedFile);
					
					try {
						//csResolver = astResolver.GetResolverStateBefore (node);
						Console.WriteLine(csResolver.LocalVariables.Count());
					} catch (Exception  e) {
						Console.WriteLine("E!!!" + e);
					}
					
				} else {
					csResolver = GetState();
				}
			}
			AddContextCompletion(wrapper, csResolver, node);
			
			return wrapper.Result;
		}
		
		void AddContextCompletion(CompletionDataWrapper wrapper, CSharpResolver state, AstNode node)
		{
			if (state != null && !(node is AstType)) {
				foreach (var variable in state.LocalVariables) {
					if (variable.Region.IsInside(location.Line, location.Column - 1)) {
						continue;
					}
					wrapper.AddVariable(variable);
				}
			}

			if (state.CurrentMember is IParameterizedMember && !(node is AstType)) {
				var param = (IParameterizedMember)state.CurrentMember;
				foreach (var p in param.Parameters) {
					wrapper.AddVariable(p);
				}
			}
			
			if (state.CurrentMember is IMethod) {
				var method = (IMethod)state.CurrentMember;
				foreach (var p in method.TypeParameters) {
					wrapper.AddTypeParameter(p);
				}
			}
			
			Func<IType, IType> typePred = null;
			if (IsAttributeContext(node)) {
				var attribute = Compilation.FindType(KnownTypeCode.Attribute);
				typePred = t => {
					return t.GetAllBaseTypeDefinitions().Any(bt => bt.Equals(attribute)) ? t : null;
				};
			}
			AddTypesAndNamespaces(wrapper, state, node, typePred);
			
			wrapper.Result.Add(factory.CreateLiteralCompletionData("global"));
			
			if (!(node is AstType)) {
				if (currentMember != null || node is Expression) {
					AddKeywords(wrapper, statementStartKeywords);
					AddKeywords(wrapper, expressionLevelKeywords);
					if (node == null || node is TypeDeclaration)
						AddKeywords(wrapper, typeLevelKeywords);
				} else if (currentType != null) {
					AddKeywords(wrapper, typeLevelKeywords);
				} else {
					AddKeywords(wrapper, globalLevelKeywords);
				}
				var prop = currentMember as IUnresolvedProperty;
				if (prop != null && prop.Setter != null && prop.Setter.Region.IsInside(location)) {
					wrapper.AddCustom("value");
				} 
				if (currentMember is IUnresolvedEvent) {
					wrapper.AddCustom("value");
				} 
				
				if (IsInSwitchContext(node)) {
					wrapper.AddCustom("case"); 
				}
			} else {
				if (((AstType)node).Parent is ParameterDeclaration) {
					AddKeywords(wrapper, parameterTypePredecessorKeywords);
				}
			}
			AddKeywords(wrapper, primitiveTypesKeywords);
			if (currentMember != null && (node is IdentifierExpression || node is SimpleType) && (node.Parent is ExpressionStatement || node.Parent is ForeachStatement || node.Parent is UsingStatement)) {
				wrapper.AddCustom("var");
				wrapper.AddCustom("dynamic");
			} 
			wrapper.Result.AddRange(factory.CreateCodeTemplateCompletionData());
			if (node != null && node.Role == Roles.Argument) {
				var resolved = ResolveExpression(node.Parent);
				var invokeResult = resolved != null ? resolved.Item1 as CSharpInvocationResolveResult : null;
				if (invokeResult != null) {
					int argNum = 0;
					foreach (var arg in node.Parent.Children.Where (c => c.Role == Roles.Argument)) {
						if (arg == node) {
							break;
						}
						argNum++;
					}
					var param = argNum < invokeResult.Member.Parameters.Count ? invokeResult.Member.Parameters [argNum] : null;
					if (param != null && param.Type.Kind == TypeKind.Enum) {
						AddEnumMembers(wrapper, param.Type, state);
					}
				}
			}
			
			if (node is Expression) {
				var root = node;
				while (root.Parent != null)
					root = root.Parent;
				var astResolver = CompletionContextProvider.GetResolver(state, root);
				foreach (var type in CreateFieldAction.GetValidTypes(astResolver, (Expression)node)) {
					if (type.Kind == TypeKind.Enum) {
						AddEnumMembers(wrapper, type, state);
					} else if (type.Kind == TypeKind.Delegate) {
						AddDelegateHandlers(wrapper, type, true, true);
						AutoSelect = false;
						AutoCompleteEmptyMatch = false;
					}
				}
			}
			
			// Add 'this' keyword for first parameter (extension method case)
			if (node != null && node.Parent is ParameterDeclaration && 
				node.Parent.PrevSibling != null && node.Parent.PrevSibling.Role == Roles.LPar) {
				wrapper.AddCustom("this");
			}
		}
		
		static bool IsInSwitchContext(AstNode node)
		{
			var n = node;
			while (n != null && !(n is EntityDeclaration)) {
				if (n is SwitchStatement) {
					return true;
				}
				if (n is BlockStatement) {
					return false;
				}
				n = n.Parent;
			}
			return false;
		}
		
		void AddTypesAndNamespaces(CompletionDataWrapper wrapper, CSharpResolver state, AstNode node, Func<IType, IType> typePred = null, Predicate<IMember> memberPred = null, Action<ICompletionData, IType> callback = null)
		{
			var lookup = new MemberLookup(ctx.CurrentTypeDefinition, Compilation.MainAssembly);
			if (currentType != null) {
				for (var ct = ctx.CurrentTypeDefinition; ct != null; ct = ct.DeclaringTypeDefinition) {
					foreach (var nestedType in ct.NestedTypes) {
						string name = nestedType.Name;
						if (IsAttributeContext(node) && name.EndsWith("Attribute") && name.Length > "Attribute".Length) {
							name = name.Substring(0, name.Length - "Attribute".Length);
						}
						
						if (typePred == null) {
							wrapper.AddType(nestedType, name);
							continue;
						}
						
						var type = typePred(nestedType);
						if (type != null) {
							var a2 = wrapper.AddType(type, name);
							if (a2 != null && callback != null) {
								callback(a2, type);
							}
						}
						continue;
					}
				}
				if (this.currentMember != null && !(node is AstType)) {
					var def = ctx.CurrentTypeDefinition ?? Compilation.MainAssembly.GetTypeDefinition(currentType);
					if (def != null) {
						bool isProtectedAllowed = true;
						foreach (var member in def.GetMembers ()) {
							if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize") {
								continue;
							}
							if (member.EntityType == EntityType.Operator) {
								continue;
							}
							if (member.IsExplicitInterfaceImplementation) {
								continue;
							}
							if (!lookup.IsAccessible(member, isProtectedAllowed)) {
								continue;
							}
							
							if (memberPred == null || memberPred(member)) {
								wrapper.AddMember(member);
							}
						}
						var declaring = def.DeclaringTypeDefinition;
						while (declaring != null) {
							foreach (var member in declaring.GetMembers (m => m.IsStatic)) {
								if (memberPred == null || memberPred(member)) {
									wrapper.AddMember(member);
								}
							}
							declaring = declaring.DeclaringTypeDefinition;
						}
					}
				}
				if (ctx.CurrentTypeDefinition != null) {
					foreach (var p in ctx.CurrentTypeDefinition.TypeParameters) {
						wrapper.AddTypeParameter(p);
					}
				}
			}
			var scope = ctx.CurrentUsingScope;
			
			for (var n = scope; n != null; n = n.Parent) {
				foreach (var pair in n.UsingAliases) {
					wrapper.AddAlias(pair.Key);
				}
				foreach (var u in n.Usings) {
					foreach (var type in u.Types) {
						if (!lookup.IsAccessible(type, false))
							continue;
						
						IType addType = typePred != null ? typePred(type) : type;
						if (addType != null) {
							string name = type.Name;
							if (IsAttributeContext(node) && name.EndsWith("Attribute") && name.Length > "Attribute".Length) {
								name = name.Substring(0, name.Length - "Attribute".Length);
							}
							var a = wrapper.AddType(addType, name);
							if (a != null && callback != null) {
								callback(a, type);
							}
						}
					}
				}
				
				foreach (var type in n.Namespace.Types) {
					if (!lookup.IsAccessible(type, false))
						continue;
					IType addType = typePred != null ? typePred(type) : type;
					if (addType != null) {
						var a2 = wrapper.AddType(addType, addType.Name);
						if (a2 != null && callback != null) {
							callback(a2, type);
						}
					}
				}
				
				foreach (var curNs in n.Namespace.ChildNamespaces) {
					wrapper.AddNamespace(curNs);
				}
			}
		}
		
		IEnumerable<ICompletionData> HandleKeywordCompletion(int wordStart, string word)
		{
			if (IsInsideCommentStringOrDirective()) {
				if (IsInPreprocessorDirective()) {
					if (word == "if" || word == "elif") {
						if (wordStart > 0 && document.GetCharAt(wordStart - 1) == '#') {
							return factory.CreatePreProcessorDefinesCompletionData();
						}
					}
				}
				return null;
			}
			switch (word) {
				case "namespace":
					return null;
				case "using":
					if (currentType != null) {
						return null;
					}
					var wrapper = new CompletionDataWrapper(this);
					AddTypesAndNamespaces(wrapper, GetState(), null, t => null);
					return wrapper.Result;
				case "case":
					return CreateCaseCompletionData(location);
			//				case ",":
			//				case ":":
			//					if (result.ExpressionContext == ExpressionContext.InheritableType) {
			//						IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
			//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
			//						List<string > namespaceList = GetUsedNamespaces ();
			//						var col = new CSharpTextEditorCompletion.CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, null, location);
			//						bool isInterface = false;
			//						HashSet<string > baseTypeNames = new HashSet<string> ();
			//						if (cls != null) {
			//							baseTypeNames.Add (cls.Name);
			//							if (cls.ClassType == ClassType.Struct)
			//								isInterface = true;
			//						}
			//						int tokenIndex = offset;
			//	
			//						// Search base types " : [Type1, ... ,TypeN,] <Caret>"
			//						string token = null;
			//						do {
			//							token = GetPreviousToken (ref tokenIndex, false);
			//							if (string.IsNullOrEmpty (token))
			//								break;
			//							token = token.Trim ();
			//							if (Char.IsLetterOrDigit (token [0]) || token [0] == '_') {
			//								IType baseType = dom.SearchType (Document.CompilationUnit, cls, result.Region.Start, token);
			//								if (baseType != null) {
			//									if (baseType.ClassType != ClassType.Interface)
			//										isInterface = true;
			//									baseTypeNames.Add (baseType.Name);
			//								}
			//							}
			//						} while (token != ":");
			//						foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
			//							IType type = o as IType;
			//							if (type != null && (type.IsStatic || type.IsSealed || baseTypeNames.Contains (type.Name) || isInterface && type.ClassType != ClassType.Interface)) {
			//								continue;
			//							}
			//							if (o is Namespace && !namespaceList.Any (ns => ns.StartsWith (((Namespace)o).FullName)))
			//								continue;
			//							col.Add (o);
			//						}
			//						// Add inner classes
			//						Stack<IType > innerStack = new Stack<IType> ();
			//						innerStack.Push (cls);
			//						while (innerStack.Count > 0) {
			//							IType curType = innerStack.Pop ();
			//							if (curType == null)
			//								continue;
			//							foreach (IType innerType in curType.InnerTypes) {
			//								if (innerType != cls)
			//									// don't add the calling class as possible base type
			//									col.Add (innerType);
			//							}
			//							if (curType.DeclaringType != null)
			//								innerStack.Push (curType.DeclaringType);
			//						}
			//						return completionList;
			//					}
			//					break;
				case "is":
				case "as":
					if (currentType == null) {
						return null;
					}
					IType isAsType = null;
					var isAsExpression = GetExpressionAt(wordStart);
					if (isAsExpression != null) {
						var parent = isAsExpression.Node.Parent;
						if (parent is VariableInitializer) {
							parent = parent.Parent;
						}
						if (parent is VariableDeclarationStatement) {
							var resolved = ResolveExpression(parent);
							if (resolved != null) {
								isAsType = resolved.Item1.Type;
							}
						}
					}
					var isAsWrapper = new CompletionDataWrapper(this);
					var def = isAsType != null ? isAsType.GetDefinition() : null;
					AddTypesAndNamespaces(
					isAsWrapper,
					GetState(),
					null,
					t => t.GetDefinition() == null || def == null || t.GetDefinition().IsDerivedFrom(def) ? t : null,
					m => false);
					return isAsWrapper.Result;
			//					{
			//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
			//						ExpressionResult expressionResult = FindExpression (dom, completionContext, wordStart - document.Caret.Offset);
			//						NRefactoryResolver resolver = CreateResolver ();
			//						ResolveResult resolveResult = resolver.Resolve (expressionResult, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
			//						if (resolveResult != null && resolveResult.ResolvedType != null) {
			//							CompletionDataCollector col = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
			//							IType foundType = null;
			//							if (word == "as") {
			//								ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForAsCompletion (document, Document.CompilationUnit, Document.FileName, resolver.CallingType);
			//								if (exactContext is ExpressionContext.TypeExpressionContext) {
			//									foundType = resolver.SearchType (((ExpressionContext.TypeExpressionContext)exactContext).Type);
			//									AddAsCompletionData (col, foundType);
			//								}
			//							}
			//						
			//							if (foundType == null)
			//								foundType = resolver.SearchType (resolveResult.ResolvedType);
			//						
			//							if (foundType != null) {
			//								if (foundType.ClassType == ClassType.Interface)
			//									foundType = resolver.SearchType (DomReturnType.Object);
			//							
			//								foreach (IType type in dom.GetSubclasses (foundType)) {
			//									if (type.IsSpecialName || type.Name.StartsWith ("<"))
			//										continue;
			//									AddAsCompletionData (col, type);
			//								}
			//							}
			//							List<string > namespaceList = GetUsedNamespaces ();
			//							foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
			//								if (o is IType) {
			//									IType type = (IType)o;
			//									if (type.ClassType != ClassType.Interface || type.IsSpecialName || type.Name.StartsWith ("<"))
			//										continue;
			//	//								if (foundType != null && !dom.GetInheritanceTree (foundType).Any (x => x.FullName == type.FullName))
			//	//									continue;
			//									AddAsCompletionData (col, type);
			//									continue;
			//								}
			//								if (o is Namespace)
			//									continue;
			//								col.Add (o);
			//							}
			//							return completionList;
			//						}
			//						result.ExpressionContext = ExpressionContext.TypeName;
			//						return CreateCtrlSpaceCompletionData (completionContext, result);
			//					}
				case "override":
				// Look for modifiers, in order to find the beginning of the declaration
					int firstMod = wordStart;
					int i = wordStart;
					for (int n = 0; n < 3; n++) {
						string mod = GetPreviousToken(ref i, true);
						if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
							firstMod = i;
						} else if (mod == "static") {
							// static methods are not overridable
							return null;
						} else {
							break;
						}
					}
					if (!IsLineEmptyUpToEol()) {
						return null;
					}
					if (currentType != null && (currentType.Kind == TypeKind.Class || currentType.Kind == TypeKind.Struct)) {
						string modifiers = document.GetText(firstMod, wordStart - firstMod);
						return GetOverrideCompletionData(currentType, modifiers);
					}
					return null;
				case "partial":
				// Look for modifiers, in order to find the beginning of the declaration
					firstMod = wordStart;
					i = wordStart;
					for (int n = 0; n < 3; n++) {
						string mod = GetPreviousToken(ref i, true);
						if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
							firstMod = i;
						} else if (mod == "static") {
							// static methods are not overridable
							return null;
						} else {
							break;
						}
					}
					if (!IsLineEmptyUpToEol()) {
						return null;
					}
					var state = GetState();
				
					if (state.CurrentTypeDefinition != null && (state.CurrentTypeDefinition.Kind == TypeKind.Class || state.CurrentTypeDefinition.Kind == TypeKind.Struct)) {
						string modifiers = document.GetText(firstMod, wordStart - firstMod);
						return GetPartialCompletionData(state.CurrentTypeDefinition, modifiers);
					}
					return null;
				
				case "public":
				case "protected":
				case "private":
				case "internal":
				case "sealed":
				case "static":
					var accessorContext = HandleAccessorContext();
					if (accessorContext != null) {
						return accessorContext;
					}
					wrapper = new CompletionDataWrapper(this);
					state = GetState();
					if (currentType != null) {
						AddTypesAndNamespaces(wrapper, state, null, null, m => false);
						AddKeywords(wrapper, primitiveTypesKeywords);
					}
					AddKeywords(wrapper, typeLevelKeywords);
					return wrapper.Result;
				case "new":
					int j = offset - 4;
				//				string token = GetPreviousToken (ref j, true);
				
					IType hintType = null;
					var expressionOrVariableDeclaration = GetNewExpressionAt(j);
					if (expressionOrVariableDeclaration == null)
						return null;
					var astResolver = CompletionContextProvider.GetResolver(GetState(), expressionOrVariableDeclaration.Unit);
					hintType = CreateFieldAction.GetValidTypes(
					astResolver,
					expressionOrVariableDeclaration.Node as Expression
					)
					.FirstOrDefault();
				
					return CreateTypeCompletionData(hintType);
				case "yield":
					var yieldDataList = new CompletionDataWrapper(this);
					DefaultCompletionString = "return";
					yieldDataList.AddCustom("break");
					yieldDataList.AddCustom("return");
					return yieldDataList.Result;
				case "in":
					var inList = new CompletionDataWrapper(this);
				
					var expr = GetExpressionAtCursor();
					var rr = ResolveExpression(expr);
				
					AddContextCompletion(
					inList,
					rr != null ? rr.Item2 : GetState(),
					expr.Node
					);
					return inList.Result;
			}
			return null;
		}
		
		bool IsLineEmptyUpToEol()
		{
			var line = document.GetLineByNumber(location.Line);
			for (int j = offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt(j);
				if (!char.IsWhiteSpace(ch)) {
					return false;
				}
			}
			return true;
		}
		
		string GetLineIndent(int lineNr)
		{
			var line = document.GetLineByNumber(lineNr);
			for (int j = line.Offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt(j);
				if (!char.IsWhiteSpace(ch)) {
					return document.GetText(line.Offset, j - line.Offset - 1);
				}
			}
			return "";
		}
		
		static CSharpAmbience amb = new CSharpAmbience();
		
		class Category : CompletionCategory
		{
			public Category(string displayText, string icon) : base (displayText, icon)
			{
			}
			
			public override int CompareTo(CompletionCategory other)
			{
				return 0;
			}
		}
		
		IEnumerable<ICompletionData> CreateTypeCompletionData(IType hintType)
		{
			var wrapper = new CompletionDataWrapper(this);
			var state = GetState();
			Func<IType, IType> pred = null;
			Action<ICompletionData, IType> typeCallback = null;
			var inferredTypesCategory = new Category("Inferred Types", null);
			var derivedTypesCategory = new Category("Derived Types", null);
			
			if (hintType != null) {
				if (hintType.Kind != TypeKind.Unknown) {
					var lookup = new MemberLookup(
						ctx.CurrentTypeDefinition,
						Compilation.MainAssembly
					);
					typeCallback = (data, t) => {
						//check if type is in inheritance tree.
						if (hintType.GetDefinition() != null &&
							t.GetDefinition() != null &&
							t.GetDefinition().IsDerivedFrom(hintType.GetDefinition())) {
							data.CompletionCategory = derivedTypesCategory;
						}
					};
					pred = t => {
						if (t.Kind == TypeKind.Interface && hintType.Kind != TypeKind.Array) {
							return null;
						}
						// check for valid constructors
						if (t.GetConstructors().Count() > 0) {
							bool isProtectedAllowed = currentType != null ? 
								currentType.Resolve(ctx).GetDefinition().IsDerivedFrom(t.GetDefinition()) : 
									false;
							if (!t.GetConstructors().Any(m => lookup.IsAccessible(
								m,
								isProtectedAllowed
							)
							)) {
								return null;
							}
						}
						
						var typeInference = new TypeInference(Compilation);
						typeInference.Algorithm = TypeInferenceAlgorithm.ImprovedReturnAllResults;
						var inferedType = typeInference.FindTypeInBounds(
							new [] { t },
						new [] { hintType }
						);
						if (inferedType != SpecialType.UnknownType) {
							var newType = wrapper.AddType(inferedType, amb.ConvertType(inferedType));
							if (newType != null) {
								newType.CompletionCategory = inferredTypesCategory;
							}
							return null;
						}
						return t;
					};
					if (!(hintType.Kind == TypeKind.Interface && hintType.Kind != TypeKind.Array)) {
						DefaultCompletionString = GetShortType(hintType, GetState());
						var hint = wrapper.AddType(hintType, DefaultCompletionString);
						if (hint != null) {
							hint.CompletionCategory = derivedTypesCategory;
						}
					}
					if (hintType is ParameterizedType && hintType.TypeParameterCount == 1 && hintType.FullName == "System.Collections.Generic.IEnumerable") {
						var arg = ((ParameterizedType)hintType).TypeArguments.FirstOrDefault();
						var array = new ArrayTypeReference(arg.ToTypeReference(), 1).Resolve(ctx);
						wrapper.AddType(array, amb.ConvertType(array));
					}
				} else {
					var hint = wrapper.AddType(hintType, DefaultCompletionString);
					if (hint != null) {
						DefaultCompletionString = hint.DisplayText;
						hint.CompletionCategory = derivedTypesCategory;
					}
				}
			} 
			AddTypesAndNamespaces(wrapper, state, null, pred, m => false, typeCallback);
			if (hintType == null || hintType == SpecialType.UnknownType) {
				AddKeywords(wrapper, primitiveTypesKeywords.Where(k => k != "void"));
			}
			
			CloseOnSquareBrackets = true;
			AutoCompleteEmptyMatch = true;
			return wrapper.Result;
		}
		
		IEnumerable<ICompletionData> GetOverrideCompletionData(IUnresolvedTypeDefinition type, string modifiers)
		{
			var wrapper = new CompletionDataWrapper(this);
			var alreadyInserted = new List<IMember>();
			//bool addedVirtuals = false;
			
			int declarationBegin = offset;
			int j = declarationBegin;
			for (int i = 0; i < 3; i++) {
				switch (GetPreviousToken(ref j, true)) {
					case "public":
					case "protected":
					case "private":
					case "internal":
					case "sealed":
					case "override":
						declarationBegin = j;
						break;
					case "static":
						return null; // don't add override completion for static members
				}
			}
			AddVirtuals(
				alreadyInserted,
				wrapper,
				modifiers,
				type.Resolve(ctx),
				declarationBegin
			);
			return wrapper.Result;
		}
		
		IEnumerable<ICompletionData> GetPartialCompletionData(ITypeDefinition type, string modifiers)
		{
			var wrapper = new CompletionDataWrapper(this);
			int declarationBegin = offset;
			int j = declarationBegin;
			for (int i = 0; i < 3; i++) {
				switch (GetPreviousToken(ref j, true)) {
					case "public":
					case "protected":
					case "private":
					case "internal":
					case "sealed":
					case "override":
						declarationBegin = j;
						break;
					case "static":
						return null; // don't add override completion for static members
				}
			}
			
			var methods = new List<IUnresolvedMethod>();
			
			foreach (var part in type.Parts) {
				foreach (var method in part.Methods) {
					if (method.BodyRegion.IsEmpty) {
						if (GetImplementation(type, method) != null) {
							continue;
						}
						methods.Add(method);
					}
				}	
			}
			
			foreach (var method in methods) {
				wrapper.Add(factory.CreateNewPartialCompletionData(
					declarationBegin,
					method.DeclaringTypeDefinition,
					method
				)
				);
			} 
			
			return wrapper.Result;
		}
		
		IMethod GetImplementation(ITypeDefinition type, IUnresolvedMethod method)
		{
			foreach (var cur in type.Methods) {
				if (cur.Name == method.Name && cur.Parameters.Count == method.Parameters.Count && !cur.BodyRegion.IsEmpty) {
					bool equal = true;
					/*for (int i = 0; i < cur.Parameters.Count; i++) {
						if (!cur.Parameters [i].Type.Equals (method.Parameters [i].Type)) {
							equal = false;
							break;
						}
					}*/
					if (equal) {
						return cur;
					}
				}
			}
			return null;
		}
		
		void AddVirtuals(List<IMember> alreadyInserted, CompletionDataWrapper col, string modifiers, IType curType, int declarationBegin)
		{
			if (curType == null) {
				return;
			}
			foreach (var m in curType.GetMembers ().Reverse ()) {
				if (curType.Kind != TypeKind.Interface && !m.IsOverridable) {
					continue;
				}
				// filter out the "Finalize" methods, because finalizers should be done with destructors.
				if (m is IMethod && m.Name == "Finalize") {
					continue;
				}
				
				var data = factory.CreateNewOverrideCompletionData(
					declarationBegin,
					currentType,
					m
				);
				// check if the member is already implemented
				bool foundMember = curType.GetMembers().Any(cm => SignatureComparer.Ordinal.Equals(
					cm,
					m
				) && cm.DeclaringTypeDefinition == curType.GetDefinition()
				);
				if (foundMember) {
					continue;
				}
				if (alreadyInserted.Any(cm => SignatureComparer.Ordinal.Equals(cm, m)))
					continue;
				alreadyInserted.Add(m);
				data.CompletionCategory = col.GetCompletionCategory(m.DeclaringTypeDefinition);
				col.Add(data);
			}
		}
		
		static void AddKeywords(CompletionDataWrapper wrapper, IEnumerable<string> keywords)
		{
			foreach (string keyword in keywords) {
				if (wrapper.Result.Any(data => data.DisplayText == keyword))
					continue;
				wrapper.AddCustom(keyword);
			}
		}
		
		public string GetPreviousMemberReferenceExpression(int tokenIndex)
		{
			string result = GetPreviousToken(ref tokenIndex, false);
			result = GetPreviousToken(ref tokenIndex, false);
			if (result != ".") {
				result = null;
			} else {
				var names = new List<string>();
				while (result == ".") {
					result = GetPreviousToken(ref tokenIndex, false);
					if (result == "this") {
						names.Add("handle");
					} else if (result != null) {
						string trimmedName = result.Trim();
						if (trimmedName.Length == 0) {
							break;
						}
						names.Insert(0, trimmedName);
					}
					result = GetPreviousToken(ref tokenIndex, false);
				}
				result = String.Join("", names.ToArray());
				foreach (char ch in result) {
					if (!char.IsLetterOrDigit(ch) && ch != '_') {
						result = "";
						break;
					}
				}
			}
			return result;
		}
		
		bool MatchDelegate(IType delegateType, IMethod method)
		{
			var delegateMethod = delegateType.GetDelegateInvokeMethod();
			if (delegateMethod == null || delegateMethod.Parameters.Count != method.Parameters.Count) {
				return false;
			}
			
			for (int i = 0; i < delegateMethod.Parameters.Count; i++) {
				if (!delegateMethod.Parameters [i].Type.Equals(method.Parameters [i].Type)) {
					return false;
				}
			}
			return true;
		}
		
		string AddDelegateHandlers(CompletionDataWrapper completionList, IType delegateType, bool addSemicolon = true, bool addDefault = true)
		{
			IMethod delegateMethod = delegateType.GetDelegateInvokeMethod();
			var thisLineIndent = GetLineIndent(location.Line);
			string delegateEndString = EolMarker + thisLineIndent + "}" + (addSemicolon ? ";" : "");
			//bool containsDelegateData = completionList.Result.Any(d => d.DisplayText.StartsWith("delegate("));
			if (addDefault) {
				var oldDelegate = completionList.Result.FirstOrDefault(cd => cd.DisplayText == "delegate");
				if (oldDelegate != null)
					completionList.Result.Remove(oldDelegate);
				completionList.AddCustom(
					"delegate",
					"Creates anonymous delegate.",
					"delegate {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
					);
			}
			var sb = new StringBuilder("(");
			var sbWithoutTypes = new StringBuilder("(");
			var state = GetState();
			var builder = new TypeSystemAstBuilder(state);
			
			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
				if (k > 0) {
					sb.Append(", ");
					sbWithoutTypes.Append(", ");
				}
				var convertedParameter = builder.ConvertParameter (delegateMethod.Parameters [k]);
				if (convertedParameter.ParameterModifier == ParameterModifier.Params)
					convertedParameter.ParameterModifier = ParameterModifier.None;
				sb.Append(convertedParameter.GetText (FormattingPolicy));
				sbWithoutTypes.Append(delegateMethod.Parameters [k].Name);
			}
			
			sb.Append(")");
			sbWithoutTypes.Append(")");
			completionList.AddCustom(
				"delegate" + sb,
				"Creates anonymous delegate.",
				"delegate" + sb + " {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
				);

			if (!completionList.Result.Any(data => data.DisplayText == sb.ToString())) {
				completionList.AddCustom(
					sb.ToString(),
					"Creates typed lambda expression.",
					sb + " => |" + (addSemicolon ? ";" : "")
					);
			}

			if (!delegateMethod.Parameters.Any (p => p.IsOut || p.IsRef) && !completionList.Result.Any(data => data.DisplayText == sbWithoutTypes.ToString())) {
				completionList.AddCustom(
					sbWithoutTypes.ToString(),
					"Creates lambda expression.",
					sbWithoutTypes + " => |" + (addSemicolon ? ";" : "")
					);
			}
			/* TODO:Make factory method out of it.
			// It's  needed to temporarly disable inserting auto matching bracket because the anonymous delegates are selectable with '('
			// otherwise we would end up with () => )
			if (!containsDelegateData) {
				var savedValue = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;
				MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = false;
				completionList.Result.CompletionListClosed += delegate {
					MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = savedValue;
				};
			}*/
			return sb.ToString();
		}

		bool IsAccessibleFrom(IEntity member, ITypeDefinition calledType, IMember currentMember, bool includeProtected)
		{
			if (currentMember == null) {
				return member.IsStatic || member.IsPublic;
			}
			//			if (currentMember is MonoDevelop.Projects.Dom.BaseResolveResult.BaseMemberDecorator) 
			//				return member.IsPublic | member.IsProtected;
			//		if (member.IsStatic && !IsStatic)
			//			return false;
			if (member.IsPublic || calledType != null && calledType.Kind == TypeKind.Interface && !member.IsProtected) {
				return true;
			}
			if (member.DeclaringTypeDefinition != null) {
				if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) { 
					return IsAccessibleFrom(
						member.DeclaringTypeDefinition,
						calledType,
						currentMember,
						includeProtected
					);
				}
				
				if (member.IsProtected && !(member.DeclaringTypeDefinition.IsProtectedOrInternal && !includeProtected)) {
					return includeProtected;
				}
			}
			if (member.IsInternal || member.IsProtectedAndInternal || member.IsProtectedOrInternal) {
				//var type1 = member is ITypeDefinition ? (ITypeDefinition)member : member.DeclaringTypeDefinition;
				//var type2 = currentMember is ITypeDefinition ? (ITypeDefinition)currentMember : currentMember.DeclaringTypeDefinition;
				bool result = true;
				// easy case, projects are the same
				/*//				if (type1.ProjectContent == type2.ProjectContent) {
				//					result = true; 
				//				} else 
				if (type1.ProjectContent != null) {
					// maybe type2 hasn't project dom set (may occur in some cases), check if the file is in the project
					//TODO !!
					//					result = type1.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type2.Region.FileName) != null;
					result = false;
				} else if (type2.ProjectContent != null) {
					//TODO!!
					//					result = type2.ProjectContent.Annotation<MonoDevelop.Projects.Project> ().GetProjectFile (type1.Region.FileName) != null;
					result = false;
				} else {
					// should never happen !
					result = true;
				}*/
				return member.IsProtectedAndInternal ? includeProtected && result : result;
			}
			
			if (!(currentMember is IType) && (currentMember.DeclaringTypeDefinition == null || member.DeclaringTypeDefinition == null)) {
				return false;
			}
			
			// inner class 
			var declaringType = currentMember.DeclaringTypeDefinition;
			while (declaringType != null) {
				if (declaringType.ReflectionName == currentMember.DeclaringType.ReflectionName) {
					return true;
				}
				declaringType = declaringType.DeclaringTypeDefinition;
			}
			
			
			return currentMember.DeclaringTypeDefinition != null && member.DeclaringTypeDefinition.FullName == currentMember.DeclaringTypeDefinition.FullName;
		}
		
		static bool IsAttributeContext(AstNode node)
		{
			AstNode n = node;
			while (n is AstType) {
				n = n.Parent;
			}
			return n is Attribute;
		}
		
		IEnumerable<ICompletionData> CreateTypeAndNamespaceCompletionData(TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null || resolveResult.IsError) {
				return null;
			}
			var exprParent = resolvedNode.GetParent<Expression>();
			var unit = exprParent != null ? exprParent.GetParent<SyntaxTree>() : null;
			
			var astResolver = unit != null ? CompletionContextProvider.GetResolver(state, unit) : null;
			IType hintType = exprParent != null && astResolver != null ? 
				CreateFieldAction.GetValidTypes(astResolver, exprParent) .FirstOrDefault() :
					null;
			var result = new CompletionDataWrapper(this);
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				if (!(resolvedNode.Parent is UsingDeclaration || resolvedNode.Parent != null && resolvedNode.Parent.Parent is UsingDeclaration)) {
					foreach (var cl in nr.Namespace.Types) {
						string name = cl.Name;
						if (hintType != null && hintType.Kind != TypeKind.Array && cl.Kind == TypeKind.Interface) {
							continue;
						}
						if (IsAttributeContext(resolvedNode) && name.EndsWith("Attribute") && name.Length > "Attribute".Length) {
							name = name.Substring(0, name.Length - "Attribute".Length);
						}
						result.AddType(cl, name);
					}
				}
				foreach (var ns in nr.Namespace.ChildNamespaces) {
					result.AddNamespace(ns);
				}
			} else if (resolveResult is TypeResolveResult) {
				var type = resolveResult.Type;
				foreach (var nested in type.GetNestedTypes ()) {
					if (hintType != null && hintType.Kind != TypeKind.Array && nested.Kind == TypeKind.Interface) {
						continue;
					}
					result.AddType(nested, nested.Name);
				}
			}
			return result.Result;
		}
		
		IEnumerable<ICompletionData> CreateTypeList()
		{
			foreach (var cl in Compilation.RootNamespace.Types) {
				yield return factory.CreateTypeCompletionData(cl, cl.Name);
			}
			
			foreach (var ns in Compilation.RootNamespace.ChildNamespaces) {
				yield return factory.CreateNamespaceCompletionData(ns);
			}
		}
		
		IEnumerable<ICompletionData> CreateParameterCompletion(MethodGroupResolveResult resolveResult, CSharpResolver state, AstNode invocation, SyntaxTree unit, int parameter, bool controlSpace)
		{
			var result = new CompletionDataWrapper(this);
			var addedEnums = new HashSet<string>();
			var addedDelegates = new HashSet<string>();
			
			foreach (var method in resolveResult.Methods) {
				if (method.Parameters.Count <= parameter) {
					continue;
				}
				var resolvedType = method.Parameters [parameter].Type;
				if (resolvedType.Kind == TypeKind.Enum) {
					if (addedEnums.Contains(resolvedType.ReflectionName)) {
						continue;
					}
					addedEnums.Add(resolvedType.ReflectionName);
					AddEnumMembers(result, resolvedType, state);
				} else if (resolvedType.Kind == TypeKind.Delegate) {
					if (addedDelegates.Contains(resolvedType.ReflectionName))
						continue;
					string parameterDefinition = AddDelegateHandlers(result, resolvedType);
					string varName = "Handle" + method.Parameters [parameter].Type.Name + method.Parameters [parameter].Name;
					result.Result.Add(
						factory.CreateEventCreationCompletionData(
						varName,
						resolvedType,
						null,
						parameterDefinition,
						currentMember,
						currentType)
					);
				}
			}
			
			foreach (var method in resolveResult.Methods) {
				if (parameter < method.Parameters.Count && method.Parameters [parameter].Type.Kind == TypeKind.Delegate) {
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
				}
				foreach (var p in method.Parameters) {
					result.AddNamedParameterVariable(p);
				}
			}
			
			if (!controlSpace) {
				if (addedEnums.Count + addedDelegates.Count == 0) {
					return Enumerable.Empty<ICompletionData>();
				}
				AutoCompleteEmptyMatch = false;
				AutoSelect = false;
			}
			AddContextCompletion(result, state, invocation);
			
			//			resolver.AddAccessibleCodeCompletionData (ExpressionContext.MethodBody, cdc);
			//			if (addedDelegates.Count > 0) {
			//				foreach (var data in result.Result) {
			//					if (data is MemberCompletionData) 
			//						((MemberCompletionData)data).IsDelegateExpected = true;
			//				}
			//			}
			return result.Result;
		}
		
		string GetShortType(IType type, CSharpResolver state)
		{
			var builder = new TypeSystemAstBuilder(state);
			var dt = state.CurrentTypeDefinition;
			var declaring = type.DeclaringType != null ? type.DeclaringType.GetDefinition() : null;
			if (declaring != null) {
				while (dt != null) {
					if (dt.Equals(declaring)) {
						builder.AlwaysUseShortTypeNames = true;
						break;
					}
					dt = dt.DeclaringTypeDefinition;
				}
			}
			var shortType = builder.ConvertType(type);
			return shortType.GetText(FormattingPolicy);
		}
		
		void AddEnumMembers(CompletionDataWrapper completionList, IType resolvedType, CSharpResolver state)
		{
			if (resolvedType.Kind != TypeKind.Enum) {
				return;
			}
			string typeString = GetShortType(resolvedType, state);
			completionList.AddEnumMembers(resolvedType, state, typeString);
			DefaultCompletionString = typeString;
		}
		
		IEnumerable<ICompletionData> CreateCompletionData(TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state, Func<IType, IType> typePred = null)
		{
			if (resolveResult == null /*|| resolveResult.IsError*/) {
				return null;
			}
			
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				var namespaceContents = new CompletionDataWrapper(this);
				
				foreach (var cl in nr.Namespace.Types) {
					IType addType = typePred != null ? typePred(cl) : cl;
					if (addType != null)
						namespaceContents.AddType(addType, addType.Name);
				}
				
				foreach (var ns in nr.Namespace.ChildNamespaces) {
					namespaceContents.AddNamespace(ns);
				}
				return namespaceContents.Result;
			}
			
			IType type = resolveResult.Type;
			if (resolvedNode.Parent is PointerReferenceExpression && (type is PointerType)) {
				type = ((PointerType)type).ElementType;
			}
			
			//var typeDef = resolveResult.Type.GetDefinition();
			var result = new CompletionDataWrapper(this);
			bool includeStaticMembers = false;
			
			var lookup = new MemberLookup(
				ctx.CurrentTypeDefinition,
				Compilation.MainAssembly
			);
			
			
			if (resolveResult is LocalResolveResult) {
				if (resolvedNode is IdentifierExpression) {
					var mrr = (LocalResolveResult)resolveResult;
					includeStaticMembers = mrr.Variable.Name == mrr.Type.Name;
				}
			}
			if (resolveResult is TypeResolveResult && type.Kind == TypeKind.Enum) {
				foreach (var field in type.GetFields ()) {
					if (!lookup.IsAccessible(field, false))
						continue;
					result.AddMember(field);
				}
				return result.Result;
			}
			
			bool isProtectedAllowed = resolveResult is ThisResolveResult ? true : lookup.IsProtectedAccessAllowed(type);
			bool skipNonStaticMembers = (resolveResult is TypeResolveResult);
			
			if (resolveResult is MemberResolveResult && resolvedNode is IdentifierExpression) {
				var mrr = (MemberResolveResult)resolveResult;
				includeStaticMembers = mrr.Member.Name == mrr.Type.Name;
				
				TypeResolveResult trr;
				if (state.IsVariableReferenceWithSameType(
					resolveResult,
					((IdentifierExpression)resolvedNode).Identifier,
					out trr
				)) {
					if (currentMember != null && mrr.Member.IsStatic ^ currentMember.IsStatic) {
						skipNonStaticMembers = true;
						
						if (trr.Type.Kind == TypeKind.Enum) {
							foreach (var field in trr.Type.GetFields ()) {
								result.AddMember(field);
							}
							return result.Result;
						}
					}
				}
				// ADD Aliases
				var scope = ctx.CurrentUsingScope;
				
				for (var n = scope; n != null; n = n.Parent) {
					foreach (var pair in n.UsingAliases) {
						if (pair.Key == mrr.Member.Name) {
							foreach (var r in CreateCompletionData (location, pair.Value, resolvedNode, state)) {
								if (r is IEntityCompletionData && ((IEntityCompletionData)r).Entity is IMember) {
									result.AddMember((IMember)((IEntityCompletionData)r).Entity);
								} else {
									result.Add(r);
								}
							}
						}
					}
				}				
				
				
			}
			if (resolveResult is TypeResolveResult && (resolvedNode is IdentifierExpression || resolvedNode is MemberReferenceExpression)) {
				includeStaticMembers = true;
			}
			
			//			Console.WriteLine ("type:" + type +"/"+type.GetType ());
			//			Console.WriteLine ("current:" + ctx.CurrentTypeDefinition);
			//			Console.WriteLine ("IS PROT ALLOWED:" + isProtectedAllowed + " static: "+ includeStaticMembers);
			//			Console.WriteLine (resolveResult);
			//			Console.WriteLine ("node:" + resolvedNode);
			//			Console.WriteLine (currentMember !=  null ? currentMember.IsStatic : "currentMember == null");
			
			if (resolvedNode.Annotation<ObjectCreateExpression>() == null) {
				//tags the created expression as part of an object create expression.
				
				var filteredList = new List<IMember>();
				foreach (var member in type.GetMembers ()) {
					if (member.EntityType == EntityType.Indexer || member.EntityType == EntityType.Operator || member.EntityType == EntityType.Constructor || member.EntityType == EntityType.Destructor) {
						continue;
					}
					if (member.IsExplicitInterfaceImplementation) {
						continue;
					}
					//					Console.WriteLine ("member:" + member + member.IsShadowing);
					if (!lookup.IsAccessible(member, isProtectedAllowed)) {
						//						Console.WriteLine ("skip access: " + member.FullName);
						continue;
					}
					if (resolvedNode is BaseReferenceExpression && member.IsAbstract) {
						continue;
					}
					bool memberIsStatic = member.IsStatic;
					if (!includeStaticMembers && memberIsStatic && !(resolveResult is TypeResolveResult)) {
						//						Console.WriteLine ("skip static member: " + member.FullName);
						continue;
					}
					var field = member as IField;
					if (field != null) {
						memberIsStatic |= field.IsConst;
					}
					
					if (!memberIsStatic && skipNonStaticMembers) {
						continue;
					}
					
					if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize") {
						continue;
					}
					if (member.EntityType == EntityType.Operator) {
						continue;
					}
					if (member.IsExplicitInterfaceImplementation) {
						continue;
					}
					if (member.IsShadowing) {
						filteredList.RemoveAll(m => m.Name == member.Name);
					}
					filteredList.Add(member);
				}
				
				foreach (var member in filteredList) {
					//					Console.WriteLine ("add:" + member + "/" + member.IsStatic);
					result.AddMember(member);
				}
			}
			
			if (resolveResult is TypeResolveResult || includeStaticMembers) {
				foreach (var nested in type.GetNestedTypes ()) {
					if (!lookup.IsAccessible(nested.GetDefinition(), isProtectedAllowed))
						continue;
					IType addType = typePred != null ? typePred(nested) : nested;
					if (addType != null)
						result.AddType(addType, addType.Name);
				}
				
			} else {
				foreach (var meths in state.GetExtensionMethods (type)) {
					foreach (var m in meths) {
						result.AddMember(m);
					}
				}
			}
			
			//			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
			//			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver != null ? resolver.CallingType : null, location);
			//			col.HideExtensionParameter = !resolveResult.StaticResolve;
			//			col.NamePrefix = expressionResult.Expression;
			//			bool showOnlyTypes = expressionResult.Contexts.Any (ctx => ctx == ExpressionContext.InheritableType || ctx == ExpressionContext.Constraints);
			//			if (objects != null) {
			//				foreach (object obj in objects) {
			//					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
			//						continue;
			//					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
			//						continue;
			//					if (showOnlyTypes && !(obj is IType))
			//						continue;
			//					CompletionData data = col.Add (obj);
			//					if (data != null && expressionResult.ExpressionContext == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
			//						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
			//						data.SetText (newText);
			//					}
			//				}
			//			}
			
			return result.Result;
		}
		
		IEnumerable<ICompletionData> CreateCaseCompletionData(TextLocation location)
		{
			var unit = ParseStub("a: break;");
			if (unit == null) {
				return null;
			}
			var s = unit.GetNodeAt<SwitchStatement>(location);
			if (s == null) {
				return null;
			}
			
			var offset = document.GetOffset(s.Expression.StartLocation);
			var expr = GetExpressionAt(offset);
			if (expr == null) {
				return null;
			}
			
			var resolveResult = ResolveExpression(expr);
			if (resolveResult == null || resolveResult.Item1.Type.Kind != TypeKind.Enum) { 
				return null;
			}
			var wrapper = new CompletionDataWrapper(this);
			AddEnumMembers(wrapper, resolveResult.Item1.Type, resolveResult.Item2);
			AutoCompleteEmptyMatch = false;
			return wrapper.Result;
		}
		
		#region Parsing methods
		ExpressionResult GetExpressionBeforeCursor()
		{
			SyntaxTree baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub("a", false);
				var type = baseUnit.GetNodeAt<MemberType>(location);
				if (type == null) {
					baseUnit = ParseStub("a;", false);
					type = baseUnit.GetNodeAt<MemberType>(location);
				}
				
				if (type == null) {
					baseUnit = ParseStub("A a;", false);
					type = baseUnit.GetNodeAt<MemberType>(location);
				}
				if (type != null) {
					return new ExpressionResult((AstNode)type.Target, baseUnit);
				}
			}
			
			baseUnit = ParseStub("a", false);
			var curNode = baseUnit.GetNodeAt(location);
			// hack for local variable declaration missing ';' issue - remove that if it works.
			if (curNode is EntityDeclaration || baseUnit.GetNodeAt<Expression>(location) == null && baseUnit.GetNodeAt<MemberType>(location) == null) {
				baseUnit = ParseStub("a");
				curNode = baseUnit.GetNodeAt(location);
			}
			
			// Hack for handle object initializer continuation expressions
			if (curNode is EntityDeclaration || baseUnit.GetNodeAt<Expression>(location) == null && baseUnit.GetNodeAt<MemberType>(location) == null) {
				baseUnit = ParseStub("a};");
			}
			var mref = baseUnit.GetNodeAt<MemberReferenceExpression>(location); 
			if (currentMember == null && currentType == null) {
				if (mref != null) {
					return new ExpressionResult((AstNode)mref.Target, baseUnit);
				}
				return null;
			}
			
			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			if (mref == null) {
				var type = baseUnit.GetNodeAt<MemberType>(location); 
				if (type != null) {
					return new ExpressionResult((AstNode)type.Target, baseUnit);
				}
				
				var pref = baseUnit.GetNodeAt<PointerReferenceExpression>(location); 
				if (pref != null) {
					return new ExpressionResult((AstNode)pref.Target, baseUnit);
				}
			}
			AstNode expr = null;
			if (mref != null) {
				expr = mref.Target;
			} else {
				Expression tref = baseUnit.GetNodeAt<TypeReferenceExpression>(location); 
				MemberType memberType = tref != null ? ((TypeReferenceExpression)tref).Type as MemberType : null;
				if (memberType == null) {
					memberType = baseUnit.GetNodeAt<MemberType>(location); 
					if (memberType != null) {
						if (memberType.Parent is ObjectCreateExpression) {
							var mt = memberType.Target.Clone();
							memberType.ReplaceWith(mt);
							expr = mt;
							goto exit;
						} else {
							tref = baseUnit.GetNodeAt<Expression>(location); 
							if (tref == null) {
								tref = new TypeReferenceExpression(memberType.Clone());
								memberType.Parent.AddChild(tref, Roles.Expression);
							}
							if (tref is ObjectCreateExpression) {
								expr = new TypeReferenceExpression(memberType.Target.Clone());
								expr.AddAnnotation(new ObjectCreateExpression());
							}
						}
					}
				}
				
				if (memberType == null) {
					return null;
				}
				if (expr == null) {
					expr = new TypeReferenceExpression(memberType.Target.Clone());
				}
				tref.ReplaceWith(expr);
			}
			exit:
			return new ExpressionResult((AstNode)expr, baseUnit);
		}
		
		ExpressionResult GetExpressionAtCursor()
		{
			//			TextLocation memberLocation;
			//			if (currentMember != null) {
			//				memberLocation = currentMember.Region.Begin;
			//			} else if (currentType != null) {
			//				memberLocation = currentType.Region.Begin;
			//			} else {
			//				memberLocation = location;
			//			}
			var baseUnit = ParseStub("a");
			var tmpUnit = baseUnit;
			AstNode expr = baseUnit.GetNodeAt(
				location,
				n => n is IdentifierExpression || n is MemberReferenceExpression
			);
			
			if (expr == null) {
				expr = baseUnit.GetNodeAt<AstType>(location.Line, location.Column - 1);
			}
			if (expr == null)
				expr = baseUnit.GetNodeAt<Identifier>(location.Line, location.Column - 1);
			// try insertStatement
			if (expr == null && baseUnit.GetNodeAt<EmptyStatement>(
				location.Line,
				location.Column
			) != null) {
				tmpUnit = baseUnit = ParseStub("a();", false);
				expr = baseUnit.GetNodeAt<InvocationExpression>(
					location.Line,
					location.Column + 1
				); 
			}
			
			if (expr == null) {
				baseUnit = ParseStub("()");
				expr = baseUnit.GetNodeAt<IdentifierExpression>(
					location.Line,
					location.Column - 1
				); 
				if (expr == null) {
					expr = baseUnit.GetNodeAt<MemberType>(location.Line, location.Column - 1); 
				}
			}
			
			if (expr == null) {
				baseUnit = ParseStub("a", false);
				expr = baseUnit.GetNodeAt(
					location,
					n => n is IdentifierExpression || n is MemberReferenceExpression || n is CatchClause
				);
			}
			
			// try statement 
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<SwitchStatement>(
					location.Line,
					location.Column - 1
				); 
				baseUnit = tmpUnit;
			}
			
			if (expr == null) {
				var block = tmpUnit.GetNodeAt<BlockStatement>(location); 
				var node = block != null ? block.Statements.LastOrDefault() : null;
				
				var forStmt = node != null ? node.PrevSibling as ForStatement : null;
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					expr = forStmt;
					var id = new IdentifierExpression("stub");
					forStmt.EmbeddedStatement = new BlockStatement() { Statements = { new ExpressionStatement (id) }};
					expr = id;
					baseUnit = tmpUnit;
				}
			}
			
			if (expr == null) {
				var forStmt = tmpUnit.GetNodeAt<ForeachStatement>(
					location.Line,
					location.Column - 3
				); 
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					forStmt.VariableNameToken = Identifier.Create("stub");
					expr = forStmt.VariableNameToken;
					baseUnit = tmpUnit;
				}
			}
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<VariableInitializer>(
					location.Line,
					location.Column - 1
				);
				baseUnit = tmpUnit;
			}
			
			// try parameter declaration type
			if (expr == null) {
				baseUnit = ParseStub(">", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration>(
					location.Line,
					location.Column - 1
				); 
			}
			
			// try parameter declaration method
			if (expr == null) {
				baseUnit = ParseStub("> ()", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration>(
					location.Line,
					location.Column - 1
				); 
			}
			
			// try expression in anonymous type "new { sample = x$" case
			if (expr == null) {
				baseUnit = ParseStub("a", false);
				expr = baseUnit.GetNodeAt<AnonymousTypeCreateExpression>(
					location.Line,
					location.Column
				); 
				if (expr != null) {
					expr = baseUnit.GetNodeAt<Expression>(location.Line, location.Column) ?? expr;
				} 
				if (expr == null) {
					expr = baseUnit.GetNodeAt<AstType>(location.Line, location.Column);
				} 
			}
			
			if (expr == null) {
				return null;
			}
			return new ExpressionResult(expr, baseUnit);
		}
		
		ExpressionResult GetExpressionAt(int offset)
		{
			var parser = new CSharpParser();
			string text = this.document.GetText(0, this.offset); 
			var sb = new StringBuilder(text);
			sb.Append("a;");
			AppendMissingClosingBrackets(sb, text, false);
			var completionUnit = parser.Parse(sb.ToString());
			var loc = document.GetLocation(offset);
			
			var expr = completionUnit.GetNodeAt(
				loc,
				n => n is Expression || n is VariableDeclarationStatement
			);
			if (expr == null) {
				return null;
			}
			return new ExpressionResult(expr, completionUnit);
		}
		
		ExpressionResult GetNewExpressionAt(int offset)
		{
			var parser = new CSharpParser();
			string text = this.document.GetText(0, this.offset); 
			var sb = new StringBuilder(text);
			sb.Append("a ();");
			AppendMissingClosingBrackets(sb, text, false);
			
			var completionUnit = parser.Parse(sb.ToString());
			var loc = document.GetLocation(offset);
			
			var expr = completionUnit.GetNodeAt(loc, n => n is Expression);
			if (expr == null) {
				// try without ";"
				sb = new StringBuilder(text);
				sb.Append("a ()");
				AppendMissingClosingBrackets(sb, text, false);
				completionUnit = parser.Parse(sb.ToString());
				loc = document.GetLocation(offset);
				
				expr = completionUnit.GetNodeAt(loc, n => n is Expression);
				if (expr == null) {
					return null;
				}
			}
			return new ExpressionResult(expr, completionUnit);
		}
		
		
#endregion
		
		#region Helper methods
		string GetPreviousToken(ref int i, bool allowLineChange)
		{
			char c;
			if (i <= 0) {
				return null;
			}
			
			do {
				c = document.GetCharAt(--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0) {
				return null;
			}
			
			if (!char.IsLetterOrDigit(c)) {
				return new string(c, 1);
			}
			
			int endOffset = i + 1;
			
			do {
				c = document.GetCharAt(i - 1);
				if (!(char.IsLetterOrDigit(c) || c == '_')) {
					break;
				}
				
				i--;
			} while (i > 0);
			
			return document.GetText(i, endOffset - i);
		}
		
#endregion
		
		#region Preprocessor
		
		IEnumerable<ICompletionData> GetDirectiveCompletionData()
		{
			yield return factory.CreateLiteralCompletionData("if");
			yield return factory.CreateLiteralCompletionData("else");
			yield return factory.CreateLiteralCompletionData("elif");
			yield return factory.CreateLiteralCompletionData("endif");
			yield return factory.CreateLiteralCompletionData("define");
			yield return factory.CreateLiteralCompletionData("undef");
			yield return factory.CreateLiteralCompletionData("warning");
			yield return factory.CreateLiteralCompletionData("error");
			yield return factory.CreateLiteralCompletionData("pragma");
			yield return factory.CreateLiteralCompletionData("line");
			yield return factory.CreateLiteralCompletionData("line hidden");
			yield return factory.CreateLiteralCompletionData("line default");
			yield return factory.CreateLiteralCompletionData("region");
			yield return factory.CreateLiteralCompletionData("endregion");
		}
#endregion
		
		#region Xml Comments
		static readonly List<string> commentTags = new List<string>(new string[] {
			"c",
			"code",
			"example",
			"exception",
			"include",
			"list",
			"listheader",
			"item",
			"term",
			"description",
			"para",
			"param",
			"paramref",
			"permission",
			"remarks",
			"returns",
			"see",
			"seealso",
			"summary",
			"value"
		}
			);
		
		string GetLastClosingXmlCommentTag()
		{
			var line = document.GetLineByNumber(location.Line);
			
			restart:
			string lineText = document.GetText(line);
			if (!lineText.Trim().StartsWith("///"))
				return null;
			int startIndex = Math.Min(location.Column - 1, lineText.Length - 1) - 1;
			while (startIndex > 0 && lineText [startIndex] != '<') {
				--startIndex;
				if (lineText [startIndex] == '/') {
					// already closed.
					startIndex = -1;
					break;
				}
			}
			if (startIndex < 0 && line.PreviousLine != null) {
				line = line.PreviousLine;
				goto restart;
			}
			
			if (startIndex >= 0) {
				int endIndex = startIndex;
				while (endIndex + 1 < lineText.Length && lineText [endIndex] != '>' && !Char.IsWhiteSpace (lineText [endIndex + 1])) {
					endIndex++;
				}
				string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring(
					startIndex + 1,
					endIndex - startIndex - 1
				) : null;
				if (!string.IsNullOrEmpty(tag) && commentTags.IndexOf(tag) >= 0) {
					return tag;
				}
			}
			return null;
		}
		
		IEnumerable<ICompletionData> GetXmlDocumentationCompletionData()
		{
			var closingTag = GetLastClosingXmlCommentTag();
			if (closingTag != null) {
				yield return factory.CreateLiteralCompletionData(
					"/" + closingTag + ">"
				);
			}
			
			yield return factory.CreateLiteralCompletionData(
				"c",
				"Set text in a code-like font"
			);
			yield return factory.CreateLiteralCompletionData(
				"code",
				"Set one or more lines of source code or program output"
			);
			yield return factory.CreateLiteralCompletionData(
				"example",
				"Indicate an example"
			);
			yield return factory.CreateLiteralCompletionData(
				"exception",
				"Identifies the exceptions a method can throw",
				"exception cref=\"|\"></exception>"
			);
			yield return factory.CreateLiteralCompletionData(
				"include",
				"Includes comments from a external file",
				"include file=\"|\" path=\"\">"
			);
			yield return factory.CreateLiteralCompletionData(
				"list",
				"Create a list or table",
				"list type=\"|\">"
			);
			yield return factory.CreateLiteralCompletionData(
				"listheader",
				"Define the heading row"
			);
			yield return factory.CreateLiteralCompletionData(
				"item",
				"Defines list or table item"
			);
			
			yield return factory.CreateLiteralCompletionData("term", "A term to define");
			yield return factory.CreateLiteralCompletionData(
				"description",
				"Describes a list item"
			);
			yield return factory.CreateLiteralCompletionData(
				"para",
				"Permit structure to be added to text"
			);
			
			yield return factory.CreateLiteralCompletionData(
				"param",
				"Describe a parameter for a method or constructor",
				"param name=\"|\">"
			);
			yield return factory.CreateLiteralCompletionData(
				"paramref",
				"Identify that a word is a parameter name",
				"paramref name=\"|\"/>"
			);
			
			yield return factory.CreateLiteralCompletionData(
				"permission",
				"Document the security accessibility of a member",
				"permission cref=\"|\""
			);
			yield return factory.CreateLiteralCompletionData(
				"remarks",
				"Describe a type"
			);
			yield return factory.CreateLiteralCompletionData(
				"returns",
				"Describe the return value of a method"
			);
			yield return factory.CreateLiteralCompletionData(
				"see",
				"Specify a link",
				"see cref=\"|\"/>"
			);
			yield return factory.CreateLiteralCompletionData(
				"seealso",
				"Generate a See Also entry",
				"seealso cref=\"|\"/>"
			);
			yield return factory.CreateLiteralCompletionData(
				"summary",
				"Describe a member of a type"
			);
			yield return factory.CreateLiteralCompletionData(
				"typeparam",
				"Describe a type parameter for a generic type or method"
			);
			yield return factory.CreateLiteralCompletionData(
				"typeparamref",
				"Identify that a word is a type parameter name"
			);
			yield return factory.CreateLiteralCompletionData(
				"value",
				"Describe a property"
			);
			
		}
#endregion
		
		#region Keywords
		static string[] expressionLevelKeywords = new string [] {
			"as",
			"is",
			"else",
			"out",
			"ref",
			"null",
			"delegate",
			"default"
		};
		static string[] primitiveTypesKeywords = new string [] {
			"void",
			"object",
			"bool",
			"byte",
			"sbyte",
			"char",
			"short",
			"int",
			"long",
			"ushort",
			"uint",
			"ulong",
			"float",
			"double",
			"decimal",
			"string"
		};
		static string[] statementStartKeywords = new string [] { "base", "new", "sizeof", "this", 
			"true", "false", "typeof", "checked", "unchecked", "from", "break", "checked",
			"unchecked", "const", "continue", "do", "finally", "fixed", "for", "foreach",
			"goto", "if", "lock", "return", "stackalloc", "switch", "throw", "try", "unsafe", 
			"using", "while", "yield",
			"catch"
		};
		static string[] globalLevelKeywords = new string [] {
			"namespace", "using", "extern", "public", "internal", 
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial"
		};
		static string[] accessorModifierKeywords = new string [] {
			"public", "internal", "protected", "private"
		};
		static string[] typeLevelKeywords = new string [] {
			"public", "internal", "protected", "private",
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial",
			"const", "event", "extern", "fixed","new", 
			"operator", "explicit", "implicit", 
			"override", "readonly", "virtual", "volatile"
		};
		static string[] linqKeywords = new string[] {
			"from",
			"where",
			"select",
			"group",
			"into",
			"orderby",
			"join",
			"let",
			"in",
			"on",
			"equals",
			"by",
			"ascending",
			"descending"
		};
		static string[] parameterTypePredecessorKeywords = new string[] {
			"out",
			"ref",
			"params"
		};
#endregion
	}
}

