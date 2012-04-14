// 
// AstNode.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class AstNode : AbstractAnnotatable, ICSharpCode.NRefactory.TypeSystem.IFreezable, PatternMatching.INode
	{
		// the Root role must be available when creating the null nodes, so we can't put it in the Roles class
		internal static readonly Role<AstNode> RootRole = new Role<AstNode> ("Root");
		
		#region Null
		public static readonly AstNode Null = new NullAstNode ();
		
		sealed class NullAstNode : AstNode
		{
			public override NodeType NodeType {
				get {
					return NodeType.Unknown;
				}
			}
			
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return default (T);
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch (AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator AstNode (PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder (pattern) : null;
		}
		
		sealed class PatternPlaceholder : AstNode, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder (PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override NodeType NodeType {
				get { return NodeType.Pattern; }
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitPatternPlaceholder (this, child);
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitPatternPlaceholder (this, child);
			}

			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder (this, child, data);
			}
			
			protected internal override bool DoMatch (AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch (other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection (Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection (role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		AstNode parent;
		AstNode prevSibling;
		AstNode nextSibling;
		AstNode firstChild;
		AstNode lastChild;
		
		// Flags, from least significant to most significant bits:
		// - Role.RoleIndexBits: role index
		// - 1 bit: IsFrozen
		protected uint flags = RootRole.Index;
		// Derived classes may also use a few bits,
		// for example Identifier uses 1 bit for IsVerbatim
		
		const uint roleIndexMask = (1u << Role.RoleIndexBits) - 1;
		const uint frozenBit = 1u << Role.RoleIndexBits;
		protected const int AstNodeFlagsUsedBits = Role.RoleIndexBits + 1;
		
		protected AstNode()
		{
			if (IsNull)
				Freeze();
		}
		
		public bool IsFrozen {
			get { return (flags & frozenBit) != 0; }
		}
		
		public void Freeze()
		{
			if (!IsFrozen) {
				for (AstNode child = firstChild; child != null; child = child.nextSibling)
					child.Freeze();
				flags |= frozenBit;
			}
		}
		
		protected void ThrowIfFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("Cannot mutate frozen " + GetType().Name);
		}
		
		public abstract NodeType NodeType {
			get;
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		public virtual TextLocation StartLocation {
			get {
				var child = firstChild;
				if (child == null)
					return TextLocation.Empty;
				return child.StartLocation;
			}
		}
		
		public virtual TextLocation EndLocation {
			get {
				var child = lastChild;
				if (child == null)
					return TextLocation.Empty;
				return child.EndLocation;
			}
		}
		
		/// <summary>
		/// Gets the region from StartLocation to EndLocation for this node.
		/// The file name of the region is set based on the parent CompilationUnit's file name.
		/// If this node is not connected to a whole compilation, the file name will be null.
		/// </summary>
		public ICSharpCode.NRefactory.TypeSystem.DomRegion GetRegion()
		{
			var cu = (this.Ancestors.LastOrDefault() ?? this) as CompilationUnit;
			string fileName = (cu != null ? cu.FileName : null);
			return new ICSharpCode.NRefactory.TypeSystem.DomRegion(fileName, this.StartLocation, this.EndLocation);
		}
		
		public AstNode Parent {
			get { return parent; }
		}
		
		public Role Role {
			get {
				return Role.GetByIndex(flags & roleIndexMask);
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				if (!value.IsValid(this))
					throw new ArgumentException("This node is not valid in the new role.");
				ThrowIfFrozen();
				SetRole(value);
			}
		}
		
		void SetRole(Role role)
		{
			flags = (flags & ~roleIndexMask) | role.Index;
		}
		
		public AstNode NextSibling {
			get { return nextSibling; }
		}
		
		public AstNode PrevSibling {
			get { return prevSibling; }
		}
		
		public AstNode FirstChild {
			get { return firstChild; }
		}
		
		public AstNode LastChild {
			get { return lastChild; }
		}
		
		public bool HasChildren {
			get {
				return firstChild != null;
			}
		}
		
		public IEnumerable<AstNode> Children {
			get {
				AstNode next;
				for (AstNode cur = firstChild; cur != null; cur = next) {
					Debug.Assert (cur.parent == this);
					// Remember next before yielding cur.
					// This allows removing/replacing nodes while iterating through the list.
					next = cur.nextSibling;
					yield return cur;
				}
			}
		}
		
		/// <summary>
		/// Gets the ancestors of this node (excluding this node itself)
		/// </summary>
		public IEnumerable<AstNode> Ancestors {
			get {
				for (AstNode cur = parent; cur != null; cur = cur.parent) {
					yield return cur;
				}
			}
		}
		
		/// <summary>
		/// Gets the ancestors of this node (including this node itself)
		/// </summary>
		public IEnumerable<AstNode> AncestorsAndSelf {
			get {
				for (AstNode cur = this; cur != null; cur = cur.parent) {
					yield return cur;
				}
			}
		}
		
		/// <summary>
		/// Gets all descendants of this node (excluding this node itself).
		/// </summary>
		public IEnumerable<AstNode> Descendants {
			get {
				return Utils.TreeTraversal.PreOrder (this.Children, n => n.Children);
			}
		}
		
		/// <summary>
		/// Gets all descendants of this node (including this node itself).
		/// </summary>
		public IEnumerable<AstNode> DescendantsAndSelf {
			get {
				return Utils.TreeTraversal.PreOrder (this, n => n.Children);
			}
		}
		
		/// <summary>
		/// Gets the first child with the specified role.
		/// Returns the role's null object if the child is not found.
		/// </summary>
		public T GetChildByRole<T>(Role<T> role) where T : AstNode
		{
			if (role == null)
				throw new ArgumentNullException ("role");
			uint roleIndex = role.Index;
			for (var cur = firstChild; cur != null; cur = cur.nextSibling) {
				if ((cur.flags & roleIndexMask) == roleIndex)
					return (T)cur;
			}
			return role.NullObject;
		}
		
		public T GetParent<T>() where T : AstNode
		{
			return Ancestors.OfType<T>().FirstOrDefault();
		}
				
		public AstNodeCollection<T> GetChildrenByRole<T> (Role<T> role) where T : AstNode
		{
			return new AstNodeCollection<T> (this, role);
		}
		
		protected void SetChildByRole<T> (Role<T> role, T newChild) where T : AstNode
		{
			AstNode oldChild = GetChildByRole (role);
			if (oldChild.IsNull)
				AddChild (newChild, role);
			else
				oldChild.ReplaceWith (newChild);
		}
		
		public void AddChild<T> (T child, Role<T> role) where T : AstNode
		{
			if (role == null)
				throw new ArgumentNullException ("role");
			if (child == null || child.IsNull)
				return;
			ThrowIfFrozen();
			if (child.parent != null)
				throw new ArgumentException ("Node is already used in another tree.", "child");
			if (child.IsFrozen)
				throw new ArgumentException ("Cannot add a frozen node.", "child");
			AddChildUnsafe (child, role);
		}
		
		/// <summary>
		/// Adds a child without performing any safety checks.
		/// </summary>
		void AddChildUnsafe (AstNode child, Role role)
		{
			child.parent = this;
			child.SetRole(role);
			if (firstChild == null) {
				lastChild = firstChild = child;
			} else {
				lastChild.nextSibling = child;
				child.prevSibling = lastChild;
				lastChild = child;
			}
		}

		public void InsertChildsBefore<T>(AstNode nextSibling, Role<T> role, params T[] child) where T : AstNode
		{
			foreach (var cur in child) {
				InsertChildBefore(nextSibling, cur, role);
			}
		}
		
		public void InsertChildBefore<T> (AstNode nextSibling, T child, Role<T> role) where T : AstNode
		{
			if (role == null)
				throw new ArgumentNullException ("role");
			if (nextSibling == null || nextSibling.IsNull) {
				AddChild (child, role);
				return;
			}
			
			if (child == null || child.IsNull)
				return;
			ThrowIfFrozen();
			if (child.parent != null)
				throw new ArgumentException ("Node is already used in another tree.", "child");
			if (child.IsFrozen)
				throw new ArgumentException ("Cannot add a frozen node.", "child");
			if (nextSibling.parent != this)
				throw new ArgumentException ("NextSibling is not a child of this node.", "nextSibling");
			// No need to test for "Cannot add children to null nodes",
			// as there isn't any valid nextSibling in null nodes.
			InsertChildBeforeUnsafe (nextSibling, child, role);
		}
		
		void InsertChildBeforeUnsafe (AstNode nextSibling, AstNode child, Role role)
		{
			child.parent = this;
			child.SetRole(role);
			child.nextSibling = nextSibling;
			child.prevSibling = nextSibling.prevSibling;
			
			if (nextSibling.prevSibling != null) {
				Debug.Assert (nextSibling.prevSibling.nextSibling == nextSibling);
				nextSibling.prevSibling.nextSibling = child;
			} else {
				Debug.Assert (firstChild == nextSibling);
				firstChild = child;
			}
			nextSibling.prevSibling = child;
		}
		
		public void InsertChildAfter<T> (AstNode prevSibling, T child, Role<T> role) where T : AstNode
		{
			InsertChildBefore ((prevSibling == null || prevSibling.IsNull) ? firstChild : prevSibling.nextSibling, child, role);
		}
		
		/// <summary>
		/// Removes this node from its parent.
		/// </summary>
		public void Remove ()
		{
			if (parent != null) {
				ThrowIfFrozen();
				if (prevSibling != null) {
					Debug.Assert (prevSibling.nextSibling == this);
					prevSibling.nextSibling = nextSibling;
				} else {
					Debug.Assert (parent.firstChild == this);
					parent.firstChild = nextSibling;
				}
				if (nextSibling != null) {
					Debug.Assert (nextSibling.prevSibling == this);
					nextSibling.prevSibling = prevSibling;
				} else {
					Debug.Assert (parent.lastChild == this);
					parent.lastChild = prevSibling;
				}
				parent = null;
				prevSibling = null;
				nextSibling = null;
			}
		}
		
		/// <summary>
		/// Replaces this node with the new node.
		/// </summary>
		public void ReplaceWith (AstNode newNode)
		{
			if (newNode == null || newNode.IsNull) {
				Remove ();
				return;
			}
			if (newNode == this)
				return; // nothing to do...
			if (parent == null) {
				throw new InvalidOperationException (this.IsNull ? "Cannot replace the null nodes" : "Cannot replace the root node");
			}
			ThrowIfFrozen();
			// Because this method doesn't statically check the new node's type with the role,
			// we perform a runtime test:
			if (!this.Role.IsValid (newNode)) {
				throw new ArgumentException (string.Format ("The new node '{0}' is not valid in the role {1}", newNode.GetType ().Name, this.Role.ToString ()), "newNode");
			}
			if (newNode.parent != null) {
				// newNode is used within this tree?
				if (newNode.Ancestors.Contains (this)) {
					// e.g. "parenthesizedExpr.ReplaceWith(parenthesizedExpr.Expression);"
					// enable automatic removal
					newNode.Remove ();
				} else {
					throw new ArgumentException ("Node is already used in another tree.", "newNode");
				}
			}
			if (newNode.IsFrozen)
				throw new ArgumentException ("Cannot add a frozen node.", "newNode");
			
			newNode.parent = parent;
			newNode.SetRole(this.Role);
			newNode.prevSibling = prevSibling;
			newNode.nextSibling = nextSibling;
			if (parent != null) {
				if (prevSibling != null) {
					Debug.Assert (prevSibling.nextSibling == this);
					prevSibling.nextSibling = newNode;
				} else {
					Debug.Assert (parent.firstChild == this);
					parent.firstChild = newNode;
				}
				if (nextSibling != null) {
					Debug.Assert (nextSibling.prevSibling == this);
					nextSibling.prevSibling = newNode;
				} else {
					Debug.Assert (parent.lastChild == this);
					parent.lastChild = newNode;
				}
				parent = null;
				prevSibling = null;
				nextSibling = null;
			}
		}
		
		public AstNode ReplaceWith (Func<AstNode, AstNode> replaceFunction)
		{
			if (replaceFunction == null)
				throw new ArgumentNullException ("replaceFunction");
			if (parent == null) {
				throw new InvalidOperationException (this.IsNull ? "Cannot replace the null nodes" : "Cannot replace the root node");
			}
			AstNode oldParent = parent;
			AstNode oldSuccessor = nextSibling;
			Role oldRole = this.Role;
			Remove ();
			AstNode replacement = replaceFunction (this);
			if (oldSuccessor != null && oldSuccessor.parent != oldParent)
				throw new InvalidOperationException ("replace function changed nextSibling of node being replaced?");
			if (!(replacement == null || replacement.IsNull)) {
				if (replacement.parent != null)
					throw new InvalidOperationException ("replace function must return the root of a tree");
				if (!oldRole.IsValid (replacement)) {
					throw new InvalidOperationException (string.Format ("The new node '{0}' is not valid in the role {1}", replacement.GetType ().Name, oldRole.ToString ()));
				}
				
				if (oldSuccessor != null)
					oldParent.InsertChildBeforeUnsafe (oldSuccessor, replacement, oldRole);
				else
					oldParent.AddChildUnsafe (replacement, oldRole);
			}
			return replacement;
		}
		
		/// <summary>
		/// Clones the whole subtree starting at this AST node.
		/// </summary>
		/// <remarks>Annotations are copied over to the new nodes; and any annotations implementing ICloneable will be cloned.</remarks>
		public AstNode Clone ()
		{
			AstNode copy = (AstNode)MemberwiseClone ();
			// First, reset the shallow pointer copies
			copy.parent = null;
			copy.firstChild = null;
			copy.lastChild = null;
			copy.prevSibling = null;
			copy.nextSibling = null;
			copy.flags &= ~frozenBit; // unfreeze the copy
			
			// Then perform a deep copy:
			for (AstNode cur = firstChild; cur != null; cur = cur.nextSibling) {
				copy.AddChildUnsafe (cur.Clone (), cur.Role);
			}
			
			// Finally, clone the annotation, if necessary
			copy.CloneAnnotations();
			
			return copy;
		}
		
		public abstract void AcceptVisitor (IAstVisitor visitor);
		
		public abstract T AcceptVisitor<T> (IAstVisitor<T> visitor);
		
		public abstract S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data);
		
		#region Pattern Matching
		protected static bool MatchString (string pattern, string text)
		{
			return PatternMatching.Pattern.MatchString(pattern, text);
		}
		
		protected internal abstract bool DoMatch (AstNode other, PatternMatching.Match match);
		
		bool PatternMatching.INode.DoMatch (PatternMatching.INode other, PatternMatching.Match match)
		{
			AstNode o = other as AstNode;
			// try matching if other is null, or if other is an AstNode
			return (other == null || o != null) && DoMatch (o, match);
		}
		
		bool PatternMatching.INode.DoMatchCollection (Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
		{
			AstNode o = pos as AstNode;
			return (pos == null || o != null) && DoMatch (o, match);
		}
		
		PatternMatching.INode PatternMatching.INode.NextSibling {
			get { return nextSibling; }
		}
		
		PatternMatching.INode PatternMatching.INode.FirstChild {
			get { return firstChild; }
		}

		#endregion
		
		public AstNode GetNextNode ()
		{
			if (NextSibling != null)
				return NextSibling;
			if (Parent != null)
				return Parent.GetNextNode ();
			return null;
		}

		public AstNode GetPrevNode ()
		{
			if (PrevSibling != null)
				return PrevSibling;
			if (Parent != null)
				return Parent.GetPrevNode ();
			return null;
		}
		// filters all non c# nodes (comments, white spaces or pre processor directives)
		public AstNode GetCSharpNodeBefore (AstNode node)
		{
			var n = node.PrevSibling;
			while (n != null) {
				if (n.Role != Roles.Comment)
					return n;
				n = n.GetPrevNode ();
			}
			return null;
		}
		
		#region GetNodeAt
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt (int line, int column, Predicate<AstNode> pred = null)
		{
			return GetNodeAt (new TextLocation (line, column), pred);
		}
		
		/// <summary>
		/// Gets the node specified by pred at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public AstNode GetNodeAt (TextLocation location, Predicate<AstNode> pred = null)
		{
			AstNode result = null;
			AstNode node = this;
			while (node.FirstChild != null) {
				var child = node.FirstChild;
				while (child != null) {
					if (child.StartLocation <= location && location < child.EndLocation) {
						if (pred == null || pred (child))
							result = child;
						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return result;
		}
		
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T> (int line, int column) where T : AstNode
		{
			return GetNodeAt<T> (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Gets the node specified by T at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End exclusive)
		/// </summary>
		public T GetNodeAt<T> (TextLocation location) where T : AstNode
		{
			T result = null;
			AstNode node = this;
			while (node.FirstChild != null) {
				var child = node.FirstChild;
				while (child != null) {
					if (child.StartLocation <= location && location < child.EndLocation) {
						if (child is T)
							result = (T)child;
						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return result;
		}

		#endregion

		#region GetAdjacentNodeAt
		/// <summary>
		/// Gets the node specified by pred at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End inclusive)
		/// </summary>
		public AstNode GetAdjacentNodeAt(int line, int column, Predicate<AstNode> pred = null)
		{
			return GetAdjacentNodeAt (new TextLocation (line, column), pred);
		}
		
		/// <summary>
		/// Gets the node specified by pred at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End inclusive)
		/// </summary>
		public AstNode GetAdjacentNodeAt (TextLocation location, Predicate<AstNode> pred = null)
		{
			AstNode result = null;
			AstNode node = this;
			while (node.FirstChild != null) {
				var child = node.FirstChild;
				while (child != null) {
					if (child.StartLocation <= location && location <= child.EndLocation) {
						if (pred == null || pred (child))
							result = child;
						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return result;
		}
		
		/// <summary>
		/// Gets the node specified by T at the location line, column. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End inclusive)
		/// </summary>
		public T GetAdjacentNodeAt<T>(int line, int column) where T : AstNode
		{
			return GetAdjacentNodeAt<T> (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Gets the node specified by T at location. This is useful for getting a specific node from the tree. For example searching
		/// the current method declaration.
		/// (End inclusive)
		/// </summary>
		public T GetAdjacentNodeAt<T> (TextLocation location) where T : AstNode
		{
			T result = null;
			AstNode node = this;
			while (node.FirstChild != null) {
				var child = node.FirstChild;
				while (child != null) {
					if (child.StartLocation <= location && location < child.EndLocation) {
						if (child is T)
							result = (T)child;
						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return result;
		}
		#endregion


		/// <summary>
		/// Gets the node that fully contains the range from startLocation to endLocation.
		/// </summary>
		public AstNode GetNodeContaining(TextLocation startLocation, TextLocation endLocation)
		{
			for (AstNode child = firstChild; child != null; child = child.nextSibling) {
				if (child.StartLocation <= startLocation && endLocation <= child.EndLocation)
					return child.GetNodeContaining(startLocation, endLocation);
			}
			return this;
		}
		
		public IEnumerable<AstNode> GetNodesBetween (int startLine, int startColumn, int endLine, int endColumn)
		{
			return GetNodesBetween (new TextLocation (startLine, startColumn), new TextLocation (endLine, endColumn));
		}
		
		public IEnumerable<AstNode> GetNodesBetween (TextLocation start, TextLocation end)
		{
			AstNode node = this;
			while (node != null) {
				AstNode next;
				if (start <= node.StartLocation && node.EndLocation <= end) {
					// Remember next before yielding node.
					// This allows iteration to continue when the caller removes/replaces the node.
					next = node.NextSibling;
					yield return node;
				} else {
					if (node.EndLocation <= start) {
						next = node.NextSibling;
					} else {
						next = node.FirstChild;
					}
				}
				
				if (next != null && next.StartLocation > end)
					yield break;
				node = next;
			}
		}
		
		/// <summary>
		/// Gets the node as formatted C# output.
		/// </summary>
		/// <param name='formattingOptions'>
		/// Formatting options.
		/// </param>
		public virtual string GetText (CSharpFormattingOptions formattingOptions = null)
		{
			if (IsNull)
				return "";
			var w = new StringWriter ();
			AcceptVisitor (new CSharpOutputVisitor (w, formattingOptions ?? FormattingOptionsFactory.CreateMono ()));
			return w.ToString ();
		}
		
		/// <summary>
		/// Returns true, if the given coordinates (line, column) are in the node.
		/// </summary>
		/// <returns>
		/// True, if the given coordinates are between StartLocation and EndLocation (exclusive); otherwise, false.
		/// </returns>
		public bool Contains (int line, int column)
		{
			return Contains (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Returns true, if the given coordinates are in the node.
		/// </summary>
		/// <returns>
		/// True, if location is between StartLocation and EndLocation (exclusive); otherwise, false.
		/// </returns>
		public bool Contains (TextLocation location)
		{
			return this.StartLocation <= location && location < this.EndLocation;
		}
		
		/// <summary>
		/// Returns true, if the given coordinates (line, column) are in the node.
		/// </summary>
		/// <returns>
		/// True, if the given coordinates are between StartLocation and EndLocation (inclusive); otherwise, false.
		/// </returns>
		public bool IsInside (int line, int column)
		{
			return IsInside (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Returns true, if the given coordinates are in the node.
		/// </summary>
		/// <returns>
		/// True, if location is between StartLocation and EndLocation (inclusive); otherwise, false.
		/// </returns>
		public bool IsInside (TextLocation location)
		{
			return this.StartLocation <= location && location <= this.EndLocation;
		}
		
		public override void AddAnnotation (object annotation)
		{
			if (this.IsNull)
				throw new InvalidOperationException ("Cannot add annotations to the null node");
			base.AddAnnotation (annotation);
		}
		
		internal string DebugToString()
		{
			if (IsNull)
				return "Null";
			string text = GetText();
			text = text.TrimEnd().Replace("\t", "").Replace(Environment.NewLine, " ");
			if (text.Length > 100)
				return text.Substring(0, 97) + "...";
			else
				return text;
		}
	}
}
