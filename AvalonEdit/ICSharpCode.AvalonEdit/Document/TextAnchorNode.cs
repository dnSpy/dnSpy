// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// A TextAnchorNode is placed in the TextAnchorTree.
	/// It describes a section of text with a text anchor at the end of the section.
	/// A weak reference is used to refer to the TextAnchor. (to save memory, we derive from WeakReference instead of referencing it)
	/// </summary>
	sealed class TextAnchorNode : WeakReference
	{
		internal TextAnchorNode left, right, parent;
		internal bool color;
		internal int length;
		internal int totalLength; // totalLength = length + left.totalLength + right.totalLength
		
		public TextAnchorNode(TextAnchor anchor) : base(anchor)
		{
		}
		
		internal TextAnchorNode LeftMost {
			get {
				TextAnchorNode node = this;
				while (node.left != null)
					node = node.left;
				return node;
			}
		}
		
		internal TextAnchorNode RightMost {
			get {
				TextAnchorNode node = this;
				while (node.right != null)
					node = node.right;
				return node;
			}
		}
		
		/// <summary>
		/// Gets the inorder successor of the node.
		/// </summary>
		internal TextAnchorNode Successor {
			get {
				if (right != null) {
					return right.LeftMost;
				} else {
					TextAnchorNode node = this;
					TextAnchorNode oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// go up until we are coming out of a left subtree
					} while (node != null && node.right == oldNode);
					return node;
				}
			}
		}
		
		/// <summary>
		/// Gets the inorder predecessor of the node.
		/// </summary>
		internal TextAnchorNode Predecessor {
			get {
				if (left != null) {
					return left.RightMost;
				} else {
					TextAnchorNode node = this;
					TextAnchorNode oldNode;
					do {
						oldNode = node;
						node = node.parent;
						// go up until we are coming out of a right subtree
					} while (node != null && node.left == oldNode);
					return node;
				}
			}
		}
		
		public override string ToString()
		{
			return "[TextAnchorNode Length=" + length + " TotalLength=" + totalLength + " Target=" + Target + "]";
		}
	}
}
