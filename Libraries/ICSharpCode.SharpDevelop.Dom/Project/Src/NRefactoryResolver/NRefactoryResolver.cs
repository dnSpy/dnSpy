// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.SharpDevelop.Dom.CSharp;
using ICSharpCode.SharpDevelop.Dom.VBNet;
using NR = ICSharpCode.NRefactory;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	/// <summary>
	/// NRefactoryResolver implements the IResolver interface for the NRefactory languages (C# and VB).
	/// </summary>
	/// <remarks>
	/// About implementing code-completion for other languages:
	/// 
	/// It possible to convert from your AST to NRefactory (to C# or VB) (or even let your parser create
	/// NRefactory AST objects directly), but then code-completion might be incorrect when the rules of your language
	/// differ from the C#/VB language rules.
	/// If you want to correctly implement code-completion for your own language, you should implement your own resolver.
	/// </remarks>
	public class NRefactoryResolver : IResolver
	{
		ICompilationUnit cu;
		IClass callingClass;
		IMember callingMember;
		ICSharpCode.NRefactory.Visitors.LookupTableVisitor lookupTableVisitor;
		IProjectContent projectContent;
		
		readonly NR.SupportedLanguage language;
		
		int caretLine;
		int caretColumn;
		
		bool inferAllowed;
		internal bool allowMethodGroupResolveResult;
		
		public NR.SupportedLanguage Language {
			get {
				return language;
			}
		}
		
		public IProjectContent ProjectContent {
			get {
				return projectContent;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				projectContent = value;
			}
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return cu;
			}
		}
		
		public IClass CallingClass {
			get {
				return callingClass;
			}
		}
		
		public IMember CallingMember {
			get {
				return callingMember;
			}
		}
		
		public int CaretLine {
			get {
				return caretLine;
			}
		}
		
		public int CaretColumn {
			get {
				return caretColumn;
			}
		}
		
		readonly LanguageProperties languageProperties;
		
		public LanguageProperties LanguageProperties {
			get {
				return languageProperties;
			}
		}
		
		public NRefactoryResolver(LanguageProperties languageProperties)
		{
			if (languageProperties == null)
				throw new ArgumentNullException("languageProperties");
			this.languageProperties = languageProperties;
			if (languageProperties is LanguageProperties.CSharpProperties) {
				language = NR.SupportedLanguage.CSharp;
				inferAllowed = true;
				allowMethodGroupResolveResult = true;
			} else if (languageProperties is LanguageProperties.VBNetProperties) {
				language = NR.SupportedLanguage.VBNet;
				inferAllowed = false;
				allowMethodGroupResolveResult = false;
			} else {
				throw new NotSupportedException("The language " + languageProperties.ToString() + " is not supported in the NRefactoryResolver");
			}
		}
		
		Expression ParseExpression(string expression, int caretColumnOffset)
		{
			Expression expr = SpecialConstructs(expression);
			if (expr == null) {
				// SEMICOLON HACK: Parsing expressions without trailing semicolon does not work correctly
				if (language == NR.SupportedLanguage.CSharp && !expression.EndsWith(";"))
					expression += ";";
				using (NR.IParser p = NR.ParserFactory.CreateParser(language, new System.IO.StringReader(expression))) {
					p.Lexer.SetInitialLocation(new NR.Location(caretColumn + caretColumnOffset, caretLine));
					expr = p.ParseExpression();
				}
			}
			return expr;
		}

		
		string GetFixedExpression(ExpressionResult expressionResult)
		{
			string expression = expressionResult.Expression;
			if (expression == null) {
				expression = "";
			}
			expression = expression.TrimStart();
			
			return expression;
		}
		
		public bool Initialize(ParseInformation parseInfo, int caretLine, int caretColumn)
		{
			this.caretLine   = caretLine;
			this.caretColumn = caretColumn;
			callingClass = null;
			callingMember = null;
			
			if (parseInfo == null) {
				return false;
			}
			
			cu = parseInfo.CompilationUnit;
			if (cu == null || cu.ProjectContent == null) {
				return false;
			}
			this.ProjectContent = cu.ProjectContent;
			
			if (language == SupportedLanguage.VBNet) {
				IVBNetOptionProvider provider = (IVBNetOptionProvider)cu;
				
				inferAllowed = provider.OptionInfer ?? false;
			}
			
			callingClass = cu.GetInnermostClass(caretLine, caretColumn);
			callingMember = GetCallingMember();
			return true;
		}
		
		public ResolveResult Resolve(ExpressionResult expressionResult,
		                             ParseInformation parseInfo,
		                             string fileContent)
		{
			string expression = GetFixedExpression(expressionResult);
			
			if (!Initialize(parseInfo, expressionResult.Region.BeginLine, expressionResult.Region.BeginColumn))
				return null;
			
			Expression expr = null;
			if (language == NR.SupportedLanguage.VBNet) {
				if (expression.Length == 0 || expression[0] == '.' && (expression.Length > 1 && !char.IsDigit(expression[1]))) {
					return WithResolve(expression, fileContent);
				} else if ("global".Equals(expression, StringComparison.InvariantCultureIgnoreCase)) {
					return new NamespaceResolveResult(null, null, "");
				}
				// array
			}
			if (expressionResult.Context.IsTypeContext && !expressionResult.Context.IsObjectCreation) {
				expr = ParseTypeReference(expression, language);
			}
			if (expr == null) {
				expr = ParseExpression(expression, 0);
				if (expr == null) {
					return null;
				}
				
				// "new" is missing
				if (expressionResult.Context.IsObjectCreation && !(expr is ObjectCreateExpression)) {
					Expression tmp = expr;
					while (tmp != null) {
						if (tmp is IdentifierExpression)
							return ResolveInternal(expr, ExpressionContext.Type);
						if (tmp is MemberReferenceExpression)
							tmp = (tmp as MemberReferenceExpression).TargetObject;
						else
							break;
					}
					expr = ParseExpression("new " + expression, -4);
					if (expr == null) {
						return null;
					}
				}
			}
			
			ResolveResult rr;
			if (expressionResult.Context == ExpressionContext.Attribute) {
				return ResolveAttribute(expr, new NR.Location(caretColumn, caretLine));
			} else if (expressionResult.Context == CSharpExpressionContext.ObjectInitializer && expr is IdentifierExpression) {
				bool isCollectionInitializer;
				rr = ResolveObjectInitializer((expr as IdentifierExpression).Identifier, fileContent, out isCollectionInitializer);
				if (!isCollectionInitializer || rr != null) {
					return rr;
				}
			}
			
			RunLookupTableVisitor(fileContent);
			
			rr = CtrlSpaceResolveHelper.GetResultFromDeclarationLine(callingClass, callingMember as IMethodOrProperty, caretLine, caretColumn, expressionResult);
			if (rr != null) return rr;
			
			return ResolveInternal(expr, expressionResult.Context);
		}
		
		TypeReferenceExpression ParseTypeReference(string typeReference, SupportedLanguage language)
		{
			string typeOfFunc = "typeof";
			
			if (language == SupportedLanguage.VBNet)
				typeOfFunc = "GetType";
			
			TypeOfExpression toe = ParseExpression(typeOfFunc + "(" + typeReference + ")", -(typeOfFunc.Length + 1)) as TypeOfExpression;
			
			if (toe != null)
				return new TypeReferenceExpression(toe.TypeReference);
			
			return null;
		}
		
		ResolveResult WithResolve(string expression, string fileContent)
		{
			RunLookupTableVisitor(fileContent);
			
			WithStatement innermost = null;
			if (lookupTableVisitor.WithStatements != null) {
				foreach (WithStatement with in lookupTableVisitor.WithStatements) {
					if (IsInside(new NR.Location(caretColumn, caretLine), with.StartLocation, with.EndLocation)) {
						innermost = with;
					}
				}
			}
			if (innermost != null) {
				if (expression.Length > 1) {
					Expression expr = ParseExpression(DummyFindVisitor.dummyName + expression, -DummyFindVisitor.dummyName.Length);
					if (expr == null) return null;
					DummyFindVisitor v = new DummyFindVisitor();
					expr.AcceptVisitor(v, null);
					if (v.result == null) return null;
					v.result.TargetObject = innermost.Expression;
					return ResolveInternal(expr, ExpressionContext.Default);
				} else {
					return ResolveInternal(innermost.Expression, ExpressionContext.Default);
				}
			} else {
				return null;
			}
		}
		private class DummyFindVisitor : AbstractAstVisitor {
			internal const string dummyName = "___withStatementExpressionDummy";
			internal MemberReferenceExpression result;
			public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
			{
				IdentifierExpression ie = fieldReferenceExpression.TargetObject as IdentifierExpression;
				if (ie != null && ie.Identifier == dummyName)
					result = fieldReferenceExpression;
				return base.VisitMemberReferenceExpression(fieldReferenceExpression, data);
			}
		}
		
		public INode ParseCurrentMember(string fileContent)
		{
			CompilationUnit cu = ParseCurrentMemberAsCompilationUnit(fileContent);
			if (cu != null && cu.Children.Count > 0) {
				TypeDeclaration td = cu.Children[0] as TypeDeclaration;
				if (td != null && td.Children.Count > 0) {
					return td.Children[0];
				}
			}
			return null;
		}
		
		public CompilationUnit ParseCurrentMemberAsCompilationUnit(string fileContent)
		{
			System.IO.TextReader content = ExtractCurrentMethod(fileContent);
			if (content != null) {
				NR.IParser p = NR.ParserFactory.CreateParser(language, content);
				p.Parse();
				return p.CompilationUnit;
			} else {
				return null;
			}
		}
		
		void RunLookupTableVisitor(string fileContent)
		{
			lookupTableVisitor = new LookupTableVisitor(language);
			
			if (callingMember != null) {
				CompilationUnit cu = ParseCurrentMemberAsCompilationUnit(fileContent);
				if (cu != null) {
					lookupTableVisitor.VisitCompilationUnit(cu, null);
				}
			}
		}
		
		public void RunLookupTableVisitor(INode currentMemberNode)
		{
			if (currentMemberNode == null)
				throw new ArgumentNullException("currentMemberNode");
			lookupTableVisitor = new LookupTableVisitor(language);
			currentMemberNode.AcceptVisitor(lookupTableVisitor, null);
		}
		
		string GetAttributeName(Expression expr)
		{
			if (expr is IdentifierExpression) {
				return (expr as IdentifierExpression).Identifier;
			} else if (expr is MemberReferenceExpression) {
				ResolveVisitor typeVisitor = new ResolveVisitor(this);
				MemberReferenceExpression fieldReferenceExpression = (MemberReferenceExpression)expr;
				ResolveResult rr = typeVisitor.Resolve(fieldReferenceExpression.TargetObject);
				if (rr is NamespaceResolveResult) {
					return ((NamespaceResolveResult)rr).Name + "." + fieldReferenceExpression.MemberName;
				}
			} else if (expr is TypeReferenceExpression) {
				return (expr as TypeReferenceExpression).TypeReference.Type;
			}
			return null;
		}
		
		IClass GetAttribute(string name, NR.Location position)
		{
			if (name == null)
				return null;
			IClass c = SearchClass(name, 0, position);
			if (c != null) {
				if (c.IsTypeInInheritanceTree(c.ProjectContent.SystemTypes.Attribute.GetUnderlyingClass()))
					return c;
			}
			return SearchClass(name + "Attribute", 0, position);
		}
		
		ResolveResult ResolveAttribute(Expression expr, NR.Location position)
		{
			string attributeName = GetAttributeName(expr);
			if (attributeName != null) {
				IClass c = GetAttribute(attributeName, position);
				if (c != null) {
					return new TypeResolveResult(callingClass, callingMember, c);
				} else {
					string ns = SearchNamespace(attributeName, position);
					if (ns != null) {
						return new NamespaceResolveResult(callingClass, callingMember, ns);
					}
				}
				return new UnknownIdentifierResolveResult(callingClass, callingMember, attributeName);
			} else if (expr is InvocationExpression) {
				InvocationExpression ie = (InvocationExpression)expr;
				attributeName = GetAttributeName(ie.TargetObject);
				IClass c = GetAttribute(attributeName, position);
				if (c != null) {
					ResolveVisitor resolveVisitor = new ResolveVisitor(this);
					return resolveVisitor.ResolveConstructorOverload(c, ie.Arguments);
				}
				return CreateUnknownMethodResolveResult(expr as InvocationExpression);
			}
			return null;
		}
		
		internal UnknownMethodResolveResult CreateUnknownMethodResolveResult(InvocationExpression invocationExpression)
		{
			List<IReturnType> arguments = new List<IReturnType>();
			ResolveVisitor resolveVisitor = new ResolveVisitor(this);
			
			foreach (Expression ex in invocationExpression.Arguments) {
				ResolveResult rr = resolveVisitor.Resolve(ex);
				if (rr != null)
					arguments.Add(rr.ResolvedType ?? UnknownReturnType.Instance);
				else
					arguments.Add(UnknownReturnType.Instance);
			}

			IReturnType target = null;
			
			if (callingClass != null)
				target = callingClass.DefaultReturnType;
			
			string methodName = "";
			bool isStatic = false;
			
			if (invocationExpression.TargetObject is IdentifierExpression) {
				IdentifierExpression ie = invocationExpression.TargetObject as IdentifierExpression;
				methodName = ie.Identifier;
				isStatic = callingMember != null && callingMember.Modifiers.HasFlag(ModifierEnum.Static);
			}
			
			if (invocationExpression.TargetObject is MemberReferenceExpression) {
				MemberReferenceExpression mre = invocationExpression.TargetObject as MemberReferenceExpression;
				var rr = resolveVisitor.Resolve(mre.TargetObject);
				isStatic = rr is TypeResolveResult;
				if (rr != null)
					target = rr.ResolvedType;
				methodName = mre.MemberName;
			}
			
			return new UnknownMethodResolveResult(callingClass, callingMember, target, methodName, isStatic, arguments);
		}
		
		public ResolveResult ResolveInternal(Expression expr, ExpressionContext context)
		{
			if (projectContent == null)
				throw new InvalidOperationException("Cannot use uninitialized resolver");
			
			// we need to special-case this to pass the context to ResolveIdentifier
			if (expr is IdentifierExpression)
				return ResolveIdentifier(expr as IdentifierExpression, context);
			
			ResolveVisitor resolveVisitor = new ResolveVisitor(this);
			return resolveVisitor.Resolve(expr);
		}
		
		/// <summary>
		/// Used for the fix for SD2-511.
		/// </summary>
		public int LimitMethodExtractionUntilLine;
		
		public TextReader ExtractCurrentMethod(string fileContent)
		{
			if (callingMember == null)
				return null;
			return ExtractMethod(fileContent, callingMember, language, LimitMethodExtractionUntilLine);
		}
		
		/// <summary>
		/// Creates a new class containing only the specified member.
		/// This is useful because we only want to parse current method for local variables,
		/// as all fields etc. are already prepared in the AST.
		/// </summary>
		public static TextReader ExtractMethod(string fileContent, IMember member,
		                                       NR.SupportedLanguage language, int extractUntilLine)
		{
			// As the parse information is always some seconds old, the end line could be wrong
			// if the user just inserted a line in the method.
			// We can ignore that case because it is sufficient for the parser when the first part of the
			// method body is ok.
			// Since we are operating directly on the edited buffer, the parser might not be
			// able to resolve invalid declarations.
			// We can ignore even that because the 'invalid line' is the line the user is currently
			// editing, and the declarations he is using are always above that line.
			
			
			// The ExtractMethod-approach has the advantage that the method contents do not have
			// do be parsed and stored in memory before they are needed.
			// Previous SharpDevelop versions always stored the SharpRefactory[VB] parse tree as 'Tag'
			// to the AST CompilationUnit.
			// This approach doesn't need that, so one could even go and implement a special parser
			// mode that does not parse the method bodies for the normal run (in the ParserUpdateThread or
			// SolutionLoadThread). That could improve the parser's speed dramatically.
			
			if (member.Region.IsEmpty) return null;
			int startLine = member.Region.BeginLine;
			if (startLine < 1) return null;
			DomRegion bodyRegion = member.BodyRegion;
			if (bodyRegion.IsEmpty) bodyRegion = member.Region;
			int endLine = bodyRegion.EndLine;
			
			// Fix for SD2-511 (Code completion in inserted line)
			if (extractUntilLine > startLine && extractUntilLine < endLine)
				endLine = extractUntilLine;
			
			int offset = 0;
			for (int i = 0; i < startLine - 1; ++i) { // -1 because the startLine must be included
				offset = fileContent.IndexOf('\n', offset) + 1;
				if (offset <= 0) return null;
			}
			int startOffset = offset;
			for (int i = startLine - 1; i < endLine; ++i) {
				int newOffset = fileContent.IndexOf('\n', offset) + 1;
				if (newOffset <= 0) break;
				offset = newOffset;
			}
			int length = offset - startOffset;
			string classDecl, endClassDecl;
			if (language == NR.SupportedLanguage.VBNet) {
				classDecl = "Class A";
				endClassDecl = "End Class\n";
			} else {
				classDecl = "class A {";
				endClassDecl = "}\n";
			}
			System.Text.StringBuilder b = new System.Text.StringBuilder(classDecl, length + classDecl.Length + endClassDecl.Length + startLine - 1);
			b.Append('\n', startLine - 1);
			b.Append(fileContent, startOffset, length);
			b.Append(endClassDecl);
			return new System.IO.StringReader(b.ToString());
		}
		
		#region Resolve Identifier
		internal IReturnType ConstructType(IReturnType baseType, List<TypeReference> typeArguments)
		{
			if (typeArguments == null || typeArguments.Count == 0)
				return baseType;
			return new ConstructedReturnType(baseType,
			                                 typeArguments.ConvertAll(r => TypeVisitor.CreateReturnType(r, this)));
		}
		
		public ResolveResult ResolveIdentifier(IdentifierExpression expr, ExpressionContext context)
		{
			ResolveResult result = ResolveIdentifierInternal(expr);
			if (result is TypeResolveResult)
				return result;
			
			NR.Location position = expr.StartLocation;
			string identifier = expr.Identifier;
			ResolveResult result2 = null;
			
			if (callingClass != null) {
				if (callingMember is IMethod) {
					foreach (ITypeParameter typeParameter in (callingMember as IMethod).TypeParameters) {
						if (IsSameName(identifier, typeParameter.Name)) {
							return new TypeResolveResult(callingClass, callingMember, new GenericReturnType(typeParameter));
						}
					}
				}
				foreach (ITypeParameter typeParameter in callingClass.TypeParameters) {
					if (IsSameName(identifier, typeParameter.Name)) {
						return new TypeResolveResult(callingClass, callingMember, new GenericReturnType(typeParameter));
					}
				}
			}
			
			SearchTypeResult typeResult = SearchType(identifier, expr.TypeArguments.Count, position);
			IReturnType t = typeResult.Result;
			if (t != null) {
				result2 = new TypeResolveResult(callingClass, callingMember, ConstructType(t, expr.TypeArguments));
			} else if (result == null && typeResult.NamespaceResult != null) {
				return new NamespaceResolveResult(callingClass, callingMember, typeResult.NamespaceResult);
			}
			
			if (result == null && result2 == null)
				return new UnknownIdentifierResolveResult(CallingClass, CallingMember, expr.Identifier);

			if (result == null)  return result2;
			if (result2 == null) return result;
			if (context == ExpressionContext.Type)
				return result2;
			return new MixedResolveResult(result, result2);
		}
		
		public ResolveResult ResolveIdentifier(string identifier, NR.Location position, ExpressionContext context)
		{
			return ResolveIdentifier(new IdentifierExpression(identifier) { StartLocation = position }, context);
		}
		
		IField CreateLocalVariableField(LocalLookupVariable variable)
		{
			IReturnType type = GetVariableType(variable);
			var f = new DefaultField.LocalVariableField(type, variable.Name, DomRegion.FromLocation(variable.StartPos, variable.EndPos), callingClass);
			if (variable.IsConst) {
				f.Modifiers |= ModifierEnum.Const;
			}
			return f;
		}
		
		ResolveResult ResolveIdentifierInternal(IdentifierExpression identifierExpression)
		{
			NR.Location position = identifierExpression.StartLocation;
			string identifier = identifierExpression.Identifier;
			if (callingMember != null) { // LocalResolveResult requires callingMember to be set
				LocalLookupVariable var = SearchVariable(identifier, position);
				if (var != null) {
					return new LocalResolveResult(callingMember, CreateLocalVariableField(var));
				}
				IParameter para = SearchMethodParameter(identifier);
				if (para != null) {
					IField field = new DefaultField.ParameterField(para.ReturnType, para.Name, para.Region, callingClass);
					return new LocalResolveResult(callingMember, field);
				}
				if (IsSameName(identifier, "value")) {
					IProperty property = callingMember as IProperty;
					if (property != null && property.SetterRegion.IsInside(position.Line, position.Column)
					    || callingMember is IEvent)
					{
						IField field = new DefaultField.ParameterField(callingMember.ReturnType, "value", callingMember.Region, callingClass);
						return new LocalResolveResult(callingMember, field);
					}
				}
			}
			if (callingClass != null) {
				IClass tmp = callingClass;
				do {
					ResolveResult rr = ResolveMember(tmp.DefaultReturnType, identifier,
					                                 identifierExpression.TypeArguments,
					                                 IsInvoked(identifierExpression),
					                                 false, true);
					if (rr != null && rr.IsValid)
						return rr;
					// also try to resolve the member in outer classes
					tmp = tmp.DeclaringType;
				} while (tmp != null);
			}
			
			if (languageProperties.CanImportClasses) {
				IUsingScope scope = callingClass != null ? callingClass.UsingScope : cu.UsingScope;
				for (; scope != null; scope = scope.Parent) {
					foreach (IUsing @using in scope.Usings) {
						foreach (string import in @using.Usings) {
							IClass c = GetClass(import, 0);
							if (c != null) {
								ResolveResult rr = ResolveMember(c.DefaultReturnType, identifier,
								                                 identifierExpression.TypeArguments,
								                                 IsInvoked(identifierExpression),
								                                 false, null);
								if (rr != null && rr.IsValid)
									return rr;
							}
						}
					}
				}
			}
			
			if (languageProperties.ImportModules) {
				List<ICompletionEntry> list = new List<ICompletionEntry>();
				CtrlSpaceResolveHelper.AddImportedNamespaceContents(list, cu, callingClass);
				List<IMember> resultMembers = new List<IMember>();
				foreach (ICompletionEntry o in list) {
					IClass c = o as IClass;
					if (c != null && IsSameName(identifier, c.Name)) {
						return new TypeResolveResult(callingClass, callingMember, c);
					}
					IMember member = o as IMember;
					if (member != null && IsSameName(identifier, member.Name)) {
						resultMembers.Add(member);
					}
				}
				return CreateMemberOrMethodGroupResolveResult(null, identifier, new IList<IMember>[] { resultMembers }, false, null);
			}
			
			return null;
		}
		#endregion
		
		private ResolveResult CreateMemberResolveResult(IMember member)
		{
			if (member == null) return null;
			return new MemberResolveResult(callingClass, callingMember, member);
		}
		
		#region ResolveMember
		internal ResolveResult ResolveMember(IReturnType declaringType, string memberName,
		                                     List<TypeReference> typeArguments, bool isInvocation,
		                                     bool allowExtensionMethods, bool? isAccessThoughReferenceOfCurrentClass)
		{
			// in VB everything can be used with invocation syntax (because the same syntax also accesses indexers),
			// don't use 'isInvocation'.
			if (language == NR.SupportedLanguage.VBNet)
				isInvocation = false;
			
			List<IList<IMember>> members = MemberLookupHelper.LookupMember(declaringType, memberName, callingClass, languageProperties, isInvocation, isAccessThoughReferenceOfCurrentClass);
			List<IReturnType> typeArgs = null;
			if (members != null && typeArguments != null && typeArguments.Count != 0) {
				typeArgs = typeArguments.ConvertAll(r => TypeVisitor.CreateReturnType(r, this));
				
				// For all member-groups:
				// Remove all non-methods and methods with incorrect type argument count, then
				// apply the type arguments to the remaining methods.
				members = members.Select(memberGroup => ApplyTypeArgumentsToMethods(memberGroup, typeArgs))
					.Where(memberGroup => memberGroup.Count > 0) // keep only non-empty groups
					.ToList();
			}
			if (language == NR.SupportedLanguage.VBNet && members != null && members.Count > 0) {
				// use the correct casing of the member name
				memberName = members[0][0].Name;
			}
			return CreateMemberOrMethodGroupResolveResult(declaringType, memberName, members, allowExtensionMethods, typeArgs);
		}
		
		static IList<IMember> ApplyTypeArgumentsToMethods(IList<IMember> memberGroup, IList<IReturnType> typeArgs)
		{
			if (typeArgs == null || typeArgs.Count == 0)
				return memberGroup;
			else
				return ApplyTypeArgumentsToMethods(memberGroup.OfType<IMethod>(), typeArgs).Select(m=>(IMember)m).ToList();
		}
		
		static IEnumerable<IMethod> ApplyTypeArgumentsToMethods(IEnumerable<IMethod> methodGroup, IList<IReturnType> typeArgs)
		{
			if (typeArgs == null || typeArgs.Count == 0)
				return methodGroup;
			// we have type arguments, so:
			// - remove all non-methods
			// - remove all methods with incorrect type parameter count
			// - apply type arguments to remaining methods
			return methodGroup
				.Where((IMethod m) => m.TypeParameters.Count == typeArgs.Count)
				.Select((IMethod originalMethod) => {
				        	IMethod m = (IMethod)originalMethod.CreateSpecializedMember();
				        	m.ReturnType = ConstructedReturnType.TranslateType(m.ReturnType, typeArgs, true);
				        	for (int j = 0; j < m.Parameters.Count; ++j) {
				        		m.Parameters[j].ReturnType = ConstructedReturnType.TranslateType(m.Parameters[j].ReturnType, typeArgs, true);
				        	}
				        	return m;
				        });
		}
		
		/// <summary>
		/// Creates a MemberResolveResult or MethodGroupResolveResult.
		/// </summary>
		/// <param name="declaringType">The type on which to search the member.</param>
		/// <param name="memberName">The name of the member.</param>
		/// <param name="members">The list of members to put into the MethodGroupResolveResult</param>
		/// <param name="allowExtensionMethods">Whether to add extension methods to the resolve result</param>
		/// <param name="typeArgs">Type arguments to apply to the extension methods</param>
		internal ResolveResult CreateMemberOrMethodGroupResolveResult(
			IReturnType declaringType, string memberName, IList<IList<IMember>> members,
			bool allowExtensionMethods, IList<IReturnType> typeArgs)
		{
			List<MethodGroup> methods = new List<MethodGroup>();
			if (members != null) {
				foreach (IList<IMember> memberGroup in members) {
					if (memberGroup != null && memberGroup.Count > 0) {
						MethodGroup methodGroup = new MethodGroup();
						foreach (IMember m in memberGroup) {
							if (m is IMethod)
								methodGroup.Add(m as IMethod);
							else
								return new MemberResolveResult(callingClass, callingMember, m);
						}
						methods.Add(methodGroup);
					}
				}
			}
			if (allowExtensionMethods == false || declaringType == null) {
				if (methods.Count == 0)
					return null;
				else
					return new MethodGroupResolveResult(callingClass, callingMember,
					                                    declaringType ?? methods[0][0].DeclaringTypeReference,
					                                    memberName, methods, language == SupportedLanguage.VBNet, allowMethodGroupResolveResult) { IsVBNetAddressOf = allowMethodGroupResolveResult && language == SupportedLanguage.VBNet };
			} else {
				methods.Add(
					new MethodGroup(
						new LazyList<IMethod>(() => ApplyTypeArgumentsToMethods(SearchExtensionMethods(memberName), typeArgs).ToList())
					)
					{ IsExtensionMethodGroup = true });
				return new MethodGroupResolveResult(callingClass, callingMember,
				                                    declaringType,
				                                    memberName, methods, language == SupportedLanguage.VBNet, allowMethodGroupResolveResult) { IsVBNetAddressOf = allowMethodGroupResolveResult && language == SupportedLanguage.VBNet };
			}
		}
		#endregion
		
		#region Resolve In Object Initializer
		ResolveResult ResolveObjectInitializer(string identifier, string fileContent, out bool isCollectionInitializer)
		{
			foreach (IMember m in ObjectInitializerCtrlSpace(fileContent, out isCollectionInitializer)) {
				if (IsSameName(m.Name, identifier))
					return CreateMemberResolveResult(m);
			}
			return null;
		}
		
		List<IMember> ObjectInitializerCtrlSpace(string fileContent, out bool isCollectionInitializer)
		{
			isCollectionInitializer = true;
			if (callingMember == null) {
				return new List<IMember>();
			}
			CompilationUnit parsedCu = ParseCurrentMemberAsCompilationUnit(fileContent);
			if (parsedCu == null) {
				return new List<IMember>();
			}
			return ObjectInitializerCtrlSpace(parsedCu, new NR.Location(caretColumn, caretLine), out isCollectionInitializer);
		}
		
		List<IMember> ObjectInitializerCtrlSpace(CompilationUnit parsedCu, NR.Location location, out bool isCollectionInitializer)
		{
			FindObjectInitializerExpressionContainingCaretVisitor v = new FindObjectInitializerExpressionContainingCaretVisitor(location);
			parsedCu.AcceptVisitor(v, null);
			return ObjectInitializerCtrlSpace(v.result, out isCollectionInitializer);
		}
		
		List<IMember> ObjectInitializerCtrlSpace(CollectionInitializerExpression collectionInitializer, out bool isCollectionInitializer)
		{
			isCollectionInitializer = true;
			List<IMember> results = new List<IMember>();
			if (collectionInitializer != null) {
				ObjectCreateExpression oce = collectionInitializer.Parent as ObjectCreateExpression;
				MemberInitializerExpression mie = collectionInitializer.Parent as MemberInitializerExpression;
				if (oce != null && !oce.IsAnonymousType) {
					IReturnType resolvedType = TypeVisitor.CreateReturnType(oce.CreateType, this);
					ObjectInitializerCtrlSpaceInternal(results, resolvedType, out isCollectionInitializer);
				}
				else if (mie != null) {
					IMember member = ResolveMemberInitializerExpressionInObjectInitializer(mie);
					if (member != null) {
						ObjectInitializerCtrlSpaceInternal(results, member.ReturnType, out isCollectionInitializer);
					}
				}
			}
			return results;
		}
		
		IMember ResolveMemberInitializerExpressionInObjectInitializer(MemberInitializerExpression mie)
		{
			CollectionInitializerExpression parentCI = mie.Parent as CollectionInitializerExpression;
			bool tmp;
			return ObjectInitializerCtrlSpace(parentCI, out tmp).Find(m => IsSameName(m.Name, mie.Name));
		}
		
		void ObjectInitializerCtrlSpaceInternal(List<IMember> results, IReturnType resolvedType, out bool isCollectionInitializer)
		{
			isCollectionInitializer = MemberLookupHelper.ConversionExists(resolvedType, new GetClassReturnType(projectContent, "System.Collections.IEnumerable", 0));
			if (resolvedType != null) {
				bool isClassInInheritanceTree = false;
				if (callingClass != null)
					isClassInInheritanceTree = callingClass.IsTypeInInheritanceTree(resolvedType.GetUnderlyingClass());
				foreach (IField f in resolvedType.GetFields()) {
					if (languageProperties.ShowMember(f, false)
					    && f.IsAccessible(callingClass, isClassInInheritanceTree)
					    && !(f.IsReadonly && IsValueType(f.ReturnType)))
					{
						results.Add(f);
					}
				}
				foreach (IProperty p in resolvedType.GetProperties()) {
					if (languageProperties.ShowMember(p, false)
					    && p.IsAccessible(callingClass, isClassInInheritanceTree)
					    && !(p.CanSet == false && IsValueType(p.ReturnType)))
					{
						results.Add(p);
					}
				}
			}
		}
		
		static bool IsValueType(IReturnType rt)
		{
			if (rt == null)
				return false;
			IClass c = rt.GetUnderlyingClass();
			return c != null && (c.ClassType == ClassType.Struct || c.ClassType == ClassType.Enum);
		}
		
		// Finds the inner most CollectionInitializerExpression containing the specified caret position
		sealed class FindObjectInitializerExpressionContainingCaretVisitor : AbstractAstVisitor
		{
			NR.Location caretPosition;
			internal CollectionInitializerExpression result;
			
			public FindObjectInitializerExpressionContainingCaretVisitor(ICSharpCode.NRefactory.Location caretPosition)
			{
				this.caretPosition = caretPosition;
			}
			
			public override object VisitCollectionInitializerExpression(CollectionInitializerExpression collectionInitializerExpression, object data)
			{
				base.VisitCollectionInitializerExpression(collectionInitializerExpression, data);
				if (result == null
				    && collectionInitializerExpression.StartLocation <= caretPosition
				    && collectionInitializerExpression.EndLocation >= caretPosition)
				{
					result = collectionInitializerExpression;
				}
				return null;
			}
		}
		#endregion
		
		Expression SpecialConstructs(string expression)
		{
			if (language == NR.SupportedLanguage.VBNet) {
				// MyBase and MyClass are no expressions, only MyBase.Identifier and MyClass.Identifier
				if ("mybase".Equals(expression, StringComparison.InvariantCultureIgnoreCase)) {
					return new BaseReferenceExpression();
				} else if ("myclass".Equals(expression, StringComparison.InvariantCultureIgnoreCase)) {
					return new ClassReferenceExpression();
				} // Global is handled in Resolve() because we don't need an expression for that
			}
			return null;
		}
		
		public bool IsSameName(string name1, string name2)
		{
			return languageProperties.NameComparer.Equals(name1, name2);
		}
		
		bool IsInside(NR.Location between, NR.Location start, NR.Location end)
		{
			if (between.Y < start.Y || between.Y > end.Y) {
				return false;
			}
			if (between.Y > start.Y) {
				if (between.Y < end.Y) {
					return true;
				}
				// between.Y == end.Y
				return between.X <= end.X;
			}
			// between.Y == start.Y
			if (between.X < start.X) {
				return false;
			}
			// start is OK and between.Y <= end.Y
			return between.Y < end.Y || between.X <= end.X;
		}
		
		IMember GetCallingMember()
		{
			if (callingClass == null)
				return null;
			foreach (IMethod method in callingClass.Methods) {
				if (method.Region.IsInside(caretLine, caretColumn) || method.BodyRegion.IsInside(caretLine, caretColumn)) {
					return method;
				}
			}
			foreach (IProperty property in callingClass.Properties) {
				if (property.Region.IsInside(caretLine, caretColumn) || property.BodyRegion.IsInside(caretLine, caretColumn)) {
					return property;
				}
			}
			foreach (IEvent ev in callingClass.Events) {
				if (ev.Region.IsInside(caretLine, caretColumn) || ev.BodyRegion.IsInside(caretLine, caretColumn)) {
					return ev;
				}
			}
			foreach (IField f in callingClass.Fields) {
				if (f.Region.IsInside(caretLine, caretColumn) || f.BodyRegion.IsInside(caretLine, caretColumn)) {
					return f;
				}
			}
			return null;
		}
		
		/// <remarks>
		/// use the usings to find the correct name of a namespace
		/// </remarks>
		public string SearchNamespace(string name, NR.Location position)
		{
			return SearchType(name, 0, position).NamespaceResult;
		}
		
		public IClass GetClass(string fullName, int typeArgumentCount)
		{
			return projectContent.GetClass(fullName, typeArgumentCount);
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchClass(string name, int typeArgumentCount, NR.Location position)
		{
			IReturnType t = SearchType(name, typeArgumentCount, position).Result;
			return (t != null) ? t.GetUnderlyingClass() : null;
		}
		
		public SearchTypeResult SearchType(string name, int typeArgumentCount, NR.Location position)
		{
			if (position.IsEmpty)
				return projectContent.SearchType(new SearchTypeRequest(name, typeArgumentCount, callingClass, cu, caretLine, caretColumn));
			else
				return projectContent.SearchType(new SearchTypeRequest(name, typeArgumentCount, callingClass, cu, position.Line, position.Column));
		}
		
		public IList<IMethod> SearchExtensionMethods(string name)
		{
			List<IMethod> results = new List<IMethod>();
			foreach (IMethodOrProperty m in SearchAllExtensionMethods()) {
				if (IsSameName(name, m.Name)) {
					results.Add((IMethod)m);
				}
			}
			return results;
		}
		
		ReadOnlyCollection<IMethodOrProperty> cachedExtensionMethods;
		IClass cachedExtensionMethods_LastClass; // invalidate cache when callingClass != LastClass
		
		public ReadOnlyCollection<IMethodOrProperty> SearchAllExtensionMethods(bool searchInAllNamespaces = false)
		{
			if (callingClass == null)
				return EmptyList<IMethodOrProperty>.Instance;
			if (callingClass != cachedExtensionMethods_LastClass) {
				cachedExtensionMethods_LastClass = callingClass;
				cachedExtensionMethods = new ReadOnlyCollection<IMethodOrProperty>(CtrlSpaceResolveHelper.FindAllExtensions(languageProperties, callingClass, searchInAllNamespaces));
			}
			return cachedExtensionMethods;
		}
		
		#region DynamicLookup
		IParameter SearchMethodParameter(string parameter)
		{
			IMethodOrProperty method = callingMember as IMethodOrProperty;
			if (method == null) {
				return null;
			}
			foreach (IParameter p in method.Parameters) {
				if (IsSameName(p.Name, parameter)) {
					return p;
				}
			}
			return null;
		}
		
		Dictionary<LocalLookupVariable, IReturnType> variableReturnTypeCache = new Dictionary<LocalLookupVariable, IReturnType>();
		
		IReturnType GetVariableType(LocalLookupVariable v)
		{
			if (v == null) {
				return null;
			}
			
			if (v.ParentLambdaExpression != null && (v.TypeRef == null || v.TypeRef.IsNull)) {
				IReturnType[] lambdaParameterTypes = GetImplicitLambdaParameterTypes(v.ParentLambdaExpression);
				if (lambdaParameterTypes != null) {
					int parameterIndex = v.ParentLambdaExpression.Parameters.FindIndex(p => p.ParameterName == v.Name);
					if (parameterIndex >= 0 && parameterIndex < lambdaParameterTypes.Length) {
						return lambdaParameterTypes[parameterIndex];
					}
				}
			}
			
			// Don't create multiple IReturnType's for the same local variable.
			// This is required to ensure that the protection against infinite recursion
			// for type inference cycles in InferredReturnType works correctly.
			// It also helps performance if we infer every local variable only once.
			IReturnType rt;
			if (variableReturnTypeCache.TryGetValue(v, out rt))
				return rt;
			
			if (v.TypeRef == null || v.TypeRef.IsNull || v.TypeRef.Type == "var") {
				if (inferAllowed) {
					if (v.ParentLambdaExpression != null) {
						rt = new LambdaParameterReturnType(v.ParentLambdaExpression, v.Name, this);
					} else {
						rt = new InferredReturnType(v.Initializer, this);
						if (v.IsLoopVariable) {
							rt = new ElementReturnType(this.projectContent, rt);
						}
					}
				} else {
					rt = this.projectContent.SystemTypes.Object;
				}
			} else {
				rt = TypeVisitor.CreateReturnType(v.TypeRef, this);
			}
			variableReturnTypeCache[v] = rt;
			return rt;
		}
		
		LocalLookupVariable SearchVariable(string name, NR.Location position)
		{
			if (lookupTableVisitor == null || !lookupTableVisitor.Variables.ContainsKey(name))
				return null;
			List<LocalLookupVariable> variables = lookupTableVisitor.Variables[name];
			if (variables.Count <= 0) {
				return null;
			}
			
			foreach (LocalLookupVariable v in variables) {
				if (IsInside(position, v.StartPos, v.EndPos)) {
					return v;
				}
			}
			return null;
		}
		#endregion
		
		IClass GetPrimitiveClass(string systemType, string newName)
		{
			IClass c = projectContent.GetClass(systemType, 0);
			if (c == null)
				return null;
			DefaultClass c2 = new DefaultClass(c.CompilationUnit, newName);
			c2.ClassType = c.ClassType;
			c2.Modifiers = c.Modifiers;
			c2.Documentation = c.Documentation;
			c2.BaseTypes.AddRange(c.BaseTypes);
			c2.Methods.AddRange(c.Methods);
			c2.Fields.AddRange(c.Fields);
			c2.Properties.AddRange(c.Properties);
			c2.Events.AddRange(c.Events);
			return c2;
		}
		
		static void AddCSharpKeywords(List<ICompletionEntry> ar, BitArray keywords)
		{
			for (int i = 0; i < keywords.Length; i++) {
				if (keywords[i]) {
					ar.Add(new KeywordEntry(NR.Parser.CSharp.Tokens.GetTokenString(i)));
				}
			}
		}
		
		/// <summary>
		/// Returns code completion entries for given context.
		/// </summary>
		/// <param name="caretLine"></param>
		/// <param name="caretColumn"></param>
		/// <param name="parseInfo"></param>
		/// <param name="fileContent"></param>
		/// <param name="context"></param>
		/// <param name="showEntriesFromAllNamespaces">If true, returns entries from all namespaces, regardless of current imports.</param>
		/// <returns></returns>
		public List<ICompletionEntry> CtrlSpace(int caretLine, int caretColumn, ParseInformation parseInfo, string fileContent, ExpressionContext context, bool showEntriesFromAllNamespaces = false)
		{
			if (!Initialize(parseInfo, caretLine, caretColumn))
				return null;
			
			List<ICompletionEntry> result = new List<ICompletionEntry>();
			if (language == NR.SupportedLanguage.VBNet) {
				CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
			} else {
				if (context == ExpressionContext.TypeDeclaration) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.TypeLevel);
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == CSharpExpressionContext.InterfaceDeclaration) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.InterfaceLevel);
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == ExpressionContext.MethodBody) {
					result.Add(new KeywordEntry("var"));
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.StatementStart);
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == ExpressionContext.Global) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.GlobalLevel);
				} else if (context == CSharpExpressionContext.InterfacePropertyDeclaration) {
					result.Add(new KeywordEntry("get"));
					result.Add(new KeywordEntry("set"));
				} else if (context == CSharpExpressionContext.BaseConstructorCall) {
					result.Add(new KeywordEntry("this"));
					result.Add(new KeywordEntry("base"));
				} else if (context == CSharpExpressionContext.ConstraintsStart) {
					result.Add(new KeywordEntry("where"));
				} else if (context == CSharpExpressionContext.Constraints) {
					result.Add(new KeywordEntry("where"));
					result.Add(new KeywordEntry("new"));
					result.Add(new KeywordEntry("struct"));
					result.Add(new KeywordEntry("class"));
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == ExpressionContext.InheritableType) {
					result.Add(new KeywordEntry("where")); // the inheritance list can be followed by constraints
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == CSharpExpressionContext.PropertyDeclaration) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.InPropertyDeclaration);
				} else if (context == CSharpExpressionContext.EventDeclaration) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.InEventDeclaration);
				} else if (context == CSharpExpressionContext.FullyQualifiedType) {
					cu.ProjectContent.AddNamespaceContents(result, "", languageProperties, true);
				} else if (context == CSharpExpressionContext.ParameterType || context == CSharpExpressionContext.FirstParameterType) {
					result.Add(new KeywordEntry("ref"));
					result.Add(new KeywordEntry("out"));
					result.Add(new KeywordEntry("params"));
					if (context == CSharpExpressionContext.FirstParameterType && languageProperties.SupportsExtensionMethods) {
						if (callingMember != null && callingMember.IsStatic) {
							result.Add(new KeywordEntry("this"));
						}
					}
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else if (context == CSharpExpressionContext.ObjectInitializer) {
					bool isCollectionInitializer;
					result.AddRange(ObjectInitializerCtrlSpace(fileContent, out isCollectionInitializer));
					if (isCollectionInitializer) {
						AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.ExpressionStart);
						AddCSharpPrimitiveTypes(result);
						CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
					}
				} else if (context == ExpressionContext.Attribute) {
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
					result.Add(new KeywordEntry("assembly"));
					result.Add(new KeywordEntry("module"));
					result.Add(new KeywordEntry("field"));
					result.Add(new KeywordEntry("event"));
					result.Add(new KeywordEntry("method"));
					result.Add(new KeywordEntry("param"));
					result.Add(new KeywordEntry("property"));
					result.Add(new KeywordEntry("return"));
					result.Add(new KeywordEntry("type"));
				} else if (context == ExpressionContext.Default) {
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.ExpressionStart);
					AddCSharpKeywords(result, NR.Parser.CSharp.Tokens.ExpressionContent);
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				} else {
					// e.g. some ExpressionContext.TypeDerivingFrom()
					AddCSharpPrimitiveTypes(result);
					CtrlSpaceInternal(result, fileContent, showEntriesFromAllNamespaces);
				}
			}
			return result;
		}
		
		void AddCSharpPrimitiveTypes(List<ICompletionEntry> result)
		{
			foreach (KeyValuePair<string, string> pair in TypeReference.PrimitiveTypesCSharp) {
				IClass c = GetPrimitiveClass(pair.Value, pair.Key);
				if (c != null) result.Add(c);
			}
		}
		
		void CtrlSpaceInternal(List<ICompletionEntry> result, string fileContent, bool showEntriesFromAllNamespaces)
		{
			lookupTableVisitor = new LookupTableVisitor(language);
			
			if (callingMember != null) {
				CompilationUnit parsedCu = ParseCurrentMemberAsCompilationUnit(fileContent);
				if (parsedCu != null) {
					lookupTableVisitor.VisitCompilationUnit(parsedCu, null);
				}
			}
			
			CtrlSpaceResolveHelper.AddContentsFromCalling(result, callingClass, callingMember);
			
			foreach (KeyValuePair<string, List<LocalLookupVariable>> pair in lookupTableVisitor.Variables) {
				if (pair.Value != null && pair.Value.Count > 0) {
					foreach (LocalLookupVariable v in pair.Value) {
						if (IsInside(new NR.Location(caretColumn, caretLine), v.StartPos, v.EndPos)) {
							// convert to a field for display
							result.Add(CreateLocalVariableField(v));
							break;
						}
					}
				}
			}
			if (callingMember is IProperty) {
				IProperty property = (IProperty)callingMember;
				if (property.SetterRegion.IsInside(caretLine, caretColumn)) {
					result.Add(new DefaultField.ParameterField(property.ReturnType, "value", property.Region, callingClass));
				}
			}
			if (callingMember is IEvent) {
				result.Add(new DefaultField.ParameterField(callingMember.ReturnType, "value", callingMember.Region, callingClass));
			}
			
			if (showEntriesFromAllNamespaces) {
				// CC contains contents of all referenced assemblies
				CtrlSpaceResolveHelper.AddReferencedProjectsContents(result, cu, callingClass);
			} else {
				// CC contains contents of all imported namespaces
				CtrlSpaceResolveHelper.AddImportedNamespaceContents(result, cu, callingClass);
			}
		}
		
		sealed class CompareLambdaByLocation : IEqualityComparer<LambdaExpression>
		{
			public bool Equals(LambdaExpression x, LambdaExpression y)
			{
				return x.StartLocation == y.StartLocation && x.EndLocation == y.EndLocation;
			}
			
			public int GetHashCode(LambdaExpression obj)
			{
				return unchecked (8123351 * obj.StartLocation.GetHashCode() + obj.EndLocation.GetHashCode());
			}
		}
		
		Dictionary<LambdaExpression, IReturnType[]> lambdaParameterTypes = new Dictionary<LambdaExpression, IReturnType[]>(new CompareLambdaByLocation());
		
		internal void SetImplicitLambdaParameterTypes(LambdaExpression lambda, IReturnType[] types)
		{
			lambdaParameterTypes[lambda] = types;
		}
		
		internal void UnsetImplicitLambdaParameterTypes(LambdaExpression lambda)
		{
			lambdaParameterTypes.Remove(lambda);
		}
		
		IReturnType[] GetImplicitLambdaParameterTypes(LambdaExpression lambda)
		{
			IReturnType[] types;
			if (lambdaParameterTypes.TryGetValue(lambda, out types))
				return types;
			else
				return null;
		}
		
		internal static bool IsInvoked(Expression expr)
		{
			InvocationExpression ie = expr.Parent as InvocationExpression;
			if (ie != null) {
				return ie.TargetObject == expr;
			}
			return false;
		}
		
		public IReturnType GetExpectedTypeFromContext(Expression expr)
		{
			if (expr == null)
				return null;
			
			InvocationExpression ie = expr.Parent as InvocationExpression;
			if (ie != null) {
				int index = ie.Arguments.IndexOf(expr);
				if (index < 0)
					return null;
				
				ResolveResult rr = ResolveInternal(ie, ExpressionContext.Default);
				IMethod m;
				if (rr is MemberResolveResult) {
					m = (rr as MemberResolveResult).ResolvedMember as IMethod;
					if ((rr as MemberResolveResult).IsExtensionMethodCall)
						index++;
				} else if (rr is DelegateCallResolveResult) {
					m = (rr as DelegateCallResolveResult).DelegateInvokeMethod;
				} else {
					m = null;
				}
				if (m != null && index < m.Parameters.Count)
					return m.Parameters[index].ReturnType;
			}
			
			ObjectCreateExpression oce = expr.Parent as ObjectCreateExpression;
			if (oce != null) {
				int index = oce.Parameters.IndexOf(expr);
				if (index < 0)
					return null;
				
				MemberResolveResult mrr = ResolveInternal(oce, ExpressionContext.Default) as MemberResolveResult;
				if (mrr != null) {
					IMethod m = mrr.ResolvedMember as IMethod;
					if (m != null && index < m.Parameters.Count)
						return m.Parameters[index].ReturnType;
				}
			}
			
			if (expr.Parent is CastExpression) {
				ResolveResult rr = ResolveInternal((Expression)expr.Parent, ExpressionContext.Default);
				if (rr != null)
					return rr.ResolvedType;
			} else if (expr.Parent is LambdaExpression) {
				IReturnType delegateType = GetExpectedTypeFromContext(expr.Parent as Expression);
				IMethod sig = CSharp.TypeInference.GetDelegateOrExpressionTreeSignature(delegateType, true);
				if (sig != null)
					return sig.ReturnType;
			} else if (expr.Parent is ReturnStatement) {
				Expression lambda = GetParentAnonymousMethodOrLambda(expr.Parent);
				if (lambda != null) {
					IReturnType delegateType = GetExpectedTypeFromContext(lambda);
					IMethod sig = CSharp.TypeInference.GetDelegateOrExpressionTreeSignature(delegateType, true);
					if (sig != null)
						return sig.ReturnType;
				} else {
					if (callingMember != null)
						return callingMember.ReturnType;
				}
			} else if (expr.Parent is ParenthesizedExpression) {
				return GetExpectedTypeFromContext(expr.Parent as Expression);
			} else if (expr.Parent is VariableDeclaration) {
				return GetTypeFromVariableDeclaration((VariableDeclaration)expr.Parent);
			} else if (expr.Parent is AssignmentExpression) {
				ResolveResult rr = ResolveInternal((expr.Parent as AssignmentExpression).Left, ExpressionContext.Default);
				if (rr != null)
					return rr.ResolvedType;
			} else if (expr.Parent is MemberInitializerExpression) {
				IMember m = ResolveMemberInitializerExpressionInObjectInitializer((MemberInitializerExpression)expr.Parent);
				if (m != null)
					return m.ReturnType;
			} else if (expr.Parent is CollectionInitializerExpression) {
				IReturnType collectionType;
				if (expr.Parent.Parent is ObjectCreateExpression)
					collectionType = TypeVisitor.CreateReturnType(((ObjectCreateExpression)expr.Parent.Parent).CreateType, this);
				else if (expr.Parent.Parent is ArrayCreateExpression)
					collectionType = TypeVisitor.CreateReturnType(((ArrayCreateExpression)expr.Parent.Parent).CreateType, this);
				else if (expr.Parent.Parent is VariableDeclaration)
					collectionType = GetTypeFromVariableDeclaration((VariableDeclaration)expr.Parent.Parent);
				else
					collectionType = null;
				if (collectionType != null)
					return new ElementReturnType(projectContent, collectionType);
			} else if (expr.Parent is IndexerExpression) {
				IndexerExpression indexerExpr = expr.Parent as IndexerExpression;
				MemberResolveResult indexer = ResolveInternal(indexerExpr, ExpressionContext.Default)
					as MemberResolveResult;
				
				if (indexer != null && indexer.ResolvedMember is IProperty) {
					IProperty p = indexer.ResolvedMember as IProperty;
					int position = indexerExpr.Indexes.IndexOf(expr);
					if (position < 0 || position >= p.Parameters.Count)
						return null;
					return p.Parameters[position].ReturnType;
				}
			}
			return null;
		}
		
		Expression GetParentAnonymousMethodOrLambda(INode node)
		{
			while (node != null) {
				if (node is AnonymousMethodExpression || node is LambdaExpression)
					return (Expression)node;
				node = node.Parent;
			}
			return null;
		}

		IReturnType GetTypeFromVariableDeclaration(VariableDeclaration varDecl)
		{
			TypeReference typeRef = varDecl.TypeReference;
			if (typeRef == null || typeRef.IsNull) {
				LocalVariableDeclaration lvd = varDecl.Parent as LocalVariableDeclaration;
				if (lvd != null) typeRef = lvd.TypeReference;
				FieldDeclaration fd = varDecl.Parent as FieldDeclaration;
				if (fd != null) typeRef = fd.TypeReference;
			}
			return TypeVisitor.CreateReturnType(typeRef, this);
		}
	}
	
	/// <summary>
	/// Used in CtrlSpace-results to represent a keyword.
	/// </summary>
	public class KeywordEntry : ICompletionEntry
	{
		public string Name { get; private set; }
		
		public KeywordEntry(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.Name = name;
		}
		
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
		
		public override bool Equals(object obj)
		{
			KeywordEntry e = obj as KeywordEntry;
			return e != null && e.Name == this.Name;
		}
		
		public override string ToString()
		{
			return Name;
		}
	}
}
