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
	public class CSharpAstResolver
	{
		readonly CSharpResolver initialResolverState;
		readonly AstNode rootNode;
		readonly CSharpParsedFile parsedFile;
		ResolveVisitor resolveVisitor;
		
		/// <summary>
		/// Creates a new C# AST resolver.
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
		}
		
		/// <summary>
		/// Creates a new C# AST resolver.
		/// </summary>
		/// <param name="resolver">The resolver state at the root node.</param>
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
		}
		
		/// <summary>
		/// Gets the type resolve context for the root resolver.
		/// </summary>
		public CSharpTypeResolveContext TypeResolveContext {
			get { return initialResolverState.CurrentTypeResolveContext; }
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
			if (resolveVisitor != null)
				throw new InvalidOperationException("Applying a navigator is only valid as the first operation on the CSharpAstResolver.");
			resolveVisitor = new ResolveVisitor(initialResolverState, parsedFile, navigator);
			lock (resolveVisitor)
				resolveVisitor.Scan(rootNode);
		}
		
		/// <summary>
		/// Resolves the specified node.
		/// </summary>
		public ResolveResult Resolve(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (node == null || node.IsNull)
				return ErrorResolveResult.UnknownError;
			InitResolver(node);
			lock (resolveVisitor) {
				ResolveResult rr = resolveVisitor.GetResolveResult(node);
				Debug.Assert(rr != null);
				return rr;
			}
		}
		
		void InitResolver(AstNode firstNodeToResolve)
		{
			if (resolveVisitor == null) {
				resolveVisitor = new ResolveVisitor(initialResolverState, parsedFile, new NodeListResolveVisitorNavigator(firstNodeToResolve));
				resolveVisitor.Scan(rootNode);
			}
		}
		
		public CSharpResolver GetResolverStateBefore(AstNode node, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (node == null || node.IsNull)
				throw new ArgumentNullException("node");
			InitResolver(node);
			lock (resolveVisitor) {
				CSharpResolver resolver = resolveVisitor.GetResolverStateBefore(node);
				Debug.Assert(resolver != null);
				return resolver;
			}
		}
		
		/// <summary>
		/// Gets the expected type for the specified node. This is the type being that a node is being converted to.
		/// </summary>
		public IType GetExpectedType(Expression expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Gets the conversion that is being applied to the specified expression.
		/// </summary>
		public Conversion GetConversion(Expression expr, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Gets whether the specified node is unresolvable.
		/// </summary>
		public static bool IsUnresolvableNode(AstNode node)
		{
			return (node.NodeType == NodeType.Whitespace || node is ArraySpecifier || node is NamedArgumentExpression);
		}
	}
}
