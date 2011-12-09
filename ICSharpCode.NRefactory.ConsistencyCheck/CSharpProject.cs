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
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	public class CSharpProject
	{
		public readonly Solution Solution;
		public readonly string Title;
		public readonly string AssemblyName;
		public readonly string FileName;
		
		public readonly List<CSharpFile> Files = new List<CSharpFile>();
		
		public readonly bool AllowUnsafeBlocks;
		public readonly bool CheckForOverflowUnderflow;
		public readonly string[] PreprocessorDefines;
		
		public IProjectContent ProjectContent;
		
		public ICompilation Compilation {
			get {
				return Solution.SolutionSnapshot.GetCompilation(ProjectContent);
			}
		}
		
		public CSharpProject(Solution solution, string title, string fileName)
		{
			this.Solution = solution;
			this.Title = title;
			this.FileName = fileName;
			
			var p = new Microsoft.Build.Evaluation.Project(fileName);
			this.AssemblyName = p.GetPropertyValue("AssemblyName");
			this.AllowUnsafeBlocks = GetBoolProperty(p, "AllowUnsafeBlocks") ?? false;
			this.CheckForOverflowUnderflow = GetBoolProperty(p, "CheckForOverflowUnderflow") ?? false;
			this.PreprocessorDefines = p.GetPropertyValue("DefineConstants").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var item in p.GetItems("Compile")) {
				Files.Add(new CSharpFile(this, Path.Combine(p.DirectoryPath, item.EvaluatedInclude)));
			}
			List<IAssemblyReference> references = new List<IAssemblyReference>();
			string mscorlib = FindAssembly(Program.AssemblySearchPaths, "mscorlib");
			if (mscorlib != null) {
				references.Add(Program.LoadAssembly(mscorlib));
			} else {
				Console.WriteLine("Could not find mscorlib");
			}
			foreach (var item in p.GetItems("Reference")) {
				string assemblyFileName = null;
				if (item.HasMetadata("HintPath")) {
					assemblyFileName = Path.Combine(p.DirectoryPath, item.GetMetadataValue("HintPath"));
					if (!File.Exists(assemblyFileName))
						assemblyFileName = null;
				}
				if (assemblyFileName == null) {
					assemblyFileName = FindAssembly(Program.AssemblySearchPaths, item.EvaluatedInclude);
				}
				if (assemblyFileName != null) {
					references.Add(Program.LoadAssembly(assemblyFileName));
				} else {
					Console.WriteLine("Could not find referenced assembly " + item.EvaluatedInclude);
				}
			}
			foreach (var item in p.GetItems("ProjectReference")) {
				references.Add(new ProjectReference(solution, item.GetMetadataValue("Name")));
			}
			this.ProjectContent = new CSharpProjectContent()
				.SetAssemblyName(this.AssemblyName)
				.AddAssemblyReferences(references)
				.UpdateProjectContent(null, Files.Select(f => f.ParsedFile));
		}
		
		string FindAssembly(IEnumerable<string> assemblySearchPaths, string evaluatedInclude)
		{
			if (evaluatedInclude.IndexOf(',') >= 0)
				evaluatedInclude = evaluatedInclude.Substring(0, evaluatedInclude.IndexOf(','));
			foreach (string searchPath in assemblySearchPaths) {
				string assemblyFile = Path.Combine(searchPath, evaluatedInclude + ".dll");
				if (File.Exists(assemblyFile))
					return assemblyFile;
			}
			return null;
		}
		
		static bool? GetBoolProperty(Microsoft.Build.Evaluation.Project p, string propertyName)
		{
			string val = p.GetPropertyValue(propertyName);
			if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
				return true;
			if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
				return false;
			return null;
		}
		
		public CSharpParser CreateParser()
		{
			List<string> args = new List<string>();
			if (AllowUnsafeBlocks)
				args.Add("-unsafe");
			foreach (string define in PreprocessorDefines)
				args.Add("-d:" + define);
			return new CSharpParser(args.ToArray());
		}
	}
	
	public class ProjectReference : IAssemblyReference
	{
		readonly Solution solution;
		readonly string projectTitle;
		
		public ProjectReference(Solution solution, string projectTitle)
		{
			this.solution = solution;
			this.projectTitle = projectTitle;
		}
		
		public IAssembly Resolve(ITypeResolveContext context)
		{
			var project = solution.Projects.Single(p => string.Equals(p.Title, projectTitle, StringComparison.OrdinalIgnoreCase));
			return project.ProjectContent.Resolve(context);
		}
	}
	
	public class CSharpFile
	{
		public readonly CSharpProject Project;
		public readonly string FileName;
		
		public readonly ITextSource Content;
		public readonly int LinesOfCode;
		public CompilationUnit CompilationUnit;
		public CSharpParsedFile ParsedFile;
		
		public CSharpFile(CSharpProject project, string fileName)
		{
			this.Project = project;
			this.FileName = fileName;
			this.Content = new StringTextSource(File.ReadAllText(FileName));
			this.LinesOfCode = 1 + this.Content.Text.Count(c => c == '\n');
			
			CSharpParser p = project.CreateParser();
			this.CompilationUnit = p.Parse(Content.CreateReader(), fileName);
			if (p.HasErrors) {
				Console.WriteLine("Error parsing " + fileName + ":");
				foreach (var error in p.ErrorPrinter.Errors) {
					Console.WriteLine("  " + error.Region + " " + error.Message);
				}
			}
			this.ParsedFile = this.CompilationUnit.ToTypeSystem();
		}
	}
}
