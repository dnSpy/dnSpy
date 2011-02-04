// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

using System.Text;

namespace ICSharpCode.AvalonEdit.Utils
{
	// Class used to represent a node in the tree.
	// There are three types of nodes:
	// Concat nodes: height>0, left!=null, right!=null, contents==null
	// Leaf nodes: height==0, left==null, right==null, contents!=null
	// Function nodes: height==0, left==null, right==null, contents==null, are of type FunctionNode<T>
	
	[Serializable]
	class RopeNode<T>
	{
		internal const int NodeSize = 256;
		
		internal static readonly RopeNode<T> emptyRopeNode = new RopeNode<T> { isShared = true, contents = new T[RopeNode<T>.NodeSize] };
		
		// Fields for pointers to sub-nodes. Only non-null for concat nodes (height>=1)
		internal RopeNode<T> left, right;
		internal volatile bool isShared; // specifies whether this node is shared between multiple ropes
		// the total length of all text in this subtree
		internal int length;
		// the height of this subtree: 0 for leaf nodes; 1+max(left.height,right.height) for concat nodes
		internal byte height;
		
		// The character data. Only non-null for leaf nodes (height=0) that aren't function nodes.
		internal T[] contents;
		
		internal int Balance {
			get { return right.height - left.height; }
		}
		
		[Conditional("DATACONSISTENCYTEST")]
		internal void CheckInvariants()
		{
			if (height == 0) {
				Debug.Assert(left == null && right == null);
				if (contents == null) {
					Debug.Assert(this is FunctionNode<T>);
					Debug.Assert(length > 0);
					Debug.Assert(isShared);
				} else {
					Debug.Assert(contents != null && contents.Length == NodeSize);
					Debug.Assert(length >= 0 && length <= NodeSize);
				}
			} else {
				Debug.Assert(left != null && right != null);
				Debug.Assert(contents == null);
				Debug.Assert(length == left.length + right.length);
				Debug.Assert(height == 1 + Math.Max(left.height, right.height));
				Debug.Assert(Math.Abs(this.Balance) <= 1);
				
				// this is an additional invariant that forces the tree to combine small leafs to prevent excessive memory usage:
				Debug.Assert(length > NodeSize);
				// note that this invariant ensures that all nodes except for the empty rope's single node have at least length 1
				
				if (isShared)
					Debug.Assert(left.isShared && right.isShared);
				left.CheckInvariants();
				right.CheckInvariants();
			}
		}
		
		internal RopeNode<T> Clone()
		{
			if (height == 0) {
				// If a function node needs cloning, we'll evaluate it.
				if (contents == null)
					return GetContentNode().Clone();
				T[] newContents = new T[NodeSize];
				contents.CopyTo(newContents, 0);
				return new RopeNode<T> {
					length = this.length,
					contents = newContents
				};
			} else {
				return new RopeNode<T> {
					left = this.left,
					right = this.right,
					length = this.length,
					height = this.height
				};
			}
		}
		
		internal RopeNode<T> CloneIfShared()
		{
			if (isShared)
				return Clone();
			else
				return this;
		}
		
		internal void Publish()
		{
			if (!isShared) {
				if (left != null)
					left.Publish();
				if (right != null)
					right.Publish();
				// it's important that isShared=true is set at the end:
				// Publish() must not return until the whole subtree is marked as shared, even when
				// Publish() is called concurrently.
				isShared = true;
			}
		}
		
		internal static RopeNode<T> CreateFromArray(T[] arr, int index, int length)
		{
			if (length == 0) {
				return emptyRopeNode;
			}
			RopeNode<T> node = CreateNodes(length);
			return node.StoreElements(0, arr, index, length);
		}
		
		internal static RopeNode<T> CreateNodes(int totalLength)
		{
			int leafCount = (totalLength + NodeSize - 1) / NodeSize;
			return CreateNodes(leafCount, totalLength);
		}
		
