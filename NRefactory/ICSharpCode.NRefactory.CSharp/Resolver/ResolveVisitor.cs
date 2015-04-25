// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
	sealed class ResolveVisitor : IAstVisitor<ResolveResult>
	{
		// The ResolveVisitor is also responsible for handling lambda expressions.
		
		static readonly ResolveResult errorResult = ErrorResolveResult.UnknownError;
		
		CSharpResolver resolver;
		/// <summary>Resolve result of the current LINQ query.</summary>
		/// <remarks>We do not have to put this into the stored state (resolver) because
		/// query expressions are always resolved in a single operation.</remarks>
		ResolveResult currentQueryResult;
		readonly CSharpUnresolvedFile unresolvedFile;
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
		public ResolveVisitor(CSharpResolver resolver, CSharpUnresolvedFile unresolvedFile)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.unresolvedFile = unresolvedFile;
			this.navigator = skipAllNavigator;
		}
		
		internal void SetNavigator(IResolveVisitorNavigator navigator)
		{
			this.navigator = navigator ?? skipAllNavigator;
		}
		
		ResolveResult voidResult {
			get {
				return new ResolveResult(resolver.Compilation.FindType(KnownTypeCode.Void));
			}
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
					ResolveResult result = node.AcceptVisitor(this);
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
		/// Otherwise, use <c>resolver.Scan(syntaxTree); var result = resolver.GetResolveResult(node);</c>
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
				result = node.AcceptVisitor(this) ?? errorResult;
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
				Debug.Assert(oldResolver.LocalVariables.SequenceEqual(resolver.LocalVariables));
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
			// Don't use ConversionResolveResult as a result, because it can get
			// confused with an implicit conversion.
			Debug.Assert(!(result is ConversionResolveResult) || result is CastResolveResult);
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
			if (targetType.Kind == TypeKind.Unknown) {
				// no need to resolve the expression right now
				Scan(expr);
			} else {
				ProcessConversion(expr, Resolve(expr), targetType);
			}
		}
		
		void ProcessConversionResult(Expression expr, ConversionResolveResult rr)
		{
			if (rr != null && !(rr is CastResolveResult))
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
		
		void MarkUnknownNamedArguments(IEnumerable<Expression> arguments)
		{
			foreach (var nae in arguments.OfType<NamedArgumentExpression>()) {
				StoreCurrentState(nae);
				StoreResult(nae, new NamedArgumentResolveResult(nae.Name, resolveResultCache[nae.Expression]));
			}
		}
		
		void ProcessInvocationResult(Expression target, IEnumerable<Expression> arguments, ResolveResult invocation)
		{
			if (invocation is CSharpInvocationResolveResult || invocation is DynamicInvocationResolveResult) {
				int i = 0;
				IList<ResolveResult> argumentsRR;
				if (invocation is CSharpInvocationResolveResult) {
					var csi = (CSharpInvocationResolveResult)invocation;
					if (csi.IsExtensionMethodInvocation) {
						Debug.Assert(arguments.Count() + 1 == csi.Arguments.Count);
						ProcessConversionResult(target, csi.Arguments[0] as ConversionResolveResult);
						i = 1;
					} else {
						Debug.Assert(arguments.Count() == csi.Arguments.Count);
					}
					argumentsRR = csi.Arguments;
				}
				else {
					argumentsRR = ((DynamicInvocationResolveResult)invocation).Arguments;
				}

				foreach (Expression arg in arguments) {
					ResolveResult argRR = argumentsRR[i++];
					NamedArgumentExpression nae = arg as NamedArgumentExpression;
					NamedArgumentResolveResult nrr = argRR as NamedArgumentResolveResult;
					Debug.Assert((nae == null) == (nrr == null));
					if (nae != null && nrr != null) {
						StoreCurrentState(nae);
						StoreResult(nae, nrr);
						ProcessConversionResult(nae.Expression, nrr.Argument as ConversionResolveResult);
					} else {
						ProcessConversionResult(arg, argRR as ConversionResolveResult);
					}
				}
			}
			else {
				MarkUnknownNamedArguments(arguments);
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
			
			AstNode parent;
			CSharpResolver storedResolver = GetPreviouslyScannedContext(node, out parent);
			ResetContext(
				storedResolver,
				delegate {
					navigator = new NodeListResolveVisitorNavigator(node);
					Debug.Assert(!resolverEnabled);
					Scan(parent);
					navigator = skipAllNavigator;
				});
			
			MergeUndecidedLambdas();
			return resolveResultCache[node];
		}
		
		CSharpResolver GetPreviouslyScannedContext(AstNode node, out AstNode parent)
		{
			parent = node;
			CSharpResolver storedResolver;
			while (!resolverBeforeDict.TryGetValue(parent, out storedResolver)) {
				AstNode tmp = parent.Parent;
				if (tmp == null)
					throw new InvalidOperationException("Could not find a resolver state for any parent of the specified node. Are you trying to resolve a node that is not a descendant of the CSharpAstResolver's root node?");
				if (tmp.NodeType == NodeType.Whitespace)
					return resolver; // special case: resolve expression within preprocessor directive
				parent = tmp;
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
				ResolveResult rr = GetResolveResult(expr);
				return new ConversionWithTargetType(Conversion.IdentityConversion, rr.Type);
			}
		}
		#endregion
		
		#region Track UsingScope
		ResolveResult IAstVisitor<ResolveResult>.VisitSyntaxTree(SyntaxTree unit)
		{
			CSharpResolver previousResolver = resolver;
			try {
				if (unresolvedFile != null) {
					resolver = resolver.WithCurrentUsingScope(unresolvedFile.RootUsingScope.Resolve(resolver.Compilation));
				} else {
					var cv = new TypeSystemConvertVisitor(unit.FileName ?? string.Empty);
					ApplyVisitorToUsings(cv, unit.Children);
					PushUsingScope(cv.UnresolvedFile.RootUsingScope);
				}
				ScanChildren(unit);
				return voidResult;
			} finally {
				resolver = previousResolver;
			}
		}
		
		void ApplyVisitorToUsings(TypeSystemConvertVisitor visitor, IEnumerable<AstNode> children)
		{
			foreach (var child in children) {
				if (child is ExternAliasDeclaration || child is UsingDeclaration || child is UsingAliasDeclaration) {
					child.AcceptVisitor(visitor);
				}
			}
		}
		
		void PushUsingScope(UsingScope usingScope)
		{
			usingScope.Freeze();
			resolver = resolver.WithCurrentUsingScope(new ResolvedUsingScope(resolver.CurrentTypeResolveContext, usingScope));
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			CSharpResolver previousResolver = resolver;
			try {
				var nsName = namespaceDeclaration.NamespaceName;
				AstNode child = namespaceDeclaration.FirstChild;

				for (; child != null && child.Role != Roles.LBrace; child = child.NextSibling) {
					Scan(child);
				}

				if (unresolvedFile != null) {
					resolver = resolver.WithCurrentUsingScope(unresolvedFile.GetUsingScope(namespaceDeclaration.StartLocation).Resolve(resolver.Compilation));

				} else {
//					string fileName = namespaceDeclaration.GetRegion().FileName ?? string.Empty;
					// Fetch parent using scope
					// Create root using scope if necessary
					if (resolver.CurrentUsingScope == null)
						PushUsingScope(new UsingScope());
					
					// Create child using scope
					DomRegion region = namespaceDeclaration.GetRegion();
					var identifiers = namespaceDeclaration.Identifiers.ToList();
					// For all but the last identifier:
					UsingScope usingScope;
					for (int i = 0; i < identifiers.Count - 1; i++) {
						usingScope = new UsingScope(resolver.CurrentUsingScope.UnresolvedUsingScope, identifiers[i]);
						usingScope.Region = region;
						PushUsingScope(usingScope);
					}
					// Last using scope:
					usingScope = new UsingScope(resolver.CurrentUsingScope.UnresolvedUsingScope, identifiers.Last());
					usingScope.Region = region;
					var cv = new TypeSystemConvertVisitor(new CSharpUnresolvedFile(), usingScope);
					ApplyVisitorToUsings(cv, namespaceDeclaration.Children);
					PushUsingScope(usingScope);
				}
				for (; child != null; child = child.NextSibling) {
					Scan(child);
				}

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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			return VisitTypeOrDelegate(typeDeclaration, typeDeclaration.Name, typeDeclaration.TypeParameters.Count);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			return VisitTypeOrDelegate(delegateDeclaration, delegateDeclaration.Name, delegateDeclaration.TypeParameters.Count);
		}
		#endregion
		
		#region Track CurrentMember
		ResolveResult IAstVisitor<ResolveResult>.VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			return VisitFieldOrEventDeclaration(fieldDeclaration, SymbolKind.Field);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			return VisitFieldOrEventDeclaration(fixedFieldDeclaration, SymbolKind.Field);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			return VisitFieldOrEventDeclaration(eventDeclaration, SymbolKind.Event);
		}
		
		ResolveResult VisitFieldOrEventDeclaration(EntityDeclaration fieldOrEventDeclaration, SymbolKind symbolKind)
		{
			//int initializerCount = fieldOrEventDeclaration.GetChildrenByRole(Roles.Variable).Count;
			CSharpResolver oldResolver = resolver;
			for (AstNode node = fieldOrEventDeclaration.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == Roles.Variable || node.Role == FixedFieldDeclaration.VariableRole) {
					IMember member;
					if (unresolvedFile != null) {
						member = GetMemberFromLocation(node);
					} else {
						string name = ((VariableInitializer)node).Name;
						member = AbstractUnresolvedMember.Resolve(resolver.CurrentTypeResolveContext, symbolKind, name);
					}
					resolver = resolver.WithCurrentMember(member);
					
					Scan(node);
					
					resolver = oldResolver;
				} else {
					Scan(node);
				}
			}
			return voidResult;
		}
		
		IMember GetMemberFromLocation(AstNode node)
		{
			ITypeDefinition typeDef = resolver.CurrentTypeDefinition;
			if (typeDef == null)
				return null;
			TextLocation location = TypeSystemConvertVisitor.GetStartLocationAfterAttributes(node);
			return typeDef.GetMembers(
				delegate (IUnresolvedMember m) {
					if (m.UnresolvedFile != unresolvedFile)
						return false;
					DomRegion region = m.Region;
					return !region.IsEmpty && region.Begin <= location && region.End > location;
				},
				GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions
			).FirstOrDefault();
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitVariableInitializer(VariableInitializer variableInitializer)
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
						result = new MemberResolveResult(null, resolver.CurrentMember, false);
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
					int[] sizes = new int[arrayType.Dimensions];
					UnpackArrayInitializer(initializerElements, sizes, aie, 0, true);
					ResolveResult[] initializerElementResults = new ResolveResult[initializerElements.Count];
					for (int i = 0; i < initializerElementResults.Length; i++) {
						initializerElementResults[i] = Resolve(initializerElements[i]);
					}
					var arrayCreation = resolver.ResolveArrayCreation(arrayType.ElementType, sizes, initializerElementResults);
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (resolver.CurrentMember != null) {
					result = new MemberResolveResult(null, resolver.CurrentMember, false);
				}
				ResolveAndProcessConversion(fixedVariableInitializer.CountExpression, resolver.Compilation.FindType(KnownTypeCode.Int32));
				return result;
			} else {
				ScanChildren(fixedVariableInitializer);
				return null;
			}
		}
		
		ResolveResult VisitMethodMember(EntityDeclaration memberDeclaration)
		{
			CSharpResolver oldResolver = resolver;
			try {
				IMember member = null;
				if (unresolvedFile != null) {
					member = GetMemberFromLocation(memberDeclaration);
				}
				if (member == null) {
					// Re-discover the method:
					SymbolKind symbolKind = memberDeclaration.SymbolKind;
					var parameterTypes = TypeSystemConvertVisitor.GetParameterTypes(memberDeclaration.GetChildrenByRole(Roles.Parameter), InterningProvider.Dummy);
					if (symbolKind == SymbolKind.Constructor) {
						string name = memberDeclaration.HasModifier(Modifiers.Static) ? ".cctor" : ".ctor";
						member = AbstractUnresolvedMember.Resolve(
							resolver.CurrentTypeResolveContext, symbolKind, name,
							parameterTypeReferences: parameterTypes);
					} else if (symbolKind == SymbolKind.Destructor) {
						member = AbstractUnresolvedMember.Resolve(resolver.CurrentTypeResolveContext, symbolKind, "Finalize");
					} else {
						string[] typeParameterNames = memberDeclaration.GetChildrenByRole(Roles.TypeParameter).Select(tp => tp.Name).ToArray();
						AstType explicitInterfaceAstType = memberDeclaration.GetChildByRole(EntityDeclaration.PrivateImplementationTypeRole);
						ITypeReference explicitInterfaceType = null;
						if (!explicitInterfaceAstType.IsNull) {
							explicitInterfaceType = explicitInterfaceAstType.ToTypeReference();
						}
						member = AbstractUnresolvedMember.Resolve(
							resolver.CurrentTypeResolveContext, symbolKind, memberDeclaration.Name,
							explicitInterfaceType, typeParameterNames, parameterTypes);
					}
				}
				resolver = resolver.WithCurrentMember(member);
				ScanChildren(memberDeclaration);
				
				if (member != null)
					return new MemberResolveResult(null, member, false);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			return VisitMethodMember(methodDeclaration);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			return VisitMethodMember(operatorDeclaration);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			return VisitMethodMember(constructorDeclaration);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			return VisitMethodMember(destructorDeclaration);
		}
		
		// handle properties/indexers
		ResolveResult VisitPropertyMember(EntityDeclaration propertyOrIndexerDeclaration)
		{
			CSharpResolver oldResolver = resolver;
			try {
				IMember member;
				if (unresolvedFile != null) {
					member = GetMemberFromLocation(propertyOrIndexerDeclaration);
				} else {
					// Re-discover the property:
					string name = propertyOrIndexerDeclaration.Name;
					var parameterTypeReferences = TypeSystemConvertVisitor.GetParameterTypes(propertyOrIndexerDeclaration.GetChildrenByRole(Roles.Parameter), InterningProvider.Dummy);
					AstType explicitInterfaceAstType = propertyOrIndexerDeclaration.GetChildByRole(EntityDeclaration.PrivateImplementationTypeRole);
					ITypeReference explicitInterfaceType = null;
					if (!explicitInterfaceAstType.IsNull) {
						explicitInterfaceType = explicitInterfaceAstType.ToTypeReference();
					}
					member = AbstractUnresolvedMember.Resolve(
						resolver.CurrentTypeResolveContext, propertyOrIndexerDeclaration.SymbolKind, name,
						explicitInterfaceType, parameterTypeReferences: parameterTypeReferences);
				}
				// We need to use the property as current member so that indexer parameters can be resolved correctly.
				resolver = resolver.WithCurrentMember(member);
				var resolverWithPropertyAsMember = resolver;
				
				for (AstNode node = propertyOrIndexerDeclaration.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == PropertyDeclaration.GetterRole && member is IProperty) {
						resolver = resolver.WithCurrentMember(((IProperty)member).Getter);
						Scan(node);
						resolver = resolverWithPropertyAsMember;
					} else if (node.Role == PropertyDeclaration.SetterRole && member is IProperty) {
						resolver = resolver.WithCurrentMember(((IProperty)member).Setter);
						Scan(node);
						resolver = resolverWithPropertyAsMember;
					} else {
						Scan(node);
					}
				}
				if (member != null)
					return new MemberResolveResult(null, member, false);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			return VisitPropertyMember(propertyDeclaration);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			return VisitPropertyMember(indexerDeclaration);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			CSharpResolver oldResolver = resolver;
			try {
				IMember member;
				if (unresolvedFile != null) {
					member = GetMemberFromLocation(eventDeclaration);
				} else {
					string name = eventDeclaration.Name;
					AstType explicitInterfaceAstType = eventDeclaration.PrivateImplementationType;
					if (explicitInterfaceAstType.IsNull) {
						member = AbstractUnresolvedMember.Resolve(resolver.CurrentTypeResolveContext, SymbolKind.Event, name);
					} else {
						member = AbstractUnresolvedMember.Resolve(resolver.CurrentTypeResolveContext, SymbolKind.Event, name,
						                                          explicitInterfaceAstType.ToTypeReference());
					}
				}
				resolver = resolver.WithCurrentMember(member);
				var resolverWithEventAsMember = resolver;
				
				for (AstNode node = eventDeclaration.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == CustomEventDeclaration.AddAccessorRole && member is IEvent) {
						resolver = resolver.WithCurrentMember(((IEvent)member).AddAccessor);
						Scan(node);
						resolver = resolverWithEventAsMember;
					} else if (node.Role == CustomEventDeclaration.RemoveAccessorRole && member is IEvent) {
						resolver = resolver.WithCurrentMember(((IEvent)member).RemoveAccessor);
						Scan(node);
						resolver = resolverWithEventAsMember;
					} else {
						Scan(node);
					}
				}

				if (member != null)
					return new MemberResolveResult(null, member, false);
				else
					return errorResult;
			} finally {
				resolver = oldResolver;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			ScanChildren(parameterDeclaration);
			if (resolverEnabled) {
				string name = parameterDeclaration.Name;
				
				if (parameterDeclaration.Parent is DocumentationReference) {
					// create a dummy parameter
					IType type = ResolveType(parameterDeclaration.Type);
					switch (parameterDeclaration.ParameterModifier) {
						case ParameterModifier.Ref:
						case ParameterModifier.Out:
							type = new ByReferenceType(type);
							break;
					}
					return new LocalResolveResult(new DefaultParameter(
						type, name,
						isRef: parameterDeclaration.ParameterModifier == ParameterModifier.Ref,
						isOut: parameterDeclaration.ParameterModifier == ParameterModifier.Out,
						isParams: parameterDeclaration.ParameterModifier == ParameterModifier.Params));
				}
				
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			CSharpResolver oldResolver = resolver;
			try {
				// Scan enum member attributes before setting resolver.CurrentMember, so that
				// enum values used as attribute arguments have the correct type.
				// (within an enum member, all other enum members are treated as having their underlying type)
				foreach (var attributeSection in enumMemberDeclaration.Attributes)
					Scan(attributeSection);
				
				IMember member = null;
				if (unresolvedFile != null) {
					member = GetMemberFromLocation(enumMemberDeclaration);
				} else if (resolver.CurrentTypeDefinition != null) {
					string name = enumMemberDeclaration.Name;
					member = resolver.CurrentTypeDefinition.GetFields(f => f.Name == name, GetMemberOptions.IgnoreInheritedMembers).FirstOrDefault();
				}
				resolver = resolver.WithCurrentMember(member);
				
				if (resolverEnabled && resolver.CurrentTypeDefinition != null) {
					ResolveAndProcessConversion(enumMemberDeclaration.Initializer, resolver.CurrentTypeDefinition.EnumUnderlyingType);
					if (resolverEnabled && member != null)
						return new MemberResolveResult(null, member, false);
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
		ResolveResult IAstVisitor<ResolveResult>.VisitCheckedExpression(CheckedExpression checkedExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitCheckedStatement(CheckedStatement checkedStatement)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
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
				return namedArgExpr.Name;
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
		
		class AnonymousTypeMember
		{
			public readonly Expression Expression;
			public readonly ResolveResult Initializer;
			
			public AnonymousTypeMember(Expression expression, ResolveResult initializer)
			{
				this.Expression = expression;
				this.Initializer = initializer;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			// 7.6.10.6 Anonymous object creation expressions
			List<IUnresolvedProperty> unresolvedProperties = new List<IUnresolvedProperty>();
			List<AnonymousTypeMember> members = new List<AnonymousTypeMember>();
			foreach (var expr in anonymousTypeCreateExpression.Initializers) {
				Expression resolveExpr;
				var name = GetAnonymousTypePropertyName(expr, out resolveExpr);
				if (resolveExpr != null) {
					var initRR = Resolve(resolveExpr);
					var returnTypeRef = initRR.Type.ToTypeReference();
					var property = new DefaultUnresolvedProperty();
					property.Name = name;
					property.Accessibility = Accessibility.Public;
					property.ReturnType = returnTypeRef;
					property.Getter = new DefaultUnresolvedMethod {
						Name = "get_" + name,
						Accessibility = Accessibility.Public,
						ReturnType = returnTypeRef,
						SymbolKind = SymbolKind.Accessor,
						AccessorOwner = property
					};
					unresolvedProperties.Add(property);
					members.Add(new AnonymousTypeMember(expr, initRR));
				} else {
					Scan(expr);
				}
			}
			var anonymousType = new AnonymousType(resolver.Compilation, unresolvedProperties);
			var properties = anonymousType.GetProperties().ToList();
			Debug.Assert(properties.Count == members.Count);
			List<ResolveResult> assignments = new List<ResolveResult>();
			for (int i = 0; i < members.Count; i++) {
				ResolveResult lhs = new MemberResolveResult(new InitializedObjectResolveResult(anonymousType), properties[i]);
				ResolveResult rhs = members[i].Initializer;
				ResolveResult assignment = resolver.ResolveAssignment(AssignmentOperatorType.Assign, lhs, rhs);
				var ne = members[i].Expression as NamedExpression;
				if (ne != null) {
					StoreCurrentState(ne);
					// ne.Expression was already resolved by the first loop
					StoreResult(ne, lhs);
				}
				assignments.Add(assignment);
			}
			var anonymousCtor = DefaultResolvedMethod.GetDummyConstructor(resolver.Compilation, anonymousType);
			return new InvocationResolveResult(null, anonymousCtor, initializerStatements: assignments);
		}
		#endregion
		
		#region Visit Expressions
		ResolveResult IAstVisitor<ResolveResult>.VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
		{
			int dimensions = arrayCreateExpression.Arguments.Count;
			IEnumerable<Expression> sizeArgumentExpressions;
			ResolveResult[] sizeArguments;
			IEnumerable<ArraySpecifier> additionalArraySpecifiers;
			if (dimensions == 0) {
				var firstSpecifier = arrayCreateExpression.AdditionalArraySpecifiers.FirstOrDefault();
				if (firstSpecifier != null) {
					dimensions = firstSpecifier.Dimensions;
					additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers.Skip(1);
				} else {
					// No array specifiers (neither with nor without size) - can happen if there are syntax errors.
					// Dimensions must be at least one; otherwise 'new ArrayType' will crash.
					dimensions = 1;
					additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers;
				}
				sizeArguments = null;
				sizeArgumentExpressions = null;
			} else {
				sizeArgumentExpressions = arrayCreateExpression.Arguments;
				sizeArguments = new ResolveResult[dimensions];
				int pos = 0;
				foreach (var node in sizeArgumentExpressions)
					sizeArguments[pos++] = Resolve(node);
				additionalArraySpecifiers = arrayCreateExpression.AdditionalArraySpecifiers;
			}
			
			int[] sizes;
			List<Expression> initializerElements;
			ResolveResult[] initializerElementResults;
			if (arrayCreateExpression.Initializer.IsNull) {
				sizes = null;
				initializerElements = null;
				initializerElementResults = null;
			} else {
				StoreCurrentState(arrayCreateExpression.Initializer);
				
				initializerElements = new List<Expression>();
				sizes = new int[dimensions];
				UnpackArrayInitializer(initializerElements, sizes, arrayCreateExpression.Initializer, 0, true);
				initializerElementResults = new ResolveResult[initializerElements.Count];
				for (int i = 0; i < initializerElementResults.Length; i++) {
					initializerElementResults[i] = Resolve(initializerElements[i]);
				}
				StoreResult(arrayCreateExpression.Initializer, voidResult);
			}
			
			IType elementType;
			if (arrayCreateExpression.Type.IsNull) {
				elementType = null;
			} else {
				elementType = ResolveType(arrayCreateExpression.Type);
				foreach (var spec in additionalArraySpecifiers.Reverse()) {
					elementType = new ArrayType(resolver.Compilation, elementType, spec.Dimensions);
				}
			}
			ArrayCreateResolveResult acrr;
			if (sizeArguments != null) {
				acrr = resolver.ResolveArrayCreation(elementType, sizeArguments, initializerElementResults);
			} else if (sizes != null) {
				acrr = resolver.ResolveArrayCreation(elementType, sizes, initializerElementResults);
			} else {
				// neither size arguments nor an initializer exist -> error
				return new ErrorResolveResult(new ArrayType(resolver.Compilation, elementType ?? SpecialType.UnknownType, dimensions));
			}
			if (sizeArgumentExpressions != null)
				ProcessConversionResults(sizeArgumentExpressions, acrr.SizeArguments);
			if (acrr.InitializerElements != null)
				ProcessConversionResults(initializerElements, acrr.InitializerElements);
			return acrr;
		}
		
		void UnpackArrayInitializer(List<Expression> elementList, int[] sizes, ArrayInitializerExpression initializer, int dimension, bool resolveNestedInitializersToVoid)
		{
			Debug.Assert(dimension < sizes.Length);
			int elementCount = 0;
			if (dimension + 1 < sizes.Length) {
				foreach (var node in initializer.Elements) {
					ArrayInitializerExpression aie = node as ArrayInitializerExpression;
					if (aie != null) {
						if (resolveNestedInitializersToVoid) {
							StoreCurrentState(aie);
							StoreResult(aie, voidResult);
						}
						UnpackArrayInitializer(elementList, sizes, aie, dimension + 1, resolveNestedInitializersToVoid);
					} else {
						elementList.Add(node);
					}
					elementCount++;
				}
			} else {
				foreach (var expr in initializer.Elements) {
					elementList.Add(expr);
					elementCount++;
				}
			}
			if (sizes[dimension] == 0) // 0 = uninitialized
				sizes[dimension] = elementCount;
			else if (sizes[dimension] != elementCount)
				sizes[dimension] = -1; // -1 = error
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			// Array initializers are handled by their parent expression.
			ScanChildren(arrayInitializerExpression);
			return errorResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitAsExpression(AsExpression asExpression)
		{
			if (resolverEnabled) {
				ResolveResult input = Resolve(asExpression.Expression);
				var targetType = ResolveType(asExpression.Type);
				return new CastResolveResult(targetType, input, Conversion.TryCast, resolver.CheckForOverflow);
			} else {
				ScanChildren(asExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitAssignmentExpression(AssignmentExpression assignmentExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			if (resolverEnabled) {
				return resolver.ResolveBaseReference();
			} else {
				ScanChildren(baseReferenceExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitCastExpression(CastExpression castExpression)
		{
			if (resolverEnabled) {
				var targetType = ResolveType(castExpression.Type);
				var expr = castExpression.Expression;
				var rr = resolver.ResolveCast(targetType, Resolve(expr));
				var crr = rr as ConversionResolveResult;
				if (crr != null) {
					Debug.Assert(!(crr is CastResolveResult));
					ProcessConversion(expr, crr.Input, crr.Conversion, targetType);
					rr = new CastResolveResult(crr);
				}
				return rr;
			} else {
				ScanChildren(castExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitConditionalExpression(ConditionalExpression conditionalExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
		{
			if (resolverEnabled) {
				return resolver.ResolveDefaultValue(ResolveType(defaultValueExpression.Type));
			} else {
				ScanChildren(defaultValueExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitDirectionExpression(DirectionExpression directionExpression)
		{
			if (resolverEnabled) {
				ResolveResult rr = Resolve(directionExpression.Expression);
				return new ByReferenceResolveResult(rr, directionExpression.FieldDirection == FieldDirection.Out);
			} else {
				ScanChildren(directionExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			if (resolverEnabled || NeedsResolvingDueToNamedArguments(indexerExpression)) {
				Expression target = indexerExpression.Target;
				ResolveResult targetResult = Resolve(target);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(indexerExpression.Arguments, out argumentNames);
				ResolveResult rr = resolver.ResolveIndexer(targetResult, arguments, argumentNames);
				ArrayAccessResolveResult aarr = rr as ArrayAccessResolveResult;
				if (aarr != null) {
					MarkUnknownNamedArguments(indexerExpression.Arguments);
					ProcessConversionResults(indexerExpression.Arguments, aarr.Indexes);
				} else {
					ProcessInvocationResult(target, indexerExpression.Arguments, rr);
				}
				return rr;
			} else {
				ScanChildren(indexerExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitIsExpression(IsExpression isExpression)
		{
			if (resolverEnabled) {
				ResolveResult input = Resolve(isExpression.Expression);
				IType targetType = ResolveType(isExpression.Type);
				IType booleanType = resolver.Compilation.FindType(KnownTypeCode.Boolean);
				return new TypeIsResolveResult(input, targetType, booleanType);
			} else {
				ScanChildren(isExpression);
				return null;
			}
		}
		
		// NamedArgumentExpression is "identifier: Expression"
		ResolveResult IAstVisitor<ResolveResult>.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			// The parent expression takes care of handling NamedArgumentExpressions
			// by calling GetArguments().
			// This method gets called only when scanning, or when the named argument is used
			// in an invalid context.
			if (resolverEnabled) {
				return new NamedArgumentResolveResult(namedArgumentExpression.Name, Resolve(namedArgumentExpression.Expression));
			} else {
				Scan(namedArgumentExpression.Expression);
				return null;
			}
		}
		
		// NamedExpression is "identifier = Expression" in object initializers and attributes
		ResolveResult IAstVisitor<ResolveResult>.VisitNamedExpression(NamedExpression namedExpression)
		{
			// The parent expression takes care of handling NamedExpression
			// by calling HandleObjectInitializer() or HandleNamedExpression().
			// This method gets called only when scanning, or when the named expression is used
			// in an invalid context.
			ScanChildren(namedExpression);
			return null;
		}
		
		void HandleNamedExpression(NamedExpression namedExpression, List<ResolveResult> initializerStatements)
		{
			StoreCurrentState(namedExpression);
			Expression rhs = namedExpression.Expression;
			ResolveResult lhsRR = resolver.ResolveIdentifierInObjectInitializer(namedExpression.Name);
			if (rhs is ArrayInitializerExpression) {
				HandleObjectInitializer(lhsRR, (ArrayInitializerExpression)rhs, initializerStatements);
			} else {
				var rhsRR = Resolve(rhs);
				var rr = resolver.ResolveAssignment(AssignmentOperatorType.Assign, lhsRR, rhsRR) as OperatorResolveResult;
				if (rr != null) {
					ProcessConversionResult(rhs, rr.Operands[1] as ConversionResolveResult);
					initializerStatements.Add(rr);
				}
			}
			StoreResult(namedExpression, lhsRR);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
		{
			return resolver.ResolvePrimitive(null);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			var typeResolveResult = Resolve(objectCreateExpression.Type);
			if (typeResolveResult.IsError) {
				ScanChildren (objectCreateExpression);
				return typeResolveResult;
			}
			IType type = typeResolveResult.Type;
			
			List<ResolveResult> initializerStatements = null;
			var initializer = objectCreateExpression.Initializer;
			if (!initializer.IsNull) {
				initializerStatements = new List<ResolveResult>();
				HandleObjectInitializer(new InitializedObjectResolveResult(type), initializer, initializerStatements);
			}
			
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(objectCreateExpression.Arguments, out argumentNames);
			
			ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames, false, initializerStatements);
			if (arguments.Length == 1 && rr.Type.Kind == TypeKind.Delegate) {
				MarkUnknownNamedArguments(objectCreateExpression.Arguments);
				// Apply conversion to argument if it directly wraps the argument
				// (but not when creating a delegate from a delegate, as then there would be a MGRR for .Invoke in between)
				// This is necessary for lambda type inference.
				var crr = rr as ConversionResolveResult;
				if (crr != null) {
					if (objectCreateExpression.Arguments.Count == 1)
						ProcessConversionResult(objectCreateExpression.Arguments.Single(), crr);
					
					// wrap the result so that the delegate creation is not handled as a reference
					// to the target method - otherwise FindReferencedEntities would produce two results for
					// the same delegate creation.
					return new CastResolveResult(crr);
				} else {
					return rr;
				}
			} else {
				// process conversions in all other cases
				ProcessInvocationResult(null, objectCreateExpression.Arguments, rr);
				return rr;
			}
		}
		
		void HandleObjectInitializer(ResolveResult initializedObject, ArrayInitializerExpression initializer, List<ResolveResult> initializerStatements)
		{
			StoreCurrentState(initializer);
			resolver = resolver.PushObjectInitializer(initializedObject);
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
					var addRR = memberLookup.Lookup(initializedObject, "Add", EmptyList<IType>.Instance, true);
					var mgrr = addRR as MethodGroupResolveResult;
					if (mgrr != null) {
						OverloadResolution or = mgrr.PerformOverloadResolution(resolver.Compilation, addArguments, null, false, false, false, resolver.CheckForOverflow, resolver.conversions);
						var invocationRR = or.CreateResolveResult(initializedObject);
						StoreResult(aie, invocationRR);
						ProcessInvocationResult(null, aie.Elements, invocationRR);
						initializerStatements.Add(invocationRR);
					} else {
						StoreResult(aie, addRR);
					}
				} else if (element is NamedExpression) {
					HandleNamedExpression((NamedExpression)element, initializerStatements);
				} else {
					// unknown kind of expression
					Scan(element);
				}
			}
			resolver = resolver.PopObjectInitializer();
			StoreResult(initializer, voidResult);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			if (resolverEnabled) {
				return Resolve(parenthesizedExpression.Expression);
			} else {
				Scan(parenthesizedExpression.Expression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
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
				                                    GetNameLookupMode(pointerReferenceExpression));
			} else {
				ScanChildren(pointerReferenceExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			return resolver.ResolvePrimitive(primitiveExpression.Value);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			return resolver.ResolveSizeOf(ResolveType(sizeOfExpression.Type));
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			ResolveAndProcessConversion(stackAllocExpression.CountExpression, resolver.Compilation.FindType(KnownTypeCode.Int32));
			return new ResolveResult(new PointerType(ResolveType(stackAllocExpression.Type)));
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			return resolver.ResolveThisReference();
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			if (resolverEnabled) {
				return resolver.ResolveTypeOf(ResolveType(typeOfExpression.Type));
			} else {
				Scan(typeOfExpression.Type);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			if (resolverEnabled) {
				return Resolve(typeReferenceExpression.Type).ShallowClone();
			} else {
				Scan(typeReferenceExpression.Type);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
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
		
		/// <summary>
		/// Gets and resolves the arguments; unpacking any NamedArgumentExpressions.
		/// </summary>
		/// <remarks>
		/// Callers of GetArguments must also call either ProcessConversionsInInvocation or MarkUnknownNamedArguments
		/// to ensure the named arguments get resolved.
		/// Also, as named arguments get resolved by the parent node, the parent node must not scan
		/// into the argument list without being resolved - see NeedsResolvingDueToNamedArguments().
		/// </remarks>
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
					argumentNames[i] = nae.Name;
					argumentValue = nae.Expression;
				} else {
					argumentValue = argument;
				}
				arguments[i++] = Resolve(argumentValue);
			}
			return arguments;
		}
		
		bool NeedsResolvingDueToNamedArguments(Expression nodeWithArguments)
		{
			for (AstNode child = nodeWithArguments.FirstChild; child != null; child = child.NextSibling) {
				if (child is NamedArgumentExpression)
					return true;
			}
			return false;
		}

		static NameLookupMode GetNameLookupMode(Expression expr)
		{
			InvocationExpression ie = expr.Parent as InvocationExpression;
			if (ie != null && ie.Target == expr)
				return NameLookupMode.InvocationTarget;
			else
				return NameLookupMode.Expression;
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			// Note: this method is not called when it occurs in a situation where an ambiguity between
			// simple names and type names might occur.
			if (resolverEnabled) {
				var typeArguments = ResolveTypeArguments(identifierExpression.TypeArguments);
				var lookupMode = GetNameLookupMode(identifierExpression);
				return resolver.LookupSimpleNameOrTypeName(
					identifierExpression.Identifier, typeArguments, lookupMode);
			} else {
				ScanChildren(identifierExpression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
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
				if (resolver.IsVariableReferenceWithSameType(target, identifierExpression.Identifier, out trr)) {
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
				GetNameLookupMode(memberReferenceExpression));
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitInvocationExpression(InvocationExpression invocationExpression)
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
				if (resolver.IsVariableReferenceWithSameType(idRR, identifierExpression.Identifier, out trr)) {
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
				if (resolverEnabled || NeedsResolvingDueToNamedArguments(invocationExpression)) {
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
			ProcessInvocationResult(invocationExpression.Target, invocationExpression.Arguments, rr);
			return rr;
		}
		#endregion
		
		#region Lamdbas / Anonymous Functions
		ResolveResult IAstVisitor<ResolveResult>.VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			return HandleExplicitlyTypedLambda(
				anonymousMethodExpression.Parameters, anonymousMethodExpression.Body,
				isAnonymousMethod: true,
				hasParameterList: anonymousMethodExpression.HasParameterList,
				isAsync: anonymousMethodExpression.IsAsync);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitLambdaExpression(LambdaExpression lambdaExpression)
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
			//bool oldIsWithinLambdaExpression = resolver.IsWithinLambdaExpression;
			resolver = resolver.WithIsWithinLambdaExpression(true);
			foreach (var pd in parameterDeclarations) {
				IType type = ResolveType(pd.Type);
				if (pd.ParameterModifier == ParameterModifier.Ref || pd.ParameterModifier == ParameterModifier.Out)
					type = new ByReferenceType(type);
				
				IParameter p = new DefaultParameter(type, pd.Name,
				                                    region: MakeRegion(pd),
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
			if (unresolvedFile != null)
				return new DomRegion(unresolvedFile.FileName, node.StartLocation, node.EndLocation);
			else
				return node.GetRegion();
		}
		
		sealed class ExplicitlyTypedLambda : LambdaBase
		{
			readonly IList<IParameter> parameters;
			readonly bool isAnonymousMethod;
			readonly bool isAsync;
			
			CSharpResolver storedContext;
			ResolveVisitor visitor;
			AstNode body;
			ResolveResult bodyRR;
			
			IType inferredReturnType;
			IList<Expression> returnExpressions;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			bool isEndpointUnreachable;
			
			// The actual return type is set when the lambda is applied by the conversion.
			// For async lambdas, this includes the task type
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
					if (bodyRR != null)
						return bodyRR;

					if (body is Expression) {
						Analyze();
						if (returnValues.Count == 1) {
							bodyRR = returnValues[0];
							if (actualReturnType != null) {
								IType unpackedActualReturnType = isAsync ? visitor.UnpackTask(actualReturnType) : actualReturnType;
								if (unpackedActualReturnType.Kind != TypeKind.Void) {
									var conv = storedContext.conversions.ImplicitConversion(bodyRR, unpackedActualReturnType);
									if (!conv.IsIdentityConversion)
										bodyRR = new ConversionResolveResult(unpackedActualReturnType, bodyRR, conv, storedContext.CheckForOverflow);
								}
							}
							return bodyRR;
						}
					}
					return bodyRR = visitor.voidResult;
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
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, CSharpConversions conversions)
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
			
			public override IType ReturnType {
				get {
					return actualReturnType ?? SpecialType.UnknownType;
				}
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
				IType unpackedReturnType = isAsync ? visitor.UnpackTask(returnType) : returnType;
				Log.WriteLine("Applying return type {0} to explicitly-typed lambda {1}", unpackedReturnType, this.LambdaExpression);
				if (unpackedReturnType.Kind != TypeKind.Void || body is BlockStatement) {
					for (int i = 0; i < returnExpressions.Count; i++) {
						visitor.ProcessConversion(returnExpressions[i], returnValues[i], unpackedReturnType);
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
		// Implicitly-typed lambdas are really complex, as the lambda depends on the target type (the delegate to which
		// the lambda is converted), but figuring out the target type might involve overload resolution (for method
		// calls in which the lambda is used as argument), which requires knowledge about the lamdba.
		// 
		// The implementation in NRefactory works like this:
		// 1. The lambda resolves to a ImplicitlyTypedLambda (derived from LambdaResolveResult).
		//     The lambda body is not resolved yet (one of the few places where ResolveVisitor
		//     deviates from the usual depth-first AST traversal).
		// 2. The parent statement is resolved as usual. This might require analyzing the lambda in detail (for example
		//    as part of overload resolution). Such analysis happens using LambdaResolveResult.IsValid, where the caller
		//    (i.e. the overload resolution algorithm) supplies the parameter types to the lambda body. For every IsValid()
		//    call, a nested LambdaTypeHypothesis is constructed for analyzing the lambda using the supplied type assignment.
		//    Multiple IsValid() calls may use several LambdaTypeHypothesis instances, one for each set of parameter types.
		// 3. When the resolver reports the conversions that occurred as part of the parent statement (as with any
		//    conversions), the results from the LambdaTypeHypothesis corresponding to the actually chosen
		//    conversion are merged into the main resolver.
		// 4. LambdaResolveResult.Body is set to the main resolve result from the chosen nested resolver. I think this
		//    is the only place where NRefactory is mutating a ResolveResult (normally all resolve results are immutable).
		//    As this step is guaranteed to occur before the resolver returns the LamdbaResolveResult to user code, the
		//    mutation shouldn't cause any problems.
		sealed class ImplicitlyTypedLambda : LambdaBase
		{
			readonly LambdaExpression lambda;
			readonly QuerySelectClause selectClause;
			
			readonly CSharpResolver storedContext;
			readonly CSharpUnresolvedFile unresolvedFile;
			readonly List<LambdaTypeHypothesis> hypotheses = new List<LambdaTypeHypothesis>();
			internal IList<IParameter> parameters = new List<IParameter>();
			
			internal IType actualReturnType;
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
				this.unresolvedFile = parentVisitor.unresolvedFile;
				this.bodyResult = parentVisitor.voidResult;
			}
			
			public ImplicitlyTypedLambda(LambdaExpression lambda, ResolveVisitor parentVisitor)
				: this(parentVisitor)
			{
				this.lambda = lambda;
				foreach (var pd in lambda.Parameters) {
					parameters.Add(new DefaultParameter(SpecialType.UnknownType, pd.Name, region: parentVisitor.MakeRegion(pd)));
				}
				RegisterUndecidedLambda();
			}
			
			public ImplicitlyTypedLambda(QuerySelectClause selectClause, IEnumerable<IParameter> parameters, ResolveVisitor parentVisitor)
				: this(parentVisitor)
			{
				this.selectClause = selectClause;
				foreach (IParameter p in parameters)
					this.parameters.Add(p);
				
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
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, CSharpConversions conversions)
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
				ResolveVisitor visitor = new ResolveVisitor(storedContext, unresolvedFile);
				var newHypothesis = new LambdaTypeHypothesis(this, parameterTypes, visitor, lambda != null ? lambda.Parameters : null, storedContext);
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
			
			public override IType ReturnType {
				get { return actualReturnType ?? SpecialType.UnknownType; }
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
			readonly IParameter[] lambdaParameters;
			internal readonly IType[] parameterTypes;
			readonly ResolveVisitor visitor;
			readonly CSharpResolver storedContext;
			
			internal readonly IType inferredReturnType;
			IList<Expression> returnExpressions;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			bool isEndpointUnreachable;
			internal bool success;
			
			public LambdaTypeHypothesis(ImplicitlyTypedLambda lambda, IType[] parameterTypes, ResolveVisitor visitor,
			                            ICollection<ParameterDeclaration> parameterDeclarations, CSharpResolver storedContext)
			{
				Debug.Assert(parameterTypes.Length == lambda.Parameters.Count);
				
				this.lambda = lambda;
				this.parameterTypes = parameterTypes;
				this.visitor = visitor;
				this.storedContext = storedContext;
				visitor.SetNavigator(this);
				
				Log.WriteLine("Analyzing " + ToString() + "...");
				Log.Indent();
				CSharpResolver oldResolver = visitor.resolver;
				visitor.resolver = visitor.resolver.WithIsWithinLambdaExpression(true);
				lambdaParameters = new IParameter[parameterTypes.Length];
				if (parameterDeclarations != null) {
					int i = 0;
					foreach (var pd in parameterDeclarations) {
						lambdaParameters[i] = new DefaultParameter(parameterTypes[i], pd.Name, region: visitor.MakeRegion(pd));
						visitor.resolver = visitor.resolver.AddVariable(lambdaParameters[i]);
						i++;
						visitor.Scan(pd);
					}
				} else {
					for (int i = 0; i < parameterTypes.Length; i++) {
						var p = lambda.Parameters[i];
						lambdaParameters[i] = new DefaultParameter(parameterTypes[i], p.Name, region: p.Region);
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
			
			public Conversion IsValid(IType returnType, CSharpConversions conversions)
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
				
				lambda.actualReturnType = returnType;
				if (lambda.IsAsync)
					returnType = parentVisitor.UnpackTask(returnType);
				
				lambda.winningHypothesis = this;
				lambda.parameters = lambdaParameters; // replace untyped parameters with typed parameters
				if (lambda.BodyExpression is Expression && returnValues.Count == 1) {
					lambda.bodyResult = returnValues[0];
					if (returnType.Kind != TypeKind.Void) {
						var conv = storedContext.conversions.ImplicitConversion(lambda.bodyResult, returnType);
						if (!conv.IsIdentityConversion)
							lambda.bodyResult = new ConversionResolveResult(returnType, lambda.bodyResult, conv, storedContext.CheckForOverflow);
					}
				}
				
				Log.WriteLine("Applying return type {0} to implicitly-typed lambda {1}", returnType, lambda.LambdaExpression);
				if (returnType.Kind != TypeKind.Void || lambda.BodyExpression is Statement) {
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
			
			public override ResolveResult ShallowClone()
			{
				if (IsUndecided)
					throw new NotSupportedException();
				return base.ShallowClone();
			}
		}
		
		void MergeUndecidedLambdas()
		{
			if (undecidedLambdas == null || undecidedLambdas.Count == 0)
				return;
			Log.WriteLine("MergeUndecidedLambdas()...");
			Log.Indent();
			while (undecidedLambdas.Count > 0) {
				LambdaBase lambda = undecidedLambdas[0];
				// may happen caused by parse error l =>
				if (lambda.LambdaExpression == null) {
					undecidedLambdas.Remove (lambda);
					continue;
				}
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
				body.AcceptVisitor(alv);
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

					// async lambdas without return statements are resolved as Task return types.
					if (returnExpressions.Count == 0 && isAsync) {
						inferredReturnType = resolver.Compilation.FindType(KnownTypeCode.Task);
						Log.WriteLine("Lambda return type was inferred to: " + inferredReturnType);
						return;
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
		
		static bool IsValidLambda(bool isValidAsVoidMethod, bool isEndpointUnreachable, bool isAsync, IList<ResolveResult> returnValues, IType returnType, CSharpConversions conversions)
		{
			if (returnType.Kind == TypeKind.Void) {
				// Lambdas that are valid statement lambdas or expression lambdas with a statement-expression
				// can be converted to delegates with void return type.
				// This holds for both async and regular lambdas.
				return isValidAsVoidMethod;
			} else if (isAsync && TaskType.IsTask(returnType) && returnType.TypeParameterCount == 0) {
				// Additionally, async lambdas with the above property can be converted to non-generic Task.
				return isValidAsVoidMethod;
			} else {
				if (returnValues.Count == 0)
					return isEndpointUnreachable;
				if (isAsync) {
					// async lambdas must return Task<T>
					if (!(TaskType.IsTask(returnType) && returnType.TypeParameterCount == 1))
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
		
		IType UnpackTask(IType type)
		{
			return TaskType.UnpackTask(resolver.Compilation, type);
		}
		
		sealed class AnalyzeLambdaVisitor : DepthFirstAstVisitor
		{
			public bool HasVoidReturnStatements;
			public List<Expression> ReturnExpressions = new List<Expression>();
			
			public override void VisitReturnStatement(ReturnStatement returnStatement)
			{
				Expression expr = returnStatement.Expression;
				if (expr.IsNull) {
					HasVoidReturnStatements = true;
				} else {
					ReturnExpressions.Add(expr);
				}
			}
			
			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
			{
				// don't go into nested lambdas
			}
			
			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
			{
				// don't go into nested lambdas
			}
		}
		#endregion
		#endregion
		
		#region ForEach Statement
		ResolveResult IAstVisitor<ResolveResult>.VisitForeachStatement(ForeachStatement foreachStatement)
		{
			var compilation = resolver.Compilation;
			ResolveResult expression = Resolve(foreachStatement.InExpression);
			bool isImplicitlyTypedVariable = foreachStatement.VariableType.IsVar();
			var memberLookup = resolver.CreateMemberLookup();
			
			IType collectionType, enumeratorType, elementType;
			ResolveResult getEnumeratorInvocation;
			ResolveResult currentRR = null;
			// C# 4.0 spec: §8.8.4 The foreach statement
			if (expression.Type.Kind == TypeKind.Array || expression.Type.Kind == TypeKind.Dynamic) {
				collectionType = compilation.FindType(KnownTypeCode.IEnumerable);
				enumeratorType = compilation.FindType(KnownTypeCode.IEnumerator);
				if (expression.Type.Kind == TypeKind.Array) {
					elementType = ((ArrayType)expression.Type).ElementType;
				} else {
					elementType = isImplicitlyTypedVariable ? SpecialType.Dynamic : compilation.FindType(KnownTypeCode.Object);
				}
				getEnumeratorInvocation = resolver.ResolveCast(collectionType, expression);
				getEnumeratorInvocation = resolver.ResolveMemberAccess(getEnumeratorInvocation, "GetEnumerator", EmptyList<IType>.Instance, NameLookupMode.InvocationTarget);
				getEnumeratorInvocation = resolver.ResolveInvocation(getEnumeratorInvocation, new ResolveResult[0]);
			} else {
				var getEnumeratorMethodGroup = memberLookup.Lookup(expression, "GetEnumerator", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
				if (getEnumeratorMethodGroup != null) {
					var or = getEnumeratorMethodGroup.PerformOverloadResolution(
						compilation, new ResolveResult[0],
						allowExtensionMethods: false, allowExpandingParams: false, allowOptionalParameters: false);
					if (or.FoundApplicableCandidate && !or.IsAmbiguous && !or.BestCandidate.IsStatic && or.BestCandidate.IsPublic) {
						collectionType = expression.Type;
						getEnumeratorInvocation = or.CreateResolveResult(expression);
						enumeratorType = getEnumeratorInvocation.Type;
						currentRR = memberLookup.Lookup(new ResolveResult(enumeratorType), "Current", EmptyList<IType>.Instance, false);
						elementType = currentRR.Type;
					} else {
						CheckForEnumerableInterface(expression, out collectionType, out enumeratorType, out elementType, out getEnumeratorInvocation);
					}
				} else {
					CheckForEnumerableInterface(expression, out collectionType, out enumeratorType, out elementType, out getEnumeratorInvocation);
				}
			}
			IMethod moveNextMethod = null;
			var moveNextMethodGroup = memberLookup.Lookup(new ResolveResult(enumeratorType), "MoveNext", EmptyList<IType>.Instance, false) as MethodGroupResolveResult;
			if (moveNextMethodGroup != null) {
				var or = moveNextMethodGroup.PerformOverloadResolution(
					compilation, new ResolveResult[0],
					allowExtensionMethods: false, allowExpandingParams: false, allowOptionalParameters: false);
				moveNextMethod = or.GetBestCandidateWithSubstitutedTypeArguments() as IMethod;
			}
			
			if (currentRR == null)
				currentRR = memberLookup.Lookup(new ResolveResult(enumeratorType), "Current", EmptyList<IType>.Instance, false);
			IProperty currentProperty = null;
			if (currentRR is MemberResolveResult)
				currentProperty = ((MemberResolveResult)currentRR).Member as IProperty;
			// end of foreach resolve logic
			// back to resolve visitor:
			
			resolver = resolver.PushBlock();
			IVariable v;
			if (isImplicitlyTypedVariable) {
				StoreCurrentState(foreachStatement.VariableType);
				StoreResult(foreachStatement.VariableType, new TypeResolveResult(elementType));
				v = MakeVariable(elementType, foreachStatement.VariableNameToken);
			} else {
				IType variableType = ResolveType(foreachStatement.VariableType);
				v = MakeVariable(variableType, foreachStatement.VariableNameToken);
			}
			StoreCurrentState(foreachStatement.VariableNameToken);
			resolver = resolver.AddVariable(v);
			
			StoreResult(foreachStatement.VariableNameToken, new LocalResolveResult(v));
			
			Scan(foreachStatement.EmbeddedStatement);
			resolver = resolver.PopBlock();
			return new ForEachResolveResult(getEnumeratorInvocation, collectionType, enumeratorType, elementType,
			                                v, currentProperty, moveNextMethod, voidResult.Type);
		}
		
		void CheckForEnumerableInterface(ResolveResult expression, out IType collectionType, out IType enumeratorType, out IType elementType, out ResolveResult getEnumeratorInvocation)
		{
			var compilation = resolver.Compilation;
			bool? isGeneric;
			elementType = GetElementTypeFromIEnumerable(expression.Type, compilation, false, out isGeneric);
			if (isGeneric == true) {
				ITypeDefinition enumerableOfT = compilation.FindType(KnownTypeCode.IEnumerableOfT).GetDefinition();
				if (enumerableOfT != null)
					collectionType = new ParameterizedType(enumerableOfT, new [] { elementType });
				else
					collectionType = SpecialType.UnknownType;
				
				ITypeDefinition enumeratorOfT = compilation.FindType(KnownTypeCode.IEnumeratorOfT).GetDefinition();
				if (enumeratorOfT != null)
					enumeratorType = new ParameterizedType(enumeratorOfT, new [] { elementType });
				else
					enumeratorType = SpecialType.UnknownType;
			} else if (isGeneric == false) {
				collectionType = compilation.FindType(KnownTypeCode.IEnumerable);
				enumeratorType = compilation.FindType(KnownTypeCode.IEnumerator);
			} else {
				collectionType = SpecialType.UnknownType;
				enumeratorType = SpecialType.UnknownType;
			}
			getEnumeratorInvocation = resolver.ResolveCast(collectionType, expression);
			getEnumeratorInvocation = resolver.ResolveMemberAccess(getEnumeratorInvocation, "GetEnumerator", EmptyList<IType>.Instance, NameLookupMode.InvocationTarget);
			getEnumeratorInvocation = resolver.ResolveInvocation(getEnumeratorInvocation, new ResolveResult[0]);
		}
		#endregion
		
		#region Local Variable Scopes (Block Statements)
		ResolveResult IAstVisitor<ResolveResult>.VisitBlockStatement(BlockStatement blockStatement)
		{
			resolver = resolver.PushBlock();
			ScanChildren(blockStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUsingStatement(UsingStatement usingStatement)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitFixedStatement(FixedStatement fixedStatement)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitSwitchStatement(SwitchStatement switchStatement)
		{
			resolver = resolver.PushBlock();
			ScanChildren(switchStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitCatchClause(CatchClause catchClause)
		{
			resolver = resolver.PushBlock();
			if (string.IsNullOrEmpty(catchClause.VariableName)) {
				Scan(catchClause.Type);
			} else {
				//DomRegion region = MakeRegion(catchClause.VariableNameToken);
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
		ResolveResult IAstVisitor<ResolveResult>.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			bool isConst = (variableDeclarationStatement.Modifiers & Modifiers.Const) != 0;
			if (!isConst && variableDeclarationStatement.Type.IsVar() && variableDeclarationStatement.Variables.Count == 1) {
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
						ResolveResult rr = Resolve(vi.Initializer);
						rr = resolver.ResolveCast(type, rr);
						v = MakeConstant(type, vi.NameToken, rr.ConstantValue);
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
		ResolveResult IAstVisitor<ResolveResult>.VisitForStatement(ForStatement forStatement)
		{
			resolver = resolver.PushBlock();
			var result = HandleConditionStatement(forStatement);
			resolver = resolver.PopBlock();
			return result;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			return HandleConditionStatement(ifElseStatement);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitWhileStatement(WhileStatement whileStatement)
		{
			return HandleConditionStatement(whileStatement);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			return HandleConditionStatement(doWhileStatement);
		}
		
		ResolveResult HandleConditionStatement(Statement conditionStatement)
		{
			if (resolverEnabled) {
				for (AstNode child = conditionStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == Roles.Condition) {
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
		ResolveResult IAstVisitor<ResolveResult>.VisitReturnStatement(ReturnStatement returnStatement)
		{
			if (resolverEnabled && !resolver.IsWithinLambdaExpression && resolver.CurrentMember != null) {
				IType type = resolver.CurrentMember.ReturnType;
				if (TaskType.IsTask(type)) {
					var methodDecl = returnStatement.Ancestors.OfType<EntityDeclaration>().FirstOrDefault();
					if (methodDecl != null && (methodDecl.Modifiers & Modifiers.Async) == Modifiers.Async)
						type = UnpackTask(type);
				}
				ResolveAndProcessConversion(returnStatement.Expression, type);
			} else {
				Scan(returnStatement.Expression);
			}
			return resolverEnabled ? voidResult : null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitYieldReturnStatement(YieldReturnStatement yieldStatement)
		{
			if (resolverEnabled && resolver.CurrentMember != null) {
				IType returnType = resolver.CurrentMember.ReturnType;
				bool? isGeneric;
				IType elementType = GetElementTypeFromIEnumerable(returnType, resolver.Compilation, true, out isGeneric);
				ResolveAndProcessConversion(yieldStatement.Expression, elementType);
			} else {
				Scan(yieldStatement.Expression);
			}
			return resolverEnabled ? voidResult : null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			return voidResult;
		}
		#endregion
		
		#region Other statements
		ResolveResult IAstVisitor<ResolveResult>.VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			ScanChildren(expressionStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitLockStatement(LockStatement lockStatement)
		{
			ScanChildren(lockStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitBreakStatement(BreakStatement breakStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitContinueStatement(ContinueStatement continueStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitThrowStatement(ThrowStatement throwStatement)
		{
			if (resolverEnabled) {
				ResolveAndProcessConversion(throwStatement.Expression, resolver.Compilation.FindType(KnownTypeCode.Exception));
				return voidResult;
			} else {
				Scan(throwStatement.Expression);
				return null;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			ScanChildren(tryCatchStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
		{
			ScanChildren(gotoCaseStatement);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitGotoStatement(GotoStatement gotoStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitLabelStatement(LabelStatement labelStatement)
		{
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			resolver = resolver.PushBlock();
			ScanChildren(unsafeStatement);
			resolver = resolver.PopBlock();
			return voidResult;
		}
		#endregion
		
		#region Local Variable Type Inference
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
			
			public SymbolKind SymbolKind {
				get { return SymbolKind.Variable; }
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

			public ISymbolReference ToReference()
			{
				return new VariableReference(type.ToTypeReference(), name, region, IsConst, ConstantValue);
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
		
		static IType GetElementTypeFromIEnumerable(IType collectionType, ICompilation compilation, bool allowIEnumerator, out bool? isGeneric)
		{
			bool foundNonGenericIEnumerable = false;
			foreach (IType baseType in collectionType.GetAllBaseTypes()) {
				ITypeDefinition baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef != null) {
					KnownTypeCode typeCode = baseTypeDef.KnownTypeCode;
					if (typeCode == KnownTypeCode.IEnumerableOfT || (allowIEnumerator && typeCode == KnownTypeCode.IEnumeratorOfT)) {
						ParameterizedType pt = baseType as ParameterizedType;
						if (pt != null) {
							isGeneric = true;
							return pt.GetTypeArgument(0);
						}
					}
					if (typeCode == KnownTypeCode.IEnumerable || (allowIEnumerator && typeCode == KnownTypeCode.IEnumerator))
						foundNonGenericIEnumerable = true;
				}
			}
			// System.Collections.IEnumerable found in type hierarchy -> Object is element type.
			if (foundNonGenericIEnumerable) {
				isGeneric = false;
				return compilation.FindType(KnownTypeCode.Object);
			}
			isGeneric = null;
			return SpecialType.UnknownType;
		}
		#endregion
		
		#region Attributes
		ResolveResult IAstVisitor<ResolveResult>.VisitAttribute(Attribute attribute)
		{
			var type = ResolveType(attribute.Type);
			
			// Separate arguments into ctor arguments and non-ctor arguments:
			var constructorArguments = attribute.Arguments.Where(a => !(a is NamedExpression));
			var nonConstructorArguments = attribute.Arguments.OfType<NamedExpression>();
			
			// Scan the non-constructor arguments
			resolver = resolver.PushObjectInitializer(new InitializedObjectResolveResult(type));
			List<ResolveResult> initializerStatements = new List<ResolveResult>();
			foreach (var arg in nonConstructorArguments)
				HandleNamedExpression(arg, initializerStatements);
			resolver = resolver.PopObjectInitializer();
			
			// Resolve the ctor arguments and find the matching ctor overload
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(constructorArguments, out argumentNames);
			ResolveResult rr = resolver.ResolveObjectCreation(type, arguments, argumentNames, false, initializerStatements);
			ProcessInvocationResult(null, constructorArguments, rr);
			return rr;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitAttributeSection(AttributeSection attributeSection)
		{
			ScanChildren(attributeSection);
			return voidResult;
		}
		#endregion
		
		#region Using Declaration
		ResolveResult IAstVisitor<ResolveResult>.VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			ScanChildren(usingDeclaration);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
		{
			ScanChildren(usingDeclaration);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			return voidResult;
		}
		#endregion
		
		#region Type References
		ResolveResult IAstVisitor<ResolveResult>.VisitPrimitiveType(PrimitiveType primitiveType)
		{
			if (!resolverEnabled)
				return null;
			KnownTypeCode typeCode = primitiveType.KnownTypeCode;
			if (typeCode == KnownTypeCode.None && primitiveType.Parent is Constraint && primitiveType.Role == Roles.BaseType) {
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitSimpleType(SimpleType simpleType)
		{
			if (!resolverEnabled) {
				ScanChildren(simpleType);
				return null;
			}
			
			// Figure out the correct lookup mode:
			NameLookupMode lookupMode = simpleType.GetNameLookupMode();
			
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitMemberType(MemberType memberType)
		{
			ResolveResult target;
			NameLookupMode lookupMode = memberType.GetNameLookupMode();
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
			ResolveResult rr = resolver.ResolveMemberAccess(target, identifier.Name, typeArguments, lookupMode);
			if (memberType.Parent is Attribute && !identifier.IsVerbatim) {
				var withSuffix = resolver.ResolveMemberAccess(target, identifier.Name + "Attribute", typeArguments, lookupMode);
				if (AttributeTypeReference.PreferAttributeTypeWithSuffix(rr.Type, withSuffix.Type, resolver.Compilation))
					return withSuffix;
			}
			return rr;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitComposedType(ComposedType composedType)
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
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryExpression(QueryExpression queryExpression)
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
				return WrapResult(currentQueryResult);
			} finally {
				currentQueryResult = oldQueryResult;
				cancellationToken = oldCancellationToken;
				resolver = resolver.PopBlock();
			}
		}
		
		IType GetTypeForQueryVariable(IType type)
		{
			bool? isGeneric;
			// This assumes queries are only used on IEnumerable.
			var result = GetElementTypeFromIEnumerable(type, resolver.Compilation, false, out isGeneric);

			// If that fails try to resolve the Select method and resolve the projection.
			if (result.Kind == TypeKind.Unknown) {
				var selectAccess = resolver.ResolveMemberAccess(new ResolveResult (type), "Select", EmptyList<IType>.Instance);
				ResolveResult[] arguments = {
					new QueryExpressionLambda(1, voidResult) 
				};
				 
				var rr = resolver.ResolveInvocation(selectAccess, arguments) as CSharpInvocationResolveResult; 
				if (rr != null && rr.Arguments.Count == 2) {
					var invokeMethod = rr.Arguments[1].Type.GetDelegateInvokeMethod();
					if (invokeMethod != null && invokeMethod.Parameters.Count > 0)
						return invokeMethod.Parameters[0].Type;
				}
			}
			return result;
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
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, CSharpConversions conversions)
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
			
			public override IType ReturnType {
				get {
					return bodyExpression.Type;
				}
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryFromClause(QueryFromClause queryFromClause)
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
				ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { v.Type }, NameLookupMode.InvocationTarget);
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
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "SelectMany", EmptyList<IType>.Instance, NameLookupMode.InvocationTarget);
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
			return new CastResolveResult(result.Type, result, Conversion.IdentityConversion, resolver.CheckForOverflow);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			ResolveResult rr = Resolve(queryContinuationClause.PrecedingQuery);
			IType variableType = GetTypeForQueryVariable(rr.Type);
			StoreCurrentState(queryContinuationClause.IdentifierToken);
			IVariable v = MakeVariable(variableType, queryContinuationClause.IdentifierToken);
			resolver = resolver.AddVariable(v);
			StoreResult(queryContinuationClause.IdentifierToken, new LocalResolveResult(v));
			return WrapResult(rr);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			ResolveResult expr = Resolve(queryLetClause.Expression);
			StoreCurrentState(queryLetClause.IdentifierToken);
			IVariable v = MakeVariable(expr.Type, queryLetClause.IdentifierToken);
			resolver = resolver.AddVariable(v);
			StoreResult(queryLetClause.IdentifierToken, new LocalResolveResult(v));
			if (currentQueryResult != null) {
				// resolve the .Select() call
				ResolveResult methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Select", EmptyList<IType>.Instance, NameLookupMode.InvocationTarget);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, MakeTransparentIdentifierResolveResult()) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return errorResult;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryJoinClause(QueryJoinClause queryJoinClause)
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
				ResolveResult methodGroup = resolver.ResolveMemberAccess(expr, "Cast", new[] { variableType }, NameLookupMode.InvocationTarget);
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
				if (implicitlyTypedLambda.Parameters.Count == 2) {
					StoreCurrentState(queryJoinClause.IntoIdentifierToken);
					groupVariable = implicitlyTypedLambda.Parameters[1];
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			ResolveResult condition = Resolve(queryWhereClause.Condition);
			IType boolType = resolver.Compilation.FindType(KnownTypeCode.Boolean);
			Conversion conversionToBool = resolver.conversions.ImplicitConversion(condition, boolType);
			ProcessConversion(queryWhereClause.Condition, condition, conversionToBool, boolType);
			if (currentQueryResult != null) {
				if (conversionToBool != Conversion.IdentityConversion && conversionToBool != Conversion.None) {
					condition = new ConversionResolveResult(boolType, condition, conversionToBool, resolver.CheckForOverflow);
				}
				
				var methodGroup = resolver.ResolveMemberAccess(currentQueryResult, "Where", EmptyList<IType>.Instance);
				ResolveResult[] arguments = { new QueryExpressionLambda(1, condition) };
				return resolver.ResolveInvocation(methodGroup, arguments);
			} else {
				return errorResult;
			}
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQuerySelectClause(QuerySelectClause querySelectClause)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryGroupClause(QueryGroupClause queryGroupClause)
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
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			foreach (QueryOrdering ordering in queryOrderClause.Orderings) {
				currentQueryResult = Resolve(ordering);
			}
			return WrapResult(currentQueryResult);
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitQueryOrdering(QueryOrdering queryOrdering)
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
		ResolveResult IAstVisitor<ResolveResult>.VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			ResolveResult target;
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base) {
				target = resolver.ResolveBaseReference();
			} else {
				target = resolver.ResolveThisReference();
			}
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(constructorInitializer.Arguments, out argumentNames);
			ResolveResult rr = resolver.ResolveObjectCreation(target.Type, arguments, argumentNames, allowProtectedAccess: true);
			ProcessInvocationResult(null, constructorInitializer.Arguments, rr);
			return rr;
		}
		#endregion
		
		#region Other Nodes
		// Token nodes
		ResolveResult IAstVisitor<ResolveResult>.VisitIdentifier(Identifier identifier)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitComment (Comment comment)
		{
			return null;
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitNewLine (NewLineNode comment)
		{
			return null;
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitText(TextNode textNode)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective)
		{
			return null;
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitArraySpecifier(ArraySpecifier arraySpecifier)
		{
			return null;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitNullNode(AstNode nullNode)
		{
			return null;
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitErrorNode(AstNode errorNode)
		{
			return null;
		}

		ResolveResult IAstVisitor<ResolveResult>.VisitPatternPlaceholder(AstNode placeholder, ICSharpCode.NRefactory.PatternMatching.Pattern pattern)
		{
			return null;
		}
		
		// Nodes where we just need to visit the children:
		ResolveResult IAstVisitor<ResolveResult>.VisitAccessor(Accessor accessor)
		{
			ScanChildren(accessor);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitSwitchSection(SwitchSection switchSection)
		{
			ScanChildren(switchSection);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitCaseLabel(CaseLabel caseLabel)
		{
			ScanChildren(caseLabel);
			return voidResult;
		}
		
		ResolveResult IAstVisitor<ResolveResult>.VisitConstraint(Constraint constraint)
		{
			ScanChildren(constraint);
			return voidResult;
		}
		#endregion
		
		#region Documentation Reference
		ResolveResult IAstVisitor<ResolveResult>.VisitDocumentationReference(DocumentationReference documentationReference)
		{
			// Resolve child nodes:
			ITypeDefinition declaringTypeDef;
			if (documentationReference.DeclaringType.IsNull)
				declaringTypeDef = resolver.CurrentTypeDefinition;
			else
				declaringTypeDef = ResolveType(documentationReference.DeclaringType).GetDefinition();
			IType[] typeArguments = documentationReference.TypeArguments.Select(ResolveType).ToArray();
			IType conversionOperatorReturnType = ResolveType(documentationReference.ConversionOperatorReturnType);
			IParameter[] parameters = documentationReference.Parameters.Select(ResolveXmlDocParameter).ToArray();
			
			if (documentationReference.SymbolKind == SymbolKind.TypeDefinition) {
				if (declaringTypeDef != null)
					return new TypeResolveResult(declaringTypeDef);
				else
					return errorResult;
			}
			
			if (documentationReference.SymbolKind == SymbolKind.None) {
				// might be a type, member or ctor
				string memberName = documentationReference.MemberName;
				ResolveResult rr;
				if (documentationReference.DeclaringType.IsNull) {
					rr = resolver.LookupSimpleNameOrTypeName(memberName, typeArguments, NameLookupMode.Expression);
				} else {
					var target = Resolve(documentationReference.DeclaringType);
					rr = resolver.ResolveMemberAccess(target, memberName, typeArguments);
				}
				// reduce to definition:
				if (rr.IsError) {
					return rr;
				} else if (rr is TypeResolveResult) {
					var typeDef = rr.Type.GetDefinition();
					if (typeDef == null)
						return errorResult;
					if (documentationReference.HasParameterList) {
						var ctors = typeDef.GetConstructors(options: GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions);
						return FindByParameters(ctors, parameters);
					} else {
						return new TypeResolveResult(typeDef);
					}
				} else if (rr is MemberResolveResult) {
					var mrr = (MemberResolveResult)rr;
					return new MemberResolveResult(null, mrr.Member.MemberDefinition);
				} else if (rr is MethodGroupResolveResult) {
					var mgrr = (MethodGroupResolveResult)rr;
					var methods = mgrr.MethodsGroupedByDeclaringType.Reverse()
						.SelectMany(ml => ml.Select(m => (IParameterizedMember)m.MemberDefinition));
					return FindByParameters(methods, parameters);
				}
				return rr;
			}
			
			// Indexer or operator
			if (declaringTypeDef == null)
				return errorResult;
			if (documentationReference.SymbolKind == SymbolKind.Indexer) {
				var indexers = declaringTypeDef.Properties.Where(p => p.IsIndexer && !p.IsExplicitInterfaceImplementation);
				return FindByParameters(indexers, parameters);
			} else if (documentationReference.SymbolKind == SymbolKind.Operator) {
				var opType = documentationReference.OperatorType;
				string memberName = OperatorDeclaration.GetName(opType);
				var methods = declaringTypeDef.Methods.Where(m => m.IsOperator && m.Name == memberName);
				if (opType == OperatorType.Implicit || opType == OperatorType.Explicit) {
					// conversion operator
					foreach (var method in methods) {
						if (ParameterListComparer.Instance.Equals(method.Parameters, parameters)) {
							if (method.ReturnType.Equals(conversionOperatorReturnType))
								return new MemberResolveResult(null, method);
						}
					}
					return new MemberResolveResult(null, methods.FirstOrDefault());
				} else {
					// not a conversion operator
					return FindByParameters(methods, parameters);
				}
			} else {
				throw new NotSupportedException(); // unknown entity type
			}
		}
		
		IParameter ResolveXmlDocParameter(ParameterDeclaration p)
		{
			var lrr = Resolve(p) as LocalResolveResult;
			if (lrr != null && lrr.IsParameter)
				return (IParameter)lrr.Variable;
			else
				return new DefaultParameter(SpecialType.UnknownType, string.Empty);
		}
		
		ResolveResult FindByParameters(IEnumerable<IParameterizedMember> methods, IList<IParameter> parameters)
		{
			foreach (var method in methods) {
				if (ParameterListComparer.Instance.Equals(method.Parameters, parameters))
					return new MemberResolveResult(null, method);
			}
			return new MemberResolveResult(null, methods.FirstOrDefault());
		}
		#endregion
	}
}
