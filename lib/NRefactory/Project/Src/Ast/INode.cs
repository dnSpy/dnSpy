// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Ast
{
	public interface INode
	{
		INode Parent { 
			get;
			set;
		}
		
		IList<INode> Children {
			get;
		}
		
		Location StartLocation {
			get;
			set;
		}
		
		Location EndLocation {
			get;
			set;
		}
		
		object UserData {
			get;
			set;
		}
		
		/// <summary>
		/// Visits all children
		/// </summary>
		/// <param name="visitor">The visitor to accept</param>
		/// <param name="data">Additional data for the visitor</param>
		/// <returns>The paremeter <paramref name="data"/></returns>
		object AcceptChildren(IAstVisitor visitor, object data);
		
		/// <summary>
		/// Accept the visitor
		/// </summary>
		/// <param name="visitor">The visitor to accept</param>
		/// <param name="data">Additional data for the visitor</param>
		/// <returns>The value the visitor returns after the visit</returns>
		object AcceptVisitor(IAstVisitor visitor, object data);
	}
	
	public static class INodeExtensionMethods
	{
		public static void Remove(this INode node)
		{
			node.Parent.Children.Remove(node);
		}
		
		public static INode Previous(this INode node)
		{
			if (node.Parent == null) return null;
			int myIndex = node.Parent.Children.IndexOf(node);
			int index = myIndex - 1;
			if (0 <= index && index < node.Parent.Children.Count) {
				return node.Parent.Children[index];
			} else {
				return null;
			}
		}
		
		public static INode Next(this INode node)
		{
			if (node.Parent == null) return null;
			int myIndex = node.Parent.Children.IndexOf(node);
			int index = myIndex + 1;
			if (0 <= index && index < node.Parent.Children.Count) {
				return node.Parent.Children[index];
			} else {
				return null;
			}
		}
		
		public static void ReplaceWith(this INode node, INode newNode)
		{
			int myIndex = node.Parent.Children.IndexOf(node);
			node.Parent.Children[myIndex] = newNode;
		}
	}
}
