// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Iterates through an internal object tree.
	/// </summary>
	sealed class ObjectIterator
	{
		Stack<InternalObject[]> listStack = new Stack<InternalObject[]>();
		Stack<int> indexStack = new Stack<int>();
		
		InternalObject[] objects;
		int currentIndex;
		InternalObject currentObject;
		int currentPosition;
		internal bool StopAtElementEnd;
		bool isAtElementEnd;
		
		public ObjectIterator(InternalObject[] objects, int startPosition = 0)
		{
			this.currentPosition = startPosition;
			this.objects = objects;
			if (objects.Length > 0)
				this.currentObject = objects[0];
		}
		
		public InternalObject CurrentObject {
			get { return currentObject; }
		}
		
		public int CurrentPosition {
			get { return currentPosition; }
		}
		
		public bool IsAtElementEnd {
			get { return isAtElementEnd; }
		}
		
		public int Depth {
			get { return listStack.Count; }
		}
		
		public void MoveNext()
		{
			if (currentObject == null)
				return;
			currentIndex++;
			currentPosition += currentObject.Length;
			isAtElementEnd = false;
			while (currentIndex >= objects.Length && listStack.Count > 0) {
				objects = listStack.Pop();
				currentIndex = indexStack.Pop();
				if (this.StopAtElementEnd) {
					isAtElementEnd = true;
					break;
				} else {
					currentIndex++;
				}
			}
			currentObject = (currentIndex < objects.Length ? objects[currentIndex] : null);
		}
		
		public void MoveInto()
		{
			if (isAtElementEnd || !(currentObject is InternalElement)) {
				MoveNext();
			} else {
				listStack.Push(objects);
				indexStack.Push(currentIndex);
				objects = currentObject.NestedObjects;
				currentIndex = 0;
				currentObject = objects[0];
			}
		}
		
		/// <summary>
		/// Skips all nodes in front of 'position'
		/// </summary>
		public void SkipTo(int position)
		{
			while (currentObject != null && currentPosition < position) {
				if (currentPosition + currentObject.Length <= position)
					MoveNext();
				else
					MoveInto();
			}
		}
	}
}
