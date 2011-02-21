// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp
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
				var e = GetEnumerator();
				int count = 0;
				while (e.MoveNext())
					count++;
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
		public override bool Equals(object obj)
		{
			if (obj is AstNodeCollection<T>) {
				return ((AstNodeCollection<T>)obj) == this;
			} else {
				return false;
			}
		}
		
		public override int GetHashCode()
		{
			return node.GetHashCode() ^ role.GetHashCode();
		}
		
		public static bool operator ==(AstNodeCollection<T> left, AstNodeCollection<T> right)
		{
			return left.role == right.role && left.node == right.node;
		}
		
		public static bool operator !=(AstNodeCollection<T> left, AstNodeCollection<T> right)
		{
			return !(left.role == right.role && left.node == right.node);
		}
		#endregion
		
		internal bool DoMatch(AstNodeCollection<T> other, Match match)
		{
			Stack<AstNode> patternStack = new Stack<AstNode>();
			Stack<Pattern.PossibleMatch> stack = new Stack<Pattern.PossibleMatch>();
			patternStack.Push(this.node.FirstChild);
			stack.Push(new Pattern.PossibleMatch(other.node.FirstChild, match.CheckPoint()));
			while (stack.Count > 0) {
				AstNode cur1 = patternStack.Pop();
				AstNode cur2 = stack.Peek().NextOther;
				match.RestoreCheckPoint(stack.Pop().Checkpoint);
				bool success = true;
				while (cur1 != null && success) {
					while (cur1 != null && cur1.Role != role)
						cur1 = cur1.NextSibling;
					while (cur2 != null && cur2.Role != role)
						cur2 = cur2.NextSibling;
					if (cur1 == null)
						break;
					
					Pattern pattern = cur1 as Pattern;
					if (pattern == null && cur1.NodeType == NodeType.Pattern)
						pattern = cur1.GetChildByRole(TypePlaceholder.ChildRole) as Pattern;
					if (pattern != null) {
						Debug.Assert(stack.Count == patternStack.Count);
						success = pattern.DoMatchCollection(role, cur2, match, stack);
						Debug.Assert(stack.Count >= patternStack.Count);
						while (stack.Count > patternStack.Count)
							patternStack.Push(cur1.NextSibling);
					} else {
						success = cur1.DoMatch(cur2, match);
					}
					cur1 = cur1.NextSibling;
					if (cur2 != null)
						cur2 = cur2.NextSibling;
				}
				while (cur2 != null && cur2.Role != role)
					cur2 = cur2.NextSibling;
				if (success && cur2 == null)
					return true;
			}
			return false;
		}
	}
}
