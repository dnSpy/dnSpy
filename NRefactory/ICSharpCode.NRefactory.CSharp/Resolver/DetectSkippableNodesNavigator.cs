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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// When an <see cref="IResolveVisitorNavigator"/> is searching for specific nodes
	/// (e.g. all IdentifierExpressions), it has to scan the whole syntax tree for those nodes.
	/// However, scanning in the ResolveVisitor is expensive (e.g. any lambda that is scanned must be resolved),
	/// so it makes sense to detect when a whole subtree is scan-only, and skip that tree instead.
	/// 
	/// The DetectSkippableNodesNavigator performs this job by running the input IResolveVisitorNavigator
	/// over the whole AST, and detecting subtrees that are scan-only, and replaces them with Skip.
	/// </summary>
	public sealed class DetectSkippableNodesNavigator : IResolveVisitorNavigator
	{
		readonly Dictionary<AstNode, ResolveVisitorNavigationMode> dict = new Dictionary<AstNode, ResolveVisitorNavigationMode>();
		IResolveVisitorNavigator navigator;
		
		public DetectSkippableNodesNavigator(IResolveVisitorNavigator navigator, AstNode root)
		{
			this.navigator = navigator;
			Init(root);
		}
		
		bool Init(AstNode node)
		{
			var mode = navigator.Scan(node);
			if (mode == ResolveVisitorNavigationMode.Skip)
				return false;
			
			bool needsResolve = (mode != ResolveVisitorNavigationMode.Scan);
			
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				needsResolve |= Init(child);
			}
			
			if (needsResolve) {
				// If this node or any child node needs resolving, store the mode in the dictionary.
				dict.Add(node, mode);
			}
			return needsResolve;
		}
		
		/// <inheritdoc/>
		public ResolveVisitorNavigationMode Scan(AstNode node)
		{
			ResolveVisitorNavigationMode mode;
			if (dict.TryGetValue(node, out mode)) {
				return mode;
			} else {
				return ResolveVisitorNavigationMode.Skip;
			}
		}
		
		/// <inheritdoc/>
		public void Resolved(AstNode node, ResolveResult result)
		{
			navigator.Resolved(node, result);
		}
		
		/// <inheritdoc/>
		public void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
		{
			navigator.ProcessConversion(expression, result, conversion, targetType);
		}
	}
}