		static RopeNode<T> CreateNodes(int leafCount, int totalLength)
		{
			Debug.Assert(leafCount > 0);
			Debug.Assert(totalLength > 0);
			RopeNode<T> result = new RopeNode<T>();
			result.length = totalLength;
			if (leafCount == 1) {
				result.contents = new T[NodeSize];
			} else {
				int rightSide = leafCount / 2;
				int leftSide = leafCount - rightSide;
				int leftLength = leftSide * NodeSize;
				result.left = CreateNodes(leftSide, leftLength);
				result.right = CreateNodes(rightSide, totalLength - leftLength);
				result.height = (byte)(1 + Math.Max(result.left.height, result.right.height));
			}
			return result;
		}
		
		/// <summary>
		/// Balances this node and recomputes the 'height' field.
		/// This method assumes that the children of this node are already balanced and have an up-to-date 'height' value.
		/// </summary>
		internal void Rebalance()
		{
			// Rebalance() shouldn't be called on shared nodes - it's only called after modifications!
			Debug.Assert(!isShared);
			// leaf nodes are always balanced (we don't use 'height' to detect leaf nodes here
			// because Balance is supposed to recompute the height).
			if (left == null)
				return;
			
			// ensure we didn't miss a MergeIfPossible step
			Debug.Assert(this.length > NodeSize);
			
			// We need to loop until it's balanced. Rotations might cause two small leaves to combine to a larger one,
			// which changes the height and might mean we need additional balancing steps.
			while (Math.Abs(this.Balance) > 1) {
				// AVL balancing
				// note: because we don't care about the identity of concat nodes, this works a little different than usual
				// tree rotations: in our implementation, the "this" node will stay at the top, only its children are rearranged
				if (this.Balance > 1) {
					if (right.Balance < 0) {
						right = right.CloneIfShared();
						right.RotateRight();
					}
					this.RotateLeft();
					// If 'this' was unbalanced by more than 2, we've shifted some of the inbalance to the left node; so rebalance that.
					this.left.Rebalance();
				} else if (this.Balance < -1) {
					if (left.Balance > 0) {
						left = left.CloneIfShared();
						left.RotateLeft();
					}
					this.RotateRight();
					// If 'this' was unbalanced by more than 2, we've shifted some of the inbalance to the right node; so rebalance that.
					this.right.Rebalance();
				}
			}
			
			Debug.Assert(Math.Abs(this.Balance) <= 1);
			this.height = (byte)(1 + Math.Max(left.height, right.height));
		}
		
		void RotateLeft()
		{
			Debug.Assert(!isShared);
			
			/* Rotate tree to the left
			 * 
			 *       this               this
			 *       /  \               /  \
			 *      A   right   ===>  left  C
			 *           / \          / \
			 *          B   C        A   B
			 */
			RopeNode<T> a = left;
			RopeNode<T> b = right.left;
			RopeNode<T> c = right.right;
			// reuse right concat node, if possible
			this.left = right.isShared ? new RopeNode<T>() : right;
			this.left.left = a;
			this.left.right = b;
			this.left.length = a.length + b.length;
			this.left.height = (byte)(1 + Math.Max(a.height, b.height));
			this.right = c;
			
			this.left.MergeIfPossible();
		}
		
		void RotateRight()
		{
			Debug.Assert(!isShared);
			
			/* Rotate tree to the right
			 * 
			 *       this             this
			 *       /  \             /  \
			 *     left  C   ===>    A  right
			 *     / \                   /  \
			 *    A   B                 B    C
			 */
			RopeNode<T> a = left.left;
			RopeNode<T> b = left.right;
			RopeNode<T> c = right;
			// reuse left concat node, if possible
			this.right = left.isShared ? new RopeNode<T>() : left;
			this.right.left = b;
			this.right.right = c;
			this.right.length = b.length + c.length;
			this.right.height = (byte)(1 + Math.Max(b.height, c.height));
			this.left = a;
			
			this.right.MergeIfPossible();
		}
		
