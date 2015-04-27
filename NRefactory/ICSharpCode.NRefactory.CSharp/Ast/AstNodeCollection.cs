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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents the children of an AstNode that have a specific role.
	/// </summary>
	public class AstNodeCollection<T> : ICollection<T>
		#if NET_4_5
		, IReadOnlyCollection<T>
		#endif
		where T : AstNode
	{
		readonly AstNode node;
		readonly Role<T> role;
		
		public AstNodeCollection(AstNode node, Role<T> role)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (role == null)
				throw new ArgumentNullException("role");
			this.node = node;
			this.role = role;
		}
		
		public int Count {
			get {
				int count = 0;
				uint roleIndex = role.Index;
				for (AstNode cur = node.FirstChild; cur != null; cur = cur.NextSibling) {
					if (cur.RoleIndex == roleIndex)
						count++;
				}
				return count;
			}
		}
		
		public void Add(T element)
		{
			node.AddChild(element, role);
		}
		
		public void AddRange(IEnumerable<T> nodes)
		{
			// Evaluate 'nodes' first, since it might change when we add the new children
			// Example: collection.AddRange(collection);
			if (nodes != null) {
				foreach (T node in nodes.ToList())
					Add(node);
			}
		}
		
		public void AddRange(T[] nodes)
		{
			// Fast overload for arrays - we don't need to create a copy
			if (nodes != null) {
				foreach (T node in nodes)
					Add(node);
			}
		}
		
		public void ReplaceWith(IEnumerable<T> nodes)
		{
			// Evaluate 'nodes' first, since it might change when we call Clear()
			// Example: collection.ReplaceWith(collection);
			if (nodes != null)
				nodes = nodes.ToList();
			Clear();
			if (nodes != null) {
				foreach (T node in nodes)
					Add(node);
			}
		}
		
		public void MoveTo(ICollection<T> targetCollection)
		{
			if (targetCollection == null)
				throw new ArgumentNullException("targetCollection");
			foreach (T node in this) {
				node.Remove();
				targetCollection.Add(node);
			}
		}
		
		public bool Contains(T element)
		{
			return element != null && element.Parent == node && element.RoleIndex == role.Index;
		}
		
		public bool Remove(T element)
		{
			if (Contains(element)) {
				element.Remove();
				return true;
			} else {
				return false;
			}
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T item in this)
				array[arrayIndex++] = item;
		}
		
		public void Clear()
		{
			foreach (T item in this)
				item.Remove();
		}
		
		/// <summary>
		/// Returns the first element for which the predicate returns true,
		/// or the null node (AstNode with IsNull=true) if no such object is found.
		/// </summary>
		public T FirstOrNullObject(Func<T, bool> predicate = null)
		{
			foreach (T item in this)
				if (predicate == null || predicate(item))
					return item;
			return role.NullObject;
		}
		
		/// <summary>
		/// Returns the last element for which the predicate returns true,
		/// or the null node (AstNode with IsNull=true) if no such object is found.
		/// </summary>
		public T LastOrNullObject(Func<T, bool> predicate = null)
		{
			T result = role.NullObject;
			foreach (T item in this)
				if (predicate == null || predicate(item))
					result = item;
			return result;
		}
		
		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			uint roleIndex = role.Index;
			AstNode next;
			for (AstNode cur = node.FirstChild; cur != null; cur = next) {
				Debug.Assert(cur.Parent == node);
				// Remember next before yielding cur.
				// This allows removing/replacing nodes while iterating through the list.
				next = cur.NextSibling;
				if (cur.RoleIndex == roleIndex)
					yield return (T)cur;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		#region Equals and GetHashCode implementation
		public override int GetHashCode()
		{
			return node.GetHashCode() ^ role.GetHashCode();
		}
		
		public override bool Equals(object obj)
		{
			AstNodeCollection<T> other = obj as AstNodeCollection<T>;
			if (other == null)
				return false;
			return this.node == other.node && this.role == other.role;
		}
		#endregion
		
		internal bool DoMatch(AstNodeCollection<T> other, Match match)
		{
			return Pattern.DoMatchCollection(role, node.FirstChild, other.node.FirstChild, match);
		}
		
		public void InsertAfter(T existingItem, T newItem)
		{
			node.InsertChildAfter(existingItem, newItem, role);
		}
		
		public void InsertBefore(T existingItem, T newItem)
		{
			node.InsertChildBefore(existingItem, newItem, role);
		}
		
		/// <summary>
		/// Applies the <paramref name="visitor"/> to all nodes in this collection.
		/// </summary>
		public void AcceptVisitor(IAstVisitor visitor)
		{
			uint roleIndex = role.Index;
			AstNode next;
			for (AstNode cur = node.FirstChild; cur != null; cur = next) {
				Debug.Assert(cur.Parent == node);
				// Remember next before yielding cur.
				// This allows removing/replacing nodes while iterating through the list.
				next = cur.NextSibling;
				if (cur.RoleIndex == roleIndex)
					cur.AcceptVisitor(visitor);
			}
		}
	}
}
