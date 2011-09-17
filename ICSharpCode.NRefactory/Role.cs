// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// Represents the role a node plays within its parent.
	/// </summary>
	public abstract class Role
	{
		internal Role() {} // don't allow NRefactory consumers to derive from Role
		
		/// <summary>
		/// Gets whether the specified node is valid in this role.
		/// </summary>
		public abstract bool IsValid(object node);
	}
	
	/// <summary>
	/// Represents the role a node plays within its parent.
	/// All nodes with this role have type T.
	/// </summary>
	public sealed class Role<T> : Role where T : class
	{
		readonly string name; // helps with debugging the AST
		readonly T nullObject;
		
		/// <summary>
		/// Gets the null object used when there's no node with this role.
		/// Not every role has a null object; this property returns null for roles without a null object.
		/// </summary>
		/// <remarks>
		/// Roles used for non-collections should always have a null object, so that no AST property returns null.
		/// However, roles used for collections only may leave out the null object.
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
		
		public Role(string name, T nullObject = null)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.nullObject = nullObject;
			this.name = name;
		}
		
		public override string ToString()
		{
			return name;
		}
	}
}
