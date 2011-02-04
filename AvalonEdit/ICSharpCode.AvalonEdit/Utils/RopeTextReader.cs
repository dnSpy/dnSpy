// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// TextReader implementation that reads text from a rope.
	/// </summary>
	public sealed class RopeTextReader : TextReader
	{
		Stack<RopeNode<char>> stack = new Stack<RopeNode<char>>();
		RopeNode<char> currentNode;
		int indexInsideNode;
		
		/// <summary>
		/// Creates a new RopeTextReader.
		/// Internally, this method creates a Clone of the rope; so the text reader will always read through the old
		/// version of the rope if it is modified. <seealso cref="Rope{T}.Clone()"/>
		/// </summary>
		public RopeTextReader(Rope<char> rope)
		{
			if (rope == null)
				throw new ArgumentNullException("rope");
			
			// We force the user to iterate through a clone of the rope to keep the API contract of RopeTextReader simple
			// (what happens when a rope is modified while iterating through it?)
			rope.root.Publish();
			
			// special case for the empty rope:
			// leave currentNode initialized to null (RopeTextReader doesn't support empty nodes)
			if (rope.Length != 0) {
				currentNode = rope.root;
				GoToLeftMostLeaf();
			}
		}
		
		void GoToLeftMostLeaf()
		{
			while (currentNode.contents == null) {
				if (currentNode.height == 0) {
					// this is a function node - move to its contained rope
					currentNode = currentNode.GetContentNode();
					continue;
				}
				Debug.Assert(currentNode.right != null);
				stack.Push(currentNode.right);
				currentNode = currentNode.left;
			}
			Debug.Assert(currentNode.height == 0);
		}
		
		/// <inheritdoc/>
		public override int Peek()
		{
			if (currentNode == null)
				return -1;
			return currentNode.contents[indexInsideNode];
		}
		
		/// <inheritdoc/>
		public override int Read()
		{
			if (currentNode == null)
				return -1;
			char result = currentNode.contents[indexInsideNode++];
			if (indexInsideNode >= currentNode.length)
				GoToNextNode();
			return result;
		}
		
		void GoToNextNode()
		{
			if (stack.Count == 0) {
				currentNode = null;
			} else {
				indexInsideNode = 0;
				currentNode = stack.Pop();
				GoToLeftMostLeaf();
			}
		}
		
		/// <inheritdoc/>
		public override int Read(char[] buffer, int index, int count)
		{
			if (currentNode == null)
				return 0;
			int amountInCurrentNode = currentNode.length - indexInsideNode;
			if (count < amountInCurrentNode) {
				Array.Copy(currentNode.contents, indexInsideNode, buffer, index, count);
				indexInsideNode += count;
				return count;
			} else {
				// read to end of current node
				Array.Copy(currentNode.contents, indexInsideNode, buffer, index, amountInCurrentNode);
				GoToNextNode();
				return amountInCurrentNode;
			}
		}
	}
}
