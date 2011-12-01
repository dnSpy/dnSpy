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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an assembly consisting of source code (parsed files).
	/// </summary>
	public interface IProjectContent : IUnresolvedAssembly
	{
		/// <summary>
		/// Gets a parsed file by its file name.
		/// </summary>
		IParsedFile GetFile(string fileName);
		
		/// <summary>
		/// Gets the list of all parsed files in the project content.
		/// </summary>
		IEnumerable<IParsedFile> Files { get; }
		
		/// <summary>
		/// Gets the referenced assemblies.
		/// </summary>
		IEnumerable<IAssemblyReference> AssemblyReferences { get; }
		
		/// <summary>
		/// Creates a new <see cref="ICompilation"/> that allows resolving within this project.
		/// </summary>
		/// <remarks>
		/// An ICompilation is immutable, it operates on a snapshot of this project.
		/// </remarks>
		ICompilation CreateCompilation();
		
		/// <summary>
		/// Creates a new <see cref="ICompilation"/> that allows resolving within this project.
		/// </summary>
		/// <param name="solutionSnapshot">The parent solution snapshot to use for the compilation.</param>
		/// <remarks>
		/// An ICompilation is immutable, it operates on a snapshot of this project.
		/// </remarks>
		ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot);
		
		/// <summary>
		/// Changes the assembly name of this project content.
		/// </summary>
		IProjectContent SetAssemblyName(string newAssemblyName);
		
		/// <summary>
		/// Add assembly references to this project content.
		/// </summary>
		IProjectContent AddAssemblyReferences(IEnumerable<IAssemblyReference> references);
		
		/// <summary>
		/// Removes assembly references from this project content.
		/// </summary>
		IProjectContent RemoveAssemblyReferences(IEnumerable<IAssemblyReference> references);
		
		/// <summary>
		/// Removes types and attributes from oldFile from the project, and adds those from newFile.
		/// </summary>
		IProjectContent UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile);
		
		/// <summary>
		/// Removes types and attributes from oldFiles from the project, and adds those from newFiles.
		/// </summary>
		IProjectContent UpdateProjectContent(IEnumerable<IParsedFile> oldFiles, IEnumerable<IParsedFile> newFiles);
	}
}
