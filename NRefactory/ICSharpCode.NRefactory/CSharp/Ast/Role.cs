// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
