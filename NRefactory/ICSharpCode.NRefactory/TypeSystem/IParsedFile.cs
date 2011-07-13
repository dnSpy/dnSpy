// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a single file that was parsed.
	/// </summary>
	public interface IParsedFile : IFreezable
	{
		/// <summary>
		/// Gets the parent project content.
		/// </summary>
		IProjectContent ProjectContent { get; }
		
		/// <summary>
		/// Returns the full path of the file.
		/// </summary>
		string FileName { get; }
		
		/// <summary>
		/// Gets the time of object creation.
		/// </summary>
		DateTime ParseTime { get; }
		
		/// <summary>
		/// Gets all top-level type definitions.
		/// </summary>
		IList<ITypeDefinition> TopLevelTypeDefinitions { get; }
		
		/// <summary>
		/// Gets all assembly attributes that are defined in this file.
		/// </summary>
		IList<IAttribute> AssemblyAttributes { get; }
		
		/// <summary>
		/// Gets the top-level type defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		ITypeDefinition GetTopLevelTypeDefinition(AstLocation location);
		
		/// <summary>
		/// Gets the type (potentially a nested type) defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		ITypeDefinition GetTypeDefinition(AstLocation location);
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		IMember GetMember(AstLocation location);
		
		/// <summary>
		/// Gets the parser errors.
		/// </summary>
		IList<Error> Errors { get; }
	}
}
