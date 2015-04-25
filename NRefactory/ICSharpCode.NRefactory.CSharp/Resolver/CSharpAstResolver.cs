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
using System.Diagnostics;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Resolves C# AST nodes.
	/// </summary>
	/// <remarks>This class is thread-safe.</remarks>
	public class CSharpAstResolver
	{
		readonly CSharpResolver initialResolverState;
		readonly AstNode rootNode;
		readonly CSharpUnresolvedFile unresolvedFile;
		readonly ResolveVisitor resolveVisitor;
		bool resolverInitialized;
		
		/// <summary>
		/// Creates a new C# AST resolver.
		/// Use this overload if you are resolving within a complete C# file.
		/// </summary>
		/// <param name="compilation">The current compilation.</param>
		/// <param name="syntaxTree">The syntax tree to be resolved.</param>
		/// <param name="unresolvedFile">
		/// Optional: Result of <see cref="SyntaxTree.ToTypeSystem()"/> for the file being resolved.
		/// <para>
		/// This is used for setting up the context on the resolver. The unresolved file must be registered in the compilation.
		/// </para>
		/// <para>
		/// When a unresolvedFile is specified, the resolver will use the member's StartLocation/EndLocation to identify
		/// member declarations in the AST with members in the type system.
		/// When no unresolvedFile is specified (<c>null</c> value for this parameter), the resolver will instead compare the
		/// member's signature in the AST with the signature in the type system.
		/// </para>
		/// </param>
		public CSharpAstResolver(ICompilation compilation, SyntaxTree syntaxTree, CSharpUnresolvedFile unresolvedFile = null)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");
			this.initialResolverState = new CSharpResolver(compilation);
			this.rootNode = syntaxTree;
			this.unresolvedFile = unresolvedFile;
			this.resolveVisitor = new ResolveVisitor(initialResolverState, unresolvedFile);
		}
		
		/// <summary>
		/// Creates a new C# AST resolver.
		/// Use this overload if you are resolving code snippets (not necessarily complete files).
		/// </summary>
		/// <param name="resolver">The resolver state at the root node (to be more precise: just outside the root node).</param>
		/// <param name="rootNode">The root node of the tree to be resolved.</param>
		/// <param name="unresolvedFile">
		/// Optional: Result of <see cref="SyntaxTree.ToTypeSystem()"/> for the file being resolved.
		/// <para>
		/// This is used for setting up the context on the resolver. The unresolved file must be registered in the compilation.
		/// </para>
		/// <para>
		/// When a unresolvedFile is specified, the resolver will use the member's StartLocation/EndLocation to identify
		/// member declarations in the AST with members in the type system.
		/// When no unresolvedFile is specified (<c>null</c> value for this parameter), the resolver will instead compare the
		/// member's signature in the AST with the signature in the type system.
		/// </para>
		/// </param>
		public CSharpAstResolver(CSharpResolver resolver, AstNode rootNode, CSharpUnresolvedFile unresolvedFile = null)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			if (rootNode == null)
				throw new ArgumentNullException("rootNode");
			this.initialResolverState = resolver;
			this.rootNode = rootNode;
			this.unresolvedFile = unresolvedFile;
			this.resolveVisitor = new ResolveVisitor(initialResolverState, unresolvedFile);
		}
		
		/// <summary>
		/// Gets the type resolve context for the root resolver.
		/// </summary>
		public CSharpTypeResolveContext TypeResolveContext {
			get { return initialResolverState.CurrentTypeResolveContext; }
		}
		
		/// <summary>
		/// Gets the compilation for this resolver.
		/// </summary>
		public ICompilation Compilation {
			get { return initialResolverState.Compilation; }
		}
		
		/// <summary>
		/// Gets the root node for which this CSharpAstResolver was created.
		/// </summary>
		public AstNode RootNode {
			get { return rootNode; }
		}
		
		/// <summary>
		/// Gets the unresolved file used by this CSharpAstResolver.
		/// Can return null.
		/// </summary>
		public CSharpUnresolvedFile UnresolvedFile {
			get { return unresolvedFile; }
		}
		
		/// <summary>
		/// Applies a resolver navigator. This will resolve the nodes requested by the navigator, and will inform the
		/// navigator of the results.
		/// This method must be called as the first operation on the CSharpAstResolver, it is invalid to apply a navigator
		/// after a portion of the file was already resolved.
		/// </summary>
		public void ApplyNavigator(IResolveVisitorNavigator navigator, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (navigator == null)
				throw new ArgumentNullException("navigator");
			
			lock (resolveVisitor) {
				if (resolverInitialized)
					throw new InvalidOperationException("Applying a navigator is only valid as the first operation on the CSharpAstResolver.");
				
				resolverInitialized = true;
				resolveVisitor.cancellationToken = cancellationToken;
				resolveVisitor.SetNavigator(navigator);
				try {
					resolveVisitor.Scan(rootNode);
				} finally {
					resolveVisitor.SetNavigator(null);
					resolveVisitor.cancellationToken = CancellationToken.None;
				}
			}
		}
		
		/// <summary>
		/// Resolves the specified node.
		/// </summary>
		public ResolveResult Resolve(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (node == null || node.IsNull || IsUnresolvableNode(node))
				return ErrorResolveResult.UnknownError;
			lock (resolveVisitor) {
				InitResolver();
				resolveVisitor.cancellationToken = cancellationToken;
				try {
					ResolveResult rr = resolveVisitor.GetResolveResult(node);
					if (rr == null)
						Debug.Fail (node.GetType () + " resolved to null.", node.StartLocation + ":'" + node.ToString () + "'");
					return rr;
				} finally {
					resolveVisitor.cancellationToken = CancellationToken.None;
				}
			}
		}
		
		void InitResolver()
		{
			if (!resolverInitialized) {
				resolverInitialized = true;
				resolveVisitor.Scan(rootNode);
			}
		}
		
		/// <summary>
		/// Gets the resolver state immediately before the specified node.
		/// That is, if the node is a variable declaration, the returned state will not contain the newly declared variable.
		/// </summary>
		public CSharpResolver GetResolverStateBefore(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (node == null || node.IsNull)
				throw new ArgumentNullException("node");
			lock (resolveVisitor) {
				InitResolver();
				resolveVisitor.cancellationToken = cancellationToken;
				try {
					CSharpResolver resolver = resolveVisitor.GetResolverStateBefore(node);
					Debug.Assert(resolver != null);
					return resolver;
				} finally {
					resolveVisitor.cancellationToken = CancellationToken.None;
				}
			}
		}
		
		/// <summary>
		/// Gets the resolver state immediately after the specified node.
		/// That is, if the node is a variable declaration, the returned state will include the newly declared variable.
		/// </summary>
		public CSharpResolver GetResolverStateAfter(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (node == null || node.IsNull)
				throw new ArgumentNullException("node");
			while (node != null && IsUnresolvableNode(node))
				node = node.Parent;
			if (node == null)
				return initialResolverState;
			lock (resolveVisitor) {
				InitResolver();
				resolveVisitor.cancellationToken = cancellationToken;
				try {
					CSharpResolver resolver = resolveVisitor.GetResolverStateAfter(node);
					Debug.Assert(resolver != null);
					return resolver;
				} finally {
					resolveVisitor.cancellationToken = CancellationToken.None;
				}
			}
		}
		
		ResolveVisitor.ConversionWithTargetType GetConversionWithTargetType(Expression expr, CancellationToken cancellationToken)
		{
			if (expr == null || expr.IsNull)
				return new ResolveVisitor.ConversionWithTargetType(Conversion.None, SpecialType.UnknownType);
			lock (resolveVisitor) {
				InitResolver();
				resolveVisitor.cancellationToken = cancellationToken;
				try {
					return resolveVisitor.GetConversionWithTargetType(expr);
				} finally {
					resolveVisitor.cancellationToken = CancellationToken.None;
				}
			}
		}
		
		/// <summary>
		/// Gets the expected type for the specified node. This is the type that a node is being converted to.
		/// </summary>
		public IType GetExpectedType(Expression expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetConversionWithTargetType(expr, cancellationToken).TargetType;
		}
		
		/// <summary>
		/// Gets the conversion that is being applied to the specified expression.
		/// </summary>
		public Conversion GetConversion(Expression expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetConversionWithTargetType(expr, cancellationToken).Conversion;
		}
		
		/// <summary>
		/// Gets whether the specified node is unresolvable.
		/// </summary>
		public static bool IsUnresolvableNode(AstNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (node.NodeType == NodeType.Token) {
				// Most tokens cannot be resolved, but there are a couple of special cases:
				if (node.Parent is QueryClause && node is Identifier) {
					return false;
				} else if (node.Role == Roles.Identifier) {
					return !(node.Parent is ForeachStatement || node.Parent is CatchClause);
				}
				return true;
			}
			return (node.NodeType == NodeType.Whitespace || node is ArraySpecifier);
		}
	}
}
