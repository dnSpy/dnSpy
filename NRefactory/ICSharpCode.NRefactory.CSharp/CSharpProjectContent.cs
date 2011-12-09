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
using System.Linq;
using System.Runtime.Serialization;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp
{
	[Serializable]
	public class CSharpProjectContent : IProjectContent
	{
		string assemblyName;
		Dictionary<string, IParsedFile> parsedFiles;
		List<IAssemblyReference> assemblyReferences;
		
		public CSharpProjectContent()
		{
			this.assemblyName = string.Empty;
			this.parsedFiles = new Dictionary<string, IParsedFile>(Platform.FileNameComparer);
			this.assemblyReferences = new List<IAssemblyReference>();
		}
		
		protected CSharpProjectContent(CSharpProjectContent pc)
		{
			this.assemblyName = pc.assemblyName;
			this.parsedFiles = new Dictionary<string, IParsedFile>(pc.parsedFiles);
			this.assemblyReferences = new List<IAssemblyReference>(pc.assemblyReferences);
		}
		
		public IEnumerable<IParsedFile> Files {
			get { return parsedFiles.Values; }
		}
		
		public IEnumerable<IAssemblyReference> AssemblyReferences {
			get { return assemblyReferences; }
		}
		
		public string AssemblyName {
			get { return assemblyName; }
		}
		
		public IEnumerable<IUnresolvedAttribute> AssemblyAttributes {
			get {
				return this.Files.SelectMany(f => f.AssemblyAttributes);
			}
		}
		
		public IEnumerable<IUnresolvedAttribute> ModuleAttributes {
			get {
				return this.Files.SelectMany(f => f.ModuleAttributes);
			}
		}
		
		public IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
			get {
				return this.Files.SelectMany(f => f.TopLevelTypeDefinitions);
			}
		}
		
		public IParsedFile GetFile(string fileName)
		{
			IParsedFile file;
			if (parsedFiles.TryGetValue(fileName, out file))
				return file;
			else
				return null;
		}
		
		public ICompilation CreateCompilation()
		{
			var solutionSnapshot = new DefaultSolutionSnapshot();
			ICompilation compilation = new SimpleCompilation(solutionSnapshot, this, assemblyReferences);
			solutionSnapshot.AddCompilation(this, compilation);
			return compilation;
		}
		
		public ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot)
		{
			return new SimpleCompilation(solutionSnapshot, this, assemblyReferences);
		}
		
		public IProjectContent SetAssemblyName(string newAssemblyName)
		{
			CSharpProjectContent pc = new CSharpProjectContent(this);
			pc.assemblyName = newAssemblyName;
			return pc;
		}
		
		public IProjectContent AddAssemblyReferences(IEnumerable<IAssemblyReference> references)
		{
			CSharpProjectContent pc = new CSharpProjectContent(this);
			pc.assemblyReferences.AddRange(references);
			return pc;
		}
		
		public IProjectContent RemoveAssemblyReferences(IEnumerable<IAssemblyReference> references)
		{
			CSharpProjectContent pc = new CSharpProjectContent(this);
			pc.assemblyReferences.RemoveAll(r => references.Contains(r));
			return pc;
		}
		
		public IProjectContent UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile)
		{
			if (oldFile == null && newFile == null)
				return this;
			if (oldFile != null && newFile != null) {
				if (!Platform.FileNameComparer.Equals(oldFile.FileName, newFile.FileName))
					throw new ArgumentException("When both oldFile and newFile are specified, they must use the same file name.");
			}
			CSharpProjectContent pc = new CSharpProjectContent(this);
			if (newFile == null)
				pc.parsedFiles.Remove(oldFile.FileName);
			else
				pc.parsedFiles[newFile.FileName] = newFile;
			return pc;
		}
		
		public IProjectContent UpdateProjectContent(IEnumerable<IParsedFile> oldFiles, IEnumerable<IParsedFile> newFiles)
		{
			CSharpProjectContent pc = new CSharpProjectContent(this);
			if (oldFiles != null) {
				foreach (var oldFile in oldFiles) {
					pc.parsedFiles.Remove(oldFile.FileName);
				}
			}
			if (newFiles != null) {
				foreach (var newFile in newFiles) {
					pc.parsedFiles.Add(newFile.FileName, newFile);
				}
			}
			return pc;
		}
		
		IAssembly IAssemblyReference.Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			var cache = context.Compilation.CacheManager;
			IAssembly asm = (IAssembly)cache.GetShared(this);
			if (asm != null) {
				return asm;
			} else {
				asm = new CSharpAssembly(context.Compilation, this);
				return (IAssembly)cache.GetOrAddShared(this, asm);
			}
		}
	}
}
