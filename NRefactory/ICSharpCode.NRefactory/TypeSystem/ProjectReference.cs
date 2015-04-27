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
	/// References another project content in the same solution.
	/// Using the <see cref="ProjectReference"/> class requires that you 
	/// </summary>
	[Serializable]
	public class ProjectReference : IAssemblyReference
	{
		readonly string projectFileName;
		
		/// <summary>
		/// Creates a new reference to the specified project (must be part of the same solution).
		/// </summary>
		/// <param name="projectFileName">Full path to the file name. Must be identical to <see cref="IProjectContent.ProjectFileName"/> of the target project; do not use a relative path.</param>
		public ProjectReference(string projectFileName)
		{
			this.projectFileName = projectFileName;
		}
		
		public IAssembly Resolve(ITypeResolveContext context)
		{
			var solution = context.Compilation.SolutionSnapshot;
			var pc = solution.GetProjectContent(projectFileName);
			if (pc != null)
				return pc.Resolve(context);
			else
				return null;
		}
		
		public override string ToString()
		{
			return string.Format("[ProjectReference {0}]", projectFileName);
		}
	}
}
