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
using System.Threading;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// Represents the role a node plays within its parent.
	/// </summary>
	public abstract class Role
	{
		public const int RoleIndexBits = 9;
		
		static readonly Role[] roles = new Role[1 << RoleIndexBits];
		static int nextRoleIndex = 0;
		
		readonly uint index;
		
		[CLSCompliant(false)]
		public uint Index {
			get { return index; }
		}
		
		// don't allow NRefactory consumers to derive from Role
		internal Role()
		{
			this.index = (uint)Interlocked.Increment(ref nextRoleIndex);
			if (this.index >= roles.Length)
				throw new InvalidOperationException("Too many roles");
			roles[this.index] = this;
		}
		
		/// <summary>
		/// Gets whether the specified node is valid in this role.
		/// </summary>
		public abstract bool IsValid(object node);
		
		/// <summary>
		/// Gets the role with the specified index.
		/// </summary>
		[CLSCompliant(false)]
		public static Role GetByIndex(uint index)
		{
			return roles[index];
		}
	}
	
	/// <summary>
	/// Represents the role a node plays within its parent.
	/// All nodes with this role have type T.
	/// </summary>
	public class Role<T> : Role where T : class
	{
		readonly string name; // helps with debugging the AST
		readonly T nullObject;
		
		/// <summary>
		/// Gets the null object used when there's no node with this role.
		/// Not every role has a null object; this property returns null for roles without a null object.
		/// </summary>
		/// <remarks>
		/// Roles used for non-collections should always have a null object, so that no AST property returns null.
		/// However, if a role used for collections only, it may leave out the null object.
		/// </remarks>
		public T NullObject {
			get { return nullObject; }
		}
		
		public override bool IsValid(object node)
		{
			return node is T;
		}
		
		public Role(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.name = name;
		}
		
		public Role(string name, T nullObject)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (nullObject == null)
				throw new ArgumentNullException ("nullObject");
			this.nullObject = nullObject;
			this.name = name;
		}
		
		public override string ToString()
		{
			return name;
		}
	}
}
