// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Allows controlling which nodes are resolved by the resolve visitor.
	/// </summary>
	/// <seealso cref="ResolveVisitor"/>
	public interface IResolveVisitorNavigator
	{
		ResolveVisitorNavigationMode Scan(AstNode node);
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
		/// Resolve the current node; but only scan subnodes which are not required for resolving the current node.
		/// </summary>
		Resolve,
		/// <summary>
		/// Resolves all nodes in the current subtree.
		/// </summary>
		ResolveAll
	}
	
	sealed class ConstantModeResolveVisitorNavigator : IResolveVisitorNavigator
	{
		ResolveVisitorNavigationMode mode;
		
		public static readonly IResolveVisitorNavigator Skip = new ConstantModeResolveVisitorNavigator { mode = ResolveVisitorNavigationMode.Skip };
		
		ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
		{
			return mode;
		}
	}
}
