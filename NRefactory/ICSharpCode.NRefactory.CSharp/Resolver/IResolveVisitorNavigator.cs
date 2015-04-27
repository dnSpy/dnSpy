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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Allows controlling which nodes are resolved by the resolve visitor.
	/// </summary>
	/// <seealso cref="ResolveVisitor"/>
	public interface IResolveVisitorNavigator
	{
		/// <summary>
		/// Asks the navigator whether to scan, skip, or resolve a node.
		/// </summary>
		ResolveVisitorNavigationMode Scan(AstNode node);
		
		/// <summary>
		/// Notifies the navigator that a node was resolved.
		/// </summary>
		/// <param name="node">The node that was resolved</param>
		/// <param name="result">Resolve result</param>
		void Resolved(AstNode node, ResolveResult result);
		
		/// <summary>
		/// Notifies the navigator that an implicit conversion was applied.
		/// </summary>
		/// <param name="expression">The expression that was resolved.</param>
		/// <param name="result">The resolve result of the expression.</param>
		/// <param name="conversion">The conversion applied to the expressed.</param>
		/// <param name="targetType">The target type of the conversion.</param>
		void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType);
	}
	
	/// <summary>
	/// Represents the operation mode of the resolve visitor.
	/// </summary>
	/// <seealso cref="ResolveVisitor"/>
	public enum ResolveVisitorNavigationMode
	{
		/// <summary>
		/// Scan into the children of the current node, without resolving the current node.
		/// </summary>
		Scan,
		/// <summary>
		/// Skip the current node - do not scan into it; do not resolve it.
		/// </summary>
		Skip,
		/// <summary>
		/// Resolve the current node.
		/// Subnodes which are not required for resolving the current node
		/// will ask the navigator again whether they should be resolved.
		/// </summary>
		Resolve
	}
	
	sealed class ConstantModeResolveVisitorNavigator : IResolveVisitorNavigator
	{
		readonly ResolveVisitorNavigationMode mode;
		readonly IResolveVisitorNavigator targetForResolveCalls;
		
		public ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode mode, IResolveVisitorNavigator targetForResolveCalls)
		{
			this.mode = mode;
			this.targetForResolveCalls = targetForResolveCalls;
		}
		
		ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
		{
			return mode;
		}
		
		void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
		{
			if (targetForResolveCalls != null)
				targetForResolveCalls.Resolved(node, result);
		}
		
		void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
		{
			if (targetForResolveCalls != null)
				targetForResolveCalls.ProcessConversion(expression, result, conversion, targetType);
		}
	}
}
