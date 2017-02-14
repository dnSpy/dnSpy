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
using System.Globalization;
using System.Linq;
using System.Text;

namespace dnSpy.Text.AvalonEdit {
	/// <summary>
	/// A kind of List&lt;T&gt;, but more efficient for random insertions/removal.
	/// Also has cheap Clone() and SubRope() implementations.
	/// </summary>
	/// <remarks>
	/// This class is not thread-safe: multiple concurrent write operations or writes concurrent to reads have undefined behaviour.
	/// Concurrent reads, however, are safe.
	/// However, clones of a rope are safe to use on other threads even though they share data with the original rope.
	/// </remarks>
	[Serializable]
	sealed class Rope<T> : IList<T>, ICloneable {
		internal RopeNode<T> root;

		internal Rope(RopeNode<T> root) {
			this.root = root;
			root.CheckInvariants();
		}

		/// <summary>
		/// Creates a rope from the specified input.
		/// This operation runs in O(N).
		/// </summary>
		/// <exception cref="ArgumentNullException">input is null.</exception>
		public Rope(IEnumerable<T> input) {
			if (input == null)
				throw new ArgumentNullException("input");
			if (input is Rope<T> inputRope) {
				// clone ropes instead of copying them
				inputRope.root.Publish();
				root = inputRope.root;
			}
			else {
				string text = input as string;
				if (text != null) {
					// if a string is IEnumerable<T>, then T must be char
					((Rope<char>)(object)this).root = CharRope.InitFromString(text);
				}
				else {
					T[] arr = ToArray(input);
					root = RopeNode<T>.CreateFromArray(arr, 0, arr.Length);
				}
			}
			root.CheckInvariants();
		}

		static T[] ToArray(IEnumerable<T> input) {
			T[] arr = input as T[];
			return arr ?? input.ToArray();
		}

		/// <summary>
		/// Clones the rope.
		/// This operation runs in linear time to the number of rope nodes touched since the last clone was created.
		/// If you count the per-node cost to the operation modifying the rope (doing this doesn't increase the complexity of the modification operations);
		/// the remainder of Clone() runs in O(1).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public Rope<T> Clone() {
			// The Publish() call actually modifies this rope instance; but this modification is thread-safe
			// as long as the tree structure doesn't change during the operation.
			root.Publish();
			return new Rope<T>(root);
		}

		object ICloneable.Clone() => Clone();

		/// <summary>
		/// Resets the rope to an empty list.
		/// Runs in O(1).
		/// </summary>
		public void Clear() {
			root = RopeNode<T>.emptyRopeNode;
			OnChanged();
		}

