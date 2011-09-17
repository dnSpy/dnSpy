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
	/// Mutable container of all classes in an assembly.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IProjectContentContract))]
	#endif
	public interface IProjectContent : ITypeResolveContext, IAnnotatable
	{
		/// <summary>
		/// Gets the assembly name (short name).
		/// </summary>
		string AssemblyName { get; }
		
		/// <summary>
		/// Gets the list of all assembly attributes in the project.
		/// </summary>
		IList<IAttribute> AssemblyAttributes { get; }
		
		/// <summary>
		/// Gets the list of all module attributes in the project.
		/// </summary>
		IList<IAttribute> ModuleAttributes { get; }
		
		/// <summary>
		/// Gets a parsed file by its file name.
		/// </summary>
		IParsedFile GetFile(string fileName);
		
		/// <summary>
		/// Gets the list of all parsed files in the project content.
		/// </summary>
		IEnumerable<IParsedFile> Files { get; }
		
		/// <summary>
		/// Removes types and attributes from oldFile from the project, and adds those from newFile.
		/// </summary>
		/// <remarks>
		/// It is not allowed to call this method from within a <c>using (var ctx = context.Synchronize())</c> block
		/// that involves this project content: this method is a write operation and might (if implemented using locks)
		/// wait until all read operations have finished, causing deadlocks within Synchronize blocks.
		/// </remarks>
		void UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IProjectContent))]
	abstract class IProjectContentContract : ITypeResolveContextContract, IProjectContent
	{
		IList<IAttribute> IProjectContent.AssemblyAttributes {
			get {
				Contract.Ensures(Contract.Result<IList<IAttribute>>() != null);
				return null;
			}
		}
		
		IParsedFile IProjectContent.GetFile(string fileName)
		{
			Contract.Requires(fileName != null);
			return;
		}
	}
	#endif
}
