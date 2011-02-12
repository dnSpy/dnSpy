// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Enum that describes the accessibility of an entity.
	/// </summary>
	public enum Accessibility : byte
	{
		// note: some code depends on the fact that these values are within the range 0-7
		
		/// <summary>
		/// The entity is completely inaccessible. This is used for C# explicit interface implementations.
		/// </summary>
		None,
		/// <summary>
		/// The entity is only accessible within the same class.
		/// </summary>
		Private,
		/// <summary>
		/// The entity is accessible everywhere.
		/// </summary>
		Public,
		/// <summary>
		/// The entity is only accessible within the same class and in derived classes.
		/// </summary>
		Protected,
		/// <summary>
		/// The entity is accessible within the same project content.
		/// </summary>
		Internal,
		/// <summary>
		/// The entity is accessible both everywhere in the project content, and in all derived classes.
		/// </summary>
		/// <remarks>This corresponds to C# 'protected internal'.</remarks>
		ProtectedOrInternal,
		/// <summary>
		/// The entity is accessible in derived classes within the same project content.
		/// </summary>
		/// <remarks>C# does not support this accessibility.</remarks>
		ProtectedAndInternal,
	}
}