		void MergeIfPossible()
		{
			Debug.Assert(!isShared);
			
			if (this.length <= NodeSize) {
				// Convert this concat node to leaf node.
				// We know left and right cannot be concat nodes (they would have merged already),
				// but they could be function nodes.
				this.height = 0;
				int lengthOnLeftSide = this.left.length;
				if (this.left.isShared) {
					this.contents = new T[NodeSize];
					left.CopyTo(0, this.contents, 0, lengthOnLeftSide);
				} else {
					// must be a leaf node: function nodes are always marked shared
					Debug.Assert(this.left.contents != null);
					// steal buffer from left side
					this.contents = this.left.contents;
					#if DEBUG
					// In debug builds, explicitly mark left node as 'damaged' - but no one else should be using it
					// because it's not shared.
					this.left.contents = Empty<T>.Array;
					#endif
				}
				this.left = null;
				right.CopyTo(0, this.contents, lengthOnLeftSide, this.right.length);
				this.right = null;
			}
		}
		
		/// <summary>
		/// Copies from the array to this node.
		/// </summary>
		internal RopeNode<T> StoreElements(int index, T[] array, int arrayIndex, int count)
		{
			RopeNode<T> result = this.CloneIfShared();
			// result cannot be function node after a call to Clone()
			if (result.height == 0) {
				// leaf node:
				Array.Copy(array, arrayIndex, result.contents, index, count);
			} else {
				// concat node:
				if (index + count <= result.left.length) {
					result.left = result.left.StoreElements(index, array, arrayIndex, count);
				} else if (index >= this.left.length) {
					result.right = result.right.StoreElements(index - result.left.length, array, arrayIndex, count);
				} else {
					int amountInLeft = result.left.length - index;
					result.left = result.left.StoreElements(index, array, arrayIndex, amountInLeft);
					result.right = result.right.StoreElements(0, array, arrayIndex + amountInLeft, count - amountInLeft);
				}
				result.Rebalance(); // tree layout might have changed if function nodes were replaced with their content
			}
			return result;
		}
		
		/// <summary>
		/// Copies from this node to the array.
		/// </summary>
		internal void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (height == 0) {
				if (this.contents == null) {
					// function node
					this.GetContentNode().CopyTo(index, array, arrayIndex, count);
				} else {
					// leaf node
					Array.Copy(this.contents, index, array, arrayIndex, count);
				}
			} else {
				// concat node
				if (index + count <= this.left.length) {
					this.left.CopyTo(index, array, arrayIndex, count);
				} else if (index >= this.left.length) {
					this.right.CopyTo(index - this.left.length, array, arrayIndex, count);
				} else {
					int amountInLeft = this.left.length - index;
					this.left.CopyTo(index, array, arrayIndex, amountInLeft);
					this.right.CopyTo(0, array, arrayIndex + amountInLeft, count - amountInLeft);
				}
			}
		}
		
		internal RopeNode<T> SetElement(int offset, T value)
		{
			RopeNode<T> result = CloneIfShared();
			// result of CloneIfShared() is leaf or concat node
			if (result.height == 0) {
				result.contents[offset] = value;
			} else {
				if (offset < result.left.length) {
					result.left = result.left.SetElement(offset, value);
				} else {
					result.right = result.right.SetElement(offset - result.left.length, value);
				}
				result.Rebalance(); // tree layout might have changed if function nodes were replaced with their content
			}
			return result;
		}
		
		internal static RopeNode<T> Concat(RopeNode<T> left, RopeNode<T> right)
		{
			if (left.length == 0)
				return right;
			if (right.length == 0)
				return left;
			
			if (left.length + right.length <= NodeSize) {
				left = left.CloneIfShared();
				// left is guaranteed to be leaf node after cloning:
				// - it cannot be function node (due to clone)
				// - it cannot be concat node (too short)
				right.CopyTo(0, left.contents, left.length, right.length);
				left.length += right.length;
				return left;
			} else {
				RopeNode<T> concatNode = new RopeNode<T>();
				concatNode.left = left;
				concatNode.right = right;
				concatNode.length = left.length + right.length;
				concatNode.Rebalance();
				return concatNode;
			}
		}
		
