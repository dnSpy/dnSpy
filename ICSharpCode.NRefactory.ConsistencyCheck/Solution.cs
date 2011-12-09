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
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	public class Solution
	{
		public readonly string Directory;
		public readonly List<CSharpProject> Projects = new List<CSharpProject>();
		public readonly ISolutionSnapshot SolutionSnapshot = new DefaultSolutionSnapshot();
		
		public IEnumerable<CSharpFile> AllFiles {
			get {
				return Projects.SelectMany(p => p.Files);
			}
		}
		
		public Solution(string fileName)
		{
			this.Directory = Path.GetDirectoryName(fileName);
			var projectLinePattern = new Regex("Project\\(\"(?<TypeGuid>.*)\"\\)\\s+=\\s+\"(?<Title>.*)\",\\s*\"(?<Location>.*)\",\\s*\"(?<Guid>.*)\"");
			foreach (string line in File.ReadLines(fileName)) {
				Match match = projectLinePattern.Match(line);
				if (match.Success) {
					string typeGuid = match.Groups["TypeGuid"].Value;
					string title    = match.Groups["Title"].Value;
					string location = match.Groups["Location"].Value;
					string guid     = match.Groups["Guid"].Value;
					switch (typeGuid.ToUpperInvariant()) {
						case "{2150E333-8FDC-42A3-9474-1A3956D46DE8}": // Solution Folder
							// ignore folders
							break;
						case "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}": // C# project
							Projects.Add(new CSharpProject(this, title, Path.Combine(Directory, location)));
							break;
						default:
							Console.WriteLine("Project {0} has unsupported type {1}", location, typeGuid);
							break;
					}
				}
			}
		}
	}
}
