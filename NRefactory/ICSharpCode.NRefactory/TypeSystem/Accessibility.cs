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
	
	public interface IHasAccessibility
	{
		/// <summary>
		/// Gets the accessibility of this entity.
		/// </summary>
		Accessibility Accessibility { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is private.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is private; otherwise, <c>false</c>.
		/// </value>
		bool IsPrivate { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is public.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is public; otherwise, <c>false</c>.
		/// </value>
		bool IsPublic { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is protected.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is protected; otherwise, <c>false</c>.
		/// </value>
		bool IsProtected { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is internal.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is internal; otherwise, <c>false</c>.
		/// </value>
		bool IsInternal { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is protected or internal.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is protected or internal; otherwise, <c>false</c>.
		/// </value>
		bool IsProtectedOrInternal { get; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is protected and internal.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is protected and internal; otherwise, <c>false</c>.
		/// </value>
		bool IsProtectedAndInternal { get; }
	}
}
