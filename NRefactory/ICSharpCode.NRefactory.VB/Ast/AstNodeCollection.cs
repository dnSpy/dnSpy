// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.VB
{
	/// <summary>
	/// Represents the children of an AstNode that have a specific role.
	/// </summary>
	public class AstNodeCollection<T> : ICollection<T> where T : AstNode
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
				for (AstNode cur = node.FirstChild; cur != null; cur = cur.NextSibling) {
					if (cur.Role == role)
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
			foreach (T node in nodes)
				Add(node);
		}
		
		public void MoveTo(ICollection<T> targetCollection)
		{
			foreach (T node in this) {
				node.Remove();
				targetCollection.Add(node);
			}
		}
		
		public bool Contains(T element)
		{
			return element != null && element.Parent == node && element.Role == role;
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
		
		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			AstNode next;
			for (AstNode cur = node.FirstChild; cur != null; cur = next) {
				Debug.Assert(cur.Parent == node);
				// Remember next before yielding cur.
				// This allows removing/replacing nodes while iterating through the list.
				next = cur.NextSibling;
				if (cur.Role == role)
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
			// TODO : does not compile (ask Daniel)!
//			Stack<AstNode> patternStack = new Stack<AstNode>();
//			Stack<Pattern.PossibleMatch> stack = new Stack<Pattern.PossibleMatch>();
//			patternStack.Push(this.node.FirstChild);
//			stack.Push(new Pattern.PossibleMatch(other.node.FirstChild, match.CheckPoint()));
//			while (stack.Count > 0) {
//				AstNode cur1 = patternStack.Pop();
//				AstNode cur2 = stack.Peek().NextOther;
//				match.RestoreCheckPoint(stack.Pop().Checkpoint);
//				bool success = true;
//				while (cur1 != null && success) {
//					while (cur1 != null && cur1.Role != role)
//						cur1 = cur1.NextSibling;
//					while (cur2 != null && cur2.Role != role)
//						cur2 = cur2.NextSibling;
//					if (cur1 == null)
//						break;
//					
//					Debug.Assert(stack.Count == patternStack.Count);
//					success = cur1.DoMatchCollection(role, cur2, match, stack);
//					Debug.Assert(stack.Count >= patternStack.Count);
//					while (stack.Count > patternStack.Count)
//						patternStack.Push(cur1.NextSibling);
//					
//					cur1 = cur1.NextSibling;
//					if (cur2 != null)
//						cur2 = cur2.NextSibling;
//				}
//				while (cur2 != null && cur2.Role != role)
//					cur2 = cur2.NextSibling;
//				if (success && cur2 == null)
//					return true;
//			}
//			return false;
			throw new NotImplementedException();
		}
		
		public void InsertAfter(T existingItem, T newItem)
		{
			node.InsertChildAfter(existingItem, newItem, role);
		}
		
		public void InsertBefore(T existingItem, T newItem)
		{
			node.InsertChildBefore(existingItem, newItem, role);
		}
	}
}