		/// <summary>
		/// Splits this leaf node at offset and returns a new node with the part of the text after offset.
		/// </summary>
		RopeNode<T> SplitAfter(int offset)
		{
			Debug.Assert(!isShared && height == 0 && contents != null);
			RopeNode<T> newPart = new RopeNode<T>();
			newPart.contents = new T[NodeSize];
			newPart.length = this.length - offset;
			Array.Copy(this.contents, offset, newPart.contents, 0, newPart.length);
			this.length = offset;
			return newPart;
		}
		
		internal RopeNode<T> Insert(int offset, RopeNode<T> newElements)
		{
			if (offset == 0) {
				return Concat(newElements, this);
			} else if (offset == this.length) {
				return Concat(this, newElements);
			}
			
			// first clone this node (converts function nodes to leaf or concat nodes)
			RopeNode<T> result = CloneIfShared();
			if (result.height == 0) {
				// leaf node: we'll need to split this node
				RopeNode<T> left = result;
				RopeNode<T> right = left.SplitAfter(offset);
				return Concat(Concat(left, newElements), right);
			} else {
				// concat node
				if (offset < result.left.length) {
					result.left = result.left.Insert(offset, newElements);
				} else {
					result.right = result.right.Insert(offset - result.left.length, newElements);
				}
				result.length += newElements.length;
				result.Rebalance();
				return result;
			}
		}
		
		internal RopeNode<T> Insert(int offset, T[] array, int arrayIndex, int count)
		{
			Debug.Assert(count > 0);
			
			if (this.length + count < RopeNode<char>.NodeSize) {
				RopeNode<T> result = CloneIfShared();
				// result must be leaf node (Clone never returns function nodes, too short for concat node)
				int lengthAfterOffset = result.length - offset;
				T[] resultContents = result.contents;
				for (int i = lengthAfterOffset; i >= 0; i--) {
					resultContents[i + offset + count] = resultContents[i + offset];
				}
				Array.Copy(array, arrayIndex, resultContents, offset, count);
				result.length += count;
				return result;
			} else if (height == 0) {
				// TODO: implement this more efficiently?
				return Insert(offset, CreateFromArray(array, arrayIndex, count));
			} else {
				// this is a concat node (both leafs and function nodes are handled by the case above)
				RopeNode<T> result = CloneIfShared();
				if (offset < result.left.length) {
					result.left = result.left.Insert(offset, array, arrayIndex, count);
				} else {
					result.right = result.right.Insert(offset - result.left.length, array, arrayIndex, count);
				}
				result.length += count;
				result.Rebalance();
				return result;
			}
		}
		
		internal RopeNode<T> RemoveRange(int index, int count)
		{
			Debug.Assert(count > 0);
			
			// produce empty node when one node is deleted completely
			if (index == 0 && count == this.length)
				return emptyRopeNode;
			
			int endIndex = index + count;
			RopeNode<T> result = CloneIfShared(); // convert function node to concat/leaf
			if (result.height == 0) {
				int remainingAfterEnd = result.length - endIndex;
				for (int i = 0; i < remainingAfterEnd; i++) {
					result.contents[index + i] = result.contents[endIndex + i];
				}
				result.length -= count;
			} else {
				if (endIndex <= result.left.length) {
					// deletion is only within the left part
					result.left = result.left.RemoveRange(index, count);
				} else if (index >= result.left.length) {
					// deletion is only within the right part
					result.right = result.right.RemoveRange(index - result.left.length, count);
				} else {
					// deletion overlaps both parts
					int deletionAmountOnLeftSide = result.left.length - index;
					result.left = result.left.RemoveRange(index, deletionAmountOnLeftSide);
					result.right = result.right.RemoveRange(0, count - deletionAmountOnLeftSide);
				}
				// The deletion might have introduced empty nodes. Those must be removed.
				if (result.left.length == 0)
					return result.right;
				if (result.right.length == 0)
					return result.left;
				
				result.length -= count;
				result.MergeIfPossible();
				result.Rebalance();
			}
			return result;
		}
		
