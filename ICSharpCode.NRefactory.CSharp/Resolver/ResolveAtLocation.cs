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
using System.Threading;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Helper class that resolves the node at a specified location.
	/// Can be used for implementing tool tips.
	/// </summary>
	public static class ResolveAtLocation
	{
		public static ResolveResult Resolve(ITypeResolveContext context, CSharpParsedFile parsedFile, CompilationUnit cu, TextLocation location,
		                                    CancellationToken cancellationToken = default(CancellationToken))
		{
			AstNode node = cu.GetNodeAt(location);
			if (node == null)
				return null;
			AstNode resolvableNode;
			if (node is AstType) {
				resolvableNode = node;
				if (resolvableNode.Parent is ComposedType) {
					while (resolvableNode.Parent is ComposedType) 
						resolvableNode = resolvableNode.Parent;
					//node is preffered over the resolvable node. Which shouldn't be done in the case of nullables, arrays etc.
					node = resolvableNode;
				}
			} else if (node is Identifier) {
				resolvableNode = node.Parent;
			} else if (node.NodeType == NodeType.Token) {
				if (node.Parent is ConstructorInitializer) {
					resolvableNode = node.Parent;
				} else {
					return null;
				}
			} else {
				// don't resolve arbitrary nodes - we don't want to show tooltips for everything
				return null;
			}
			
			InvocationExpression parentInvocation = null;
			if ((resolvableNode is IdentifierExpression || resolvableNode is MemberReferenceExpression || resolvableNode is PointerReferenceExpression)) {
				// we also need to resolve the invocation
				parentInvocation = resolvableNode.Parent as InvocationExpression;
			}
			
			IResolveVisitorNavigator navigator;
			if (parentInvocation != null)
				navigator = new NodeListResolveVisitorNavigator(new[] { resolvableNode, parentInvocation });
			else
				navigator = new NodeListResolveVisitorNavigator(new[] { resolvableNode });
			
			using (var ctx = context.Synchronize()) {
				CSharpResolver resolver = new CSharpResolver(ctx, cancellationToken);
				ResolveVisitor v = new ResolveVisitor(resolver, parsedFile, navigator);
				v.Scan(cu);
				
				// Prefer the RR from the token itself, if it was assigned a ResolveResult
				// (this can happen with the identifiers in various nodes such as catch clauses or foreach statements)
				ResolveResult rr = v.GetResolveResult(node) ?? v.GetResolveResult(resolvableNode);
				if (rr is MethodGroupResolveResult && parentInvocation != null)
					return v.GetResolveResult(parentInvocation);
				else
					return rr;
			}
		}
	}
}