		/// <summary>
		/// Gets the length of the rope.
		/// Runs in O(1).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public int Length {
			get { return root.length; }
		}

		/// <summary>
		/// Gets the length of the rope.
		/// Runs in O(1).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public int Count {
			get { return root.length; }
		}

		/// <summary>
		/// Inserts another rope into this rope.
		/// Runs in O(lg N + lg M), plus a per-node cost as if <c>newElements.Clone()</c> was called.
		/// </summary>
		/// <exception cref="ArgumentNullException">newElements is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">index or length is outside the valid range.</exception>
		public void InsertRange(int index, Rope<T> newElements) {
			if (index < 0 || index > Length) {
				throw new ArgumentOutOfRangeException("index", index, "0 <= index <= " + Length.ToString(CultureInfo.InvariantCulture));
			}
			if (newElements == null)
				throw new ArgumentNullException("newElements");
			newElements.root.Publish();
			root = root.Insert(index, newElements.root);
			OnChanged();
		}

		/// <summary>
		/// Inserts new elements into this rope.
		/// Runs in O(lg N + M), where N is the length of this rope and M is the number of new elements.
		/// </summary>
		/// <exception cref="ArgumentNullException">newElements is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">index or length is outside the valid range.</exception>
		public void InsertRange(int index, T[] array, int arrayIndex, int count) {
			if (index < 0 || index > Length) {
				throw new ArgumentOutOfRangeException("index", index, "0 <= index <= " + Length.ToString(CultureInfo.InvariantCulture));
			}
			VerifyArrayWithRange(array, arrayIndex, count);
			if (count > 0) {
				root = root.Insert(index, array, arrayIndex, count);
				OnChanged();
			}
		}

		/// <summary>
		/// Removes a range of elements from the rope.
		/// Runs in O(lg N).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		public void RemoveRange(int index, int count) {
			VerifyRange(index, count);
			if (count > 0) {
				root = root.RemoveRange(index, count);
				OnChanged();
			}
		}

		/// <summary>
		/// Creates a new rope and initializes it with a part of this rope.
		/// Runs in O(lg N) plus a per-node cost as if <c>this.Clone()</c> was called.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public Rope<T> GetRange(int index, int count) {
			VerifyRange(index, count);
			Rope<T> newRope = Clone();
			int endIndex = index + count;
			newRope.RemoveRange(endIndex, newRope.Length - endIndex);
			newRope.RemoveRange(0, index);
			return newRope;
		}

		#region Caches / Changed event
		internal struct RopeCacheEntry {
			internal readonly RopeNode<T> node;
			internal readonly int nodeStartIndex;

			internal RopeCacheEntry(RopeNode<T> node, int nodeStartOffset) {
				this.node = node;
				nodeStartIndex = nodeStartOffset;
			}

			internal bool IsInside(int offset) => offset >= nodeStartIndex && offset < nodeStartIndex + node.length;
		}

		// cached pointer to 'last used node', used to speed up accesses by index that are close together
		[NonSerialized]
		volatile ImmutableStack<RopeCacheEntry> lastUsedNodeStack;

		internal void OnChanged() {
			lastUsedNodeStack = null;

			root.CheckInvariants();
		}
		#endregion

		#region GetChar / SetChar
		/// <summary>
		/// Gets/Sets a single character.
		/// Runs in O(lg N) for random access. Sequential read-only access benefits from a special optimization and runs in amortized O(1).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to Length-1).</exception>
		/// <remarks>
		/// The getter counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public T this[int index] {
			get {
				// use unsigned integers - this way negative values for index overflow and can be tested for with the same check
				if (unchecked((uint)index >= (uint)Length)) {
					throw new ArgumentOutOfRangeException("index", index, "0 <= index < " + Length.ToString(CultureInfo.InvariantCulture));
				}
				RopeCacheEntry entry = FindNodeUsingCache(index).PeekOrDefault();
				return entry.node.contents[index - entry.nodeStartIndex];
			}
			set {
				if (index < 0 || index >= Length) {
					throw new ArgumentOutOfRangeException("index", index, "0 <= index < " + Length.ToString(CultureInfo.InvariantCulture));
				}
				root = root.SetElement(index, value);
				OnChanged();
			}
		}

		internal ImmutableStack<RopeCacheEntry> FindNodeUsingCache(int index) {
			Debug.Assert(index >= 0 && index < Length);

			// thread safety: fetch stack into local variable
			ImmutableStack<RopeCacheEntry> stack = lastUsedNodeStack;
			ImmutableStack<RopeCacheEntry> oldStack = stack;

			if (stack == null) {
				stack = ImmutableStack<RopeCacheEntry>.Empty.Push(new RopeCacheEntry(root, 0));
			}
			while (!stack.PeekOrDefault().IsInside(index))
				stack = stack.Pop();
			while (true) {
				RopeCacheEntry entry = stack.PeekOrDefault();
				// check if we've reached a leaf or function node
				if (entry.node.height == 0) {
					if (entry.node.contents == null) {
						// this is a function node - go down into its subtree
						entry = new RopeCacheEntry(entry.node.GetContentNode(), entry.nodeStartIndex);
						// entry is now guaranteed NOT to be another function node
					}
					if (entry.node.contents != null) {
						// this is a node containing actual content, so we're done
						break;
					}
				}
				// go down towards leaves
				if (index - entry.nodeStartIndex >= entry.node.left.length)
					stack = stack.Push(new RopeCacheEntry(entry.node.right, entry.nodeStartIndex + entry.node.left.length));
				else
					stack = stack.Push(new RopeCacheEntry(entry.node.left, entry.nodeStartIndex));
			}

			// write back stack to volatile cache variable
			// (in multithreaded access, it doesn't matter which of the threads wins - it's just a cache)
			if (oldStack != stack) {
				// no need to write when we the cache variable didn't change
				lastUsedNodeStack = stack;
			}

			// this method guarantees that it finds a leaf node
			Debug.Assert(stack.Peek().node.contents != null);
			return stack;
		}
		#endregion

		#region ToString / WriteTo
		internal void VerifyRange(int startIndex, int length) {
			if (startIndex < 0 || startIndex > Length) {
				throw new ArgumentOutOfRangeException("startIndex", startIndex, "0 <= startIndex <= " + Length.ToString(CultureInfo.InvariantCulture));
			}
			if (length < 0 || startIndex + length > Length) {
				throw new ArgumentOutOfRangeException("length", length, "0 <= length, startIndex(" + startIndex + ")+length <= " + Length.ToString(CultureInfo.InvariantCulture));
			}
		}

		internal static void VerifyArrayWithRange(T[] array, int arrayIndex, int count) {
			if (array == null)
				throw new ArgumentNullException("array");
			if (arrayIndex < 0 || arrayIndex > array.Length) {
				throw new ArgumentOutOfRangeException("startIndex", arrayIndex, "0 <= arrayIndex <= " + array.Length.ToString(CultureInfo.InvariantCulture));
			}
			if (count < 0 || arrayIndex + count > array.Length) {
				throw new ArgumentOutOfRangeException("count", count, "0 <= length, arrayIndex(" + arrayIndex + ")+count <= " + array.Length.ToString(CultureInfo.InvariantCulture));
			}
		}

		/// <summary>
		/// Creates a string from the rope. Runs in O(N).
		/// </summary>
		/// <returns>A string consisting of all elements in the rope as comma-separated list in {}.
		/// As a special case, Rope&lt;char&gt; will return its contents as string without any additional separators or braces,
		/// so it can be used like StringBuilder.ToString().</returns>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public override string ToString() {
			Rope<char> charRope = this as Rope<char>;
			if (charRope != null) {
				return charRope.ToString(0, Length);
			}
			else {
				StringBuilder b = new StringBuilder();
				foreach (T element in this) {
					if (b.Length == 0)
						b.Append('{');
					else
						b.Append(", ");
					b.Append(element.ToString());
				}
				b.Append('}');
				return b.ToString();
			}
		}
		#endregion

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Finds the first occurance of item.
		/// Runs in O(N).
		/// </summary>
		/// <returns>The index of the first occurance of item, or -1 if it cannot be found.</returns>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public int IndexOf(T item) => IndexOf(item, 0, Length);

		/// <summary>
		/// Gets the index of the first occurrence the specified item.
		/// </summary>
		/// <param name="item">Item to search for.</param>
		/// <param name="startIndex">Start index of the search.</param>
		/// <param name="count">Length of the area to search.</param>
		/// <returns>The first index where the item was found; or -1 if no occurrence was found.</returns>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public int IndexOf(T item, int startIndex, int count) {
			VerifyRange(startIndex, count);

			while (count > 0) {
				var entry = FindNodeUsingCache(startIndex).PeekOrDefault();
				T[] contents = entry.node.contents;
				int startWithinNode = startIndex - entry.nodeStartIndex;
				int nodeLength = Math.Min(entry.node.length, startWithinNode + count);
				int r = Array.IndexOf(contents, item, startWithinNode, nodeLength - startWithinNode);
				if (r >= 0)
					return entry.nodeStartIndex + r;
				count -= nodeLength - startWithinNode;
				startIndex = entry.nodeStartIndex + nodeLength;
			}
			return -1;
		}

		/// <summary>
		/// Gets the index of the last occurrence of the specified item in this rope.
		/// </summary>
		public int LastIndexOf(T item) => LastIndexOf(item, 0, Length);

		/// <summary>
		/// Gets the index of the last occurrence of the specified item in this rope.
		/// </summary>
		/// <param name="item">The search item</param>
		/// <param name="startIndex">Start index of the area to search.</param>
		/// <param name="count">Length of the area to search.</param>
		/// <returns>The last index where the item was found; or -1 if no occurrence was found.</returns>
		/// <remarks>The search proceeds backwards from (startIndex+count) to startIndex.
		/// This is different than the meaning of the parameters on Array.LastIndexOf!</remarks>
		public int LastIndexOf(T item, int startIndex, int count) {
			VerifyRange(startIndex, count);

			var comparer = EqualityComparer<T>.Default;
			for (int i = startIndex + count - 1; i >= startIndex; i--) {
				if (comparer.Equals(this[i], item))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Inserts the item at the specified index in the rope.
		/// Runs in O(lg N).
		/// </summary>
		public void Insert(int index, T item) => InsertRange(index, new[] { item }, 0, 1);

		/// <summary>
		/// Removes a single item from the rope.
		/// Runs in O(lg N).
		/// </summary>
		public void RemoveAt(int index) => RemoveRange(index, 1);

		/// <summary>
		/// Appends the item at the end of the rope.
		/// Runs in O(lg N).
		/// </summary>
		public void Add(T item) => InsertRange(Length, new[] { item }, 0, 1);

		/// <summary>
		/// Searches the item in the rope.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public bool Contains(T item) => IndexOf(item) >= 0;

		/// <summary>
		/// Copies the whole content of the rope into the specified array.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public void CopyTo(T[] array, int arrayIndex) => CopyTo(0, array, arrayIndex, Length);

		/// <summary>
		/// Copies the a part of the rope into the specified array.
		/// Runs in O(lg N + M).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public void CopyTo(int index, T[] array, int arrayIndex, int count) {
			VerifyRange(index, count);
			VerifyArrayWithRange(array, arrayIndex, count);
			root.CopyTo(index, array, arrayIndex, count);
		}

		/// <summary>
		/// Removes the first occurance of an item from the rope.
		/// Runs in O(N).
		/// </summary>
		public bool Remove(T item) {
			int index = IndexOf(item);
			if (index >= 0) {
				RemoveAt(index);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Retrieves an enumerator to iterate through the rope.
		/// The enumerator will reflect the state of the rope from the GetEnumerator() call, further modifications
		/// to the rope will not be visible to the enumerator.
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public IEnumerator<T> GetEnumerator() {
			root.Publish();
			return Enumerate(root);
		}

		/// <summary>
		/// Creates an array and copies the contents of the rope into it.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public T[] ToArray() {
			T[] arr = new T[Length];
			root.CopyTo(0, arr, 0, arr.Length);
			return arr;
		}

		/// <summary>
		/// Creates an array and copies the contents of the rope into it.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public T[] ToArray(int startIndex, int count) {
			VerifyRange(startIndex, count);
			T[] arr = new T[count];
			CopyTo(startIndex, arr, 0, count);
			return arr;
		}

		static IEnumerator<T> Enumerate(RopeNode<T> node) {
			Stack<RopeNode<T>> stack = new Stack<RopeNode<T>>();
			while (node != null) {
				// go to leftmost node, pushing the right parts that we'll have to visit later
				while (node.contents == null) {
					if (node.height == 0) {
						// go down into function nodes
						node = node.GetContentNode();
						continue;
					}
					Debug.Assert(node.right != null);
					stack.Push(node.right);
					node = node.left;
				}
				// yield contents of leaf node
				for (int i = 0; i < node.length; i++) {
					yield return node.contents[i];
				}
				// go up to the next node not visited yet
				if (stack.Count > 0)
					node = stack.Pop();
				else
					node = null;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
