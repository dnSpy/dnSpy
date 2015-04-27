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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a scope in which references are searched.
	/// </summary>
	public interface IFindReferenceSearchScope
	{
		/// <summary>
		/// Gets the compilation in which the entity being search for was defined.
		/// This is not necessarily the same compilation as is being searched in.
		/// </summary>
		ICompilation Compilation { get; }
		
		/// <summary>
		/// Gets the search term. Only files that contain this identifier need to be parsed.
		/// Can return null if all files need to be parsed.
		/// </summary>
		string SearchTerm { get; }
		
		/// <summary>
		/// Gets the accessibility that defines the search scope.
		/// </summary>
		Accessibility Accessibility { get; }
		
		/// <summary>
		/// Gets the top-level entity that defines the search scope.
		/// </summary>
		ITypeDefinition TopLevelTypeDefinition { get; }
		
		/// <summary>
		/// Gets the file name that defines the search scope.
		/// If null, all files are searched.
		/// </summary>
		string FileName { get; }
		
		/// <summary>
		/// Creates a navigator that can find references to this entity and reports
		/// them to the specified callback.
		/// </summary>
		IResolveVisitorNavigator GetNavigator(ICompilation compilation, FoundReferenceCallback callback);
	}
}
