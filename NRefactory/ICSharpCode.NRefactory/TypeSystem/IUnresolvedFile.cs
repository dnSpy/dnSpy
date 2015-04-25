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

namespace ICSharpCode.NRefactory.TypeSystem
{
	[Obsolete("IParsedFile was renamed to IUnresolvedFile", true)]
	public interface IParsedFile {}
	
	/// <summary>
	/// Represents a single file that was parsed.
	/// </summary>
	public interface IUnresolvedFile
	{
		/// <summary>
		/// Returns the full path of the file.
		/// </summary>
		string FileName { get; }
		
		/// <summary>
		/// Gets the time when the file was last written.
		/// </summary>
		DateTime? LastWriteTime { get; set; }
		
		/// <summary>
		/// Gets all top-level type definitions.
		/// </summary>
		IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions { get; }
		
		/// <summary>
		/// Gets all assembly attributes that are defined in this file.
		/// </summary>
		IList<IUnresolvedAttribute> AssemblyAttributes { get; }
		
		/// <summary>
		/// Gets all module attributes that are defined in this file.
		/// </summary>
		IList<IUnresolvedAttribute> ModuleAttributes { get; }
		
		/// <summary>
		/// Gets the top-level type defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		IUnresolvedTypeDefinition GetTopLevelTypeDefinition(TextLocation location);
		
		/// <summary>
		/// Gets the type (potentially a nested type) defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		IUnresolvedTypeDefinition GetInnermostTypeDefinition(TextLocation location);
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		IUnresolvedMember GetMember(TextLocation location);
		
		/// <summary>
		/// Gets the parser errors.
		/// </summary>
		IList<Error> Errors { get; }
	}
}
