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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	class Program
	{
		public static readonly string[] AssemblySearchPaths = {
			@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0",
			@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\v3.5",
			@"C:\Windows\Microsoft.NET\Framework\v2.0.50727",
			@"C:\Program Files (x86)\GtkSharp\2.12\lib\gtk-sharp-2.0",
			@"C:\Program Files (x86)\GtkSharp\2.12\lib\Mono.Posix",
			@"C:\work\SD\src\Tools\NUnit"
		};
		//public const string SolutionFile = @"C:\work\NRefactory\NRefactory.sln";
		//public const string SolutionFile = @"C:\work\SD\SharpDevelop.sln";
		public const string SolutionFile = @"C:\work\ILSpy\ILSpy.sln";
		
		public const string TempPath = @"C:\temp";
		
		static Solution solution;
		
		public static void Main(string[] args)
		{
			using (new Timer("Loading solution... ")) {
				solution = new Solution(SolutionFile);
			}
			
			Console.WriteLine("Loaded {0} lines of code ({1:f1} MB) in {2} files in {3} projects.",
			                  solution.AllFiles.Sum(f => f.LinesOfCode),
			                  solution.AllFiles.Sum(f => f.Content.TextLength) / 1024.0 / 1024.0,
			                  solution.AllFiles.Count(),
			                  solution.Projects.Count);
			
			//RunTestOnAllFiles("Roundtripping test", RoundtripTest.RunTest);
			RunTestOnAllFiles("Resolver test", ResolverTest.RunTest);
			RunTestOnAllFiles("Resolver test (randomized order)", RandomizedOrderResolverTest.RunTest);
			new FindReferencesConsistencyCheck(solution).Run();
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		
		static void RunTestOnAllFiles(string title, Action<CSharpFile> runTest)
		{
			using (new Timer(title + "... ")) {
				foreach (var file in solution.AllFiles) {
					runTest(file);
				}
			}
		}
		
		static ConcurrentDictionary<string, IUnresolvedAssembly> assemblyDict = new ConcurrentDictionary<string, IUnresolvedAssembly>(Platform.FileNameComparer);
		
		public static IUnresolvedAssembly LoadAssembly(string assemblyFileName)
		{
			return assemblyDict.GetOrAdd(assemblyFileName, file => new CecilLoader().LoadAssemblyFile(file));
		}
	}
	
	sealed class Timer : IDisposable
	{
		Stopwatch w = Stopwatch.StartNew();
		
		public Timer(string title)
		{
			Console.Write(title);
		}
		
		public void Dispose()
		{
			Console.WriteLine(w.Elapsed);
		}
	}
}