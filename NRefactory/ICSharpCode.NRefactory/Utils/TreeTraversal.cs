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

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Static helper methods for traversing trees.
	/// </summary>
	public static class TreeTraversal
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
		
		/// <summary>
		/// Converts a tree data structure into a flat list by traversing it in post-order.
		/// </summary>
		/// <param name="root">The root element of the tree.</param>
		/// <param name="recursion">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in post-order.</returns>
		public static IEnumerable<T> PostOrder<T>(T root, Func<T, IEnumerable<T>> recursion)
		{
			return PostOrder(new T[] { root }, recursion);
		}
		
		/// <summary>
		/// Converts a tree data structure into a flat list by traversing it in post-order.
		/// </summary>
		/// <param name="input">The root elements of the forest.</param>
		/// <param name="recursion">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in post-order.</returns>
		public static IEnumerable<T> PostOrder<T>(IEnumerable<T> input, Func<T, IEnumerable<T>> recursion)
		{
			Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
			try {
				stack.Push(input.GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						T element = stack.Peek().Current;
						IEnumerable<T> children = recursion(element);
						if (children != null) {
							stack.Push(children.GetEnumerator());
						} else {
							yield return element;
						}
					}
					stack.Pop().Dispose();
					if (stack.Count > 0)
						yield return stack.Peek().Current;
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
	}
}
