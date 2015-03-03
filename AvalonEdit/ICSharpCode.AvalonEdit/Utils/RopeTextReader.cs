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
