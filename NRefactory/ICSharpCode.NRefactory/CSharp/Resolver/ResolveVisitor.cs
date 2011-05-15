// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Traverses the DOM and resolves expressions.
	/// </summary>
	/// <remarks>
	/// The ResolveVisitor does two jobs at the same time: it tracks the resolve context (properties on CSharpResolver)
	/// and it resolves the expressions visited.
	/// To allow using the context tracking without having to resolve every expression in the file (e.g. when you want to resolve
	/// only a single node deep within the DOM), you can use the <see cref="IResolveVisitorNavigator"/> interface.
	/// The navigator allows you to switch the between scanning mode and resolving mode.
	/// In scanning mode, the context is tracked (local variables registered etc.), but nodes are not resolved.
	/// While scanning, the navigator will get asked about every node that the resolve visitor is about to enter.
	/// This allows the navigator whether to keep scanning, whether switch to resolving mode, or whether to completely skip the
	/// subtree rooted at that node.
	/// 
	/// In resolving mode, the context is tracked and nodes will be resolved.
	/// The resolve visitor may decide that it needs to resolve other nodes as well in order to resolve the current node.
	/// In this case, those nodes will be resolved automatically, without asking the navigator interface.
	/// For child nodes that are not essential to resolving, the resolve visitor will switch back to scanning mode (and thus will
	/// ask the navigator for further instructions).
	/// 
	/// Moreover, there is the <c>ResolveAll</c> mode - it works similar to resolving mode, but will not switch back to scanning mode.
	/// The whole subtree will be resolved without notifying the navigator.
	/// </remarks>
	public sealed class ResolveVisitor : DepthFirstAstVisitor<object, ResolveResult>
	{
		static readonly ResolveResult errorResult = new ErrorResolveResult(SharedTypes.UnknownType);
		CSharpResolver resolver;
		readonly ParsedFile parsedFile;
		readonly Dictionary<AstNode, ResolveResult> cache = new Dictionary<AstNode, ResolveResult>();
		
		readonly IResolveVisitorNavigator navigator;
		ResolveVisitorNavigationMode mode = ResolveVisitorNavigationMode.Scan;
		
		#region Constructor
		/// <summary>
		/// Creates a new ResolveVisitor instance.
		/// </summary>
		/// <param name="resolver">
		/// The CSharpResolver, describing the initial resolve context.
		/// If you visit a whole CompilationUnit with the resolve visitor, you can simply pass
		/// <c>new CSharpResolver(typeResolveContext)</c> without setting up the context.
		/// If you only visit a subtree, you need to pass a CSharpResolver initialized to the context for that subtree.
		/// </param>
		/// <param name="parsedFile">
		/// Result of the <see cref="TypeSystemConvertVisitor"/> for the file being passed. This is used for setting up the context on the resolver.
		/// You may pass <c>null</c> if you are only visiting a part of a method body and have already set up the context in the <paramref name="resolver"/>.
		/// </param>
		/// <param name="navigator">
		/// The navigator, which controls where the resolve visitor will switch between scanning mode and resolving mode.
		/// If you pass <c>null</c>, then <c>ResolveAll</c> mode will be used.
		/// </param>
		public ResolveVisitor(CSharpResolver resolver, ParsedFile parsedFile, IResolveVisitorNavigator navigator = null)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.parsedFile = parsedFile;
			this.navigator = navigator;
			if (navigator == null)
				mode = ResolveVisitorNavigationMode.ResolveAll;
		}
		#endregion
		
		/// <summary>
		/// Gets the TypeResolveContext used by this ResolveVisitor.
		/// </summary>
		public ITypeResolveContext TypeResolveContext {
			get { return resolver.Context; }
		}
		
		/// <summary>
		/// Gets the CancellationToken used by this ResolveVisitor.
		/// </summary>
		public CancellationToken CancellationToken {
			get { return resolver.cancellationToken; }
		}
		
		#region Scan / Resolve
		bool resolverEnabled {
			get { return mode != ResolveVisitorNavigationMode.Scan; }
		}
		
		public void Scan(AstNode node)
		{
			if (node == null)
				return;
			if (mode == ResolveVisitorNavigationMode.ResolveAll) {
				Resolve(node);
			} else {
				ResolveVisitorNavigationMode oldMode = mode;
				mode = navigator.Scan(node);
				switch (mode) {
					case ResolveVisitorNavigationMode.Skip:
						if (node is VariableDeclarationStatement) {
							// Enforce scanning of variable declarations.
							goto case ResolveVisitorNavigationMode.Scan;
						}
						break;
					case ResolveVisitorNavigationMode.Scan:
						node.AcceptVisitor(this, null);
						break;
					case ResolveVisitorNavigationMode.Resolve:
					case ResolveVisitorNavigationMode.ResolveAll:
						Resolve(node);
						break;
					default:
						throw new Exception("Invalid value for ResolveVisitorNavigationMode");
				}
				mode = oldMode;
			}
		}
		
		public ResolveResult Resolve(AstNode node)
		{
			if (node == null)
				return errorResult;
			bool wasScan = mode == ResolveVisitorNavigationMode.Scan;
			if (wasScan)
				mode = ResolveVisitorNavigationMode.Resolve;
			ResolveResult result;
			if (!cache.TryGetValue(node, out result)) {
				resolver.cancellationToken.ThrowIfCancellationRequested();
				result = cache[node] = node.AcceptVisitor(this, null) ?? errorResult;
			}
			if (wasScan)
				mode = ResolveVisitorNavigationMode.Scan;
			return result;
		}
		
		void ScanChildren(AstNode node)
		{
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				Scan(child);
			}
		}
		#endregion
		
		#region GetResolveResult
		/// <summary>
		/// Gets the cached resolve result for the specified node.
		/// Returns <c>null</c> if no cached result was found (e.g. if the node was not visited; or if it was visited in scanning mode).
		/// </summary>
		public ResolveResult GetResolveResult(AstNode node)
		{
			ResolveResult result;
			if (cache.TryGetValue(node, out result))
				return result;
			else
				return null;
		}
		#endregion
		
		#region Track UsingScope
		public override ResolveResult VisitCompilationUnit(CompilationUnit unit, object data)
		{
			UsingScope previousUsingScope = resolver.UsingScope;
			try {
				if (parsedFile != null)
					resolver.UsingScope = parsedFile.RootUsingScope;
				ScanChildren(unit);
				return null;
			} finally {
				resolver.UsingScope = previousUsingScope;
			}
		}
		
		public override ResolveResult VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			UsingScope previousUsingScope = resolver.UsingScope;
			try {
				if (parsedFile != null) {
					resolver.UsingScope = parsedFile.GetUsingScope(namespaceDeclaration.StartLocation);
				}
				ScanChildren(namespaceDeclaration);
				return new NamespaceResolveResult(resolver.UsingScope.NamespaceName);
			} finally {
				resolver.UsingScope = previousUsingScope;
			}
		}
		#endregion
		
		#region Track CurrentTypeDefinition
		ResolveResult VisitTypeOrDelegate(AstNode typeDeclaration)
		{
			ITypeDefinition previousTypeDefinition = resolver.CurrentTypeDefinition;
			try {
				ITypeDefinition newTypeDefinition = null;
				if (resolver.CurrentTypeDefinition != null) {
					foreach (ITypeDefinition innerClass in resolver.CurrentTypeDefinition.InnerClasses) {
						if (innerClass.Region.IsInside(typeDeclaration.StartLocation)) {
							newTypeDefinition = innerClass;
							break;
						}
					}
				} else if (parsedFile != null) {
					newTypeDefinition = parsedFile.GetTopLevelTypeDefinition(typeDeclaration.StartLocation);
				}
				if (newTypeDefinition != null)
					resolver.CurrentTypeDefinition = newTypeDefinition;
				ScanChildren(typeDeclaration);
				return newTypeDefinition != null ? new TypeResolveResult(newTypeDefinition) : errorResult;
			} finally {
				resolver.CurrentTypeDefinition = previousTypeDefinition;
			}
		}
		
		public override ResolveResult VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			return VisitTypeOrDelegate(typeDeclaration);
		}
		
		public override ResolveResult VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			return VisitTypeOrDelegate(delegateDeclaration);
		}
		#endregion
		
		#region Track CurrentMember
		public override ResolveResult VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(fieldDeclaration);
		}
		
		public override ResolveResult VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(eventDeclaration);
		}
		
		ResolveResult VisitFieldOrEventDeclaration(AttributedNode fieldOrEventDeclaration)
		{
			int initializerCount = fieldOrEventDeclaration.GetChildrenByRole(FieldDeclaration.Roles.Variable).Count();
			ResolveResult result = null;
			for (AstNode node = fieldOrEventDeclaration.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FieldDeclaration.Roles.Variable) {
					if (resolver.CurrentTypeDefinition != null) {
						resolver.CurrentMember = resolver.CurrentTypeDefinition.Fields.FirstOrDefault(f => f.Region.IsInside(node.StartLocation));
					}
					
					if (resolverEnabled && initializerCount == 1) {
						result = Resolve(node);
					} else {
						Scan(node);
					}
					
					resolver.CurrentMember = null;
				} else {
					Scan(node);
				}
			}
			return result;
		}
		
		public override ResolveResult VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			ScanChildren(variableInitializer);
			if (resolverEnabled) {
				if (variableInitializer.Parent is FieldDeclaration) {
					if (resolver.CurrentMember != null)
						return new MemberResolveResult(resolver.CurrentMember, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
				} else {
					string identifier = variableInitializer.Name;
					foreach (IVariable v in resolver.LocalVariables) {
						if (v.Name == identifier) {
							object constantValue = v.IsConst ? v.ConstantValue.GetValue(resolver.Context) : null;
							return new LocalResolveResult(v, v.Type.Resolve(resolver.Context), constantValue);
						}
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		ResolveResult VisitMethodMember(AttributedNode member)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Methods.FirstOrDefault(m => m.Region.IsInside(member.StartLocation));
				}
				
				ScanChildren(member);
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			return VisitMethodMember(methodDeclaration);
		}
		
		public override ResolveResult VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			return VisitMethodMember(operatorDeclaration);
		}
		
		public override ResolveResult VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			return VisitMethodMember(constructorDeclaration);
		}
		
		public override ResolveResult VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			return VisitMethodMember(destructorDeclaration);
		}
		
		// handle properties/indexers
		ResolveResult VisitPropertyMember(MemberDeclaration propertyOrIndexerDeclaration)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Properties.FirstOrDefault(p => p.Region.IsInside(propertyOrIndexerDeclaration.StartLocation));
				}
				
				for (AstNode node = propertyOrIndexerDeclaration.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == PropertyDeclaration.SetterRole && resolver.CurrentMember != null) {
						resolver.PushBlock();
						resolver.AddVariable(resolver.CurrentMember.ReturnType, "value");
						Scan(node);
						resolver.PopBlock();
					} else {
						Scan(node);
					}
				}
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			return VisitPropertyMember(propertyDeclaration);
		}
		
		public override ResolveResult VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			return VisitPropertyMember(indexerDeclaration);
		}
		
		public override ResolveResult VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Events.FirstOrDefault(e => e.Region.IsInside(eventDeclaration.StartLocation));
				}
				
				if (resolver.CurrentMember != null) {
					resolver.PushBlock();
					resolver.AddVariable(resolver.CurrentMember.ReturnType, "value");
					ScanChildren(eventDeclaration);
					resolver.PopBlock();
				} else {
					ScanChildren(eventDeclaration);
				}
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			ScanChildren(parameterDeclaration);
			if (resolverEnabled) {
				IParameterizedMember pm = resolver.CurrentMember as IParameterizedMember;
				if (pm != null) {
					foreach (IParameter p in pm.Parameters) {
						if (p.Name == parameterDeclaration.Name) {
							return new LocalResolveResult(p, p.Type.Resolve(resolver.Context));
						}
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Fields.FirstOrDefault(f => f.Region.IsInside(enumMemberDeclaration.StartLocation));
				}
				
				ScanChildren(enumMemberDeclaration);
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		#endregion
		
		#region Track CheckForOverflow
		public override ResolveResult VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = true;
				if (resolverEnabled) {
					return Resolve(checkedExpression.Expression);
				} else {
					ScanChildren(checkedExpression);
					return null;
				}
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = false;
				if (resolverEnabled) {
					return Resolve(uncheckedExpression.Expression);
				} else {
					ScanChildren(uncheckedExpression);
					return null;
				}
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = true;
				ScanChildren(checkedStatement);
				return null;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = false;
				ScanChildren(uncheckedStatement);
				return null;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		#endregion
		
		#region Visit Expressions
		static bool IsTargetOfInvocation(AstNode node)
		{
			InvocationExpression ie = node.Parent as InvocationExpression;
			return ie != null && ie.Target == node;
		}
		
		IType ResolveType(AstType type)
		{
			return MakeTypeReference(type).Resolve(resolver.Context);
		}
		
		public override ResolveResult VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, object data)
		{
			// TODO: ? 
			ScanChildren(undocumentedExpression);
			return new ResolveResult(resolver.Context.GetClass(typeof(RuntimeArgumentHandle)) ?? SharedTypes.UnknownType);
		}
		
		public override ResolveResult VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitAsExpression(AsExpression asExpression, object data)
		{
			if (resolverEnabled) {
				Scan(asExpression.Expression);
				return new ResolveResult(ResolveType(asExpression.Type));
			} else {
				ScanChildren(asExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult left = Resolve(assignmentExpression.Left);
				Scan(assignmentExpression.Right);
				return new ResolveResult(left.Type);
			} else {
				ScanChildren(assignmentExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveBaseReference();
			} else {
				ScanChildren(baseReferenceExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult left = Resolve(binaryOperatorExpression.Left);
				ResolveResult right = Resolve(binaryOperatorExpression.Right);
				return resolver.ResolveBinaryOperator(binaryOperatorExpression.Operator, left, right);
			} else {
				ScanChildren(binaryOperatorExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitCastExpression(CastExpression castExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveCast(ResolveType(castExpression.Type), Resolve(castExpression.Expression));
			} else {
				ScanChildren(castExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveConditional(
					Resolve(conditionalExpression.Condition),
					Resolve(conditionalExpression.TrueExpression),
					Resolve(conditionalExpression.FalseExpression));
			} else {
				ScanChildren(conditionalExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveDefaultValue(ResolveType(defaultValueExpression.Type));
			} else {
				ScanChildren(defaultValueExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult rr = Resolve(directionExpression.Expression);
				return new ByReferenceResolveResult(rr.Type, directionExpression.FieldDirection == FieldDirection.Out);
			} else {
				ScanChildren(directionExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			if (resolverEnabled) {
				// TODO: type arguments?
				return resolver.ResolveSimpleName(identifierExpression.Identifier, EmptyList<IType>.Instance,
				                                  IsTargetOfInvocation(identifierExpression));
			} else {
				ScanChildren(identifierExpression);
				return null;
			}
		}
		
		ResolveResult[] GetArguments(IEnumerable<Expression> argumentExpressions, out string[] argumentNames)
		{
			argumentNames = null; // TODO: add support for named arguments
			ResolveResult[] arguments = new ResolveResult[argumentExpressions.Count()];
			int i = 0;
			foreach (AstNode argument in argumentExpressions) {
				arguments[i++] = Resolve(argument);
			}
			return arguments;
		}
		
		public override ResolveResult VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(indexerExpression.Target);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(indexerExpression.Arguments, out argumentNames);
				return resolver.ResolveIndexer(target, arguments, argumentNames);
			} else {
				ScanChildren(indexerExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(invocationExpression.Target);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(invocationExpression.Arguments, out argumentNames);
				return resolver.ResolveInvocation(target, arguments, argumentNames);
			} else {
				ScanChildren(invocationExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitIsExpression(IsExpression isExpression, object data)
		{
			ScanChildren(isExpression);
			return new ResolveResult(KnownTypeReference.Boolean.Resolve(resolver.Context));
		}
		
		public override ResolveResult VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(memberReferenceExpression.Target);
				List<AstType> typeArgumentNodes = memberReferenceExpression.TypeArguments.ToList();
				// TODO: type arguments?
				return resolver.ResolveMemberAccess(target, memberReferenceExpression.MemberName,
				                                    EmptyList<IType>.Instance,
				                                    IsTargetOfInvocation(memberReferenceExpression));
			} else {
				ScanChildren(memberReferenceExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(null);
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (resolverEnabled) {
				IType type = ResolveType(objectCreateExpression.Type);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(objectCreateExpression.Arguments, out argumentNames);
				return resolver.ResolveObjectCreation(type, arguments, argumentNames);
			} else {
				ScanChildren(objectCreateExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (resolverEnabled) {
				return Resolve(parenthesizedExpression.Expression);
			} else {
				Scan(parenthesizedExpression.Expression);
				return null;
			}
		}
		
		public override ResolveResult VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(primitiveExpression.Value);
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveSizeOf(ResolveType(sizeOfExpression.Type));
			} else {
				ScanChildren(sizeOfExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			if (resolverEnabled) {
				Scan(stackAllocExpression.CountExpression);
				return new ResolveResult(new PointerType(ResolveType(stackAllocExpression.Type)));
			} else {
				ScanChildren(stackAllocExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return resolver.ResolveThisReference();
		}
		
		static readonly GetClassTypeReference systemType = new GetClassTypeReference("System", "Type", 0);
		
		public override ResolveResult VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			ScanChildren(typeOfExpression);
			if (resolverEnabled)
				return new ResolveResult(systemType.Resolve(resolver.Context));
			else
				return null;
		}
		
		public override ResolveResult VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult expr = Resolve(unaryOperatorExpression.Expression);
				return resolver.ResolveUnaryOperator(unaryOperatorExpression.Operator, expr);
			} else {
				ScanChildren(unaryOperatorExpression);
				return null;
			}
		}
		#endregion
		
		#region Local Variable Scopes (Block Statements)
		public override ResolveResult VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(blockStatement);
			resolver.PopBlock();
			return null;
		}
		
		public override ResolveResult VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(usingStatement);
			resolver.PopBlock();
			return null;
		}
		
		public override ResolveResult VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(fixedStatement);
			resolver.PopBlock();
			return null;
		}
		
		public override ResolveResult VisitForStatement(ForStatement forStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(forStatement);
			resolver.PopBlock();
			return null;
		}
		
		
		public override ResolveResult VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			resolver.PushBlock();
			ITypeReference type = MakeTypeReference(foreachStatement.VariableType, foreachStatement.InExpression, true);
			resolver.AddVariable(type, foreachStatement.VariableName);
			ScanChildren(foreachStatement);
			resolver.PopBlock();
			return null;
		}
		#endregion
		
		#region Simple Statements (only ScanChildren)
		public override ResolveResult VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			ScanChildren(expressionStatement);
			return null;
		}
		
		public override ResolveResult VisitBreakStatement(BreakStatement breakStatement, object data)
		{
			ScanChildren(breakStatement);
			return null;
		}
		
		public override ResolveResult VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			ScanChildren(continueStatement);
			return null;
		}
		
		public override ResolveResult VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			ScanChildren(emptyStatement);
			return null;
		}
		
		public override ResolveResult VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			ScanChildren(gotoStatement);
			return null;
		}
		
		public override ResolveResult VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			ScanChildren(ifElseStatement);
			return null;
		}
		
		public override ResolveResult VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			ScanChildren(labelStatement);
			return null;
		}
		
		public override ResolveResult VisitLockStatement(LockStatement lockStatement, object data)
		{
			ScanChildren(lockStatement);
			return null;
		}
		
		public override ResolveResult VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			ScanChildren(returnStatement);
			return null;
		}
		
		public override ResolveResult VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			ScanChildren(switchStatement);
			return null;
		}
		
		public override ResolveResult VisitSwitchSection(SwitchSection switchSection, object data)
		{
			ScanChildren(switchSection);
			return null;
		}
		
		public override ResolveResult VisitCaseLabel(CaseLabel caseLabel, object data)
		{
			ScanChildren(caseLabel);
			return null;
		}
		
		public override ResolveResult VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			ScanChildren(throwStatement);
			return null;
		}
		
		public override ResolveResult VisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			ScanChildren(unsafeStatement);
			return null;
		}
		
		public override ResolveResult VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			ScanChildren(whileStatement);
			return null;
		}
		
		public override ResolveResult VisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			ScanChildren(yieldStatement);
			return null;
		}
		#endregion
		
		#region Try / Catch
		public override ResolveResult VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			ScanChildren(tryCatchStatement);
			return null;
		}
		
		public override ResolveResult VisitCatchClause(CatchClause catchClause, object data)
		{
			resolver.PushBlock();
			if (catchClause.VariableName != null) {
				resolver.AddVariable(MakeTypeReference(catchClause.Type, null, false), catchClause.VariableName);
			}
			ScanChildren(catchClause);
			resolver.PopBlock();
			return null;
		}
		#endregion
		
		#region VariableDeclarationStatement
		public override ResolveResult VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			bool isConst = (variableDeclarationStatement.Modifiers & Modifiers.Const) != 0;
			VariableInitializer firstInitializer = variableDeclarationStatement.Variables.FirstOrDefault();
			ITypeReference type = MakeTypeReference(variableDeclarationStatement.Type,
			                                        firstInitializer != null ? firstInitializer.Initializer : null,
			                                        false);
			
			int initializerCount = variableDeclarationStatement.Variables.Count();
			ResolveResult result = null;
			for (AstNode node = variableDeclarationStatement.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FieldDeclaration.Roles.Variable) {
					VariableInitializer vi = (VariableInitializer)node;
					
					IConstantValue cv = null;
					if (isConst)
						throw new NotImplementedException();
					resolver.AddVariable(type, vi.Name, cv);
					
					if (resolverEnabled && initializerCount == 1) {
						result = Resolve(node);
					} else {
						Scan(node);
					}
				} else {
					Scan(node);
				}
			}
			return result;
		}
		#endregion
		
		#region Local Variable Type Inference
		/// <summary>
		/// Creates a type reference for the specified type node.
		/// If the type node is 'var', performs type inference on the initializer expression.
		/// </summary>
		ITypeReference MakeTypeReference(AstType type, AstNode initializerExpression, bool isForEach)
		{
			if (initializerExpression != null && IsVar(type)) {
				return new VarTypeReference(this, resolver.Clone(), initializerExpression, isForEach);
			} else {
				return MakeTypeReference(type);
			}
		}
		
		ITypeReference MakeTypeReference(AstType type)
		{
			return TypeSystemConvertVisitor.ConvertType(type, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.UsingScope, false);
		}
		
		static bool IsVar(AstType returnType)
		{
			return returnType is SimpleType
				&& ((SimpleType)returnType).Identifier == "var"
				&& ((SimpleType)returnType).TypeArguments.Count == 0;
		}
		
		sealed class VarTypeReference : ITypeReference
		{
			ResolveVisitor visitor;
			CSharpResolver storedContext;
			AstNode initializerExpression;
			bool isForEach;
			
			IType result;
			
			public VarTypeReference(ResolveVisitor visitor, CSharpResolver storedContext, AstNode initializerExpression, bool isForEach)
			{
				this.visitor = visitor;
				this.storedContext = storedContext;
				this.initializerExpression = initializerExpression;
				this.isForEach = isForEach;
			}
			
			public IType Resolve(ITypeResolveContext context)
			{
				if (visitor == null)
					return result ?? SharedTypes.UnknownType;
				
				var oldMode = visitor.mode;
				var oldResolver = visitor.resolver;
				try {
					visitor.mode = ResolveVisitorNavigationMode.Resolve;
					visitor.resolver = storedContext;
					
					result = visitor.Resolve(initializerExpression).Type;
					
					if (isForEach) {
						result = GetElementType(result);
					}
					
					return result;
				} finally {
					visitor.mode = oldMode;
					visitor.resolver = oldResolver;
					
					visitor = null;
					storedContext = null;
					initializerExpression = null;
				}
			}
			
			IType GetElementType(IType result)
			{
				foreach (IType baseType in result.GetAllBaseTypes(storedContext.Context)) {
					ITypeDefinition baseTypeDef = baseType.GetDefinition();
					if (baseTypeDef != null && baseTypeDef.Name == "IEnumerable") {
						if (baseTypeDef.Namespace == "System.Collections.Generic" && baseTypeDef.TypeParameterCount == 1) {
							ParameterizedType pt = baseType as ParameterizedType;
							if (pt != null) {
								return pt.TypeArguments[0];
							}
						} else if (baseTypeDef.Namespace == "System.Collections" && baseTypeDef.TypeParameterCount == 0) {
							return KnownTypeReference.Object.Resolve(storedContext.Context);
						}
					}
				}
				return SharedTypes.UnknownType;
			}
			
			public override string ToString()
			{
				if (visitor == null)
					return "var=" + result;
				else
					return "var (not yet resolved)";
			}
		}
		#endregion
		
		#region Attributes
		public override ResolveResult VisitAttribute(Attribute attribute, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			ScanChildren(attributeSection);
			return null;
		}
		#endregion
		
		#region Using Declaration
		public override ResolveResult VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			return null;
		}
		
		public override ResolveResult VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration, object data)
		{
			return null;
		}
		#endregion
		
		#region Type References
		public override ResolveResult VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			ScanChildren(primitiveType);
			return new TypeResolveResult(ResolveType(primitiveType));
		}
		
		public override ResolveResult VisitSimpleType(SimpleType simpleType, object data)
		{
			ScanChildren(simpleType);
			return ResolveTypeOrNamespace(simpleType);
		}
		
		ResolveResult ResolveTypeOrNamespace(AstType type)
		{
			ITypeReference typeRef = MakeTypeReference(type);
			ITypeOrNamespaceReference typeOrNsRef = typeRef as ITypeOrNamespaceReference;
			if (typeOrNsRef != null) {
				return typeOrNsRef.DoResolve(resolver.Context);
			} else {
				return new TypeResolveResult(typeRef.Resolve(resolver.Context));
			}
		}
		
		public override ResolveResult VisitMemberType(MemberType memberType, object data)
		{
			ScanChildren(memberType);
			return ResolveTypeOrNamespace(memberType);
		}
		
		public override ResolveResult VisitComposedType(ComposedType composedType, object data)
		{
			ScanChildren(composedType);
			return new TypeResolveResult(ResolveType(composedType));
		}
		#endregion
		
		#region Query Expressions
		/*
		public override ResolveResult VisitQueryExpressionFromClause(QueryExpressionFromClause queryExpressionFromClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionWhereClause(QueryExpressionWhereClause queryExpressionWhereClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionJoinClause(QueryExpressionJoinClause queryExpressionJoinClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionGroupClause(QueryExpressionGroupClause queryExpressionGroupClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionLetClause(QueryExpressionLetClause queryExpressionLetClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionOrderClause(QueryExpressionOrderClause queryExpressionOrderClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionOrdering(QueryExpressionOrdering queryExpressionOrdering, object data)
		{
			throw new NotImplementedException();
		}
		
		public override ResolveResult VisitQueryExpressionSelectClause(QueryExpressionSelectClause queryExpressionSelectClause, object data)
		{
			throw new NotImplementedException();
		}
		 */
		#endregion
		
		public override ResolveResult VisitIdentifier(Identifier identifier, object data)
		{
			return null;
		}
		
		public override ResolveResult VisitConstraint(Constraint constraint, object data)
		{
			ScanChildren(constraint);
			return null;
		}
		
		public override ResolveResult VisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			ScanChildren(constructorInitializer);
			return null;
		}
		
		public override ResolveResult VisitAccessor(Accessor accessor, object data)
		{
			ScanChildren(accessor);
			return null;
		}
	}
}
