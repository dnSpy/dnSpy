// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.TreeView
{
	/// <summary>
	/// Static helper methods for traversing trees.
	/// </summary>
	static class TreeTraversal
	{
		/// <summary>
		/// Converts a tree data structure into a flat list by traversing it in pre-order.
		/// </summary>
		/// <param name="root">The root element of the tree.</param>
		/// <param name="recursion">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in pre-order.</returns>
		public static IEnumerable<T> PreOrder<T>(T root, Func<T, IEnumerable<T>> recursion)
		{
			return PreOrder(new T[] { root }, recursion);
		}
		
		/// <summary>
		/// Converts a tree data structure into a flat list by traversing it in pre-order.
		/// </summary>
		/// <param name="input">The root elements of the forest.</param>
		/// <param name="recursion">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in pre-order.</returns>
		public static IEnumerable<T> PreOrder<T>(IEnumerable<T> input, Func<T, IEnumerable<T>> recursion)
		{
			Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
			try {
				stack.Push(input.GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						T element = stack.Peek().Current;
						yield return element;
						IEnumerable<T> children = recursion(element);
						if (children != null) {
							stack.Push(children.GetEnumerator());
						}
					}
					stack.Pop().Dispose();
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
	}
}
