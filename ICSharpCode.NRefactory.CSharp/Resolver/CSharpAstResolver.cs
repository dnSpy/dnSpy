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
		readonly CSharpParsedFile parsedFile;
		readonly ResolveVisitor resolveVisitor;
		bool resolverInitialized;
		
		/// <summary>
		/// Creates a new C# AST resolver.
		/// Use this overload if you are resolving within a complete C# file.
		/// </summary>
		/// <param name="compilation">The current compilation.</param>
		/// <param name="parsedFile">
		/// Result of the <see cref="TypeSystemConvertVisitor"/> for the file being passed. This is used for setting up the context on the resolver. The parsed file must be registered in the compilation.
		/// </param>
		/// <param name="compilationUnit">The compilation unit corresponding to the specified parsed file.</param>
		public CSharpAstResolver(ICompilation compilation, CompilationUnit compilationUnit, CSharpParsedFile parsedFile)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			this.initialResolverState = new CSharpResolver(compilation);
			this.rootNode = compilationUnit;
			this.parsedFile = parsedFile;
			this.resolveVisitor = new ResolveVisitor(initialResolverState, parsedFile);
		}
		
		/// <summary>
		/// Creates a new C# AST resolver.
		/// Use this overload if you are resolving code snippets (not necessarily complete files).
		/// </summary>
		/// <param name="resolver">The resolver state at the root node (to be more precise: outside the root node).</param>
		/// <param name="rootNode">The root node of the resolved tree.</param>
		/// <param name="parsedFile">The parsed file for the nodes being resolved. This parameter is used only
		/// when the root node is on the type level; it is not necessary when an expression is passed.
		/// This parameter may be null.</param>
		public CSharpAstResolver(CSharpResolver resolver, AstNode rootNode, CSharpParsedFile parsedFile = null)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			if (rootNode == null)
				throw new ArgumentNullException("rootNode");
			this.initialResolverState = resolver;
			this.rootNode = rootNode;
			this.parsedFile = parsedFile;
			this.resolveVisitor = new ResolveVisitor(initialResolverState, parsedFile);
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
		/// Gets the parsed file used by this CSharpAstResolver.
		/// Can return null.
		/// </summary>
		public CSharpParsedFile ParsedFile {
			get { return parsedFile; }
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
						Debug.Fail (node.GetType () + " resolved to null.", node.StartLocation + ":'" + node.GetText () + "'");
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
				throw new ArgumentNullException("expr");
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
		/// Gets the expected type for the specified node. This is the type being that a node is being converted to.
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
			return (node.NodeType == NodeType.Whitespace || node is ArraySpecifier || node is NamedArgumentExpression);
		}
	}
}