		#region Debug Output
		#if DEBUG
		internal virtual void AppendTreeToString(StringBuilder b, int indent)
		{
			b.AppendLine(ToString());
			indent += 2;
			if (left != null) {
				b.Append(' ', indent);
				b.Append("L: ");
				left.AppendTreeToString(b, indent);
			}
			if (right != null) {
				b.Append(' ', indent);
				b.Append("R: ");
				right.AppendTreeToString(b, indent);
			}
		}
		
		public override string ToString()
		{
			if (contents != null) {
				char[] charContents = contents as char[];
				if (charContents != null)
					return "[Leaf length=" + length + ", isShared=" + isShared + ", text=\"" + new string(charContents, 0, length) + "\"]";
				else
					return "[Leaf length=" + length + ", isShared=" + isShared + "\"]";
			} else {
				return "[Concat length=" + length + ", isShared=" + isShared + ", height=" + height + ", Balance=" + this.Balance + "]";
			}
		}
		
		internal string GetTreeAsString()
		{
			StringBuilder b = new StringBuilder();
			AppendTreeToString(b, 0);
			return b.ToString();
		}
		#endif
		#endregion
		
		/// <summary>
		/// Gets the root node of the subtree from a lazily evaluated function node.
		/// Such nodes are always marked as shared.
		/// GetContentNode() will return either a Concat or Leaf node, never another FunctionNode.
		/// </summary>
		internal virtual RopeNode<T> GetContentNode()
		{
			throw new InvalidOperationException("Called GetContentNode() on non-FunctionNode.");
		}
	}
	
	sealed class FunctionNode<T> : RopeNode<T>
	{
		Func<Rope<T>> initializer;
		RopeNode<T> cachedResults;
		
		public FunctionNode(int length, Func<Rope<T>> initializer)
		{
			Debug.Assert(length > 0);
			Debug.Assert(initializer != null);
			
			this.length = length;
			this.initializer = initializer;
			// Function nodes are immediately shared, but cannot be cloned.
			// This ensures we evaluate every initializer only once.
			this.isShared = true;
		}
		
		internal override RopeNode<T> GetContentNode()
		{
			lock (this) {
				if (this.cachedResults == null) {
					if (this.initializer == null)
						throw new InvalidOperationException("Trying to load this node recursively; or: a previous call to a rope initializer failed.");
					Func<Rope<T>> initializerCopy = this.initializer;
					this.initializer = null;
					Rope<T> resultRope = initializerCopy();
					if (resultRope == null)
						throw new InvalidOperationException("Rope initializer returned null.");
					RopeNode<T> resultNode = resultRope.root;
					resultNode.Publish(); // result is shared between returned rope and the rope containing this function node
					if (resultNode.length != this.length)
						throw new InvalidOperationException("Rope initializer returned rope with incorrect length.");
					if (resultNode.height == 0 && resultNode.contents == null) {
						// ResultNode is another function node.
						// We want to guarantee that GetContentNode() never returns function nodes, so we have to
						// go down further in the tree.
						this.cachedResults = resultNode.GetContentNode();
					} else {
						this.cachedResults = resultNode;
					}
				}
				return this.cachedResults;
			}
		}
		
		#if DEBUG
		internal override void AppendTreeToString(StringBuilder b, int indent)
		{
			RopeNode<T> resultNode;
			lock (this) {
				b.AppendLine(ToString());
				resultNode = cachedResults;
			}
			indent += 2;
			if (resultNode != null) {
				b.Append(' ', indent);
				b.Append("C: ");
				resultNode.AppendTreeToString(b, indent);
			}
		}
		
		public override string ToString()
		{
			return "[FunctionNode length=" + length + " initializerRan=" + (initializer == null) + "]";
		}
		#endif
	}
}
