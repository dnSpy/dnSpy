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
		string fullAssemblyName;
		string projectFileName;
		string location;
		Dictionary<string, IUnresolvedFile> unresolvedFiles;
		List<IAssemblyReference> assemblyReferences;
		CompilerSettings compilerSettings;
		
		public CSharpProjectContent()
		{
			this.unresolvedFiles = new Dictionary<string, IUnresolvedFile>(Platform.FileNameComparer);
			this.assemblyReferences = new List<IAssemblyReference>();
			this.compilerSettings = new CompilerSettings();
			compilerSettings.Freeze();
		}
		
		protected CSharpProjectContent(CSharpProjectContent pc)
		{
			this.assemblyName = pc.assemblyName;
			this.fullAssemblyName = pc.fullAssemblyName;
			this.projectFileName = pc.projectFileName;
			this.location = pc.location;
			this.unresolvedFiles = new Dictionary<string, IUnresolvedFile>(pc.unresolvedFiles, Platform.FileNameComparer);
			this.assemblyReferences = new List<IAssemblyReference>(pc.assemblyReferences);
			this.compilerSettings = pc.compilerSettings;
		}
		
		public IEnumerable<IUnresolvedFile> Files {
			get { return unresolvedFiles.Values; }
		}
		
		public IEnumerable<IAssemblyReference> AssemblyReferences {
			get { return assemblyReferences; }
		}
		
		public string ProjectFileName {
			get { return projectFileName; }
		}
		
		public string AssemblyName {
			get { return assemblyName; }
		}

		public string FullAssemblyName {
			get { return fullAssemblyName; }
		}

		public string Location {
			get { return location; }
		}

		public CompilerSettings CompilerSettings {
			get { return compilerSettings; }
		}
		
		object IProjectContent.CompilerSettings {
			get { return compilerSettings; }
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
		
		public IUnresolvedFile GetFile(string fileName)
		{
			IUnresolvedFile file;
			if (unresolvedFiles.TryGetValue(fileName, out file))
				return file;
			else
				return null;
		}
		
		public virtual ICompilation CreateCompilation()
		{
			var solutionSnapshot = new DefaultSolutionSnapshot();
			ICompilation compilation = new SimpleCompilation(solutionSnapshot, this, assemblyReferences);
			solutionSnapshot.AddCompilation(this, compilation);
			return compilation;
		}
		
		public virtual ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot)
		{
			return new SimpleCompilation(solutionSnapshot, this, assemblyReferences);
		}
		
		protected virtual CSharpProjectContent Clone()
		{
			return new CSharpProjectContent(this);
		}
		
		/// <summary>
		/// Sets both the short and the full assembly names.
		/// </summary>
		/// <param name="newAssemblyName">New full assembly name.</param>
		public IProjectContent SetAssemblyName(string newAssemblyName)
		{
			CSharpProjectContent pc = Clone();
			pc.fullAssemblyName = newAssemblyName;
			int pos = newAssemblyName != null ? newAssemblyName.IndexOf(',') : -1;
			pc.assemblyName = pos < 0 ? newAssemblyName : newAssemblyName.Substring(0, pos);
			return pc;
		}
		
		public IProjectContent SetProjectFileName(string newProjectFileName)
		{
			CSharpProjectContent pc = Clone();
			pc.projectFileName = newProjectFileName;
			return pc;
		}

		public IProjectContent SetLocation(string newLocation)
		{
			CSharpProjectContent pc = Clone();
			pc.location = newLocation;
			return pc;
		}
		
		public IProjectContent SetCompilerSettings(object compilerSettings)
		{
			if (!(compilerSettings is CompilerSettings))
				throw new ArgumentException("Settings must be an instance of " + typeof(CompilerSettings).FullName, "compilerSettings");
			CSharpProjectContent pc = Clone();
			pc.compilerSettings = (CompilerSettings)compilerSettings;
			pc.compilerSettings.Freeze();
			return pc;
		}
		
		public IProjectContent AddAssemblyReferences(IEnumerable<IAssemblyReference> references)
		{
			return AddAssemblyReferences(references.ToArray());
		}
		
		public IProjectContent AddAssemblyReferences(params IAssemblyReference[] references)
		{
			CSharpProjectContent pc = Clone();
			pc.assemblyReferences.AddRange(references);
			return pc;
		}
		
		public IProjectContent RemoveAssemblyReferences(IEnumerable<IAssemblyReference> references)
		{
			return RemoveAssemblyReferences(references.ToArray());
		}
		
		public IProjectContent RemoveAssemblyReferences(params IAssemblyReference[] references)
		{
			CSharpProjectContent pc = Clone();
			foreach (var r in references)
				pc.assemblyReferences.Remove(r);
			return pc;
		}
		
		/// <summary>
		/// Adds the specified files to the project content.
		/// If a file with the same name already exists, updated the existing file.
		/// </summary>
		public IProjectContent AddOrUpdateFiles(IEnumerable<IUnresolvedFile> newFiles)
		{
			CSharpProjectContent pc = Clone();
			foreach (var file in newFiles) {
				pc.unresolvedFiles[file.FileName] = file;
			}
			return pc;
		}
		
		/// <summary>
		/// Adds the specified files to the project content.
		/// If a file with the same name already exists, this method updates the existing file.
		/// </summary>
		public IProjectContent AddOrUpdateFiles(params IUnresolvedFile[] newFiles)
		{
			return AddOrUpdateFiles((IEnumerable<IUnresolvedFile>)newFiles);
		}
		
		/// <summary>
		/// Removes the files with the specified names.
		/// </summary>
		public IProjectContent RemoveFiles(IEnumerable<string> fileNames)
		{
			CSharpProjectContent pc = Clone();
			foreach (var fileName in fileNames) {
				pc.unresolvedFiles.Remove(fileName);
			}
			return pc;
		}
		
		/// <summary>
		/// Removes the files with the specified names.
		/// </summary>
		public IProjectContent RemoveFiles(params string[] fileNames)
		{
			return RemoveFiles((IEnumerable<string>)fileNames);
		}
		
		[Obsolete("Use RemoveFiles/AddOrUpdateFiles instead")]
		public IProjectContent UpdateProjectContent(IUnresolvedFile oldFile, IUnresolvedFile newFile)
		{
			if (oldFile == null && newFile == null)
				return this;
			if (oldFile != null && newFile != null) {
				if (!Platform.FileNameComparer.Equals(oldFile.FileName, newFile.FileName))
					throw new ArgumentException("When both oldFile and newFile are specified, they must use the same file name.");
			}
			CSharpProjectContent pc = Clone();
			if (newFile == null)
				pc.unresolvedFiles.Remove(oldFile.FileName);
			else
				pc.unresolvedFiles[newFile.FileName] = newFile;
			return pc;
		}
		
		[Obsolete("Use RemoveFiles/AddOrUpdateFiles instead")]
		public IProjectContent UpdateProjectContent(IEnumerable<IUnresolvedFile> oldFiles, IEnumerable<IUnresolvedFile> newFiles)
		{
			CSharpProjectContent pc = Clone();
			if (oldFiles != null) {
				foreach (var oldFile in oldFiles) {
					pc.unresolvedFiles.Remove(oldFile.FileName);
				}
			}
			if (newFiles != null) {
				foreach (var newFile in newFiles) {
					pc.unresolvedFiles.Add(newFile.FileName, newFile);
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
