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
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

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
	sealed class ResolveVisitor : IAstVisitor<object, ResolveResult>
	{
		// The ResolveVisitor is also responsible for handling lambda expressions.
		
		static readonly ResolveResult errorResult = ErrorResolveResult.UnknownError;
		readonly ResolveResult voidResult;
		
		CSharpResolver resolver;
		/// <summary>Resolve result of the current LINQ query.</summary>
		/// <remarks>We do not have to put this into the stored state (resolver) because
		/// query expressions are always resolved in a single operation.</remarks>
		ResolveResult currentQueryResult;
		readonly CSharpParsedFile parsedFile;
		readonly Dictionary<AstNode, ResolveResult> resolveResultCache = new Dictionary<AstNode, ResolveResult>();
		readonly Dictionary<AstNode, CSharpResolver> resolverBeforeDict = new Dictionary<AstNode, CSharpResolver>();
		readonly Dictionary<AstNode, CSharpResolver> resolverAfterDict = new Dictionary<AstNode, CSharpResolver>();
		readonly Dictionary<Expression, ConversionWithTargetType> conversionDict = new Dictionary<Expression, ConversionWithTargetType>();
		
		internal struct ConversionWithTargetType
		{
			public readonly Conversion Conversion;
			public readonly IType TargetType;
			
			public ConversionWithTargetType(Conversion conversion, IType targetType)
			{
				this.Conversion = conversion;
				this.TargetType = targetType;
			}
		}
		
		IResolveVisitorNavigator navigator;
		bool resolverEnabled;
		List<LambdaBase> undecidedLambdas;
		internal CancellationToken cancellationToken;
		
		#region Constructor
		static readonly IResolveVisitorNavigator skipAllNavigator = new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
		
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
		public ResolveVisitor(CSharpResolver resolver, CSharpParsedFile parsedFile)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.parsedFile = parsedFile;
			this.navigator = skipAllNavigator;
			this.voidResult = new ResolveResult(resolver.Compilation.FindType(KnownTypeCode.Void));
		}
		
		internal void SetNavigator(IResolveVisitorNavigator navigator)
		{
			this.navigator = navigator ?? skipAllNavigator;
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
			var oldQueryResult = this.currentQueryResult;
			try {
				this.resolverEnabled = false;
				this.resolver = storedContext;
				this.currentQueryResult = null;
				
				action();
			} finally {
				this.resolverEnabled = oldResolverEnabled;
				this.resolver = oldResolver;
				this.currentQueryResult = oldQueryResult;
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
			// don't Scan again if the node was already resolved
			if (resolveResultCache.ContainsKey(node)) {
				// Restore state change caused by this node:
				CSharpResolver newResolver;
				if (resolverAfterDict.TryGetValue(node, out newResolver))
					resolver = newResolver;
				return;
			}
			
			var mode = navigator.Scan(node);
			switch (mode) {
				case ResolveVisitorNavigationMode.Skip:
					if (node is VariableDeclarationStatement || node is SwitchSection) {
						// Enforce scanning of variable declarations.
						goto case ResolveVisitorNavigationMode.Scan;
					}
					StoreCurrentState(node);
					break;
				case ResolveVisitorNavigationMode.Scan:
					bool oldResolverEnabled = resolverEnabled;
					var oldResolver = resolver;
					resolverEnabled = false;
					StoreCurrentState(node);
					ResolveResult result = node.AcceptVisitor(this, null);
					if (result != null) {
						// If the node was resolved, store the result even though it wasn't requested.
						// This is necessary so that Visit-methods that decide to always resolve are
						// guaranteed to get called only once.
						// This is used for lambda registration.
						StoreResult(node, result);
						if (resolver != oldResolver) {
							// The node changed the resolver state:
							resolverAfterDict.Add(node, resolver);
						}
						cancellationToken.ThrowIfCancellationRequested();
					}
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
		/// --
		/// This method now is internal, because it is difficult to use correctly.
		/// Users of the public API should use Scan()+GetResolveResult() instead.
		/// </summary>
		internal ResolveResult Resolve(AstNode node)
		{
			if (node == null || node.IsNull)
				return errorResult;
			bool oldResolverEnabled = resolverEnabled;
			resolverEnabled = true;
			ResolveResult result;
			if (!resolveResultCache.TryGetValue(node, out result)) {
				cancellationToken.ThrowIfCancellationRequested();
				StoreCurrentState(node);
				var oldResolver = resolver;
				result = node.AcceptVisitor(this, null) ?? errorResult;
				StoreResult(node, result);
				if (resolver != oldResolver) {
					// The node changed the resolver state:
					resolverAfterDict.Add(node, resolver);
				}
			}
			resolverEnabled = oldResolverEnabled;
			return result;
		}
		
		IType ResolveType(AstType type)
		{
			return Resolve(type).Type;
		}
		
		void StoreCurrentState(AstNode node)
		{
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			
			#if DEBUG
			CSharpResolver oldResolver;
			if (resolverBeforeDict.TryGetValue(node, out oldResolver)) {
				Debug.Assert(oldResolver.LocalVariables.Count() == resolver.LocalVariables.Count());
			}
			#endif
			
			resolverBeforeDict[node] = resolver;
		}
		
		void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			if (node.IsNull)
				return;
			Log.WriteLine("Resolved '{0}' to {1}", node, result);
			Debug.Assert(!CSharpAstResolver.IsUnresolvableNode(node));
			// The state should be stored before the result is.
			Debug.Assert(resolverBeforeDict.ContainsKey(node));
			// Don't store results twice.
			Debug.Assert(!resolveResultCache.ContainsKey(node));
			resolveResultCache[node] = result;
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
		sealed class AnonymousFunctionConversion : Conversion
		{
			public readonly IType ReturnType;
			public readonly ExplicitlyTypedLambda ExplicitlyTypedLambda;
			public readonly LambdaTypeHypothesis Hypothesis;
			readonly bool isValid;
			
			public AnonymousFunctionConversion(IType returnType, LambdaTypeHypothesis hypothesis, bool isValid)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.Hypothesis = hypothesis;
				this.isValid = isValid;
			}
			
			public AnonymousFunctionConversion(IType returnType, ExplicitlyTypedLambda explicitlyTypedLambda, bool isValid)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.ExplicitlyTypedLambda = explicitlyTypedLambda;
				this.isValid = isValid;
			}
			
			public override bool IsValid {
				get { return isValid; }
			}
			
			public override bool IsImplicit {
				get { return true; }
			}
			
			public override bool IsAnonymousFunctionConversion {
				get { return true; }
			}
		}
		
		/// <summary>
		/// Convert 'rr' to the target type using the specified conversion.
		/// </summary>
		void ProcessConversion(Expression expr, ResolveResult rr, Conversion conversion, IType targetType)
		{
			AnonymousFunctionConversion afc = conversion as AnonymousFunctionConversion;
			if (afc != null) {
				Log.WriteLine("Processing conversion of anonymous function to " + targetType + "...");
				
				Log.Indent();
				if (afc.Hypothesis != null)
					afc.Hypothesis.MergeInto(this, afc.ReturnType);
				if (afc.ExplicitlyTypedLambda != null)
					afc.ExplicitlyTypedLambda.ApplyReturnType(this, afc.ReturnType);
				Log.Unindent();
			}
			if (expr != null && !expr.IsNull && conversion != Conversion.IdentityConversion) {
				navigator.ProcessConversion(expr, rr, conversion, targetType);
				conversionDict[expr] = new ConversionWithTargetType(conversion, targetType);
			}
		}
		
		void ImportConversions(ResolveVisitor childVisitor)
		{
			foreach (var pair in childVisitor.conversionDict) {
				conversionDict.Add(pair.Key, pair.Value);
				navigator.ProcessConversion(pair.Key, resolveResultCache[pair.Key], pair.Value.Conversion, pair.Value.TargetType);
			}
		}
		
		/// <summary>
		/// Convert 'rr' to the target type.
		/// </summary>
		void ProcessConversion(Expression expr, ResolveResult rr, IType targetType)
		{
			if (expr == null || expr.IsNull)
				return;
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
			Debug.Assert(!CSharpAstResolver.IsUnresolvableNode(node));
			
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
				storedResolver,
				delegate {
					navigator = new NodeListResolveVisitorNavigator(node, nodeToResolve);
					Debug.Assert(!resolverEnabled);
					Scan(parent);
					navigator = skipAllNavigator;
				});
			
			MergeUndecidedLambdas();
			if (resolveResultCache.TryGetValue(node, out result))
				return result;
			else
				return null;
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
				storedResolver,
				delegate {
					navigator = new NodeListResolveVisitorNavigator(new[] { node }, scanOnly: true);
					Debug.Assert(!resolverEnabled);
					// parent might already be resolved if 'node' is an unresolvable node
					Scan(parent);
					navigator = skipAllNavigator;
				});
			
			MergeUndecidedLambdas();
			while (node != null) {
				if (resolverBeforeDict.TryGetValue(node, out r))
					return r;
				node = node.Parent;
			}
			return null;
		}
		
		public CSharpResolver GetResolverStateAfter(AstNode node)
		{
			// Resolve the node to fill the resolverAfterDict
			GetResolveResult(node);
			CSharpResolver result;
			if (resolverAfterDict.TryGetValue(node, out result))
				return result;
			else
				return GetResolverStateBefore(node);
		}
		
		public ConversionWithTargetType GetConversionWithTargetType(Expression expr)
		{
			GetResolverStateBefore(expr);
			ResolveParentForConversion(expr);
			ConversionWithTargetType result;
			if (conversionDict.TryGetValue(expr, out result)) {
				return result;
			} else {
				ResolveResult rr = GetResolveResultIfResolved(expr);
				return new ConversionWithTargetType(Conversion.IdentityConversion, rr != null ? rr.Type : SpecialType.UnknownType);
			}
		}
		#endregion
		
		#region Track UsingScope
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCompilationUnit(CompilationUnit unit, object data)
		{
			CSharpResolver previousResolver = resolver;
			try {
				if (parsedFile != null)
					resolver = resolver.WithCurrentUsingScope(parsedFile.RootUsingScope.Resolve(resolver.Compilation));
				ScanChildren(unit);
				return voidResult;
			} finally {
				resolver = previousResolver;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			CSharpResolver previousResolver = resolver;
			try {
				if (parsedFile != null) {
					resolver = resolver.WithCurrentUsingScope(parsedFile.GetUsingScope(namespaceDeclaration.StartLocation).Resolve(resolver.Compilation));
				}
				ScanChildren(namespaceDeclaration);
				// merge undecided lambdas before leaving the using scope so that
				// the resolver can make better use of its cache
				MergeUndecidedLambdas();
				if (resolver.CurrentUsingScope != null && resolver.CurrentUsingScope.Namespace != null)
					return new NamespaceResolveResult(resolver.CurrentUsingScope.Namespace);
				else
					return null;
			} finally {
				resolver = previousResolver;
			}
		}
		#endregion
		
		#region Track CurrentTypeDefinition
		ResolveResult VisitTypeOrDelegate(AstNode typeDeclaration, string name, int typeParameterCount)
		{
			CSharpResolver previousResolver = resolver;
			try {
				ITypeDefinition newTypeDefinition = null;
				if (resolver.CurrentTypeDefinition != null) {
					int totalTypeParameterCount = resolver.CurrentTypeDefinition.TypeParameterCount + typeParameterCount;
					foreach (ITypeDefinition nestedType in resolver.CurrentTypeDefinition.NestedTypes) {
						if (nestedType.Name == name && nestedType.TypeParameterCount == totalTypeParameterCount) {
							newTypeDefinition = nestedType;
							break;
						}
					}
				} else if (resolver.CurrentUsingScope != null) {
					newTypeDefinition = resolver.CurrentUsingScope.Namespace.GetTypeDefinition(name, typeParameterCount);
				}
				if (newTypeDefinition != null)
					resolver = resolver.WithCurrentTypeDefinition(newTypeDefinition);
				
				ScanChildren(typeDeclaration);
				
				// merge undecided lambdas before leaving the type definition so that
				// the resolver can make better use of its cache
				MergeUndecidedLambdas();
				
				return newTypeDefinition != null ? new TypeResolveResult(newTypeDefinition) : errorResult;
			} finally {
				resolver = previousResolver;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			return VisitTypeOrDelegate(typeDeclaration, typeDeclaration.Name, typeDeclaration.TypeParameters.Count);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			return VisitTypeOrDelegate(delegateDeclaration, delegateDeclaration.Name, delegateDeclaration.TypeParameters.Count);
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
			//int initializerCount = fieldOrEventDeclaration.GetChildrenByRole(FieldDeclaration.Roles.Variable).Count;
			CSharpResolver oldResolver = resolver;
			for (AstNode node = fieldOrEventDeclaration.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FieldDeclaration.Roles.Variable) {
					resolver = resolver.WithCurrentMember(GetMemberFromLocation(node.StartLocation));
					
					Scan(node);
					
					resolver = oldResolver;
				} else {
					Scan(node);
				}
			}
			return voidResult;
		}
		
		IMember GetMemberFromLocation(TextLocation location)
		{
			ITypeDefinition typeDef = resolver.CurrentTypeDefinition;
			if (typeDef == null)
				return null;
			return typeDef.GetMembers(m => m.ParsedFile == parsedFile && m.Region.IsInside(location), GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions).FirstOrDefault();
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			// Within the variable initializer, the newly declared variable is not yet available:
			var resolverWithVariable = resolver;
			if (variableInitializer.Parent is VariableDeclarationStatement)
				resolver = resolver.PopLastVariable();
			
			ArrayInitializerExpression aie = variableInitializer.Initializer as ArrayInitializerExpression;
			if (resolverEnabled || aie != null) {
				ResolveResult result = errorResult;
				if (variableInitializer.Parent is FieldDeclaration || variableInitializer.Parent is EventDeclaration) {
					if (resolver.CurrentMember != null) {
						result = new MemberResolveResult(null, resolver.CurrentMember);
					}
				} else {
					string identifier = variableInitializer.Name;
					foreach (IVariable v in resolverWithVariable.LocalVariables) {
						if (v.Name == identifier) {
							result = new LocalResolveResult(v);
							break;
						}
					}
				}
				ArrayType arrayType = result.Type as ArrayType;
				if (aie != null && arrayType != null) {
					StoreCurrentState(aie);
					List<Expression> initializerElements = new List<Expression>();
					UnpackArrayInitializer(initializerElements, aie, arrayType.Dimensions, true);
					ResolveResult[] initializerElementResults = new ResolveResult[initializerElements.Count];
					for (int i = 0; i < initializerElementResults.Length; i++) {
						initializerElementResults[i] = Resolve(initializerElements[i]);
					}
					var arrayCreation = resolver.ResolveArrayCreation(arrayType.ElementType, arrayType.Dimensions, null, initializerElementResults);
					StoreResult(aie, arrayCreation);
					ProcessConversionResults(initializerElements, arrayCreation.InitializerElements);
				} else if (variableInitializer.Parent is FixedStatement) {
					var initRR = Resolve(variableInitializer.Initializer);
					PointerType pointerType;
					if (initRR.Type.Kind == TypeKind.Array) {
						pointerType = new PointerType(((ArrayType)initRR.Type).ElementType);
					} else if (ReflectionHelper.GetTypeCode(initRR.Type) == TypeCode.String) {
						pointerType = new PointerType(resolver.Compilation.FindType(KnownTypeCode.Char));
					} else {
						pointerType = null;
						ProcessConversion(variableInitializer.Initializer, initRR, result.Type);
					}
					if (pointerType != null) {
						var conversion = resolver.conversions.ImplicitConversion(pointerType, result.Type);
						if (conversion.IsIdentityConversion)
							conversion = Conversion.ImplicitPointerConversion;
						ProcessConversion(variableInitializer.Initializer, initRR, conversion, result.Type);
					}
				} else {
					ResolveAndProcessConversion(variableInitializer.Initializer, result.Type);
				}
				resolver = resolverWithVariable;
				return result;
			} else {
				Scan(variableInitializer.Initializer);
				resolver = resolverWithVariable;
				return null;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, object data)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (resolver.CurrentMember != null) {
					result = new MemberResolveResult(null, resolver.CurrentMember);
				}
				ResolveAndProcessConversion(fixedVariableInitializer.CountExpression, resolver.Compilation.FindType(KnownTypeCode.Int32));
				return result;
			} else {
				ScanChildren(fixedVariableInitializer);
				return null;
			}
		}
		
		ResolveResult VisitMethodMember(AttributedNode member)
		{
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCurrentMember(GetMemberFromLocation(member.StartLocation));
				
				ScanChildren(member);
				
				if (resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
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
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCurrentMember(GetMemberFromLocation(propertyOrIndexerDeclaration.StartLocation));
				
				for (AstNode node = propertyOrIndexerDeclaration.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == PropertyDeclaration.SetterRole && resolver.CurrentMember != null) {
						resolver = resolver.PushBlock();
						resolver = resolver.AddVariable(new DefaultParameter(resolver.CurrentMember.ReturnType, "value"));
						Scan(node);
						resolver = resolver.PopBlock();
					} else {
						Scan(node);
					}
				}
				if (resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
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
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCurrentMember(GetMemberFromLocation(eventDeclaration.StartLocation));
				
				if (resolver.CurrentMember != null) {
					resolver = resolver.PushBlock();
					resolver = resolver.AddVariable(new DefaultParameter(resolver.CurrentMember.ReturnType, "value"));
					ScanChildren(eventDeclaration);
				} else {
					ScanChildren(eventDeclaration);
				}
				
				if (resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
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
						return new LocalResolveResult(p);
				}
				
				IParameterizedMember pm = resolver.CurrentMember as IParameterizedMember;
				if (pm == null && resolver.CurrentTypeDefinition != null) {
					// Also consider delegate parameters:
					pm = resolver.CurrentTypeDefinition.GetDelegateInvokeMethod();
					// pm will be null if the current type isn't a delegate
				}
				if (pm != null) {
					foreach (IParameter p in pm.Parameters) {
						if (p.Name == name) {
							return new LocalResolveResult(p);
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
			CSharpResolver oldResolver = resolver;
			try {
				// Scan enum member attributes before setting resolver.CurrentMember, so that
				// enum values used as attribute arguments have the correct type.
				// (which an enum member, all other enum members are treated as having their underlying type)
				foreach (var attributeSection in enumMemberDeclaration.Attributes)
					Scan(attributeSection);
				
				resolver = resolver.WithCurrentMember(GetMemberFromLocation(enumMemberDeclaration.StartLocation));
				
				if (resolverEnabled && resolver.CurrentTypeDefinition != null) {
					ResolveAndProcessConversion(enumMemberDeclaration.Initializer, resolver.CurrentTypeDefinition.EnumUnderlyingType);
					if (resolverEnabled && resolver.CurrentMember != null)
						return new MemberResolveResult(null, resolver.CurrentMember);
					else
						return errorResult;
				} else {
					Scan(enumMemberDeclaration.Initializer);
					return null;
				}
			} finally {
				resolver = oldResolver;
			}
		}
		#endregion
		
		#region Track CheckForOverflow
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCheckForOverflow(true);
				if (resolverEnabled) {
					return Resolve(checkedExpression.Expression);
				} else {
					ScanChildren(checkedExpression);
					return null;
				}
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCheckForOverflow(false);
				if (resolverEnabled) {
					return Resolve(uncheckedExpression.Expression);
				} else {
					ScanChildren(uncheckedExpression);
					return null;
				}
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCheckForOverflow(true);
				ScanChildren(checkedStatement);
				return voidResult;
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			CSharpResolver oldResolver = resolver;
			try {
				resolver = resolver.WithCheckForOverflow(false);
				ScanChildren(uncheckedStatement);
				return voidResult;
			} finally {
				resolver = oldResolver;
			}
		}
		#endregion
		
		#region Visit AnonymousTypeCreateExpression
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
			List<IUnresolvedProperty> properties = new List<IUnresolvedProperty>();
			var initializers = anonymousTypeCreateExpression.Initializers;
			foreach (var expr in initializers) {
				Expression resolveExpr;
				var name = GetAnonymousTypePropertyName(expr, out resolveExpr);
				if (resolveExpr != null) {
					var returnType = Resolve(resolveExpr).Type;
					var returnTypeRef = returnType.ToTypeReference();
					var property = new DefaultUnresolvedProperty {
						Name = name,
						Accessibility = Accessibility.Public,
						ReturnType = returnTypeRef,
						Getter = new DefaultUnresolvedMethod {
							Name = "get_" + name,
							Accessibility = Accessibility.Public,
							ReturnType = returnTypeRef
						}
					};
					properties.Add(property);
				}
			}
			var anonymousType = new AnonymousType(resolver.Compilation, properties);
			foreach (var pair in initializers.Zip(anonymousType.GetProperties(), (expr, prop) => new { expr = expr as NamedExpression, prop })) {
				if (pair.expr != null) {
					StoreCurrentState(pair.expr);
					// pair.expr.Expression was already resolved by the first loop
					StoreResult(pair.expr, new MemberResolveResult(new ResolveResult(anonymousType), pair.prop));
				}
			}
			return new ResolveResult(anonymousType);
		}
		#endregion
		
		#region Visit Expressions
		ResolveResult IAstVisitor<object, ResolveResult>.VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
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
				StoreCurrentState(arrayCreateExpression.Initializer);
				
				initializerElements = new List<Expression>();
				UnpackArrayInitializer(initializerElements, arrayCreateExpression.Initializer, dimensions, true);
				initializerElementResults = new ResolveResult[initializerElements.Count];
				for (int i = 0; i < initializerElementResults.Length; i++) {
					initializerElementResults[i] = Resolve(initializerElements[i]);
				}
				StoreResult(arrayCreateExpression.Initializer, voidResult);
			}
			
			ArrayCreateResolveResult acrr;
			if (arrayCreateExpression.Type.IsNull) {
				acrr = resolver.ResolveArrayCreation(null, dimensions, sizeArguments, initializerElementResults);
			} else {
				IType elementType = ResolveType(arrayCreateExpression.Type);
				foreach (var spec in additionalArraySpecifiers.Reverse()) {
					elementType = new ArrayType(resolver.Compilation, elementType, spec.Dimensions);
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
						if (resolveNestedInitializesToVoid) {
							StoreCurrentState(aie);
							StoreResult(aie, voidResult);
						}
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
				Expression left = assignmentExpression.Left;
				Expression right = assignmentExpression.Right;
				ResolveResult leftResult = Resolve(left);
				ResolveResult rightResult = Resolve(right);
				ResolveResult rr = resolver.ResolveAssignment(assignmentExpression.Operator, leftResult, rightResult);
				ProcessConversionsInBinaryOperatorResult(left, right, rr);
				return rr;
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
				ProcessConversionsInBinaryOperatorResult(left, right, rr);
				return rr;
			} else {
				ScanChildren(binaryOperatorExpression);
				return null;
			}
		}
		
		ResolveResult ProcessConversionsInBinaryOperatorResult(Expression left, Expression right, ResolveResult rr)
		{
			OperatorResolveResult orr = rr as OperatorResolveResult;
			if (orr != null && orr.Operands.Count == 2) {
				ProcessConversionResult(left, orr.Operands[0] as ConversionResolveResult);
				ProcessConversionResult(right, orr.Operands[1] as ConversionResolveResult);
			} else {
				InvocationResolveResult irr = rr as InvocationResolveResult;
				if (irr != null && irr.Arguments.Count == 2) {
					ProcessConversionResult(left, irr.Arguments[0] as ConversionResolveResult);
					ProcessConversionResult(right, irr.Arguments[1] as ConversionResolveResult);
				}
			}
			return rr;
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
				OperatorResolveResult corr = rr as OperatorResolveResult;
				if (corr != null && corr.Operands.Count == 3) {
					ProcessConversionResult(condition, corr.Operands[0] as ConversionResolveResult);
					ProcessConversionResult(trueExpr, corr.Operands[1] as ConversionResolveResult);
					ProcessConversionResult(falseExpr, corr.Operands[2] as ConversionResolveResult);
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
			return new ResolveResult(resolver.Compilation.FindType(KnownTypeCode.Boolean));
		}
		
		// NamedArgumentExpression is "identifier: Expression"
		ResolveResult IAstVisitor<object, ResolveResult>.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			// The parent expression takes care of handling NamedArgumentExpressions
			// by calling GetArguments().
			// This method gets called only when scanning, or when the named argument is used
			// in an invalid context.
			Scan(namedArgumentExpression.Expression);
			return null;
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
			return resolver.ResolvePrimitive(null);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (resolverEnabled || !objectCreateExpression.Initializer.IsNull) {
				IType type = ResolveType(objectCreateExpression.Type);
				
				var initializer = objectCreateExpression.Initializer;
				if (!initializer.IsNull) {
					HandleObjectInitializer(type, initializer);
				}
				
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(objectCreateExpression.Arguments, out argumentNames);
				
				ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames);
				if (arguments.Length == 1 && rr.Type.Kind == TypeKind.Delegate) {
					// process conversion in case it's a delegate creation
					ProcessConversionResult(objectCreateExpression.Arguments.Single(), rr as ConversionResolveResult);
					// wrap the result so that the delegate creation is not handled as a reference
					// to the target method - otherwise FindReferencedEntities would produce two results for
					// the same delegate creation.
					return WrapResult(rr);
				} else {
					// process conversions in all other cases
					ProcessConversionsInInvocation(null, objectCreateExpression.Arguments, rr as CSharpInvocationResolveResult);
					return rr;
				}
			} else {
				ScanChildren(objectCreateExpression);
				return null;
			}
		}
		
		void HandleObjectInitializer(IType type, ArrayInitializerExpression initializer)
		{
			StoreCurrentState(initializer);
			resolver = resolver.PushInitializerType(type);
			foreach (Expression element in initializer.Elements) {
				ArrayInitializerExpression aie = element as ArrayInitializerExpression;
				if (aie != null) {
					StoreCurrentState(aie);
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
						OverloadResolution or = mgrr.PerformOverloadResolution(resolver.Compilation, addArguments, null, false, false, resolver.conversions);
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
			resolver = resolver.PopInitializerType();
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
			return resolver.ResolvePrimitive(primitiveExpression.Value);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			return resolver.ResolveSizeOf(ResolveType(sizeOfExpression.Type));
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			ResolveAndProcessConversion(stackAllocExpression.CountExpression, resolver.Compilation.FindType(KnownTypeCode.Int32));
			return new ResolveResult(new PointerType(ResolveType(stackAllocExpression.Type)));
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return resolver.ResolveThisReference();
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveTypeOf(ResolveType(typeOfExpression.Type));
			} else {
				Scan(typeOfExpression.Type);
				return null;
			}
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
				ITypeDefinition inputTypeDef = input.Type.GetDefinition();
				if (input.IsCompileTimeConstant && expr is PrimitiveExpression && inputTypeDef != null) {
					// Special cases for int.MinValue and long.MinValue
					if (inputTypeDef.KnownTypeCode == KnownTypeCode.UInt32 && 2147483648.Equals(input.ConstantValue)) {
						return new ConstantResolveResult(resolver.Compilation.FindType(KnownTypeCode.Int32), -2147483648);
					} else if (inputTypeDef.KnownTypeCode == KnownTypeCode.UInt64 && 9223372036854775808.Equals(input.ConstantValue)) {
						return new ConstantResolveResult(resolver.Compilation.FindType(KnownTypeCode.Int64), -9223372036854775808);
					}
				}
				ResolveResult rr = resolver.ResolveUnaryOperator(unaryOperatorExpression.Operator, input);
				OperatorResolveResult uorr = rr as OperatorResolveResult;
				if (uorr != null && uorr.Operands.Count == 1) {
					ProcessConversionResult(expr, uorr.Operands[0] as ConversionResolveResult);
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
			IType resultType;
			switch (undocumentedExpression.UndocumentedExpressionType) {
				case UndocumentedExpressionType.ArgListAccess:
				case UndocumentedExpressionType.ArgList:
					resultType = resolver.Compilation.FindType(typeof(RuntimeArgumentHandle));
					break;
				case UndocumentedExpressionType.RefValue:
					var tre = undocumentedExpression.Arguments.ElementAtOrDefault(1) as TypeReferenceExpression;
					if (tre != null)
						resultType = ResolveType(tre.Type);
					else
						resultType = SpecialType.UnknownType;
					break;
				case UndocumentedExpressionType.RefType:
					resultType = resolver.Compilation.FindType(KnownTypeCode.Type);
					break;
				case UndocumentedExpressionType.MakeRef:
					resultType = resolver.Compilation.FindType(typeof(TypedReference));
					break;
				default:
					throw new InvalidOperationException("Invalid value for UndocumentedExpressionType");
			}
			return new ResolveResult(resultType);
		}
		#endregion
		
		#region Visit Identifier/MemberReference/Invocation-Expression
		// IdentifierExpression, MemberReferenceExpression and InvocationExpression
		// are grouped together because they have to work together for
		// "7.6.4.1 Identical simple names and type names" support
		List<IType> ResolveTypeArguments(IEnumerable<AstType> typeArguments)
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
				var typeArguments = ResolveTypeArguments(identifierExpression.TypeArguments);
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
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0) {
				// Special handling for §7.6.4.1 Identicial simple names and type names
				StoreCurrentState(identifierExpression);
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
					return ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
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
			var typeArguments = ResolveTypeArguments(memberReferenceExpression.TypeArguments);
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
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0) {
				// Special handling for §7.6.4.1 Identicial simple names and type names
				
				StoreCurrentState(identifierExpression);
				StoreCurrentState(mre);
				
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
					return ResolveInvocationOnGivenTarget(target, invocationExpression);
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
			CSharpResolver oldResolver = resolver;
			List<IParameter> parameters = (hasParameterList || parameterDeclarations.Any()) ? new List<IParameter>() : null;
			bool oldIsWithinLambdaExpression = resolver.IsWithinLambdaExpression;
			resolver = resolver.WithIsWithinLambdaExpression(true);
			foreach (var pd in parameterDeclarations) {
				IType type = ResolveType(pd.Type);
				if (pd.ParameterModifier == ParameterModifier.Ref || pd.ParameterModifier == ParameterModifier.Out)
					type = new ByReferenceType(type);
				
				IParameter p = new DefaultParameter(type, pd.Name, MakeRegion(pd),
				                                    isRef: pd.ParameterModifier == ParameterModifier.Ref,
				                                    isOut: pd.ParameterModifier == ParameterModifier.Out);
				// The parameter declaration must be scanned in the current context (without the new parameter)
				// in order to be consistent with the context in which we resolved pd.Type.
				StoreCurrentState(pd);
				StoreResult(pd, new LocalResolveResult(p));
				ScanChildren(pd);
				
				resolver = resolver.AddVariable(p);
				parameters.Add(p);
			}
			
			var lambda = new ExplicitlyTypedLambda(parameters, isAnonymousMethod, isAsync, resolver, this, body);
			
			// Don't scan the lambda body here - we'll do that later when analyzing the ExplicitlyTypedLambda.
			
			resolver = oldResolver;
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
			bool isEndpointUnreachable;
			
			// The actual return type is set when the lambda is applied by the conversion.
			IType actualReturnType;
			
			internal override bool IsUndecided {
				get { return actualReturnType == null; }
			}
			
			internal override AstNode LambdaExpression {
				get { return body.Parent; }
			}
			
			internal override AstNode BodyExpression {
				get { return body; }
			}
			
			public override ResolveResult Body {
				get {
					if (body is Expression) {
						Analyze();
						if (returnValues.Count == 1)
							return returnValues[0];
					}
					return visitor.voidResult;
				}
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
							visitor.AnalyzeLambda(body, isAsync, out isValidAsVoidMethod, out isEndpointUnreachable, out inferredReturnType, out returnExpressions, out returnValues);
							visitor.navigator = oldNavigator;
						});
					Log.Unindent();
					Log.WriteLine("Finished analyzing " + this.LambdaExpression);
					
					if (inferredReturnType == null)
						throw new InvalidOperationException("AnalyzeLambda() didn't set inferredReturnType");
				}
				return true;
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Log.WriteLine("Testing validity of {0} for return-type {1}...", this, returnType);
				Log.Indent();
				bool valid = Analyze() && IsValidLambda(isValidAsVoidMethod, isEndpointUnreachable, isAsync, returnValues, returnType, conversions);
				Log.Unindent();
				Log.WriteLine("{0} is {1} for return-type {2}", this, valid ? "valid" : "invalid", returnType);
				return new AnonymousFunctionConversion(returnType, this, valid);
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
				if (returnType.Kind != TypeKind.Void) {
					for (int i = 0; i < returnExpressions.Count; i++) {
						visitor.ProcessConversion(returnExpressions[i], returnValues[i], returnType);
					}
				}
			}
			
			internal override void EnforceMerge(ResolveVisitor parentVisitor)
			{
				ApplyReturnType(parentVisitor, SpecialType.UnknownType);
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
			internal ResolveResult bodyResult;
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
			
			internal override AstNode BodyExpression {
				get {
					if (selectClause != null)
						return selectClause.Expression;
					else
						return lambda.Body;
				}
			}
			
			public override ResolveResult Body {
				get { return bodyResult; }
			}
			
			private ImplicitlyTypedLambda(ResolveVisitor parentVisitor)
			{
				this.parentVisitor = parentVisitor;
				this.storedContext = parentVisitor.resolver;
				this.parsedFile = parentVisitor.parsedFile;
				this.bodyResult = parentVisitor.voidResult;
			}
			
			public ImplicitlyTypedLambda(LambdaExpression lambda, ResolveVisitor parentVisitor)
				: this(parentVisitor)
			{
				this.lambda = lambda;
				foreach (var pd in lambda.Parameters) {
					parameters.Add(new DefaultParameter(SpecialType.UnknownType, pd.Name, parentVisitor.MakeRegion(pd)));
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
				Log.WriteLine("{0} is {1} for return-type {2}", hypothesis, c.IsValid ? "valid" : "invalid", returnType);
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
				ResolveVisitor visitor = new ResolveVisitor(storedContext, parsedFile);
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
						parameterTypes[i] = SpecialType.UnknownType;
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
				GetAnyHypothesis().MergeInto(parentVisitor, SpecialType.UnknownType);
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
				get { return lambda != null && lambda.IsAsync; }
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
		sealed class LambdaTypeHypothesis : IResolveVisitorNavigator
		{
			readonly ImplicitlyTypedLambda lambda;
			internal readonly IParameter[] lambdaParameters;
			internal readonly IType[] parameterTypes;
			readonly ResolveVisitor visitor;
			
			internal readonly IType inferredReturnType;
			IList<Expression> returnExpressions;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			bool isEndpointUnreachable;
			internal bool success;
			
			public LambdaTypeHypothesis(ImplicitlyTypedLambda lambda, IType[] parameterTypes, ResolveVisitor visitor,
			                            ICollection<ParameterDeclaration> parameterDeclarations)
			{
				Debug.Assert(parameterTypes.Length == lambda.Parameters.Count);
				
				this.lambda = lambda;
				this.parameterTypes = parameterTypes;
				this.visitor = visitor;
				visitor.SetNavigator(this);
				
				Log.WriteLine("Analyzing " + ToString() + "...");
				Log.Indent();
				CSharpResolver oldResolver = visitor.resolver;
				visitor.resolver = visitor.resolver.WithIsWithinLambdaExpression(true);
				lambdaParameters = new IParameter[parameterTypes.Length];
				if (parameterDeclarations != null) {
					int i = 0;
					foreach (var pd in parameterDeclarations) {
						lambdaParameters[i] = new DefaultParameter(parameterTypes[i], pd.Name, visitor.MakeRegion(pd));
						visitor.resolver = visitor.resolver.AddVariable(lambdaParameters[i]);
						i++;
						visitor.Scan(pd);
					}
				} else {
					for (int i = 0; i < parameterTypes.Length; i++) {
						var p = lambda.Parameters[i];
						lambdaParameters[i] = new DefaultParameter(parameterTypes[i], p.Name, p.Region);
						visitor.resolver = visitor.resolver.AddVariable(lambdaParameters[i]);
					}
				}
				
				success = true;
				visitor.AnalyzeLambda(lambda.BodyExpression, lambda.IsAsync, out isValidAsVoidMethod, out isEndpointUnreachable, out inferredReturnType, out returnExpressions, out returnValues);
				visitor.resolver = oldResolver;
				Log.Unindent();
				Log.WriteLine("Finished analyzing " + ToString());
			}
			
			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
				return ResolveVisitorNavigationMode.Resolve;
			}
			
			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
				if (result.IsError)
					success = false;
			}
			
			void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				success &= conversion.IsValid;
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
				bool valid = success && IsValidLambda(isValidAsVoidMethod, isEndpointUnreachable, lambda.IsAsync, returnValues, returnType, conversions);
				return new AnonymousFunctionConversion(returnType, this, valid);
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
				if (lambda.BodyExpression is Expression && returnValues.Count == 1) {
					lambda.bodyResult = returnValues[0];
				}
				
				Log.WriteLine("Applying return type {0} to implicitly-typed lambda {1}", returnType, lambda.LambdaExpression);
				if (lambda.IsAsync)
					returnType = parentVisitor.UnpackTask(returnType);
				if (returnType.Kind != TypeKind.Void) {
					for (int i = 0; i < returnExpressions.Count; i++) {
						visitor.ProcessConversion(returnExpressions[i], returnValues[i], returnType);
					}
				}
				
				visitor.MergeUndecidedLambdas();
				Log.WriteLine("Merging " + ToString());
				foreach (var pair in visitor.resolverBeforeDict) {
					Debug.Assert(!parentVisitor.resolverBeforeDict.ContainsKey(pair.Key));
					parentVisitor.resolverBeforeDict[pair.Key] = pair.Value;
				}
				foreach (var pair in visitor.resolverAfterDict) {
					Debug.Assert(!parentVisitor.resolverAfterDict.ContainsKey(pair.Key));
					parentVisitor.resolverAfterDict[pair.Key] = pair.Value;
				}
				foreach (var pair in visitor.resolveResultCache) {
					parentVisitor.StoreResult(pair.Key, pair.Value);
				}
				parentVisitor.ImportConversions(visitor);
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
				b.Append(lambda.BodyExpression.ToString());
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
			internal abstract AstNode BodyExpression { get; }
			
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
				ResolveParentForConversion(lambda.LambdaExpression);
				if (lambda.IsUndecided) {
					// Lambda wasn't merged by resolving its parent -> enforce merging
					Log.WriteLine("Lambda wasn't merged by conversion - enforce merging");
					lambda.EnforceMerge(this);
				}
			}
			Log.Unindent();
			Log.WriteLine("MergeUndecidedLambdas() finished.");
		}
		
		void ResolveParentForConversion(AstNode expression)
		{
			AstNode parent = expression.Parent;
			// Continue going upwards until we find a node that can be resolved and provides
			// an expected type.
			while (ParenthesizedExpression.ActsAsParenthesizedExpression(parent) || CSharpAstResolver.IsUnresolvableNode(parent)) {
				parent = parent.Parent;
			}
			CSharpResolver storedResolver;
			if (parent != null && resolverBeforeDict.TryGetValue(parent, out storedResolver)) {
				Log.WriteLine("Trying to resolve '" + parent + "' in order to find the conversion applied to '" + expression + "'...");
				Log.Indent();
				ResetContext(storedResolver, delegate { Resolve(parent); });
				Log.Unindent();
			} else {
				Log.WriteLine("Could not find a suitable parent for '" + expression + "'");
			}
		}
		#endregion
		
		#region AnalyzeLambda
		IType GetTaskType(IType resultType)
		{
			if (resultType.Kind == TypeKind.Unknown)
				return SpecialType.UnknownType;
			if (resultType.Kind == TypeKind.Void)
				return resolver.Compilation.FindType(KnownTypeCode.Task);
			
			ITypeDefinition def = resolver.Compilation.FindType(KnownTypeCode.TaskOfT).GetDefinition();
			if (def != null)
				return new ParameterizedType(def, new[] { resultType });
			else
				return SpecialType.UnknownType;
		}
		
		void AnalyzeLambda(AstNode body, bool isAsync, out bool isValidAsVoidMethod, out bool isEndpointUnreachable, out IType inferredReturnType, out IList<Expression> returnExpressions, out IList<ResolveResult> returnValues)
		{
			isEndpointUnreachable = false;
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
					inferredReturnType = resolver.Compilation.FindType(KnownTypeCode.Void);
				} else {
					returnExpressions = alv.ReturnExpressions;
					returnValues = new ResolveResult[returnExpressions.Count];
					for (int i = 0; i < returnValues.Count; i++) {
						returnValues[i] = resolveResultCache[returnExpressions[i]];
					}
					TypeInference ti = new TypeInference(resolver.Compilation, resolver.conversions);
					bool tiSuccess;
					inferredReturnType = ti.GetBestCommonType(returnValues, out tiSuccess);
					// Failure to infer a return type does not make the lambda invalid,
					// so we can ignore the 'tiSuccess' value
					if (isValidAsVoidMethod && returnExpressions.Count == 0 && body is Statement) {
						var reachabilityAnalysis = ReachabilityAnalysis.Create(
							(Statement)body, (node, _) => resolveResultCache[node],
							resolver.CurrentTypeResolveContext, cancellationToken);
						isEndpointUnreachable = !reachabilityAnalysis.IsEndpointReachable((Statement)body);
					}
				}
			}
			if (isAsync)
				inferredReturnType = GetTaskType(inferredReturnType);
			Log.WriteLine("Lambda return type was inferred to: " + inferredReturnType);
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
		
		static bool IsValidLambda(bool isValidAsVoidMethod, bool isEndpointUnreachable, bool isAsync, IList<ResolveResult> returnValues, IType returnType, Conversions conversions)
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
					return isEndpointUnreachable;
				if (isAsync) {
					// async lambdas must return Task<T>
					if (!(IsTask(returnType) && returnType.TypeParameterCount == 1))
						return false;
					// unpack Task<T> for testing the implicit conversions
					returnType = ((ParameterizedType)returnType).GetTypeArgument(0);
				}
				foreach (ResolveResult returnRR in returnValues) {
					if (!conversions.ImplicitConversion(returnRR, returnType).IsValid)
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
				return resolver.Compilation.FindType(KnownTypeCode.Void);
			else
				return ((ParameterizedType)type).GetTypeArgument(0);
		}
		
		/// <summary>
		/// Gets whether the specified type is Task or Task&lt;T&gt;.
		/// </summary>
		static bool IsTask(IType type)
		{
			ITypeDefinition def = type.GetDefinition();
			if (def != null) {
				if (def.KnownTypeCode == KnownTypeCode.Task)
					return true;
				if (def.KnownTypeCode == KnownTypeCode.TaskOfT)
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
			resolver = resolver.PushBlock();
			ScanChildren(blockStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			resolver = resolver.PushBlock();
			if (resolverEnabled) {
				for (AstNode child = usingStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == UsingStatement.ResourceAcquisitionRole && child is Expression) {
						ResolveAndProcessConversion((Expression)child, resolver.Compilation.FindType(KnownTypeCode.IDisposable));
					} else {
						Scan(child);
					}
				}
			} else {
				ScanChildren(usingStatement);
			}
			resolver = resolver.PopBlock();
			return resolverEnabled ? voidResult : null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			resolver = resolver.PushBlock();
			IType type = ResolveType(fixedStatement.Type);
			foreach (VariableInitializer vi in fixedStatement.Variables) {
				resolver = resolver.AddVariable(MakeVariable(type, vi.NameToken));
				Scan(vi);
			}
			Scan(fixedStatement.EmbeddedStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			resolver = resolver.PushBlock();
			IVariable v;
			if (IsVar(foreachStatement.VariableType)) {
				IType collectionType = Resolve(foreachStatement.InExpression).Type;
				IType elementType = GetElementTypeFromCollection(collectionType);
				StoreCurrentState(foreachStatement.VariableType);
				StoreResult(foreachStatement.VariableType, new TypeResolveResult(elementType));
				v = MakeVariable(elementType, foreachStatement.VariableNameToken);
			} else {
				IType elementType = ResolveType(foreachStatement.VariableType);
				Scan(foreachStatement.InExpression);
				v = MakeVariable(elementType, foreachStatement.VariableNameToken);
			}
			StoreCurrentState(foreachStatement.VariableNameToken);
			resolver = resolver.AddVariable(v);
			
			StoreResult(foreachStatement.VariableNameToken, new LocalResolveResult(v));
			
			Scan(foreachStatement.EmbeddedStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			resolver = resolver.PushBlock();
			ScanChildren(switchStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitCatchClause(CatchClause catchClause, object data)
		{
			resolver = resolver.PushBlock();
			if (string.IsNullOrEmpty(catchClause.VariableName)) {
				Scan(catchClause.Type);
			} else {
				DomRegion region = MakeRegion(catchClause.VariableNameToken);
				StoreCurrentState(catchClause.VariableNameToken);
				IVariable v = MakeVariable(ResolveType(catchClause.Type), catchClause.VariableNameToken);
				resolver = resolver.AddVariable(v);
				StoreResult(catchClause.VariableNameToken, new LocalResolveResult(v));
			}
			Scan(catchClause.Body);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		#endregion
		
		#region VariableDeclarationStatement
		ResolveResult IAstVisitor<object, ResolveResult>.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			bool isConst = (variableDeclarationStatement.Modifiers & Modifiers.Const) != 0;
			if (!isConst && IsVar(variableDeclarationStatement.Type) && variableDeclarationStatement.Variables.Count == 1) {
				VariableInitializer vi = variableDeclarationStatement.Variables.Single();
				StoreCurrentState(variableDeclarationStatement.Type);
				IType type = Resolve(vi.Initializer).Type;
				StoreResult(variableDeclarationStatement.Type, new TypeResolveResult(type));
				IVariable v = MakeVariable(type, vi.NameToken);
				resolver = resolver.AddVariable(v);
				Scan(vi);
			} else {
				IType type = ResolveType(variableDeclarationStatement.Type);

				foreach (VariableInitializer vi in variableDeclarationStatement.Variables) {
					IVariable v;
					if (isConst) {
						v = MakeConstant(type, vi.NameToken, Resolve(vi.Initializer).ConstantValue);
					} else {
						v = MakeVariable(type, vi.NameToken);
					}
					resolver = resolver.AddVariable(v);
					Scan(vi);
				}
			}
			return voidResult;
		}
		#endregion
		
		#region Condition Statements
		ResolveResult IAstVisitor<object, ResolveResult>.VisitForStatement(ForStatement forStatement, object data)
		{
			resolver = resolver.PushBlock();
			var result = HandleConditionStatement(forStatement);
			resolver = resolver.PopBlock();
			return result;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			return HandleConditionStatement(ifElseStatement);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			return HandleConditionStatement(whileStatement);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitDoWhileStatement(DoWhileStatement doWhileStatement, object data)
		{
			return HandleConditionStatement(doWhileStatement);
		}
		
		ResolveResult HandleConditionStatement(Statement conditionStatement)
		{
			if (resolverEnabled) {
				for (AstNode child = conditionStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == AstNode.Roles.Condition) {
						Expression condition = (Expression)child;
						ResolveResult conditionRR = Resolve(condition);
						ResolveResult convertedRR = resolver.ResolveCondition(conditionRR);
						if (convertedRR != conditionRR)
							ProcessConversionResult(condition, convertedRR as ConversionResolveResult);
					} else {
						Scan(child);
					}
				}
				return voidResult;
			} else {
				ScanChildren(conditionStatement);
				return null;
			}
		}
		#endregion
		
		#region Return Statements
		ResolveResult IAstVisitor<object, ResolveResult>.VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			if (resolverEnabled && !resolver.IsWithinLambdaExpression && resolver.CurrentMember != null) {
				IType type = resolver.CurrentMember.ReturnType;
				if (IsTask(type)) {
					var methodDecl = returnStatement.Ancestors.OfType<AttributedNode>().FirstOrDefault();
					if (methodDecl != null && (methodDecl.Modifiers & Modifiers.Async) == Modifiers.Async)
						type = UnpackTask(type);
				}
				ResolveAndProcessConversion(returnStatement.Expression, type);
			} else {
				Scan(returnStatement.Expression);
			}
			return resolverEnabled ? voidResult : null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitYieldReturnStatement(YieldReturnStatement yieldStatement, object data)
		{
			if (resolverEnabled && resolver.CurrentMember != null) {
				IType returnType = resolver.CurrentMember.ReturnType;
				IType elementType = GetElementTypeFromIEnumerable(returnType, resolver.Compilation, true);
				ResolveAndProcessConversion(yieldStatement.Expression, elementType);
			} else {
				Scan(yieldStatement.Expression);
			}
			return resolverEnabled ? voidResult : null;
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
			if (resolverEnabled) {
				ResolveAndProcessConversion(throwStatement.Expression, resolver.Compilation.FindType(KnownTypeCode.Exception));
				return voidResult;
			} else {
				Scan(throwStatement.Expression);
				return null;
			}
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
		
		IVariable MakeVariable(IType type, Identifier variableName)
		{
			return new SimpleVariable(MakeRegion(variableName), type, variableName.Name);
		}
		
		IVariable MakeConstant(IType type, Identifier variableName, object constantValue)
		{
			return new SimpleConstant(MakeRegion(variableName), type, variableName.Name, constantValue);
		}
		
		class SimpleVariable : IVariable
		{
			readonly DomRegion region;
			readonly IType type;
			readonly string name;
			
			public SimpleVariable(DomRegion region, IType type, string name)
			{
				Debug.Assert(type != null);
				Debug.Assert(name != null);
				this.region = region;
				this.type = type;
				this.name = name;
			}
			
			public string Name {
				get { return name; }
			}
			
			public DomRegion Region {
				get { return region; }
			}
			
			public IType Type {
				get { return type; }
			}
			
			public virtual bool IsConst {
				get { return false; }
			}
			
			public virtual object ConstantValue {
				get { return null; }
			}
			
			public override string ToString()
			{
				return type.ToString() + " " + name + ";";
			}
		}
		
		sealed class SimpleConstant : SimpleVariable
		{
			readonly object constantValue;
			
			public SimpleConstant(DomRegion region, IType type, string name, object constantValue)
				: base(region, type, name)
			{
				this.constantValue = constantValue;
			}
			
			public override bool IsConst {
				get { return true; }
			}
			
			public override object ConstantValue {
				get { return constantValue; }
			}
			
			public override string ToString()
			{
				return Type.ToString() + " " + Name + " = " + new PrimitiveExpression(constantValue).ToString() + ";";
			}
		}
		
		IType GetElementTypeFromCollection(IType collectionType)
		{
			switch (collectionType.Kind) {
				case TypeKind.Array:
					return ((ArrayType)collectionType).ElementType;
				case TypeKind.Dynamic:
					return SpecialType.Dynamic;
			}
			var memberLookup = resolver.CreateMemberLookup();
			var getEnumeratorMethodGroup = memberLookup.Lookup(new ResolveResult(collectionType), "GetEnumerator", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
			if (getEnumeratorMethodGroup != null) {
				var or = getEnumeratorMethodGroup.PerformOverloadResolution(resolver.Compilation, new ResolveResult[0]);
				if (or.FoundApplicableCandidate && !or.IsAmbiguous && !or.BestCandidate.IsStatic && or.BestCandidate.IsPublic) {
					IType enumeratorType = or.BestCandidate.ReturnType;
					return memberLookup.Lookup(new ResolveResult(enumeratorType), "Current", EmptyList<IType>.Instance, false).Type;
				}
			}
			return GetElementTypeFromIEnumerable(collectionType, resolver.Compilation, false);
		}
		
		static IType GetElementTypeFromIEnumerable(IType collectionType, ICompilation compilation, bool allowIEnumerator)
		{
			bool foundNonGenericIEnumerable = false;
			foreach (IType baseType in collectionType.GetAllBaseTypes()) {
				ITypeDefinition baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef != null) {
					KnownTypeCode typeCode = baseTypeDef.KnownTypeCode;
					if (typeCode == KnownTypeCode.IEnumerableOfT || (allowIEnumerator && typeCode == KnownTypeCode.IEnumeratorOfT)) {
						ParameterizedType pt = baseType as ParameterizedType;
						if (pt != null) {
							return pt.GetTypeArgument(0);
						}
					}
					if (typeCode == KnownTypeCode.IEnumerable || (allowIEnumerator && typeCode == KnownTypeCode.IEnumerator))
						foundNonGenericIEnumerable = true;
				}
			}
			// System.Collections.IEnumerable found in type hierarchy -> Object is element type.
			if (foundNonGenericIEnumerable)
				return compilation.FindType(KnownTypeCode.Object);
			return SpecialType.UnknownType;
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
			resolver = resolver.PushInitializerType(type);
			foreach (var arg in nonConstructorArguments)
				Scan(arg);
			resolver = resolver.PopInitializerType();
			
			// Resolve the ctor arguments and find the matching ctor overload
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(constructorArguments, out argumentNames);
			ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames);
			ProcessConversionsInInvocation(null, constructorArguments, rr as CSharpInvocationResolveResult);
			return rr;
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
			ScanChildren(usingDeclaration);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration, object data)
		{
			ScanChildren(usingDeclaration);
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
			KnownTypeCode typeCode = primitiveType.KnownTypeCode;
			if (typeCode == KnownTypeCode.None && primitiveType.Parent is Constraint && primitiveType.Role == Constraint.BaseTypeRole) {
				switch (primitiveType.Keyword) {
					case "class":
					case "struct":
					case "new":
						return voidResult;
				}
			}
			IType type = resolver.Compilation.FindType(typeCode);
			return new TypeResolveResult(type);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitSimpleType(SimpleType simpleType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(simpleType);
				return null;
			}
			
			// Figure out the correct lookup mode:
			AstType outermostType = simpleType;
			while (outermostType.Parent is AstType)
				outermostType = (AstType)outermostType.Parent;
			SimpleNameLookupMode lookupMode = SimpleNameLookupMode.Type;
			if (outermostType.Parent is UsingDeclaration || outermostType.Parent is UsingAliasDeclaration) {
				lookupMode = SimpleNameLookupMode.TypeInUsingDeclaration;
			} else if (outermostType.Parent is TypeDeclaration && outermostType.Role == TypeDeclaration.BaseTypeRole) {
				lookupMode = SimpleNameLookupMode.BaseTypeReference;
			}
			
			var typeArguments = ResolveTypeArguments(simpleType.TypeArguments);
			Identifier identifier = simpleType.IdentifierToken;
			if (string.IsNullOrEmpty(identifier.Name))
				return new TypeResolveResult(SpecialType.UnboundTypeArgument);
			ResolveResult rr = resolver.LookupSimpleNameOrTypeName(identifier.Name, typeArguments, lookupMode);
			if (simpleType.Parent is Attribute && !identifier.IsVerbatim) {
				var withSuffix = resolver.LookupSimpleNameOrTypeName(identifier.Name + "Attribute", typeArguments, lookupMode);
				if (AttributeTypeReference.PreferAttributeTypeWithSuffix(rr.Type, withSuffix.Type, resolver.Compilation))
					return withSuffix;
			}
			return rr;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitMemberType(MemberType memberType, object data)
		{
			ResolveResult target;
			if (memberType.IsDoubleColon && memberType.Target is SimpleType) {
				SimpleType t = (SimpleType)memberType.Target;
				StoreCurrentState(t);
				target = resolver.ResolveAlias(t.Identifier);
				StoreResult(t, target);
			} else {
				if (!resolverEnabled) {
					ScanChildren(memberType);
					return null;
				}
				target = Resolve(memberType.Target);
			}
			
			var typeArguments = ResolveTypeArguments(memberType.TypeArguments);
			Identifier identifier = memberType.MemberNameToken;
			ResolveResult rr = resolver.ResolveMemberType(target, identifier.Name, typeArguments);
			if (memberType.Parent is Attribute && !identifier.IsVerbatim) {
				var withSuffix = resolver.ResolveMemberType(target, identifier.Name + "Attribute", typeArguments);
				if (AttributeTypeReference.PreferAttributeTypeWithSuffix(rr.Type, withSuffix.Type, resolver.Compilation))
					return withSuffix;
			}
			return rr;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitComposedType(ComposedType composedType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(composedType);
				return null;
			}
			IType t = ResolveType(composedType.BaseType);
			if (composedType.HasNullableSpecifier) {
				t = NullableType.Create(resolver.Compilation, t);
			}
			for (int i = 0; i < composedType.PointerRank; i++) {
				t = new PointerType(t);
			}
			foreach (var a in composedType.ArraySpecifiers.Reverse()) {
				t = new ArrayType(resolver.Compilation, t, a.Dimensions);
			}
			return new TypeResolveResult(t);
		}
		#endregion
		
		#region Query Expressions
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			resolver = resolver.PushBlock();
			var oldQueryResult = currentQueryResult;
			var oldCancellationToken = cancellationToken;
			try {
				// Because currentQueryResult isn't part of the stored state,
				// query expressions must be resolved in a single operation.
				// This means we can't allow cancellation within the query expression.
				cancellationToken = CancellationToken.None;
				currentQueryResult = null;
				foreach (var clause in queryExpression.Clauses) {
					currentQueryResult = Resolve(clause);
				}
				return currentQueryResult;
			} finally {
				currentQueryResult = oldQueryResult;
				cancellationToken = oldCancellationToken;
				resolver = resolver.PopBlock();
			}
		}
		
		IType GetTypeForQueryVariable(IType type)
		{
			// This assumes queries are only used on IEnumerable.
			// We might want to look at the signature of a LINQ method (e.g. Select) instead.
			return GetElementTypeFromIEnumerable(type, resolver.Compilation, false);
		}
		
		ResolveResult MakeTransparentIdentifierResolveResult()
		{
			return new ResolveResult(new AnonymousType(resolver.Compilation, EmptyList<IUnresolvedProperty>.Instance));
		}
		
		sealed class QueryExpressionLambdaConversion : Conversion
		{
			internal readonly IType[] ParameterTypes;
			
			public QueryExpressionLambdaConversion(IType[] parameterTypes)
			{
				this.ParameterTypes = parameterTypes;
			}
			
			public override bool IsImplicit {
				get { return true; }
			}
			
			public override bool IsAnonymousFunctionConversion {
				get { return true; }
			}
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
					parameters[i] = new DefaultParameter(SpecialType.UnknownType, "x" + i);
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
					return new QueryExpressionLambdaConversion(parameterTypes);
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
			
			public override ResolveResult Body {
				get { return bodyExpression; }
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
			ResolveResult result = errorResult;
			ResolveResult expr = Resolve(queryFromClause.Expression);
			IVariable v;
			if (queryFromClause.Type.IsNull) {
				v = MakeVariable(GetTypeForQueryVariable(expr.Type), queryFromClause.IdentifierToken);
				result = expr;
			} else {
				v = MakeVariable(ResolveType(queryFromClause.Type), queryFromClause.IdentifierToken);
				
				// resolve the .Cast<>() call
				ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { v.Type }, true);
				result = resolver.ResolveInvocation(methodGroup, new ResolveResult[0]);
			}
			
			StoreCurrentState(queryFromClause.IdentifierToken);
			resolver = resolver.AddVariable(v);
			StoreResult(queryFromClause.IdentifierToken, new LocalResolveResult(v));
			
			if (currentQueryResult != null) {
				// this is a second 'from': resolve the .SelectMany() call
				QuerySelectClause selectClause = GetNextQueryClause(queryFromClause) as QuerySelectClause;
				ResolveResult selectResult;
				if (selectClause != null) {
					// from ... from ... select - the SelectMany call also performs the Select operation
					selectResult = Resolve(selectClause.Expression);
				} else {
					// from .. from ... ... - introduce a transparent identifier
					selectResult = MakeTransparentIdentifierResolveResult();
				}
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "SelectMany", EmptyList<IType>.Instance, true);
				ResolveResult[] arguments = {
					new QueryExpressionLambda(1, result),
					new QueryExpressionLambda(2, selectResult)
				};
				result = resolver.ResolveInvocation(methodGroup, arguments);
			}
			if (result == expr)
				return WrapResult(result);
			else
				return result;
		}
		
		/// <summary>
		/// Wraps the result in an identity conversion.
		/// This is necessary so that '$from x in variable$ select x*2' does not resolve
		/// to the LocalResolveResult for the variable, which would confuse find references.
		/// </summary>
		ResolveResult WrapResult(ResolveResult result)
		{
			return new ConversionResolveResult(result.Type, result, Conversion.IdentityConversion);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, object data)
		{
			ResolveResult rr = Resolve(queryContinuationClause.PrecedingQuery);
			IType variableType = GetTypeForQueryVariable(rr.Type);
			StoreCurrentState(queryContinuationClause.IdentifierToken);
			IVariable v = MakeVariable(variableType, queryContinuationClause.IdentifierToken);
			resolver = resolver.AddVariable(v);
			StoreResult(queryContinuationClause.IdentifierToken, new LocalResolveResult(v));
			return WrapResult(rr);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryLetClause(QueryLetClause queryLetClause, object data)
		{
			ResolveResult expr = Resolve(queryLetClause.Expression);
			StoreCurrentState(queryLetClause.IdentifierToken);
			IVariable v = MakeVariable(expr.Type, queryLetClause.IdentifierToken);
			resolver = resolver.AddVariable(v);
			StoreResult(queryLetClause.IdentifierToken, new LocalResolveResult(v));
			if (currentQueryResult != null) {
				// resolve the .Select() call
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Select", EmptyList<IType>.Instance, true);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, MakeTransparentIdentifierResolveResult()) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return errorResult;
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
				
				// resolve the .Cast<>() call
				ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { variableType }, true);
				inResult = resolver.ResolveInvocation(methodGroup, new ResolveResult[0]);
			}
			
			// resolve the 'On' expression in a context that contains only the previously existing range variables:
			// (before adding any variable)
			ResolveResult onResult = Resolve(queryJoinClause.OnExpression);
			
			// scan the 'Equals' expression in a context that contains only the variable 'v'
			CSharpResolver resolverOutsideQuery = resolver;
			resolverOutsideQuery = resolverOutsideQuery.PopBlock(); // pop all variables from the current query expression
			IVariable v = MakeVariable(variableType, queryJoinClause.JoinIdentifierToken);
			resolverOutsideQuery = resolverOutsideQuery.AddVariable(v);
			ResolveResult equalsResult = errorResult;
			ResetContext(resolverOutsideQuery, delegate {
			             	equalsResult = Resolve(queryJoinClause.EqualsExpression);
			             });
			StoreCurrentState(queryJoinClause.JoinIdentifierToken);
			StoreResult(queryJoinClause.JoinIdentifierToken, new LocalResolveResult(v));
			
			if (queryJoinClause.IsGroupJoin) {
				return ResolveGroupJoin(queryJoinClause, inResult, onResult, equalsResult);
			} else {
				resolver = resolver.AddVariable(v);
				if (currentQueryResult != null) {
					QuerySelectClause selectClause = GetNextQueryClause(queryJoinClause) as QuerySelectClause;
					ResolveResult selectResult;
					if (selectClause != null) {
						// from ... join ... select - the Join call also performs the Select operation
						selectResult = Resolve(selectClause.Expression);
					} else {
						// from .. join ... ... - introduce a transparent identifier
						selectResult = MakeTransparentIdentifierResolveResult();
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
					return errorResult;
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
					new DefaultParameter(SpecialType.UnknownType, "<>transparentIdentifier"),
					new DefaultParameter(SpecialType.UnknownType, queryJoinClause.IntoIdentifier, region: intoIdentifierRegion)
				};
				groupJoinLambda = new ImplicitlyTypedLambda(selectClause, selectLambdaParameters, this);
			} else {
				// from .. join ... ... - introduce a transparent identifier
				groupJoinLambda = new QueryExpressionLambda(2, MakeTransparentIdentifierResolveResult());
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
				if (implicitlyTypedLambda.winningHypothesis.parameterTypes.Length == 2) {
					StoreCurrentState(queryJoinClause.IntoIdentifierToken);
					groupVariable = implicitlyTypedLambda.winningHypothesis.lambdaParameters[1];
				} else {
					groupVariable = null;
				}
			} else {
				Debug.Assert(groupJoinLambda is QueryExpressionLambda);
				
				// Add the variable if the query expression continues after the group join
				// (there's no need to do this if there's only a select clause remaining, as
				// we already handled that in the ImplicitlyTypedLambda).
				
				// Get the inferred type of the group variable:
				IType[] inferredParameterTypes = null;
				if (invocationRR != null && invocationRR.Arguments.Count > 0) {
					ConversionResolveResult crr = invocationRR.Arguments[invocationRR.Arguments.Count - 1] as ConversionResolveResult;
					if (crr != null && crr.Conversion is QueryExpressionLambdaConversion) {
						inferredParameterTypes = ((QueryExpressionLambdaConversion)crr.Conversion).ParameterTypes;
					}
				}
				if (inferredParameterTypes == null)
					inferredParameterTypes = ((QueryExpressionLambda)groupJoinLambda).inferredParameterTypes;
				
				IType groupParameterType;
				if (inferredParameterTypes != null && inferredParameterTypes.Length == 2)
					groupParameterType = inferredParameterTypes[1];
				else
					groupParameterType = SpecialType.UnknownType;
				
				StoreCurrentState(queryJoinClause.IntoIdentifierToken);
				groupVariable = MakeVariable(groupParameterType, queryJoinClause.IntoIdentifierToken);
				resolver = resolver.AddVariable(groupVariable);
			}
			
			if (groupVariable != null) {
				StoreResult(queryJoinClause.IntoIdentifierToken, new LocalResolveResult(groupVariable));
			}
			
			return rr;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryWhereClause(QueryWhereClause queryWhereClause, object data)
		{
			ResolveResult condition = Resolve(queryWhereClause.Condition);
			IType boolType = resolver.Compilation.FindType(KnownTypeCode.Boolean);
			Conversion conversionToBool = resolver.conversions.ImplicitConversion(condition, boolType);
			ProcessConversion(queryWhereClause.Condition, condition, conversionToBool, boolType);
			if (currentQueryResult != null) {
				if (conversionToBool != Conversion.IdentityConversion && conversionToBool != Conversion.None) {
					condition = new ConversionResolveResult(boolType, condition, conversionToBool);
				}
				
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Where", EmptyList<IType>.Instance);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, condition) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return errorResult;
			}
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQuerySelectClause(QuerySelectClause querySelectClause, object data)
		{
			if (currentQueryResult == null) {
				ScanChildren(querySelectClause);
				return errorResult;
			}
			QueryClause previousQueryClause = GetPreviousQueryClause(querySelectClause);
			// If the 'select' follows on a 'SelectMany', 'Join' or 'GroupJoin' clause, then the 'select' portion
			// was already done as part of the previous clause.
			if (((previousQueryClause is QueryFromClause && GetPreviousQueryClause(previousQueryClause) != null))
			    || previousQueryClause is QueryJoinClause)
			{
				// GroupJoin already scans the following select clause in a different context,
				// so we must not scan it again.
				if (!(previousQueryClause is QueryJoinClause && ((QueryJoinClause)previousQueryClause).IsGroupJoin))
					Scan(querySelectClause.Expression);
				return WrapResult(currentQueryResult);
			}
			
			QueryExpression query = querySelectClause.Parent as QueryExpression;
			string rangeVariable = GetSingleRangeVariable(query);
			if (rangeVariable != null) {
				IdentifierExpression ident = ParenthesizedExpression.UnpackParenthesizedExpression(querySelectClause.Expression) as IdentifierExpression;
				if (ident != null && ident.Identifier == rangeVariable && !ident.TypeArguments.Any()) {
					// selecting the single identifier that is the range variable
					if (query.Clauses.Count > 2) {
						// only if the query is not degenerate:
						// the Select call will be optimized away, so directly return the previous result
						Scan(querySelectClause.Expression);
						return WrapResult(currentQueryResult);
					}
				}
			}
			
			ResolveResult expr = Resolve(querySelectClause.Expression);
			var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Select", EmptyList<IType>.Instance);
			ResolveResult[] arguments = { new QueryExpressionLambda(1, expr) };
			return resolver.ResolveInvocation(methodGroup, arguments);
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
			if (currentQueryResult == null) {
				ScanChildren(queryGroupClause);
				return errorResult;
			}
			
			// ... group projection by key
			ResolveResult projection = Resolve(queryGroupClause.Projection);
			ResolveResult key = Resolve(queryGroupClause.Key);
			
			var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "GroupBy", EmptyList<IType>.Instance);
			ResolveResult[] arguments = {
				new QueryExpressionLambda(1, key),
				new QueryExpressionLambda(1, projection)
			};
			return resolver.ResolveInvocation(methodGroup, arguments);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryOrderClause(QueryOrderClause queryOrderClause, object data)
		{
			foreach (QueryOrdering ordering in queryOrderClause.Orderings) {
				currentQueryResult = Resolve(ordering);
			}
			return WrapResult(currentQueryResult);
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitQueryOrdering(QueryOrdering queryOrdering, object data)
		{
			if (currentQueryResult == null) {
				ScanChildren(queryOrdering);
				return errorResult;
			}
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
			ResolveResult rr = resolver.ResolveObjectCreation(target.Type, arguments, argumentNames, allowProtectedAccess: true);
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
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitComment (Comment comment, object data)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<object, ResolveResult>.VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective, object data)
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
