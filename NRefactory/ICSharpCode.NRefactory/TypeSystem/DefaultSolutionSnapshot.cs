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
using System.Collections.Concurrent;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Default implementation of ISolutionSnapshot.
	/// </summary>
	public class DefaultSolutionSnapshot : ISolutionSnapshot
	{
		readonly Dictionary<string, IProjectContent> projectDictionary = new Dictionary<string, IProjectContent>(Platform.FileNameComparer);
		ConcurrentDictionary<IProjectContent, ICompilation> dictionary = new ConcurrentDictionary<IProjectContent, ICompilation>();
		
		/// <summary>
		/// Creates a new DefaultSolutionSnapshot with the specified projects.
		/// </summary>
		public DefaultSolutionSnapshot(IEnumerable<IProjectContent> projects)
		{
			foreach (var project in projects) {
				if (project.ProjectFileName != null)
					projectDictionary.Add(project.ProjectFileName, project);
			}
		}
		
		/// <summary>
		/// Creates a new DefaultSolutionSnapshot that does not support <see cref="ProjectReference"/>s.
		/// </summary>
		public DefaultSolutionSnapshot()
		{
		}
		
		public IProjectContent GetProjectContent(string projectFileName)
		{
			IProjectContent pc;
			lock (projectDictionary) {
				if (projectDictionary.TryGetValue(projectFileName, out pc))
					return pc;
				else
					return null;
			}
		}
		
		public ICompilation GetCompilation(IProjectContent project)
		{
			if (project == null)
				throw new ArgumentNullException("project");
			return dictionary.GetOrAdd(project, p => p.CreateCompilation(this));
		}
		
		public void AddCompilation(IProjectContent project, ICompilation compilation)
		{
			if (project == null)
				throw new ArgumentNullException("project");
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (!dictionary.TryAdd(project, compilation))
				throw new InvalidOperationException();
			if (project.ProjectFileName != null) {
				lock (projectDictionary) {
					projectDictionary.Add(project.ProjectFileName, project);
				}
			}
		}
	}
}
