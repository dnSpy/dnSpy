// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;
using System.Threading;

using ICSharpCode.NRefactory.Semantics;
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
	public sealed class ResolveVisitor : IAstVisitor<object, ResolveResult>
	{
		// The ResolveVisitor is also responsible for handling lambda expressions.
		
		static readonly ResolveResult errorResult = ErrorResolveResult.UnknownError;
		static readonly ResolveResult transparentIdentifierResolveResult = new ResolveResult(SharedTypes.UnboundTypeArgument);
		readonly ResolveResult voidResult;
		
		CSharpResolver resolver;
		SimpleNameLookupMode currentTypeLookupMode = SimpleNameLookupMode.Type;
		/// <summary>Resolve result of the current LINQ query</summary>
		ResolveResult currentQueryResult;
		readonly CSharpParsedFile parsedFile;
		readonly Dictionary<AstNode, ResolveResult> resolveResultCache = new Dictionary<AstNode, ResolveResult>();
		readonly Dictionary<AstNode, CSharpResolver> resolverBeforeDict = new Dictionary<AstNode, CSharpResolver>();
		
		IResolveVisitorNavigator navigator;
		bool resolverEnabled;
		List<LambdaBase> undecidedLambdas;
		
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
		/// If you pass <c>null</c>, then nothing will be resolved on the initial scan, and the resolver
		/// will resolve additional nodes on demand (when one of the Get-methods is called).
		/// </param>
		public ResolveVisitor(CSharpResolver resolver, CSharpParsedFile parsedFile, IResolveVisitorNavigator navigator = null)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.parsedFile = parsedFile;
			this.navigator = navigator ?? new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
			this.voidResult = new ResolveResult(KnownTypeReference.Void.Resolve(resolver.Context));
		}
		#endregion
		
		#region Properties
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
		#endregion
		
		#region ResetContext
		/// <summary>
		/// Resets the visitor to the stored position, runs the action, and then reverts the visitor to the previous position.
		/// </summary>
		void ResetContext(CSharpResolver storedContext, Action action)
		{
			var oldResolverEnabled = this.resolverEnabled;
			var oldResolver = this.resolver;
			var oldTypeLookupMode = this.currentTypeLookupMode;
			var oldQueryType = this.currentQueryResult;
			try {
				this.resolverEnabled = false;
				this.resolver = storedContext;
				this.currentTypeLookupMode = SimpleNameLookupMode.Type;
				this.currentQueryResult = null;
				
				action();
			} finally {
				this.resolverEnabled = oldResolverEnabled;
				this.resolver = oldResolver;
				this.currentTypeLookupMode = oldTypeLookupMode;
				this.currentQueryResult = oldQueryType;
			}
		}
		#endregion
		
		#region Scan / Resolve
		/// <summary>
		/// Scans the AST rooted at the given node.
		/// </summary>
		public void Scan(AstNode node)
		{
			if (node == null || node.IsNull)
				return;
			switch (node.NodeType) {
				case NodeType.Token:
				case NodeType.Whitespace:
					return; // skip tokens, identifiers, comments, etc.
			}
			
			var mode = navigator.Scan(node);
			switch (mode) {
				case ResolveVisitorNavigationMode.Skip:
					if (node is VariableDeclarationStatement) {
						// Enforce scanning of variable declarations.
						goto case ResolveVisitorNavigationMode.Scan;
					}
					if (resolverBeforeDict.Count == 0) {
						// If we're just starting to resolve and haven't any context cached yet,
						// make sure to cache the root node.
						StoreState(node, resolver.Clone());
					}
					break;
				case ResolveVisitorNavigationMode.Scan:
					if (node is LambdaExpression || node is AnonymousMethodExpression) {
						// lambdas must be resolved so that they get stored in the 'undecided' list only once
						goto case ResolveVisitorNavigationMode.Resolve;
					}
					
					// We shouldn't scan nodes that were already resolved.
					Debug.Assert(!resolveResultCache.ContainsKey(node));
					// Doing so should be harmless since we allow scanning twice, but it indicates
					// a bug in the logic that causes the scan.
					
					bool oldResolverEnabled = resolverEnabled;
					resolverEnabled = false;
					StoreState(node, resolver.Clone());
					node.AcceptVisitor(this, null);
					resolverEnabled = oldResolverEnabled;
					break;
				case ResolveVisitorNavigationMode.Resolve:
					Resolve(node);
					break;
				default:
					throw new InvalidOperationException("Invalid value for ResolveVisitorNavigationMode");
			}
		}
		
		/// <summary>
		/// Equivalent to 'Scan', but also resolves the node at the same time.
		/// This method should be only used if the CSharpResolver passed to the ResolveVisitor was manually set
		/// to the correct state.
		/// Otherwise, use <c>resolver.Scan(compilationUnit); var result = resolver.GetResolveResult(node);</c>
		/// instead.
		/// </summary>
		public ResolveResult Resolve(AstNode node)
		{
			if (node == null || node.IsNull)
				return errorResult;
			bool oldResolverEnabled = resolverEnabled;
			resolverEnabled = true;
			ResolveResult result;
			if (!resolveResultCache.TryGetValue(node, out result)) {
				resolver.cancellationToken.ThrowIfCancellationRequested();
				StoreState(node, resolver.Clone());
				result = node.AcceptVisitor(this, null) ?? errorResult;
				Log.WriteLine("Resolved '{0}' to {1}", node, result);
				StoreResult(node, result);
			}
			resolverEnabled = oldResolverEnabled;
			return result;
		}
		
		IType ResolveType(AstType type)
		{
			return Resolve(type).Type;
		}
		
		void StoreState(AstNode node, CSharpResolver resolverState)
		{
			Debug.Assert(resolverState != null);
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			resolverBeforeDict[node] = resolverState;
		}
		
		void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			if (node.IsNull)
				return;
			Debug.Assert(!resolveResultCache.ContainsKey(node));
			resolveResultCache.Add(node, result);
			if (navigator != null)
				navigator.Resolved(node, result);
		}
		
		void ScanChildren(AstNode node)
		{
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				Scan(child);
			}
		}
		#endregion
		
		#region Process Conversions
		sealed class AnonymousFunctionConversionData
		{
			public readonly IType ReturnType;
			public readonly ExplicitlyTypedLambda ExplicitlyTypedLambda;
			public readonly LambdaTypeHypothesis Hypothesis;
			
			public AnonymousFunctionConversionData(IType returnType, LambdaTypeHypothesis hypothesis)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.Hypothesis = hypothesis;
			}
			
			public AnonymousFunctionConversionData(IType returnType, ExplicitlyTypedLambda explicitlyTypedLambda)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.ExplicitlyTypedLambda = explicitlyTypedLambda;
			}
		}
		
		/// <summary>
		/// Convert 'rr' to the target type using the specified conversion.
		/// </summary>
		void ProcessConversion(Expression expr, ResolveResult rr, Conversion conversion, IType targetType)
		{
			if (conversion.IsAnonymousFunctionConversion) {
				Log.WriteLine("Processing conversion of anonymous function to " + targetType + "...");
				AnonymousFunctionConversionData data = conversion.data as AnonymousFunctionConversionData;
				if (data != null) {
					Log.Indent();
					if (data.Hypothesis != null)
						data.Hypothesis.MergeInto(this, data.ReturnType);
					if (data.ExplicitlyTypedLambda != null)
						data.ExplicitlyTypedLambda.ApplyReturnType(this, data.ReturnType);
					Log.Unindent();
				} else {
					Log.WriteLine("  Data not found.");
				}
			}
			if (expr != null && conversion != Conversion.IdentityConversion)
				navigator.ProcessConversion(expr, rr, conversion, targetType);
		}
		
		/// <summary>
		/// Convert 'rr' to the target type.
		/// </summary>
		void ProcessConversion(Expression expr, ResolveResult rr, IType targetType)
		{
			ProcessConversion(expr, rr, resolver.conversions.ImplicitConversion(rr, targetType), targetType);
		}
		
		/// <summary>
		/// Resolves the specified expression and processes the conversion to targetType.
		/// </summary>
		void ResolveAndProcessConversion(Expression expr, IType targetType)
		{
			if (targetType.Kind == TypeKind.Unknown || targetType.Kind == TypeKind.Void) {
				// no need to resolve the expression right now
				Scan(expr);
			} else {
				ProcessConversion(expr, Resolve(expr), targetType);
			}
		}
		
		void ProcessConversionResult(Expression expr, ConversionResolveResult rr)
		{
			if (rr != null)
				ProcessConversion(expr, rr.Input, rr.Conversion, rr.Type);
		}
		
		void ProcessConversionResults(IEnumerable<Expression> expr, IEnumerable<ResolveResult> conversionResolveResults)
		{
			Debug.Assert(expr.Count() == conversionResolveResults.Count());
			using (var e1 = expr.GetEnumerator()) {
				using (var e2 = conversionResolveResults.GetEnumerator()) {
					while (e1.MoveNext() && e2.MoveNext()) {
						ProcessConversionResult(e1.Current, e2.Current as ConversionResolveResult);
					}
				}
			}
		}
		
		void ProcessConversionsInInvocation(Expression target, IEnumerable<Expression> arguments, CSharpInvocationResolveResult invocation)
		{
			if (invocation == null)
				return;
			int i = 0;
			if (invocation.IsExtensionMethodInvocation) {
				Debug.Assert(arguments.Count() + 1 == invocation.Arguments.Count);
				ProcessConversionResult(target, invocation.Arguments[0] as ConversionResolveResult);
				i = 1;
			} else {
				Debug.Assert(arguments.Count() == invocation.Arguments.Count);
			}
			foreach (Expression arg in arguments) {
				NamedArgumentExpression nae = arg as NamedArgumentExpression;
				if (nae != null)
					ProcessConversionResult(nae.Expression, invocation.Arguments[i++] as ConversionResolveResult);
				else
					ProcessConversionResult(arg, invocation.Arguments[i++] as ConversionResolveResult);
			}
		}
		#endregion
		
		#region GetResolveResult
		/// <summary>
		/// Gets the resolve result for the specified node.
		/// If the node was not resolved by the navigator, this method will resolve it.
		/// </summary>
		public ResolveResult GetResolveResult(AstNode node)
		{
			if (IsUnresolvableNode(node))
				return null;
			
			MergeUndecidedLambdas();
			ResolveResult result;
			if (resolveResultCache.TryGetValue(node, out result))
				return result;
			
			bool needResolveParent = (node.NodeType == NodeType.Token || IsVar(node));
			
			AstNode nodeToResolve = node;
			if (needResolveParent) {
				nodeToResolve = node.Parent;
				if (resolveResultCache.ContainsKey(nodeToResolve))
					return null;
			}
			
			AstNode parent;
			CSharpResolver storedResolver = GetPreviouslyScannedContext(nodeToResolve, out parent);
			ResetContext(
				storedResolver.Clone(),
				delegate {
					navigator = new NodeListResolveVisitorNavigator(nodeToResolve);
					if (parent == nodeToResolve) {
						Resolve(nodeToResolve);
					} else {
						Debug.Assert(!resolverEnabled);
						parent.AcceptVisitor(this, null);
					}
				});
			
			MergeUndecidedLambdas();
			if (resolveResultCache.TryGetValue(node, out result))
				return result;
			else
				return null;
		}
		
		/// <summary>
		/// Gets whether the specified node is unresolvable.
		/// </summary>
		public static bool IsUnresolvableNode(AstNode node)
		{
			return (node.NodeType == NodeType.Whitespace || node is ArraySpecifier || node is NamedArgumentExpression);
		}
		
		/// <summary>
		/// Gets the resolve result for the specified node.
		/// If the node was not resolved by the navigator, this method will return null.
		/// </summary>
		public ResolveResult GetResolveResultIfResolved(AstNode node)
		{
			MergeUndecidedLambdas();
			ResolveResult result;
			if (resolveResultCache.TryGetValue(node, out result))
				return result;
			else
				return null;
		}
		
		CSharpResolver GetPreviouslyScannedContext(AstNode node, out AstNode parent)
		{
			parent = node;
			CSharpResolver storedResolver;
			while (!resolverBeforeDict.TryGetValue(parent, out storedResolver)) {
				parent = parent.Parent;
				if (parent == null)
					throw new InvalidOperationException("Could not find a resolver state for any parent of the specified node. Did you forget to call 'Scan(compilationUnit);'?");
			}
			return storedResolver;
		}
		
		/// <summary>
		/// Gets the resolver state in front of the specified node.
		/// If the node was not visited by a previous scanning process, the
		/// AST will be scanned again to determine the state.
		/// </summary>
		public CSharpResolver GetResolverStateBefore(AstNode node)
		{
			MergeUndecidedLambdas();
			CSharpResolver r;
			if (resolverBeforeDict.TryGetValue(node, out r))
				return r;
			
			AstNode parent;
			CSharpResolver storedResolver = GetPreviouslyScannedContext(node, out parent);
			ResetContext(
				storedResolver.Clone(),
				delegate {
					navigator = new NodeListResolveVisitorNavigator(new[] { node }, scanOnly: true);
					Debug.Assert(!resolverEnabled);
					parent.AcceptVisitor(this, null);
				});
			
			MergeUndecidedLambdas();
			while (node != null) {
				if (resolverBeforeDict.TryGetValue(node, out r))
					return r;
				node = node.Parent;
			}
			return null;
		}
		#endregion
		
		#region Track UsingScope
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCompilationUnit(CompilationUnit unit, object data)
		{
			UsingScope previousUsingScope = resolver.CurrentUsingScope;
			try {
				if (parsedFile != null)
					resolver.CurrentUsingScope = parsedFile.RootUsingScope;
				ScanChildren(unit);
				return voidResult;
			} finally {
				resolver.CurrentUsingScope = previousUsingScope;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			UsingScope previousUsingScope = resolver.CurrentUsingScope;
			try {
				if (parsedFile != null) {
					resolver.CurrentUsingScope = parsedFile.GetUsingScope(namespaceDeclaration.StartLocation);
				}
				ScanChildren(namespaceDeclaration);
				// merge undecided lambdas before leaving the using scope so that
				// the resolver can make better use of its cache
				MergeUndecidedLambdas();
				if (resolver.CurrentUsingScope != null)
					return new NamespaceResolveResult(resolver.CurrentUsingScope.NamespaceName);
				else
					return null;
			} finally {
				resolver.CurrentUsingScope = previousUsingScope;
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
					foreach (ITypeDefinition nestedType in resolver.CurrentTypeDefinition.NestedTypes) {
						if (nestedType.Region.IsInside(typeDeclaration.StartLocation)) {
							newTypeDefinition = nestedType;
							break;
						}
					}
				} else if (parsedFile != null) {
					newTypeDefinition = parsedFile.GetTopLevelTypeDefinition(typeDeclaration.StartLocation);
				}
				if (newTypeDefinition != null)
					resolver.CurrentTypeDefinition = newTypeDefinition;
				
				for (AstNode child = typeDeclaration.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == TypeDeclaration.BaseTypeRole) {
						currentTypeLookupMode = SimpleNameLookupMode.BaseTypeReference;
						Scan(child);
						currentTypeLookupMode = SimpleNameLookupMode.Type;
					} else {
						Scan(child);
					}
				}
				
				// merge undecided lambdas before leaving the type definition so that
				// the resolver can make better use of its cache
				MergeUndecidedLambdas();
				
				return newTypeDefinition != null ? new TypeResolveResult(newTypeDefinition) : errorResult;
			} finally {
				resolver.CurrentTypeDefinition = previousTypeDefinition;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			return VisitTypeOrDelegate(typeDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			return VisitTypeOrDelegate(delegateDeclaration);
		}
		#endregion
		
		#region Track CurrentMember
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(fieldDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(fixedFieldDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(eventDeclaration);
		}
		
		ResolveResult VisitFieldOrEventDeclaration(AttributedNode fieldOrEventDeclaration)
		{
			int initializerCount = fieldOrEventDeclaration.GetChildrenByRole(FieldDeclaration.Roles.Variable).Count;
			ResolveResult result = null;
			for (AstNode node = fieldOrEventDeclaration.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FieldDeclaration.Roles.Variable) {
					if (resolver.CurrentTypeDefinition != null) {
						IEnumerable<IMember> members;
						if (fieldOrEventDeclaration is EventDeclaration)
							members = resolver.CurrentTypeDefinition.Events;
						else
							members = resolver.CurrentTypeDefinition.Fields;
						resolver.CurrentMember = members.FirstOrDefault(f => f.Region.IsInside(node.StartLocation));
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
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (variableInitializer.Parent is FieldDeclaration || variableInitializer.Parent is EventDeclaration) {
					if (resolver.CurrentMember != null) {
						result = new MemberResolveResult(null, resolver.CurrentMember, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
					}
				} else {
					string identifier = variableInitializer.Name;
					foreach (IVariable v in resolver.LocalVariables) {
						if (v.Name == identifier) {
							object constantValue = v.IsConst ? v.ConstantValue.Resolve(resolver.Context).ConstantValue : null;
							result = new LocalResolveResult(v, v.Type.Resolve(resolver.Context), constantValue);
							break;
						}
					}
				}
				ArrayInitializerExpression aie = variableInitializer.Initializer as ArrayInitializerExpression;
				ArrayType arrayType = result.Type as ArrayType;
				if (aie != null && arrayType != null) {
					StoreState(aie, resolver.Clone());
					List<Expression> initializerElements = new List<Expression>();
					UnpackArrayInitializer(initializerElements, aie, arrayType.Dimensions, true);
					ResolveResult[] initializerElementResults = new ResolveResult[initializerElements.Count];
					for (int i = 0; i < initializerElementResults.Length; i++) {
						initializerElementResults[i] = Resolve(initializerElements[i]);
					}
					var arrayCreation = resolver.ResolveArrayCreation(arrayType.ElementType, arrayType.Dimensions, null, initializerElementResults);
					StoreResult(aie, arrayCreation);
					ProcessConversionResults(initializerElements, arrayCreation.InitializerElements);
				} else {
					ResolveAndProcessConversion(variableInitializer.Initializer, result.Type);
				}
				return result;
			} else {
				ScanChildren(variableInitializer);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, object data)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (resolver.CurrentMember != null) {
					result = new MemberResolveResult(null, resolver.CurrentMember, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
				}
				ResolveAndProcessConversion(fixedVariableInitializer.CountExpression, KnownTypeReference.Int32.Resolve(resolver.Context));
				return result;
			} else {
				ScanChildren(fixedVariableInitializer);
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
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			return VisitMethodMember(methodDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			return VisitMethodMember(operatorDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			return VisitMethodMember(constructorDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
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
						resolver.AddVariable(resolver.CurrentMember.ReturnType, DomRegion.Empty, "value");
						Scan(node);
						resolver.PopBlock();
					} else {
						Scan(node);
					}
				}
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			return VisitPropertyMember(propertyDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			return VisitPropertyMember(indexerDeclaration);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Events.FirstOrDefault(e => e.Region.IsInside(eventDeclaration.StartLocation));
				}
				
				if (resolver.CurrentMember != null) {
					resolver.PushBlock();
					resolver.AddVariable(resolver.CurrentMember.ReturnType, DomRegion.Empty, "value");
					ScanChildren(eventDeclaration);
					resolver.PopBlock();
				} else {
					ScanChildren(eventDeclaration);
				}
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			ScanChildren(parameterDeclaration);
			if (resolverEnabled) {
				string name = parameterDeclaration.Name;
				// Look in lambda parameters:
				foreach (IParameter p in resolver.LocalVariables.OfType<IParameter>()) {
					if (p.Name == name)
						return new LocalResolveResult(p, p.Type.Resolve(resolver.Context));
				}
				
				IParameterizedMember pm = resolver.CurrentMember as IParameterizedMember;
				if (pm != null) {
					foreach (IParameter p in pm.Parameters) {
						if (p.Name == name) {
							return new LocalResolveResult(p, p.Type.Resolve(resolver.Context));
						}
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			ScanChildren(typeParameterDeclaration);
			if (resolverEnabled) {
				string name = typeParameterDeclaration.Name;
				IMethod m = resolver.CurrentMember as IMethod;
				if (m != null) {
					foreach (var tp in m.TypeParameters) {
						if (tp.Name == name)
							return new TypeResolveResult(tp);
					}
				}
				if (resolver.CurrentTypeDefinition != null) {
					var typeParameters = resolver.CurrentTypeDefinition.TypeParameters;
					// look backwards so that TPs in the current type take precedence over those copied from outer types
					for (int i = typeParameters.Count - 1; i >= 0; i--) {
						if (typeParameters[i].Name == name)
							return new TypeResolveResult(typeParameters[i]);
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Fields.FirstOrDefault(f => f.Region.IsInside(enumMemberDeclaration.StartLocation));
				}
				
				ScanChildren(enumMemberDeclaration);
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		#endregion
		
		#region Track CheckForOverflow
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCheckedExpression(CheckedExpression checkedExpression, object data)
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
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
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
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = true;
				ScanChildren(checkedStatement);
				return voidResult;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = false;
				ScanChildren(uncheckedStatement);
				return voidResult;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		#endregion
		
		#region Visit Expressions
		static string GetAnonymousTypePropertyName(Expression expr, out Expression resolveExpr)
		{
			if (expr is NamedExpression) {
				var namedArgExpr = (NamedExpression)expr;
				resolveExpr = namedArgExpr.Expression;
				return namedArgExpr.Identifier;
			}
			// no name given, so it's a projection initializer
			if (expr is MemberReferenceExpression) {
				resolveExpr = expr;
				return ((MemberReferenceExpression)expr).MemberName;
			}
			if (expr is IdentifierExpression) {
				resolveExpr = expr;
				return ((IdentifierExpression)expr).Identifier;
			}
			resolveExpr = null;
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, object data)
		{
			// 7.6.10.6 Anonymous object creation expressions
			if (resolver.ProjectContent == null) {
				ScanChildren(anonymousTypeCreateExpression);
				return errorResult;
			}
			var anonymousType = new DefaultTypeDefinition(resolver.ProjectContent, string.Empty, "$Anonymous$");
			anonymousType.IsSynthetic = true;
			resolver.PushInitializerType(anonymousType);
			foreach (var expr in anonymousTypeCreateExpression.Initializers) {
				Expression resolveExpr;
				var name = GetAnonymousTypePropertyName(expr, out resolveExpr);
				if (!string.IsNullOrEmpty(name)) {
					var property = new DefaultProperty(anonymousType, name) {
						Accessibility = Accessibility.Public,
						ReturnType = new VarTypeReference(this, resolver.Clone(), resolveExpr, false)
					};
					anonymousType.Properties.Add(property);
				}
				Scan(expr);
			}
			ScanChildren(anonymousTypeCreateExpression);
			resolver.PopInitializerType();
			return new ResolveResult(anonymousType);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(arrayCreateExpression);
				return null;
			}
			
			int dimensions = arrayCreateExpression.Arguments.Count;
			ResolveResult[] sizeArguments;
			IEnumerable<ArraySpecifier> additionalArraySpecifiers;
			if (dimensions == 0) {
				var firstSpecifier = arrayCreateExpression.AdditionalArraySpecifiers.FirstOrDefault();
				if (firstSpecifier != null) {
					dimensions = firstSpecifier.Dimensions;
					additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers.Skip(1);
				} else {
					dimensions = 0;
					additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers;
				}
				sizeArguments = null;
			} else {
				sizeArguments = new ResolveResult[dimensions];
				int pos = 0;
				foreach (var node in arrayCreateExpression.Arguments)
					sizeArguments[pos++] = Resolve(node);
				additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers;
			}
			
			List<Expression> initializerElements;
			ResolveResult[] initializerElementResults;
			if (arrayCreateExpression.Initializer.IsNull) {
				initializerElements = null;
				initializerElementResults = null;
			} else {
				initializerElements = new List<Expression>();
				UnpackArrayInitializer(initializerElements, arrayCreateExpression.Initializer, dimensions, true);
				initializerElementResults = new ResolveResult[initializerElements.Count];
				for (int i = 0; i < initializerElementResults.Length; i++) {
					initializerElementResults[i] = Resolve(initializerElements[i]);
				}
				if (!resolveResultCache.ContainsKey(arrayCreateExpression.Initializer))
					StoreResult(arrayCreateExpression.Initializer, voidResult);
			}
			
			ArrayCreateResolveResult acrr;
			if (arrayCreateExpression.Type.IsNull) {
				acrr = resolver.ResolveArrayCreation(null, dimensions, sizeArguments, initializerElementResults);
			} else {
				IType elementType = ResolveType(arrayCreateExpression.Type);
				foreach (var spec in additionalArraySpecifiers.Reverse()) {
					elementType = new ArrayType(elementType, spec.Dimensions);
				}
				acrr = resolver.ResolveArrayCreation(elementType, dimensions, sizeArguments, initializerElementResults);
			}
			return acrr;
		}
		
		void UnpackArrayInitializer(List<Expression> elementList, ArrayInitializerExpression initializer, int dimensions, bool resolveNestedInitializesToVoid)
		{
			Debug.Assert(dimensions >= 1);
			if (dimensions > 1) {
				foreach (var node in initializer.Elements) {
					ArrayInitializerExpression aie = node as ArrayInitializerExpression;
					if (aie != null) {
						if (resolveNestedInitializesToVoid)
							StoreResult(aie, voidResult);
						UnpackArrayInitializer(elementList, aie, dimensions - 1, resolveNestedInitializesToVoid);
					} else {
						elementList.Add(node);
					}
				}
			} else {
				foreach (var expr in initializer.Elements)
					elementList.Add(expr);
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			// Array initializers are handled by their parent expression.
			ScanChildren(arrayInitializerExpression);
			return errorResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAsExpression(AsExpression asExpression, object data)
		{
			if (resolverEnabled) {
				Scan(asExpression.Expression);
				return new ResolveResult(ResolveType(asExpression.Type));
			} else {
				ScanChildren(asExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult left = Resolve(assignmentExpression.Left);
				ResolveAndProcessConversion(assignmentExpression.Right, left.Type);
				return new ResolveResult(left.Type);
			} else {
				ScanChildren(assignmentExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveBaseReference();
			} else {
				ScanChildren(baseReferenceExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				Expression left = binaryOperatorExpression.Left;
				Expression right = binaryOperatorExpression.Right;
				ResolveResult leftResult = Resolve(left);
				ResolveResult rightResult = Resolve(right);
				ResolveResult rr = resolver.ResolveBinaryOperator(binaryOperatorExpression.Operator, leftResult, rightResult);
				BinaryOperatorResolveResult borr = rr as BinaryOperatorResolveResult;
				if (borr != null) {
					ProcessConversionResult(left, borr.Left as ConversionResolveResult);
					ProcessConversionResult(right, borr.Right as ConversionResolveResult);
				} else {
					InvocationResolveResult irr = rr as InvocationResolveResult;
					if (irr != null && irr.Arguments.Count == 2) {
						ProcessConversionResult(left, irr.Arguments[0] as ConversionResolveResult);
						ProcessConversionResult(right, irr.Arguments[1] as ConversionResolveResult);
					}
				}
				return rr;
			} else {
				ScanChildren(binaryOperatorExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCastExpression(CastExpression castExpression, object data)
		{
			if (resolverEnabled) {
				IType targetType = ResolveType(castExpression.Type);
				Expression expr = castExpression.Expression;
				ResolveResult rr = resolver.ResolveCast(targetType, Resolve(expr));
				ProcessConversionResult(expr, rr as ConversionResolveResult);
				return rr;
			} else {
				ScanChildren(castExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			if (resolverEnabled) {
				Expression condition = conditionalExpression.Condition;
				Expression trueExpr = conditionalExpression.TrueExpression;
				Expression falseExpr = conditionalExpression.FalseExpression;
				
				ResolveResult rr = resolver.ResolveConditional(Resolve(condition), Resolve(trueExpr), Resolve(falseExpr));
				ConditionalOperatorResolveResult corr = rr as ConditionalOperatorResolveResult;
				if (corr != null) {
					ProcessConversionResult(condition, corr.Condition as ConversionResolveResult);
					ProcessConversionResult(trueExpr, corr.True as ConversionResolveResult);
					ProcessConversionResult(falseExpr, corr.False as ConversionResolveResult);
				}
				return rr;
			} else {
				ScanChildren(conditionalExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveDefaultValue(ResolveType(defaultValueExpression.Type));
			} else {
				ScanChildren(defaultValueExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult rr = Resolve(directionExpression.Expression);
				return new ByReferenceResolveResult(rr, directionExpression.FieldDirection == FieldDirection.Out);
			} else {
				ScanChildren(directionExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitEmptyExpression(EmptyExpression emptyExpression, object data)
		{
			return errorResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (resolverEnabled) {
				Expression target = indexerExpression.Target;
				ResolveResult targetResult = Resolve(target);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(indexerExpression.Arguments, out argumentNames);
				ResolveResult rr = resolver.ResolveIndexer(targetResult, arguments, argumentNames);
				ArrayAccessResolveResult aarr = rr as ArrayAccessResolveResult;
				if (aarr != null) {
					ProcessConversionResults(indexerExpression.Arguments, aarr.Indices);
				} else {
					ProcessConversionsInInvocation(target, indexerExpression.Arguments, rr as CSharpInvocationResolveResult);
				}
				return rr;
			} else {
				ScanChildren(indexerExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIsExpression(IsExpression isExpression, object data)
		{
			ScanChildren(isExpression);
			if (resolverEnabled)
				return new ResolveResult(KnownTypeReference.Boolean.Resolve(resolver.Context));
			else
				return null;
		}
		
		// NamedArgumentExpression is "identifier: Expression"
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			// The parent expression takes care of handling NamedArgumentExpressions
			// by calling GetArguments().
			// This method gets called only when scanning, or when the named argument is used
			// in an invalid context.
			Scan(namedArgumentExpression.Expression);
			return errorResult;
		}
		
		// NamedExpression is "identifier = Expression" in object initializers and attributes
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNamedExpression(NamedExpression namedExpression, object data)
		{
			Expression rhs = namedExpression.Expression;
			if (rhs is ArrayInitializerExpression) {
				ResolveResult result = resolver.ResolveIdentifierInObjectInitializer(namedExpression.Identifier);
				HandleObjectInitializer(result.Type, (ArrayInitializerExpression)rhs);
				return result;
			} else {
				if (resolverEnabled) {
					ResolveResult result = resolver.ResolveIdentifierInObjectInitializer(namedExpression.Identifier);
					ResolveAndProcessConversion(rhs, result.Type);
					return result;
				} else {
					ScanChildren(namedExpression);
					return null;
				}
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(null);
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (resolverEnabled || !objectCreateExpression.Initializer.IsNull) {
				IType type = ResolveType(objectCreateExpression.Type);
				
				var initializer = objectCreateExpression.Initializer;
				if (!initializer.IsNull) {
					HandleObjectInitializer(type, initializer);
				}
				
				if (resolverEnabled) {
					string[] argumentNames;
					ResolveResult[] arguments = GetArguments(objectCreateExpression.Arguments, out argumentNames);
					
					ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames);
					ProcessConversionsInInvocation(null, objectCreateExpression.Arguments, rr as CSharpInvocationResolveResult);
					return rr;
				} else {
					foreach (AstNode node in objectCreateExpression.Arguments) {
						Scan(node);
					}
					return null;
				}
			} else {
				ScanChildren(objectCreateExpression);
				return null;
			}
		}
		
		void HandleObjectInitializer(IType type, ArrayInitializerExpression initializer)
		{
			resolver.PushInitializerType(type);
			foreach (Expression element in initializer.Elements) {
				ArrayInitializerExpression aie = element as ArrayInitializerExpression;
				if (aie != null) {
					if (resolveResultCache.ContainsKey(aie)) {
						// Don't resolve the add call again if we already did so
						continue;
					}
					StoreState(aie, resolver.Clone());
					// constructor argument list in collection initializer
					ResolveResult[] addArguments = new ResolveResult[aie.Elements.Count];
					int i = 0;
					foreach (var addArgument in aie.Elements) {
						addArguments[i++] = Resolve(addArgument);
					}
					MemberLookup memberLookup = resolver.CreateMemberLookup();
					ResolveResult targetResult = new ResolveResult(type);
					var addRR = memberLookup.Lookup(targetResult, "Add", EmptyList<IType>.Instance, true);
					var mgrr = addRR as MethodGroupResolveResult;
					if (mgrr != null) {
						OverloadResolution or = mgrr.PerformOverloadResolution(resolver.Context, addArguments, null, false, false, resolver.conversions);
						var invocationRR = or.CreateResolveResult(targetResult);
						StoreResult(aie, invocationRR);
						ProcessConversionsInInvocation(null, aie.Elements, invocationRR);
					} else {
						StoreResult(aie, addRR);
					}
				} else {
					// assignment in object initializer (NamedExpression),
					// or some unknown kind of expression
					Scan(element);
				}
			}
			resolver.PopInitializerType();
			if (!resolveResultCache.ContainsKey(initializer))
				StoreResult(initializer, voidResult);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (resolverEnabled) {
				return Resolve(parenthesizedExpression.Expression);
			} else {
				Scan(parenthesizedExpression.Expression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(pointerReferenceExpression.Target);
				ResolveResult deferencedTarget = resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, target);
				List<IType> typeArguments = new List<IType>();
				foreach (AstType typeArgument in pointerReferenceExpression.TypeArguments) {
					typeArguments.Add(ResolveType(typeArgument));
				}
				return resolver.ResolveMemberAccess(deferencedTarget, pointerReferenceExpression.MemberName,
				                                    typeArguments,
				                                    IsTargetOfInvocation(pointerReferenceExpression));
			} else {
				ScanChildren(pointerReferenceExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(primitiveExpression.Value);
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveSizeOf(ResolveType(sizeOfExpression.Type));
			} else {
				ScanChildren(sizeOfExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			if (resolverEnabled) {
				ResolveAndProcessConversion(stackAllocExpression.CountExpression, KnownTypeReference.Int32.Resolve(resolver.Context));
				return new ResolveResult(new PointerType(ResolveType(stackAllocExpression.Type)));
			} else {
				ScanChildren(stackAllocExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			if (resolverEnabled)
				return resolver.ResolveThisReference();
			else
				return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			ScanChildren(typeOfExpression);
			if (resolverEnabled)
				return new ResolveResult(KnownTypeReference.Type.Resolve(resolver.Context));
			else
				return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return Resolve(typeReferenceExpression.Type);
			} else {
				Scan(typeReferenceExpression.Type);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				Expression expr = unaryOperatorExpression.Expression;
				ResolveResult input = Resolve(expr);
				ResolveResult rr = resolver.ResolveUnaryOperator(unaryOperatorExpression.Operator, input);
				UnaryOperatorResolveResult uorr = rr as UnaryOperatorResolveResult;
				if (uorr != null) {
					ProcessConversionResult(expr, uorr.Input as ConversionResolveResult);
				} else {
					InvocationResolveResult irr = rr as InvocationResolveResult;
					if (irr != null && irr.Arguments.Count == 1) {
						ProcessConversionResult(expr, irr.Arguments[0] as ConversionResolveResult);
					}
				}
				return rr;
			} else {
				ScanChildren(unaryOperatorExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, object data)
		{
			ScanChildren(undocumentedExpression);
			if (resolverEnabled) {
				ITypeReference resultType;
				switch (undocumentedExpression.UndocumentedExpressionType) {
					case UndocumentedExpressionType.ArgListAccess:
					case UndocumentedExpressionType.ArgList:
						resultType = typeof(RuntimeArgumentHandle).ToTypeReference();
						break;
					case UndocumentedExpressionType.RefValue:
						var tre = undocumentedExpression.Arguments.ElementAtOrDefault(1) as TypeReferenceExpression;
						if (tre != null)
							resultType = ResolveType(tre.Type);
						else
							resultType = SharedTypes.UnknownType;
						break;
					case UndocumentedExpressionType.RefType:
						resultType = KnownTypeReference.Type;
						break;
					case UndocumentedExpressionType.MakeRef:
						resultType = typeof(TypedReference).ToTypeReference();
						break;
					default:
						throw new InvalidOperationException("Invalid value for UndocumentedExpressionType");
				}
				return new ResolveResult(resultType.Resolve(resolver.Context));
			} else {
				return null;
			}
		}
		#endregion
		
		#region Visit Identifier/MemberReference/Invocation-Expression
		// IdentifierExpression, MemberReferenceExpression and InvocationExpression
		// are grouped together because they have to work together for
		// "7.6.4.1 Identical simple names and type names" support
		List<IType> GetTypeArguments(IEnumerable<AstType> typeArguments)
		{
			List<IType> result = new List<IType>();
			foreach (AstType typeArgument in typeArguments) {
				result.Add(ResolveType(typeArgument));
			}
			return result;
		}
		
		ResolveResult[] GetArguments(IEnumerable<Expression> argumentExpressions, out string[] argumentNames)
		{
			argumentNames = null;
			ResolveResult[] arguments = new ResolveResult[argumentExpressions.Count()];
			int i = 0;
			foreach (AstNode argument in argumentExpressions) {
				NamedArgumentExpression nae = argument as NamedArgumentExpression;
				AstNode argumentValue;
				if (nae != null) {
					if (argumentNames == null)
						argumentNames = new string[arguments.Length];
					argumentNames[i] = nae.Identifier;
					argumentValue = nae.Expression;
				} else {
					argumentValue = argument;
				}
				arguments[i++] = Resolve(argumentValue);
			}
			return arguments;
		}
		
		static bool IsTargetOfInvocation(AstNode node)
		{
			InvocationExpression ie = node.Parent as InvocationExpression;
			return ie != null && ie.Target == node;
		}
		
		bool IsVariableReferenceWithSameType(ResolveResult rr, string identifier, out TypeResolveResult trr)
		{
			if (!(rr is MemberResolveResult || rr is LocalResolveResult)) {
				trr = null;
				return false;
			}
			trr = resolver.LookupSimpleNameOrTypeName(identifier, EmptyList<IType>.Instance, SimpleNameLookupMode.Type) as TypeResolveResult;
			return trr != null && trr.Type.Equals(rr.Type);
		}
		
		/// <summary>
		/// Gets whether 'rr' is considered a static access on the target identifier.
		/// </summary>
		/// <param name="rr">Resolve Result of the MemberReferenceExpression</param>
		/// <param name="invocationRR">Resolve Result of the InvocationExpression</param>
		bool IsStaticResult(ResolveResult rr, ResolveResult invocationRR)
		{
			if (rr is TypeResolveResult)
				return true;
			MemberResolveResult mrr = (rr is MethodGroupResolveResult ? invocationRR : rr) as MemberResolveResult;
			return mrr != null && mrr.Member.IsStatic;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			// Note: this method is not called when it occurs in a situation where an ambiguity between
			// simple names and type names might occur.
			if (resolverEnabled) {
				var typeArguments = GetTypeArguments(identifierExpression.TypeArguments);
				return resolver.ResolveSimpleName(identifierExpression.Identifier, typeArguments,
				                                  IsTargetOfInvocation(identifierExpression));
			} else {
				ScanChildren(identifierExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			// target = Resolve(identifierExpression = memberReferenceExpression.Target)
			// trr = ResolveType(identifierExpression)
			// rr = Resolve(memberReferenceExpression)
			
			IdentifierExpression identifierExpression = memberReferenceExpression.Target as IdentifierExpression;
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0
			    && !resolveResultCache.ContainsKey(identifierExpression))
			{
				// Special handling for §7.6.4.1 Identicial simple names and type names
				StoreState(identifierExpression, resolver.Clone());
				ResolveResult target = resolver.ResolveSimpleName(identifierExpression.Identifier, EmptyList<IType>.Instance);
				TypeResolveResult trr;
				if (IsVariableReferenceWithSameType(target, identifierExpression.Identifier, out trr)) {
					// It's ambiguous
					ResolveResult rr = ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
					ResolveResult simpleNameRR = IsStaticResult(rr, null) ? trr : target;
					Log.WriteLine("Ambiguous simple name '{0}' was resolved to {1}", identifierExpression, simpleNameRR);
					StoreResult(identifierExpression, simpleNameRR);
					return rr;
				} else {
					// It's not ambiguous
					Log.WriteLine("Simple name '{0}' was resolved to {1}", identifierExpression, target);
					StoreResult(identifierExpression, target);
					if (resolverEnabled) {
						return ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
					} else {
						// Scan children (but not the IdentifierExpression which we already resolved)
						for (AstNode child = memberReferenceExpression.FirstChild; child != null; child = child.NextSibling) {
							if (child != identifierExpression)
								Scan(child);
						}
						return null;
					}
				}
			} else {
				// Regular code path
				if (resolverEnabled) {
					ResolveResult target = Resolve(memberReferenceExpression.Target);
					return ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
				} else {
					ScanChildren(memberReferenceExpression);
					return null;
				}
			}
		}
		
		ResolveResult ResolveMemberReferenceOnGivenTarget(ResolveResult target, MemberReferenceExpression memberReferenceExpression)
		{
			var typeArguments = GetTypeArguments(memberReferenceExpression.TypeArguments);
			return resolver.ResolveMemberAccess(
				target, memberReferenceExpression.MemberName, typeArguments,
				IsTargetOfInvocation(memberReferenceExpression));
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			// rr = Resolve(invocationExpression)
			// target = Resolve(memberReferenceExpression = invocationExpression.Target)
			// idRR = Resolve(identifierExpression = memberReferenceExpression.Target)
			// trr = ResolveType(identifierExpression)
			
			MemberReferenceExpression mre = invocationExpression.Target as MemberReferenceExpression;
			IdentifierExpression identifierExpression = mre != null ? mre.Target as IdentifierExpression : null;
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0
			    && !resolveResultCache.ContainsKey(identifierExpression))
			{
				// Special handling for §7.6.4.1 Identicial simple names and type names
				ResolveResult idRR = resolver.ResolveSimpleName(identifierExpression.Identifier, EmptyList<IType>.Instance);
				ResolveResult target = ResolveMemberReferenceOnGivenTarget(idRR, mre);
				Log.WriteLine("Member reference '{0}' on potentially-ambiguous simple-name was resolved to {1}", mre, target);
				StoreResult(mre, target);
				TypeResolveResult trr;
				if (IsVariableReferenceWithSameType(idRR, identifierExpression.Identifier, out trr)) {
					// It's ambiguous
					ResolveResult rr = ResolveInvocationOnGivenTarget(target, invocationExpression);
					ResolveResult simpleNameRR = IsStaticResult(target, rr) ? trr : idRR;
					Log.WriteLine("Ambiguous simple name '{0}' was resolved to {1}",
					              identifierExpression, simpleNameRR);
					StoreResult(identifierExpression, simpleNameRR);
					return rr;
				} else {
					// It's not ambiguous
					Log.WriteLine("Simple name '{0}' was resolved to {1}", identifierExpression, idRR);
					StoreResult(identifierExpression, idRR);
					if (resolverEnabled) {
						return ResolveInvocationOnGivenTarget(target, invocationExpression);
					} else {
						// Scan children (but not the MRE which we already resolved)
						for (AstNode child = invocationExpression.FirstChild; child != null; child = child.NextSibling) {
							if (child != mre)
								Scan(child);
						}
						return null;
					}
				}
			} else {
				// Regular code path
				if (resolverEnabled) {
					ResolveResult target = Resolve(invocationExpression.Target);
					return ResolveInvocationOnGivenTarget(target, invocationExpression);
				} else {
					ScanChildren(invocationExpression);
					return null;
				}
			}
		}
		
		ResolveResult ResolveInvocationOnGivenTarget(ResolveResult target, InvocationExpression invocationExpression)
		{
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(invocationExpression.Arguments, out argumentNames);
			ResolveResult rr = resolver.ResolveInvocation(target, arguments, argumentNames);
			ProcessConversionsInInvocation(invocationExpression.Target, invocationExpression.Arguments, rr as CSharpInvocationResolveResult);
			return rr;
		}
		#endregion
		
		#region Lamdbas / Anonymous Functions
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			return HandleExplicitlyTypedLambda(
				anonymousMethodExpression.Parameters, anonymousMethodExpression.Body,
				isAnonymousMethod: true,
				hasParameterList: anonymousMethodExpression.HasParameterList,
				isAsync: anonymousMethodExpression.IsAsync);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			Debug.Assert(resolverEnabled);
			bool isExplicitlyTyped = false;
			bool isImplicitlyTyped = false;
			foreach (var p in lambdaExpression.Parameters) {
				isImplicitlyTyped |= p.Type.IsNull;
				isExplicitlyTyped |= !p.Type.IsNull;
			}
			if (isExplicitlyTyped || !isImplicitlyTyped) {
				return HandleExplicitlyTypedLambda(
					lambdaExpression.Parameters, lambdaExpression.Body,
					isAnonymousMethod: false, hasParameterList: true, isAsync: lambdaExpression.IsAsync);
			} else {
				return new ImplicitlyTypedLambda(lambdaExpression, this);
			}
		}
		
		#region Explicitly typed
		ExplicitlyTypedLambda HandleExplicitlyTypedLambda(
			AstNodeCollection<ParameterDeclaration> parameterDeclarations,
			AstNode body, bool isAnonymousMethod, bool hasParameterList, bool isAsync)
		{
			List<IParameter> parameters = new List<IParameter>();
			resolver.PushLambdaBlock();
			foreach (var pd in parameterDeclarations) {
				ITypeReference type = MakeTypeReference(pd.Type);
				if (pd.ParameterModifier == ParameterModifier.Ref || pd.ParameterModifier == ParameterModifier.Out)
					type = ByReferenceTypeReference.Create(type);
				
				var p = resolver.AddLambdaParameter(type, MakeRegion(pd), pd.Name,
				                                    isRef: pd.ParameterModifier == ParameterModifier.Ref,
				                                    isOut: pd.ParameterModifier == ParameterModifier.Out);
				parameters.Add(p);
				Scan(pd);
			}
			
			var lambda = new ExplicitlyTypedLambda(parameters, isAnonymousMethod, isAsync, resolver.Clone(), this, body);
			
			// Don't scan the lambda body here - we'll do that later when analyzing the ExplicitlyTypedLambda.
			
			resolver.PopBlock();
			return lambda;
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			return new DomRegion(parsedFile != null ? parsedFile.FileName : null, node.StartLocation, node.EndLocation);
		}
		
		sealed class ExplicitlyTypedLambda : LambdaBase
		{
			readonly IList<IParameter> parameters;
			readonly bool isAnonymousMethod;
			readonly bool isAsync;
			
			CSharpResolver storedContext;
			ResolveVisitor visitor;
			AstNode body;
			
			IType inferredReturnType;
			IList<Expression> returnExpressions;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			bool success;
			
			// The actual return type is set when the lambda is applied by the conversion.
			IType actualReturnType;
			
			internal override bool IsUndecided {
				get { return actualReturnType == null; }
			}
			
			internal override AstNode LambdaExpression {
				get { return body.Parent; }
			}
			
			internal override AstNode Body {
				get { return body; }
			}
			
			public ExplicitlyTypedLambda(IList<IParameter> parameters, bool isAnonymousMethod, bool isAsync, CSharpResolver storedContext, ResolveVisitor visitor, AstNode body)
			{
				this.parameters = parameters;
				this.isAnonymousMethod = isAnonymousMethod;
				this.isAsync = isAsync;
				this.storedContext = storedContext;
				this.visitor = visitor;
				this.body = body;
				
				if (visitor.undecidedLambdas == null)
					visitor.undecidedLambdas = new List<LambdaBase>();
				visitor.undecidedLambdas.Add(this);
				Log.WriteLine("Added undecided explicitly-typed lambda: " + this.LambdaExpression);
			}
			
			public override IList<IParameter> Parameters {
				get {
					return parameters ?? EmptyList<IParameter>.Instance;
				}
			}
			
			bool Analyze()
			{
				// If it's not already analyzed
				if (inferredReturnType == null) {
					Log.WriteLine("Analyzing " + this.LambdaExpression + "...");
					Log.Indent();
					
					visitor.ResetContext(
						storedContext,
						delegate {
							var oldNavigator = visitor.navigator;
							visitor.navigator = new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Resolve, oldNavigator);
							visitor.AnalyzeLambda(body, isAsync, out success, out isValidAsVoidMethod, out inferredReturnType, out returnExpressions, out returnValues);
							visitor.navigator = oldNavigator;
						});
					Log.Unindent();
					Log.WriteLine("Finished analyzing " + this.LambdaExpression);
					
					if (inferredReturnType == null)
						throw new InvalidOperationException("AnalyzeLambda() didn't set inferredReturnType");
				}
				return success;
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Log.WriteLine("Testing validity of {0} for return-type {1}...", this, returnType);
				Log.Indent();
				bool valid = Analyze() && IsValidLambda(isValidAsVoidMethod, isAsync, returnValues, returnType, conversions);
				Log.Unindent();
				Log.WriteLine("{0} is {1} for return-type {2}", this, valid ? "valid" : "invalid", returnType);
				if (valid) {
					return Conversion.AnonymousFunctionConversion(new AnonymousFunctionConversionData(returnType, this));
				} else {
					return Conversion.None;
				}
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				Analyze();
				return inferredReturnType;
			}
			
			public override bool IsImplicitlyTyped {
				get { return false; }
			}
			
			public override bool IsAsync {
				get { return isAsync; }
			}
			
			public override bool IsAnonymousMethod {
				get { return isAnonymousMethod; }
			}
			
			public override bool HasParameterList {
				get { return parameters != null; }
			}
			
			public override string ToString()
			{
				return "[ExplicitlyTypedLambda " + this.LambdaExpression + "]";
			}
			
			public void ApplyReturnType(ResolveVisitor parentVisitor, IType returnType)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				if (parentVisitor != visitor) {
					// Explicitly typed lambdas do not use a nested visitor
					throw new InvalidOperationException();
				}
				if (actualReturnType != null) {
					if (actualReturnType.Equals(returnType))
						return; // return type already set
					throw new InvalidOperationException("inconsistent return types for explicitly-typed lambda");
				}
				actualReturnType = returnType;
				visitor.undecidedLambdas.Remove(this);
				Analyze();
				Log.WriteLine("Applying return type {0} to explicitly-typed lambda {1}", returnType, this.LambdaExpression);
				if (isAsync)
					returnType = parentVisitor.UnpackTask(returnType);
				for (int i = 0; i < returnExpressions.Count; i++) {
					visitor.ProcessConversion(returnExpressions[i], returnValues[i], returnType);
				}
			}
			
			internal override void EnforceMerge(ResolveVisitor parentVisitor)
			{
				ApplyReturnType(parentVisitor, SharedTypes.UnknownType);
			}
		}
		#endregion
		
		#region Implicitly typed
		sealed class ImplicitlyTypedLambda : LambdaBase
		{
			readonly LambdaExpression lambda;
			readonly QuerySelectClause selectClause;
			
			readonly CSharpResolver storedContext;
			readonly CSharpParsedFile parsedFile;
			readonly List<LambdaTypeHypothesis> hypotheses = new List<LambdaTypeHypothesis>();
			readonly List<IParameter> parameters = new List<IParameter>();
			
			internal LambdaTypeHypothesis winningHypothesis;
			internal readonly ResolveVisitor parentVisitor;
			
			internal override bool IsUndecided {
				get { return winningHypothesis == null;  }
			}
			
			internal override AstNode LambdaExpression {
				get {
					if (selectClause != null)
						return selectClause.Expression;
					else
						return lambda;
				}
			}
			
			internal override AstNode Body {
				get {
					if (selectClause != null)
						return selectClause.Expression;
					else
						return lambda.Body;
				}
			}
			
			private ImplicitlyTypedLambda(ResolveVisitor parentVisitor)
			{
				this.parentVisitor = parentVisitor;
				this.storedContext = parentVisitor.resolver.Clone();
				this.parsedFile = parentVisitor.parsedFile;
			}
			
			public ImplicitlyTypedLambda(LambdaExpression lambda, ResolveVisitor parentVisitor)
				: this(parentVisitor)
			{
				this.lambda = lambda;
				foreach (var pd in lambda.Parameters) {
					parameters.Add(new DefaultParameter(SharedTypes.UnknownType, pd.Name) {
					               	Region = parentVisitor.MakeRegion(pd)
					               });
				}
				RegisterUndecidedLambda();
			}
			
			public ImplicitlyTypedLambda(QuerySelectClause selectClause, IEnumerable<IParameter> parameters, ResolveVisitor parentVisitor)
				: this(parentVisitor)
			{
				this.selectClause = selectClause;
				this.parameters.AddRange(parameters);
				
				RegisterUndecidedLambda();
			}
			
			void RegisterUndecidedLambda()
			{
				if (parentVisitor.undecidedLambdas == null)
					parentVisitor.undecidedLambdas = new List<LambdaBase>();
				parentVisitor.undecidedLambdas.Add(this);
				Log.WriteLine("Added undecided implicitly-typed lambda: " + this.LambdaExpression);
			}
			
			public override IList<IParameter> Parameters {
				get { return parameters; }
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Log.WriteLine("Testing validity of {0} for parameters ({1}) and return-type {2}...",
				              this, string.Join<IType>(", ", parameterTypes), returnType);
				Log.Indent();
				var hypothesis = GetHypothesis(parameterTypes);
				Conversion c = hypothesis.IsValid(returnType, conversions);
				Log.Unindent();
				Log.WriteLine("{0} is {1} for return-type {2}", hypothesis, c ? "valid" : "invalid", returnType);
				return c;
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				return GetHypothesis(parameterTypes).inferredReturnType;
			}
			
			LambdaTypeHypothesis GetHypothesis(IType[] parameterTypes)
			{
				if (parameterTypes.Length != parameters.Count)
					throw new ArgumentException("Incorrect parameter type count");
				foreach (var h in hypotheses) {
					bool ok = true;
					for (int i = 0; i < parameterTypes.Length; i++) {
						if (!parameterTypes[i].Equals(h.parameterTypes[i])) {
							ok = false;
							break;
						}
					}
					if (ok)
						return h;
				}
				var resolveAll = new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Resolve, null);
				ResolveVisitor visitor = new ResolveVisitor(storedContext.Clone(), parsedFile, resolveAll);
				var newHypothesis = new LambdaTypeHypothesis(this, parameterTypes, visitor, lambda != null ? lambda.Parameters : null);
				hypotheses.Add(newHypothesis);
				return newHypothesis;
			}
			
			/// <summary>
			/// Get any hypothesis for this lambda.
			/// This method is used as fallback if the lambda isn't merged the normal way (AnonymousFunctionConversion)
			/// </summary>
			internal LambdaTypeHypothesis GetAnyHypothesis()
			{
				if (winningHypothesis != null)
					return winningHypothesis;
				if (hypotheses.Count == 0) {
					// make a new hypothesis with unknown parameter types
					IType[] parameterTypes = new IType[parameters.Count];
					for (int i = 0; i < parameterTypes.Length; i++) {
						parameterTypes[i] = SharedTypes.UnknownType;
					}
					return GetHypothesis(parameterTypes);
				} else {
					// We have the choice, so pick the hypothesis with the least missing parameter types
					LambdaTypeHypothesis bestHypothesis = hypotheses[0];
					int bestHypothesisUnknownParameters = bestHypothesis.CountUnknownParameters();
					for (int i = 1; i < hypotheses.Count; i++) {
						int c = hypotheses[i].CountUnknownParameters();
						if (c < bestHypothesisUnknownParameters ||
						    (c == bestHypothesisUnknownParameters && hypotheses[i].success && !bestHypothesis.success))
						{
							bestHypothesis = hypotheses[i];
							bestHypothesisUnknownParameters = c;
						}
					}
					return bestHypothesis;
				}
			}
			
			internal override void EnforceMerge(ResolveVisitor parentVisitor)
			{
				GetAnyHypothesis().MergeInto(parentVisitor, SharedTypes.UnknownType);
			}
			
			public override bool IsImplicitlyTyped {
				get { return true; }
			}
			
			public override bool IsAnonymousMethod {
				get { return false; }
			}
			
			public override bool HasParameterList {
				get { return true; }
			}
			
			public override bool IsAsync {
				get { return lambda.IsAsync; }
			}
			
			public override string ToString()
			{
				return "[ImplicitlyTypedLambda " + this.LambdaExpression + "]";
			}
		}
		
		/// <summary>
		/// Every possible set of parameter types gets its own 'hypothetical world'.
		/// It uses a nested ResolveVisitor that has its own resolve cache, so that resolve results cannot leave the hypothetical world.
		/// 
		/// Only after overload resolution is applied and the actual parameter types are known, the winning hypothesis will be merged
		/// with the parent ResolveVisitor.
		/// This is done when the AnonymousFunctionConversion is applied on the parent visitor.
		/// </summary>
		sealed class LambdaTypeHypothesis
		{
			readonly ImplicitlyTypedLambda lambda;
			internal readonly IParameter[] lambdaParameters;
			internal readonly IType[] parameterTypes;
			readonly ResolveVisitor visitor;
			
			internal readonly IType inferredReturnType;
			IList<Expression> returnExpressions;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			internal bool success;
			
			public LambdaTypeHypothesis(ImplicitlyTypedLambda lambda, IType[] parameterTypes, ResolveVisitor visitor,
			                            ICollection<ParameterDeclaration> parameterDeclarations)
			{
				Debug.Assert(parameterTypes.Length == lambda.Parameters.Count);
				
				this.lambda = lambda;
				this.parameterTypes = parameterTypes;
				this.visitor = visitor;
				
				Log.WriteLine("Analyzing " + ToString() + "...");
				Log.Indent();
				visitor.resolver.PushLambdaBlock();
				lambdaParameters = new IParameter[parameterTypes.Length];
				if (parameterDeclarations != null) {
					int i = 0;
					foreach (var pd in parameterDeclarations) {
						lambdaParameters[i] = visitor.resolver.AddLambdaParameter(parameterTypes[i], visitor.MakeRegion(pd), pd.Name);
						i++;
						visitor.Scan(pd);
					}
				} else {
					for (int i = 0; i < parameterTypes.Length; i++) {
						var p = lambda.Parameters[i];
						lambdaParameters[i] = visitor.resolver.AddLambdaParameter(parameterTypes[i], p.Region, p.Name);
					}
				}
				
				visitor.AnalyzeLambda(lambda.Body, lambda.IsAsync, out success, out isValidAsVoidMethod, out inferredReturnType, out returnExpressions, out returnValues);
				visitor.resolver.PopBlock();
				Log.Unindent();
				Log.WriteLine("Finished analyzing " + ToString());
			}
			
			internal int CountUnknownParameters()
			{
				int c = 0;
				foreach (IType t in parameterTypes) {
					if (t.Kind == TypeKind.Unknown)
						c++;
				}
				return c;
			}
			
			public Conversion IsValid(IType returnType, Conversions conversions)
			{
				if (success && IsValidLambda(isValidAsVoidMethod, lambda.IsAsync, returnValues, returnType, conversions)) {
					return Conversion.AnonymousFunctionConversion(new AnonymousFunctionConversionData(returnType, this));
				} else {
					return Conversion.None;
				}
			}
			
			public void MergeInto(ResolveVisitor parentVisitor, IType returnType)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				if (parentVisitor != lambda.parentVisitor)
					throw new InvalidOperationException("parent visitor mismatch");
				
				if (lambda.winningHypothesis == this)
					return;
				else if (lambda.winningHypothesis != null)
					throw new InvalidOperationException("Trying to merge conflicting hypotheses");
				
				lambda.winningHypothesis = this;
				
				Log.WriteLine("Applying return type {0} to implicitly-typed lambda {1}", returnType, lambda.LambdaExpression);
				if (lambda.IsAsync)
					returnType = parentVisitor.UnpackTask(returnType);
				for (int i = 0; i < returnExpressions.Count; i++) {
					visitor.ProcessConversion(returnExpressions[i], returnValues[i], returnType);
				}
				
				visitor.MergeUndecidedLambdas();
				Log.WriteLine("Merging " + ToString());
				foreach (var pair in visitor.resolveResultCache) {
					parentVisitor.StoreResult(pair.Key, pair.Value);
				}
				foreach (var pair in visitor.resolverBeforeDict) {
					parentVisitor.StoreState(pair.Key, pair.Value);
				}
				parentVisitor.undecidedLambdas.Remove(lambda);
			}
			
			public override string ToString()
			{
				StringBuilder b = new StringBuilder();
				b.Append("[LambdaTypeHypothesis (");
				for (int i = 0; i < parameterTypes.Length; i++) {
					if (i > 0) b.Append(", ");
					b.Append(parameterTypes[i]);
					b.Append(' ');
					b.Append(lambda.Parameters[i].Name);
				}
				b.Append(") => ");
				b.Append(lambda.Body.ToString());
				b.Append(']');
				return b.ToString();
			}
		}
		#endregion
		
		#region MergeUndecidedLambdas
		abstract class LambdaBase : LambdaResolveResult
		{
			internal abstract bool IsUndecided { get; }
			internal abstract AstNode LambdaExpression { get; }
			internal abstract AstNode Body { get; }
			
			internal abstract void EnforceMerge(ResolveVisitor parentVisitor);
		}
		
		void MergeUndecidedLambdas()
		{
			if (undecidedLambdas == null || undecidedLambdas.Count == 0)
				return;
			Log.WriteLine("MergeUndecidedLambdas()...");
			Log.Indent();
			while (undecidedLambdas.Count > 0) {
				LambdaBase lambda = undecidedLambdas[0];
				AstNode parent = lambda.LambdaExpression.Parent;
				// Continue going upwards until we find a node that can be resolved and provides
				// an expected type.
				while (ActsAsParenthesizedExpression(parent) || parent is NamedArgumentExpression || parent is ArrayInitializerExpression) {
					parent = parent.Parent;
				}
				CSharpResolver storedResolver;
				if (parent != null && resolverBeforeDict.TryGetValue(parent, out storedResolver)) {
					Log.WriteLine("Trying to resolve '" + parent + "' in order to merge the lambda...");
					Log.Indent();
					ResetContext(storedResolver.Clone(), delegate { Resolve(parent); });
					Log.Unindent();
				} else {
					Log.WriteLine("Could not find a suitable parent for '" + lambda);
				}
				if (lambda.IsUndecided) {
					// Lambda wasn't merged by resolving its parent -> enforce merging
					Log.WriteLine("Lambda wasn't merged by conversion - enforce merging");
					lambda.EnforceMerge(this);
				}
			}
			Log.Unindent();
			Log.WriteLine("MergeUndecidedLambdas() finished.");
		}
		
		internal static bool ActsAsParenthesizedExpression(AstNode expression)
		{
			return expression is ParenthesizedExpression || expression is CheckedExpression || expression is UncheckedExpression;
		}
		
		internal static Expression UnpackParenthesizedExpression(Expression expr)
		{
			while (ActsAsParenthesizedExpression(expr))
				expr = expr.GetChildByRole(ParenthesizedExpression.Roles.Expression);
			return expr;
		}
		#endregion
		
		#region AnalyzeLambda
		IType GetTaskType(IType resultType)
		{
			if (resultType.Kind == TypeKind.Unknown)
				return SharedTypes.UnknownType;
			if (resultType.Kind == TypeKind.Void)
				return resolver.Context.GetTypeDefinition("System.Threading.Tasks", "Task", 0, StringComparer.Ordinal) ?? SharedTypes.UnknownType;
			
			ITypeDefinition def = resolver.Context.GetTypeDefinition("System.Threading.Tasks", "Task", 1, StringComparer.Ordinal);
			if (def != null)
				return new ParameterizedType(def, new[] { resultType });
			else
				return SharedTypes.UnknownType;
		}
		
		void AnalyzeLambda(AstNode body, bool isAsync, out bool success, out bool isValidAsVoidMethod, out IType inferredReturnType, out IList<Expression> returnExpressions, out IList<ResolveResult> returnValues)
		{
			Expression expr = body as Expression;
			if (expr != null) {
				isValidAsVoidMethod = ExpressionPermittedAsStatement(expr);
				returnExpressions = new [] { expr };
				returnValues = new[] { Resolve(expr) };
				inferredReturnType = returnValues[0].Type;
			} else {
				Scan(body);
				
				AnalyzeLambdaVisitor alv = new AnalyzeLambdaVisitor();
				body.AcceptVisitor(alv, null);
				isValidAsVoidMethod = (alv.ReturnExpressions.Count == 0);
				if (alv.HasVoidReturnStatements) {
					returnExpressions = EmptyList<Expression>.Instance;
					returnValues = EmptyList<ResolveResult>.Instance;
					inferredReturnType = KnownTypeReference.Void.Resolve(resolver.Context);
				} else {
					returnExpressions = alv.ReturnExpressions;
					returnValues = new ResolveResult[returnExpressions.Count];
					for (int i = 0; i < returnValues.Count; i++) {
						returnValues[i] = resolveResultCache[returnExpressions[i]];
					}
					TypeInference ti = new TypeInference(resolver.Context, resolver.conversions);
					bool tiSuccess;
					inferredReturnType = ti.GetBestCommonType(returnValues, out tiSuccess);
					// Failure to infer a return type does not make the lambda invalid,
					// so we can ignore the 'tiSuccess' value
				}
			}
			if (isAsync)
				inferredReturnType = GetTaskType(inferredReturnType);
			Log.WriteLine("Lambda return type was inferred to: " + inferredReturnType);
			// TODO: check for compiler errors within the lambda body
			
			success = true;
		}
		
		static bool ExpressionPermittedAsStatement(Expression expr)
		{
			UnaryOperatorExpression uoe = expr as UnaryOperatorExpression;
			if (uoe != null) {
				switch (uoe.Operator) {
					case UnaryOperatorType.Increment:
					case UnaryOperatorType.Decrement:
					case UnaryOperatorType.PostIncrement:
					case UnaryOperatorType.PostDecrement:
					case UnaryOperatorType.Await:
						return true;
					default:
						return false;
				}
			}
			return expr is InvocationExpression
				|| expr is ObjectCreateExpression
				|| expr is AssignmentExpression;
		}
		
		static bool IsValidLambda(bool isValidAsVoidMethod, bool isAsync, IList<ResolveResult> returnValues, IType returnType, Conversions conversions)
		{
			if (returnType.Kind == TypeKind.Void) {
				// Lambdas that are valid statement lambdas or expression lambdas with a statement-expression
				// can be converted to delegates with void return type.
				// This holds for both async and regular lambdas.
				return isValidAsVoidMethod;
			} else if (isAsync && IsTask(returnType) && returnType.TypeParameterCount == 0) {
				// Additionally, async lambdas with the above property can be converted to non-generic Task.
				return isValidAsVoidMethod;
			} else {
				if (returnValues.Count == 0)
					return false;
				if (isAsync) {
					// async lambdas must return Task<T>
					if (!(IsTask(returnType) && returnType.TypeParameterCount == 1))
						return false;
					// unpack Task<T> for testing the implicit conversions
					returnType = ((ParameterizedType)returnType).GetTypeArgument(0);
				}
				foreach (ResolveResult returnRR in returnValues) {
					if (!conversions.ImplicitConversion(returnRR, returnType))
						return false;
				}
				return true;
			}
		}
		
		/// <summary>
		/// Gets the T in Task&lt;T&gt;.
		/// Returns void for non-generic Task.
		/// </summary>
		IType UnpackTask(IType type)
		{
			if (!IsTask(type))
				return type;
			if (type.TypeParameterCount == 0)
				return KnownTypeReference.Void.Resolve(resolver.Context);
			else
				return ((ParameterizedType)type).GetTypeArgument(0);
		}
		
		/// <summary>
		/// Gets whether the specified type is Task or Task&lt;T&gt;.
		/// </summary>
		static bool IsTask(IType type)
		{
			if (type.Kind == TypeKind.Class && type.Name == "Task" && type.Namespace == "System.Threading.Tasks") {
				if (type.TypeParameterCount == 0)
					return true;
				if (type.TypeParameterCount == 1)
					return type is ParameterizedType;
			}
			return false;
		}
		
		sealed class AnalyzeLambdaVisitor : DepthFirstAstVisitor<object, object>
		{
			public bool HasVoidReturnStatements;
			public List<Expression> ReturnExpressions = new List<Expression>();
			
			public override object VisitReturnStatement(ReturnStatement returnStatement, object data)
			{
				Expression expr = returnStatement.Expression;
				if (expr.IsNull) {
					HasVoidReturnStatements = true;
				} else {
					ReturnExpressions.Add(expr);
				}
				return null;
			}
			
			public override object VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
			{
				// don't go into nested lambdas
				return null;
			}
			
			public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
			{
				return null;
			}
		}
		#endregion
		#endregion
		
		#region Local Variable Scopes (Block Statements)
		ResolveResult IAstVisitor<object, ResolveResult>.VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(blockStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			resolver.PushBlock();
			if (resolverEnabled) {
				for (AstNode child = usingStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == UsingStatement.ResourceAcquisitionRole && child is Expression) {
						ITypeDefinition disposable = resolver.Context.GetTypeDefinition(
							"System", "IDisposable", 0, StringComparer.Ordinal);
						ResolveAndProcessConversion((Expression)child, disposable ?? SharedTypes.UnknownType);
					} else {
						Scan(child);
					}
				}
			} else {
				ScanChildren(usingStatement);
			}
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			resolver.PushBlock();
			ITypeReference type = MakeTypeReference(fixedStatement.Type);
			for (AstNode node = fixedStatement.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FixedStatement.Roles.Variable) {
					VariableInitializer vi = (VariableInitializer)node;
					resolver.AddVariable(type, MakeRegion(vi) , vi.Name);
				}
				Scan(node);
			}
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			resolver.PushBlock();
			ITypeReference type;
			if (IsVar(foreachStatement.VariableType)) {
				if (navigator.Scan(foreachStatement.VariableType) == ResolveVisitorNavigationMode.Resolve) {
					IType collectionType = Resolve(foreachStatement.InExpression).Type;
					IType elementType = GetElementType(collectionType, resolver.Context, false);
					StoreResult(foreachStatement.VariableType, new TypeResolveResult(elementType));
					type = elementType;
				} else {
					Scan(foreachStatement.InExpression);
					type = MakeVarTypeReference(foreachStatement.InExpression, true);
				}
			} else {
				type = ResolveType(foreachStatement.VariableType);
			}
			IVariable v = resolver.AddVariable(type, MakeRegion(foreachStatement.VariableNameToken), foreachStatement.VariableName);
			StoreResult(foreachStatement.VariableNameToken, new LocalResolveResult(v, v.Type.Resolve(resolver.Context)));
			Scan(foreachStatement.EmbeddedStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(switchStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCatchClause(CatchClause catchClause, object data)
		{
			resolver.PushBlock();
			if (!string.IsNullOrEmpty(catchClause.VariableName)) {
				ITypeReference variableType = MakeTypeReference(catchClause.Type);
				DomRegion region = MakeRegion(catchClause.VariableNameToken);
				IVariable v = resolver.AddVariable(variableType, region, catchClause.VariableName);
				StoreResult(catchClause.VariableNameToken, new LocalResolveResult(v, v.Type.Resolve(resolver.Context)));
			}
			ScanChildren(catchClause);
			resolver.PopBlock();
			return voidResult;
		}
		#endregion
		
		#region VariableDeclarationStatement
		ResolveResult IAstVisitor<object, ResolveResult>.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			bool isConst = (variableDeclarationStatement.Modifiers & Modifiers.Const) != 0;
			if (!isConst && IsVar(variableDeclarationStatement.Type) && variableDeclarationStatement.Variables.Count == 1) {
				VariableInitializer vi = variableDeclarationStatement.Variables.Single();
				bool needResolve = resolverEnabled
					|| navigator.Scan(variableDeclarationStatement.Type) == ResolveVisitorNavigationMode.Resolve
					|| navigator.Scan(vi) == ResolveVisitorNavigationMode.Resolve;
				ITypeReference type;
				if (needResolve) {
					type = Resolve(vi.Initializer).Type;
					if (!resolveResultCache.ContainsKey(variableDeclarationStatement.Type)) {
						StoreResult(variableDeclarationStatement.Type, new TypeResolveResult(type.Resolve(resolver.Context)));
					}
				} else {
					Scan(vi.Initializer);
					type = MakeVarTypeReference(vi.Initializer, false);
				}
				IVariable v = resolver.AddVariable(type, MakeRegion(vi), vi.Name);
				StoreState(vi, resolver.Clone());
				if (needResolve) {
					ResolveResult result;
					if (!resolveResultCache.TryGetValue(vi, out result)) {
						result = new LocalResolveResult(v, type.Resolve(resolver.Context));
						StoreResult(vi, result);
					}
					return result;
				} else {
					return null;
				}
			} else {
				ITypeReference type = MakeTypeReference(variableDeclarationStatement.Type);

				int initializerCount = variableDeclarationStatement.Variables.Count;
				ResolveResult result = null;
				for (AstNode node = variableDeclarationStatement.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == VariableDeclarationStatement.Roles.Variable) {
						VariableInitializer vi = (VariableInitializer)node;
						
						IConstantValue cv = null;
						if (isConst) {
							cv = TypeSystemConvertVisitor.ConvertConstantValue(type, vi.Initializer, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.CurrentUsingScope);
						}
						resolver.AddVariable(type, MakeRegion(vi), vi.Name, cv);
						
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
		}
		#endregion
		
		#region Condition Statements
		ResolveResult IAstVisitor<object, ResolveResult>.VisitForStatement(ForStatement forStatement, object data)
		{
			resolver.PushBlock();
			HandleConditionStatement(forStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			HandleConditionStatement(ifElseStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			HandleConditionStatement(whileStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDoWhileStatement(DoWhileStatement doWhileStatement, object data)
		{
			HandleConditionStatement(doWhileStatement);
			return voidResult;
		}
		
		void HandleConditionStatement(Statement conditionStatement)
		{
			if (resolverEnabled) {
				for (AstNode child = conditionStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == AstNode.Roles.Condition) {
						ResolveAndProcessConversion((Expression)child, KnownTypeReference.Boolean.Resolve(resolver.Context));
					} else {
						Scan(child);
					}
				}
			} else {
				ScanChildren(conditionStatement);
			}
		}
		#endregion
		
		#region Return Statements
		ResolveResult IAstVisitor<object, ResolveResult>.VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			if (resolverEnabled && !resolver.IsWithinLambdaExpression && resolver.CurrentMember != null) {
				IType type = resolver.CurrentMember.ReturnType.Resolve(resolver.Context);
				if (IsTask(type)) {
					var methodDecl = returnStatement.Ancestors.OfType<AttributedNode>().FirstOrDefault();
					if (methodDecl != null && (methodDecl.Modifiers & Modifiers.Async) == Modifiers.Async)
						type = UnpackTask(type);
				}
				ResolveAndProcessConversion(returnStatement.Expression, type);
			} else {
				Scan(returnStatement.Expression);
			}
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitYieldReturnStatement(YieldReturnStatement yieldStatement, object data)
		{
			if (resolverEnabled && resolver.CurrentMember != null) {
				IType returnType = resolver.CurrentMember.ReturnType.Resolve(resolver.Context);
				IType elementType = GetElementType(returnType, resolver.Context, true);
				ResolveAndProcessConversion(yieldStatement.Expression, elementType);
			} else {
				Scan(yieldStatement.Expression);
			}
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, object data)
		{
			return voidResult;
		}
		#endregion
		
		#region Other statements
		ResolveResult IAstVisitor<object, ResolveResult>.VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			ScanChildren(expressionStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitLockStatement(LockStatement lockStatement, object data)
		{
			ScanChildren(lockStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitBreakStatement(BreakStatement breakStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			Scan(throwStatement.Expression);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			ScanChildren(tryCatchStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, object data)
		{
			ScanChildren(gotoCaseStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			return voidResult;
		}
		#endregion
		
		#region Local Variable Type Inference
		static bool IsVar(AstNode returnType)
		{
			SimpleType st = returnType as SimpleType;
			return st != null && st.Identifier == "var" && st.TypeArguments.Count == 0;
		}
		
		ITypeReference MakeTypeReference(AstType type)
		{
			return TypeSystemConvertVisitor.ConvertType(type, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.CurrentUsingScope, currentTypeLookupMode);
		}
		
		ITypeReference MakeVarTypeReference(Expression initializer, bool isForEach)
		{
			return new VarTypeReference(this, resolver.Clone(), initializer, isForEach);
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
				
				visitor.ResetContext(
					storedContext,
					delegate {
						result = visitor.Resolve(initializerExpression).Type;
						
						if (isForEach) {
							result = GetElementType(result, storedContext.Context, false);
						}
					});
				visitor = null;
				storedContext = null;
				initializerExpression = null;
				return result;
			}
			
			public override string ToString()
			{
				if (visitor == null)
					return "var=" + result;
				else
					return "var (not yet resolved)";
			}
		}
		
		static IType GetElementType(IType result, ITypeResolveContext context, bool allowIEnumerator)
		{
			bool foundSimpleIEnumerable = false;
			foreach (IType baseType in result.GetAllBaseTypes(context)) {
				ITypeDefinition baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef != null && (
					baseTypeDef.Name == "IEnumerable" || (allowIEnumerator && baseType.Name == "IEnumerator")))
				{
					if (baseTypeDef.Namespace == "System.Collections.Generic" && baseTypeDef.TypeParameterCount == 1) {
						ParameterizedType pt = baseType as ParameterizedType;
						if (pt != null) {
							return pt.GetTypeArgument(0);
						}
					} else if (baseTypeDef.Namespace == "System.Collections" && baseTypeDef.TypeParameterCount == 0) {
						foundSimpleIEnumerable = true;
					}
				}
			}
			// System.Collections.IEnumerable found in type hierarchy -> Object is element type.
			if (foundSimpleIEnumerable)
				return KnownTypeReference.Object.Resolve(context);
			return SharedTypes.UnknownType;
		}
		#endregion
		
		#region Attributes
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAttribute(Attribute attribute, object data)
		{
			var type = ResolveType(attribute.Type);
			
			// Separate arguments into ctor arguments and non-ctor arguments:
			var constructorArguments = attribute.Arguments.Where(a => !(a is NamedExpression));
			var nonConstructorArguments = attribute.Arguments.Where(a => a is NamedExpression);
			
			// Scan the non-constructor arguments
			resolver.PushInitializerType(type);
			foreach (var arg in nonConstructorArguments)
				Scan(arg);
			resolver.PopInitializerType();
			
			if (resolverEnabled) {
				// Resolve the ctor arguments and find the matching ctor overload
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(constructorArguments, out argumentNames);
				ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames);
				ProcessConversionsInInvocation(null, constructorArguments, rr as CSharpInvocationResolveResult);
				return rr;
			} else {
				foreach (var node in constructorArguments)
					Scan(node);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			ScanChildren(attributeSection);
			return voidResult;
		}
		#endregion
		
		#region Using Declaration
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			currentTypeLookupMode = SimpleNameLookupMode.TypeInUsingDeclaration;
			ScanChildren(usingDeclaration);
			currentTypeLookupMode = SimpleNameLookupMode.Type;
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration, object data)
		{
			currentTypeLookupMode = SimpleNameLookupMode.TypeInUsingDeclaration;
			ScanChildren(usingDeclaration);
			currentTypeLookupMode = SimpleNameLookupMode.Type;
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, object data)
		{
			return voidResult;
		}
		#endregion
		
		#region Type References
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			if (!resolverEnabled)
				return null;
			IType type = MakeTypeReference(primitiveType).Resolve(resolver.Context);
			if (type.Kind != TypeKind.Unknown)
				return new TypeResolveResult(type);
			else
				return errorResult;
		}
		
		ResolveResult HandleAttributeType(AstType astType)
		{
			ScanChildren(astType);
			IType type = TypeSystemConvertVisitor.ConvertAttributeType(astType, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.CurrentUsingScope).Resolve(resolver.Context);
			if (type.Kind != TypeKind.Unknown)
				return new TypeResolveResult(type);
			else
				return errorResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSimpleType(SimpleType simpleType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(simpleType);
				return null;
			}
			if (simpleType.Parent is Attribute) {
				return HandleAttributeType(simpleType);
			}
			
			var typeArguments = GetTypeArguments(simpleType.TypeArguments);
			return resolver.LookupSimpleNameOrTypeName(simpleType.Identifier, typeArguments, currentTypeLookupMode);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitMemberType(MemberType memberType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(memberType);
				return null;
			}
			if (memberType.Parent is Attribute) {
				return HandleAttributeType(memberType);
			}
			ResolveResult target;
			if (memberType.IsDoubleColon && memberType.Target is SimpleType) {
				SimpleType t = (SimpleType)memberType.Target;
				target = resolver.ResolveAlias(t.Identifier);
				StoreResult(t, target);
			} else {
				target = Resolve(memberType.Target);
			}
			
			var typeArguments = GetTypeArguments(memberType.TypeArguments);
			return resolver.ResolveMemberType(target, memberType.MemberName, typeArguments);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitComposedType(ComposedType composedType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(composedType);
				return null;
			}
			IType t = ResolveType(composedType.BaseType);
			if (composedType.HasNullableSpecifier) {
				t = NullableType.Create(t, resolver.Context);
			}
			for (int i = 0; i < composedType.PointerRank; i++) {
				t = new PointerType(t);
			}
			foreach (var a in composedType.ArraySpecifiers.Reverse()) {
				t = new ArrayType(t, a.Dimensions);
			}
			return new TypeResolveResult(t);
		}
		#endregion
		
		#region Query Expressions
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			resolver.PushBlock();
			ResolveResult oldQueryResult = currentQueryResult;
			try {
				currentQueryResult = null;
				foreach (var clause in queryExpression.Clauses) {
					currentQueryResult = Resolve(clause);
				}
				return currentQueryResult;
			} finally {
				currentQueryResult = oldQueryResult;
				resolver.PopBlock();
			}
		}
		
		IType GetTypeForQueryVariable(IType type)
		{
			// This assumes queries are only used on IEnumerable.
			// We might want to look at the signature of a LINQ method (e.g. Select) instead.
			return GetElementType(type, resolver.Context, false);
		}
		
		sealed class QueryExpressionLambda : LambdaResolveResult
		{
			readonly IParameter[] parameters;
			readonly ResolveResult bodyExpression;
			
			internal IType[] inferredParameterTypes;
			
			public QueryExpressionLambda(int parameterCount, ResolveResult bodyExpression)
			{
				this.parameters = new IParameter[parameterCount];
				for (int i = 0; i < parameterCount; i++) {
					parameters[i] = new DefaultParameter(SharedTypes.UnknownType, "x" + i);
				}
				this.bodyExpression = bodyExpression;
			}
			
			public override IList<IParameter> Parameters {
				get { return parameters; }
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				if (parameterTypes.Length == parameters.Length) {
					this.inferredParameterTypes = parameterTypes;
					return Conversion.AnonymousFunctionConversion(parameterTypes);
				} else {
					return Conversion.None;
				}
			}
			
			public override bool IsAsync {
				get { return false; }
			}
			
			public override bool IsImplicitlyTyped {
				get { return true; }
			}
			
			public override bool IsAnonymousMethod {
				get { return false; }
			}
			
			public override bool HasParameterList {
				get { return true; }
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				return bodyExpression.Type;
			}
			
			public override string ToString()
			{
				return string.Format("[QueryExpressionLambda ({0}) => {1}]", string.Join(",", parameters.Select(p => p.Name)), bodyExpression);
			}
		}
		
		QueryClause GetPreviousQueryClause(QueryClause clause)
		{
			for (AstNode node = clause.PrevSibling; node != null; node = node.PrevSibling) {
				if (node.Role == QueryExpression.ClauseRole)
					return (QueryClause)node;
			}
			return null;
		}
		
		QueryClause GetNextQueryClause(QueryClause clause)
		{
			for (AstNode node = clause.NextSibling; node != null; node = node.NextSibling) {
				if (node.Role == QueryExpression.ClauseRole)
					return (QueryClause)node;
			}
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryFromClause(QueryFromClause queryFromClause, object data)
		{
			ResolveResult result = null;
			ResolveResult expr = Resolve(queryFromClause.Expression);
			IType variableType;
			if (queryFromClause.Type.IsNull) {
				variableType = GetTypeForQueryVariable(expr.Type);
				result = expr;
			} else {
				variableType = ResolveType(queryFromClause.Type);
				if (resolverEnabled) {
					// resolve the .Cast<>() call
					ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { variableType }, true);
					result = resolver.ResolveInvocation(methodGroup, new ResolveResult[0]);
				}
			}
			
			DomRegion region = MakeRegion(queryFromClause.IdentifierToken);
			IVariable v = resolver.AddVariable(variableType, region, queryFromClause.Identifier);
			StoreResult(queryFromClause.IdentifierToken, new LocalResolveResult(v, variableType));
			
			if (resolverEnabled && currentQueryResult != null) {
				// this is a second 'from': resolve the .SelectMany() call
				QuerySelectClause selectClause = GetNextQueryClause(queryFromClause) as QuerySelectClause;
				ResolveResult selectResult;
				if (selectClause != null) {
					// from ... from ... select - the SelectMany call also performs the Select operation
					selectResult = Resolve(selectClause.Expression);
				} else {
					// from .. from ... ... - introduce a transparent identifier
					selectResult = transparentIdentifierResolveResult;
				}
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "SelectMany", EmptyList<IType>.Instance, true);
				ResolveResult[] arguments = {
					new QueryExpressionLambda(1, result),
					new QueryExpressionLambda(2, selectResult)
				};
				result = resolver.ResolveInvocation(methodGroup, arguments);
			}
			return result;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, object data)
		{
			ResolveResult rr = Resolve(queryContinuationClause.PrecedingQuery);
			IType variableType = GetTypeForQueryVariable(rr.Type);
			DomRegion region = MakeRegion(queryContinuationClause.IdentifierToken);
			IVariable v = resolver.AddVariable(variableType, region, queryContinuationClause.Identifier);
			StoreResult(queryContinuationClause.IdentifierToken, new LocalResolveResult(v, variableType));
			return rr;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryLetClause(QueryLetClause queryLetClause, object data)
		{
			ResolveResult expr = Resolve(queryLetClause.Expression);
			DomRegion region = MakeRegion(queryLetClause.IdentifierToken);
			IVariable v = resolver.AddVariable(expr.Type, region, queryLetClause.Identifier);
			StoreResult(queryLetClause.IdentifierToken, new LocalResolveResult(v, expr.Type));
			if (resolverEnabled && currentQueryResult != null) {
				// resolve the .Select() call
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Select", EmptyList<IType>.Instance, true);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, transparentIdentifierResolveResult) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryJoinClause(QueryJoinClause queryJoinClause, object data)
		{
			// join v in expr on onExpr equals equalsExpr [into g]
			ResolveResult inResult = null;
			ResolveResult expr = Resolve(queryJoinClause.InExpression);
			IType variableType;
			if (queryJoinClause.Type.IsNull) {
				variableType = GetTypeForQueryVariable(expr.Type);
				inResult = expr;
			} else {
				variableType = ResolveType(queryJoinClause.Type);
				if (resolverEnabled) {
					// resolve the .Cast<>() call
					ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { variableType }, true);
					inResult = resolver.ResolveInvocation(methodGroup, new ResolveResult[0]);
				}
			}
			
			// resolve the 'On' expression in a context that contains only the previously existing range variables:
			// (before adding any variable)
			ResolveResult onResult = Resolve(queryJoinClause.OnExpression);
			
			// scan the 'Equals' expression in a context that contains only the variable 'v'
			CSharpResolver resolverOutsideQuery = resolver.Clone();
			resolverOutsideQuery.PopBlock(); // pop all variables from the current query expression
			DomRegion joinIdentifierRegion = MakeRegion(queryJoinClause.JoinIdentifierToken);
			IVariable v = resolverOutsideQuery.AddVariable(variableType, joinIdentifierRegion, queryJoinClause.JoinIdentifier);
			ResolveResult equalsResult = errorResult;
			ResetContext(resolverOutsideQuery, delegate {
			             	equalsResult = Resolve(queryJoinClause.EqualsExpression);
			             });
			StoreResult(queryJoinClause.JoinIdentifierToken, new LocalResolveResult(v, variableType));
			
			if (queryJoinClause.IsGroupJoin) {
				return ResolveGroupJoin(queryJoinClause, inResult, onResult, equalsResult);
			} else {
				resolver.AddVariable(variableType, joinIdentifierRegion, queryJoinClause.JoinIdentifier);
				if (resolverEnabled && currentQueryResult != null) {
					QuerySelectClause selectClause = GetNextQueryClause(queryJoinClause) as QuerySelectClause;
					ResolveResult selectResult;
					if (selectClause != null) {
						// from ... join ... select - the Join call also performs the Select operation
						selectResult = Resolve(selectClause.Expression);
					} else {
						// from .. join ... ... - introduce a transparent identifier
						selectResult = transparentIdentifierResolveResult;
					}
					
					var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Join", EmptyList<IType>.Instance);
					ResolveResult[] arguments = {
						inResult,
						new QueryExpressionLambda(1, onResult),
						new QueryExpressionLambda(1, equalsResult),
						new QueryExpressionLambda(2, selectResult)
					};
					return resolver.ResolveInvocation(methodGroup, arguments);
				} else {
					return null;
				}
			}
		}
		
		ResolveResult ResolveGroupJoin(QueryJoinClause queryJoinClause,
		                               ResolveResult inResult, ResolveResult onResult, ResolveResult equalsResult)
		{
			Debug.Assert(queryJoinClause.IsGroupJoin);
			
			DomRegion intoIdentifierRegion = MakeRegion(queryJoinClause.IntoIdentifierToken);
			
			// We need to declare the group variable, but it's a bit tricky to determine its type:
			// We'll have to resolve the GroupJoin invocation and take a look at the inferred types
			// for the lambda given as last parameter.
			var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "GroupJoin", EmptyList<IType>.Instance);
			QuerySelectClause selectClause = GetNextQueryClause(queryJoinClause) as QuerySelectClause;
			LambdaResolveResult groupJoinLambda;
			if (selectClause != null) {
				// from ... join ... into g select - the GroupJoin call also performs the Select operation
				IParameter[] selectLambdaParameters = {
					new DefaultParameter(SharedTypes.UnknownType, "<>transparentIdentifier"),
					new DefaultParameter(SharedTypes.UnknownType, queryJoinClause.IntoIdentifier) {
						Region = intoIdentifierRegion
					}
				};
				groupJoinLambda = new ImplicitlyTypedLambda(selectClause, selectLambdaParameters, this);
			} else {
				// from .. join ... ... - introduce a transparent identifier
				groupJoinLambda = new QueryExpressionLambda(2, transparentIdentifierResolveResult);
			}
			
			ResolveResult[] arguments = {
				inResult,
				new QueryExpressionLambda(1, onResult),
				new QueryExpressionLambda(1, equalsResult),
				groupJoinLambda
			};
			ResolveResult rr = resolver.ResolveInvocation(methodGroup, arguments);
			InvocationResolveResult invocationRR = rr as InvocationResolveResult;
			
			IVariable groupVariable;
			if (groupJoinLambda is ImplicitlyTypedLambda) {
				var implicitlyTypedLambda = (ImplicitlyTypedLambda)groupJoinLambda;
				
				if (invocationRR != null && invocationRR.Arguments.Count > 0) {
					ConversionResolveResult crr = invocationRR.Arguments[invocationRR.Arguments.Count - 1] as ConversionResolveResult;
					if (crr != null)
						ProcessConversion(null, crr.Input, crr.Conversion, crr.Type);
				}
				
				implicitlyTypedLambda.EnforceMerge(this);
				if (implicitlyTypedLambda.winningHypothesis.parameterTypes.Length == 2)
					groupVariable = implicitlyTypedLambda.winningHypothesis.lambdaParameters[1];
				else
					groupVariable = null;
			} else {
				Debug.Assert(groupJoinLambda is QueryExpressionLambda);
				
				// Add the variable if the query expression continues after the group join
				// (there's no need to do this if there's only a select clause remaining, as
				// we already handled that in the ImplicitlyTypedLambda).
				
				// Get the inferred type of the group variable:
				IType[] inferredParameterTypes = null;
				if (invocationRR != null && invocationRR.Arguments.Count > 0) {
					ConversionResolveResult crr = invocationRR.Arguments[invocationRR.Arguments.Count - 1] as ConversionResolveResult;
					if (crr != null && crr.Conversion.IsAnonymousFunctionConversion) {
						inferredParameterTypes = crr.Conversion.data as IType[];
					}
				}
				if (inferredParameterTypes == null)
					inferredParameterTypes = ((QueryExpressionLambda)groupJoinLambda).inferredParameterTypes;
				
				IType groupParameterType;
				if (inferredParameterTypes != null && inferredParameterTypes.Length == 2)
					groupParameterType = inferredParameterTypes[1];
				else
					groupParameterType = SharedTypes.UnknownType;
				
				groupVariable = resolver.AddVariable(groupParameterType, intoIdentifierRegion, queryJoinClause.IntoIdentifier);
			}
			
			if (groupVariable != null) {
				LocalResolveResult lrr = new LocalResolveResult(groupVariable, groupVariable.Type.Resolve(resolver.Context));
				StoreResult(queryJoinClause.IntoIdentifierToken, lrr);
			}
			
			return rr;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryWhereClause(QueryWhereClause queryWhereClause, object data)
		{
			ResolveResult condition = Resolve(queryWhereClause.Condition);
			IType boolType = KnownTypeReference.Boolean.Resolve(resolver.Context);
			Conversion conversionToBool = resolver.conversions.ImplicitConversion(condition, boolType);
			ProcessConversion(queryWhereClause.Condition, condition, conversionToBool, boolType);
			if (resolverEnabled && currentQueryResult != null) {
				if (conversionToBool != Conversion.IdentityConversion && conversionToBool != Conversion.None) {
					condition = new ConversionResolveResult(boolType, condition, conversionToBool);
				}
				
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Where", EmptyList<IType>.Instance);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, condition) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQuerySelectClause(QuerySelectClause querySelectClause, object data)
		{
			if (resolverEnabled && currentQueryResult != null) {
				QueryClause previousQueryClause = GetPreviousQueryClause(querySelectClause);
				// If the 'select' follows on a 'SelectMany', 'Join' or 'GroupJoin' clause, then the 'select' portion
				// was already done as part of the previous clause.
				if (((previousQueryClause is QueryFromClause && GetPreviousQueryClause(previousQueryClause) != null))
				    || previousQueryClause is QueryJoinClause)
				{
					Scan(querySelectClause.Expression);
					return currentQueryResult;
				}
				
				QueryExpression query = querySelectClause.Parent as QueryExpression;
				string rangeVariable = GetSingleRangeVariable(query);
				if (rangeVariable != null) {
					IdentifierExpression ident = UnpackParenthesizedExpression(querySelectClause.Expression) as IdentifierExpression;
					if (ident != null && ident.Identifier == rangeVariable && !ident.TypeArguments.Any()) {
						// selecting the single identifier that is the range variable
						if (query.Clauses.Count > 2) {
							// only if the query is not degenerate:
							// the Select call will be optimized away, so directly return the previous result
							Scan(querySelectClause.Expression);
							return currentQueryResult;
						}
					}
				}
				
				ResolveResult expr = Resolve(querySelectClause.Expression);
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Select", EmptyList<IType>.Instance);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, expr) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				Scan(querySelectClause.Expression);
				return null;
			}
		}
		
		/// <summary>
		/// Gets the name of the range variable in the specified query.
		/// If the query has multiple range variables, this method returns null.
		/// </summary>
		string GetSingleRangeVariable(QueryExpression query)
		{
			if (query == null)
				return null;
			foreach (QueryClause clause in query.Clauses.Skip(1)) {
				if (clause is QueryFromClause || clause is QueryJoinClause || clause is QueryLetClause) {
					// query has more than 1 range variable
					return null;
				}
			}
			QueryFromClause fromClause = query.Clauses.FirstOrDefault() as QueryFromClause;
			if (fromClause != null)
				return fromClause.Identifier;
			QueryContinuationClause continuationClause = query.Clauses.FirstOrDefault() as QueryContinuationClause;
			if (continuationClause != null)
				return continuationClause.Identifier;
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryGroupClause(QueryGroupClause queryGroupClause, object data)
		{
			if (resolverEnabled && currentQueryResult != null) {
				// ... group projection by key
				ResolveResult projection = Resolve(queryGroupClause.Projection);
				ResolveResult key = Resolve(queryGroupClause.Key);
				
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "GroupBy", EmptyList<IType>.Instance);
				ResolveResult[] arguments = {
					new QueryExpressionLambda(1, key),
					new QueryExpressionLambda(1, projection)
				};
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				ScanChildren(queryGroupClause);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryOrderClause(QueryOrderClause queryOrderClause, object data)
		{
			if (resolverEnabled) {
				foreach (QueryOrdering ordering in queryOrderClause.Orderings) {
					currentQueryResult = Resolve(ordering);
				}
				return currentQueryResult;
			} else {
				ScanChildren(queryOrderClause);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryOrdering(QueryOrdering queryOrdering, object data)
		{
			if (resolverEnabled && currentQueryResult != null) {
				// ... orderby sortKey [descending]
				ResolveResult sortKey = Resolve(queryOrdering.Expression);
				
				QueryOrderClause parentClause = queryOrdering.Parent as QueryOrderClause;
				bool isFirst = (parentClause == null || parentClause.Orderings.FirstOrDefault() == queryOrdering);
				string methodName = isFirst ? "OrderBy" : "ThenBy";
				if (queryOrdering.Direction == QueryOrderingDirection.Descending)
					methodName += "Descending";
				
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, methodName, EmptyList<IType>.Instance);
				ResolveResult[] arguments = {
					new QueryExpressionLambda(1, sortKey),
				};
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				Scan(queryOrdering.Expression);
				return null;
			}
		}
		#endregion
		
		#region Constructor Initializer
		ResolveResult IAstVisitor<object, ResolveResult>.VisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(constructorInitializer);
				return null;
			}
			ResolveResult target;
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base) {
				target = resolver.ResolveBaseReference();
			} else {
				target = resolver.ResolveThisReference();
			}
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(constructorInitializer.Arguments, out argumentNames);
			ResolveResult rr = resolver.ResolveObjectCreation(target.Type, arguments, argumentNames);
			ProcessConversionsInInvocation(null, constructorInitializer.Arguments, rr as CSharpInvocationResolveResult);
			return rr;
		}
		#endregion
		
		#region Other Nodes
		// Token nodes
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIdentifier(Identifier identifier, object data)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitComment(Comment comment, object data)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, object data)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPatternPlaceholder(AstNode placeholder, ICSharpCode.NRefactory.PatternMatching.Pattern pattern, object data)
		{
			return null;
		}
		
		// Nodes where we just need to visit the children:
		ResolveResult IAstVisitor<object, ResolveResult>.VisitAccessor(Accessor accessor, object data)
		{
			ScanChildren(accessor);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSwitchSection(SwitchSection switchSection, object data)
		{
			ScanChildren(switchSection);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCaseLabel(CaseLabel caseLabel, object data)
		{
			ScanChildren(caseLabel);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitConstraint(Constraint constraint, object data)
		{
			ScanChildren(constraint);
			return voidResult;
		}
		#endregion
	}
}
