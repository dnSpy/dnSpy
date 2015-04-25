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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// An immutable stack.
	/// 
	/// Using 'foreach' on the stack will return the items from top to bottom (in the order they would be popped).
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	[Serializable]
	public sealed class ImmutableStack<T> : IEnumerable<T>
	{
		/// <summary>
		/// Gets the empty stack instance.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "ImmutableStack is immutable")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		public static readonly ImmutableStack<T> Empty = new ImmutableStack<T>();
		
		readonly T value;
		readonly ImmutableStack<T> next;
		
		private ImmutableStack()
		{
		}
		
		private ImmutableStack(T value, ImmutableStack<T> next)
		{
			this.value = value;
			this.next = next;
		}
		
		/// <summary>
		/// Pushes an item on the stack. This does not modify the stack itself, but returns a new
		/// one with the value pushed.
		/// </summary>
		public ImmutableStack<T> Push(T item)
		{
			return new ImmutableStack<T>(item, this);
		}
		
		/// <summary>
		/// Gets the item on the top of the stack.
		/// </summary>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public T Peek()
		{
			if (IsEmpty)
				throw new InvalidOperationException("Operation not valid on empty stack.");
			return value;
		}
		
		/// <summary>
		/// Gets the item on the top of the stack.
		/// Returns <c>default(T)</c> if the stack is empty.
		/// </summary>
		public T PeekOrDefault()
		{
			return value;
		}
		
		/// <summary>
		/// Gets the stack with the top item removed.
		/// </summary>
		/// <exception cref="InvalidOperationException">The stack is empty.</exception>
		public ImmutableStack<T> Pop()
		{
			if (IsEmpty)
				throw new InvalidOperationException("Operation not valid on empty stack.");
			return next;
		}
		
		/// <summary>
		/// Gets if this stack is empty.
		/// </summary>
		public bool IsEmpty {
			get { return next == null; }
		}
		
		/// <summary>
		/// Gets an enumerator that iterates through the stack top-to-bottom.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			ImmutableStack<T> t = this;
			while (!t.IsEmpty) {
				yield return t.value;
				t = t.next;
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[Stack");
			foreach (T val in this) {
				b.Append(' ');
				b.Append(val);
			}
			b.Append(']');
			return b.ToString();
		}
	}
}
