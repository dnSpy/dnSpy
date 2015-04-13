/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

// Simple hack to decompile code from the command line.

using System;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using ICSharpCode.ILSpy;
using ICSharpCode.Decompiler;

namespace ilspc {
	[Serializable]
	class ErrorException : Exception {
		public ErrorException(string s)
			: base(s) {
		}
	}

	class Program {
		static bool useStdout;
		static bool isRecursive;
		static bool noGac;
		static bool noCorlibRef;
		static bool createSlnFile;
		static bool dontMaskErr;
		static string outputDir;
		static string slnName;
		static List<string> files;
		static List<string> asmPaths;
		static string language = "C#";
		static string projDirSuffix = string.Empty;

		static int Main(string[] args) {
			if (!dnlib.Settings.IsThreadSafe) {
				Console.WriteLine("dnlib wasn't compiled with THREAD_SAFE defined");
				return 1;
			}

			try {
				InitILSpy();
				ParseCommandLine(args);
				DoIt();
			}
			catch (ErrorException ex) {
				PrintHelp();
				Console.WriteLine("ERROR: {0}", ex.Message);
				return 1;
			}
			catch (Exception ex) {
				DumpEx(ex);
				return 1;
			}
			return 0;
		}

		static char PATHS_SEP = Path.PathSeparator;

		static void PrintHelp() {
			var progName = GetProgramBaseName();
			Console.WriteLine("{0} [--stdout] [--asm-path path] [--no-gac] [--no-stdlib] [--sln] [--sln-name name] [--proj-dir-suffix suffix] [--dont-mask-merr] [-r] [-o outdir] [-l lang] [fileOrDir1] [fileOrDir2] [...]", progName);
			Console.WriteLine("  --stdout     decompile to the screen");
			Console.WriteLine("  --asm-path path    assembly search path. Paths can be separated with '{0}' or multiple --asm-path's can be used", PATHS_SEP);
			Console.WriteLine("  --no-gac     don't use the GAC to look up assemblies. Useful with --no-stdlib");
			Console.WriteLine("  --no-stdlib  projects don't reference mscorlib");
			Console.WriteLine("  --sln        create a .sln file");
			Console.WriteLine("  --sln-name name   name of the .sln file");
			Console.WriteLine("  --proj-dir-suffix suffix   append 'suffix' to project dir name");
			Console.WriteLine("  --dont-mask-merr   don't catch method exceptions when decompiling");
			Console.WriteLine("  -r           recursive search");
			Console.WriteLine("  -o outdir    output directory");
			Console.WriteLine("  -l lang      set language, default is C#");
			Console.WriteLine("Languages:");
			foreach (var lang in Languages.AllLanguages)
				Console.WriteLine("  {0}", lang.Name);
			Console.WriteLine(@"Examples:
  {0} --stdout C:\some\path\file.dll
      Decompile file.dll to the screen
  {0} -o c:\out\path C:\some\path
      Decompile all .NET files in the above directory and save files in C:\out\path
  {0} -o c:\out\path -r C:\some\path
      Decompile all .NET files in the above directory and all sub directories
  {0} -o C:\out\path C:\some\path\*.dll
      Decompile all *.dll .NET files in the above directory and save files in C:\out\path
", progName);
		}

		static void DumpEx(Exception ex) {
			while (ex != null) {
				Console.WriteLine("ERROR: {0}", ex.GetType());
				Console.WriteLine("  {0}", ex.Message);
				Console.WriteLine("  {0}", ex.StackTrace);
				ex = ex.InnerException;
			}
		}

		static void InitILSpy() {
			Languages.Initialize();
		}

		static string GetProgramBaseName() {
			return GetBaseName(Environment.GetCommandLineArgs()[0]);
		}

		static string GetBaseName(string name) {
			int index = name.LastIndexOf(Path.DirectorySeparatorChar);
			if (index < 0)
				return name;
			return name.Substring(index + 1);
		}

		static void ParseCommandLine(string[] args) {
			if (args.Length == 0)
				throw new ErrorException("No options specified");

			files = new List<string>();
			asmPaths = new List<string>();
			bool canParseCommands = true;
			for (int i = 0; i < args.Length; i++) {
				var arg = args[i];
				var next = i + 1 < args.Length ? args[i + 1] : null;
				if (arg.Length == 0)
					continue;
				if (canParseCommands && arg[0] == '-') {
					switch (arg.Remove(0, 1)) {
					case "":
						canParseCommands = false;
						break;

					case "r":
					case "-recursive":
						isRecursive = true;
						break;

					case "o":
					case "-output-dir":
						if (next == null)
							throw new ErrorException("Missing output directory");
						outputDir = next;
						i++;
						break;

					case "l":
					case "-lang":
						if (next == null)
							throw new ErrorException("Missing language name");
						language = next;
						i++;
						if (!language.Equals(GetLanguage().Name, StringComparison.OrdinalIgnoreCase))
							throw new ErrorException(string.Format("Language '{0}' doesn't exist", language));
						break;

					case "-proj-dir-suffix":
						if (next == null)
							throw new ErrorException("Missing project directory suffix");
						projDirSuffix = next;
						i++;
						break;

					case "-stdout":
						useStdout = true;
						break;

					case "-asm-path":
						if (next == null)
							throw new ErrorException("Missing assembly search path");
						asmPaths.AddRange(next.Split(new char[] { PATHS_SEP }, StringSplitOptions.RemoveEmptyEntries));
						i++;
						break;

					case "-no-gac":
						noGac = true;
						break;

					case "-no-stdlib":
						noCorlibRef = true;
						break;

					case "-sln":
						createSlnFile = true;
						break;

					case "-sln-name":
						if (next == null)
							throw new ErrorException("Missing .sln name");
						slnName = next;
						i++;
						if (Path.IsPathRooted(slnName))
							throw new ErrorException(string.Format(".sln name ({0}) must be relative to project dir", slnName));
						break;

					case "-dont-mask-merr":
						dontMaskErr = true;
						break;

					default:
						throw new ErrorException(string.Format("Invalid option: {0}", arg));
					}
				}
				else
					files.Add(arg);
			}
		}

		static void DoIt() {
			if (useStdout || !createSlnFile) {
				foreach (var info in GetDotNetFiles())
					DumpNetModule(info, null);
			}
			else {
				var projectFiles = new List<ProjectInfo>(GetDotNetFiles());
				foreach (var info in projectFiles)
					DumpNetModule(info, projectFiles);

				var slnPathName = Path.Combine(outputDir, slnName ?? "solution.sln");
				using (var writer = new StreamWriter(slnPathName, false, Encoding.UTF8)) {
					const string crlf = "\r\n";	// Make sure it's always CRLF
					writer.Write(crlf);
					writer.Write("Microsoft Visual Studio Solution File, Format Version 11.00" + crlf);
					writer.Write("# Visual Studio 2010" + crlf);
					var slnGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
					foreach (var info in projectFiles) {
						writer.Write("Project(\"{0}\") = \"{1}\", \"{1}\\{2}\", \"{3}\"" + crlf,
							slnGuid,
							Path.GetFileName(Path.GetDirectoryName(info.ProjectFileName)),
							Path.GetFileName(info.ProjectFileName),
							info.ProjectGuid.ToString("B").ToUpperInvariant()
						);
						writer.Write("EndProject" + crlf);
					}
					writer.Write("Global" + crlf);
					writer.Write("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution" + crlf);
					writer.Write("\t\tDebug|Any CPU = Debug|Any CPU" + crlf);
					writer.Write("\t\tRelease|Any CPU = Release|Any CPU" + crlf);
					writer.Write("\tEndGlobalSection" + crlf);
					writer.Write("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution" + crlf);
					foreach (var info in projectFiles) {
						var prjGuid = info.ProjectGuid.ToString("B").ToUpperInvariant();
						writer.Write("\t\t{0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU" + crlf, prjGuid);
						writer.Write("\t\t{0}.Debug|Any CPU.Build.0 = Debug|Any CPU" + crlf, prjGuid);
						writer.Write("\t\t{0}.Release|Any CPU.ActiveCfg = Release|Any CPU" + crlf, prjGuid);
						writer.Write("\t\t{0}.Release|Any CPU.Build.0 = Release|Any CPU" + crlf, prjGuid);
					}
					writer.Write("\tEndGlobalSection" + crlf);
					writer.Write("\tGlobalSection(SolutionProperties) = preSolution" + crlf);
					writer.Write("\t\tHideSolutionNode = FALSE" + crlf);
					writer.Write("\tEndGlobalSection" + crlf);
					writer.Write("EndGlobal" + crlf);
				}
			}
		}

		static IEnumerable<ProjectInfo> GetDotNetFiles() {
			foreach (var file in files) {
				if (File.Exists(file)) {
					var info = OpenNetFile(file);
					if (info == null)
						throw new Exception(string.Format("{0} is not a .NET file", file));
					yield return info;
				}
				else if (Directory.Exists(file)) {
					foreach (var info in DumpDir(file, null))
						yield return info;
				}
				else {
					var path = Path.GetDirectoryName(file);
					var name = Path.GetFileName(file);
					if (Directory.Exists(path)) {
						foreach (var info in DumpDir(path, name))
							yield return info;
					}
					else
						throw new ErrorException(string.Format("File/dir '{0}' doesn't exist", file));
				}
			}
		}

		static IEnumerable<ProjectInfo> DumpDir(string path, string pattern) {
			pattern = pattern ?? "*";
			Stack<string> stack = new Stack<string>();
			stack.Push(path);
			while (stack.Count > 0) {
				path = stack.Pop();
				foreach (var info in DumpDir2(path, pattern))
					yield return info;
				if (isRecursive) {
					foreach (var di in GetDirs(path))
						stack.Push(di.FullName);
				}
			}
		}

		static IEnumerable<DirectoryInfo> GetDirs(string path) {
			IEnumerable<FileSystemInfo> fsysIter = null;
			try {
				fsysIter = new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			if (fsysIter == null)
				yield break;

			foreach (var info in fsysIter) {
				if ((info.Attributes & System.IO.FileAttributes.Directory) == 0)
					continue;
				DirectoryInfo di = null;
				try {
					di = new DirectoryInfo(info.FullName);
				}
				catch (IOException) {
				}
				catch (UnauthorizedAccessException) {
				}
				catch (SecurityException) {
				}
				if (di != null)
					yield return di;
			}
		}

		static IEnumerable<ProjectInfo> DumpDir2(string path, string pattern) {
			pattern = pattern ?? "*";
			foreach (var fi in GetFiles(path, pattern)) {
				var info = OpenNetFile(fi.FullName);
				if (info != null)
					yield return info;
			}
		}

		static IEnumerable<FileInfo> GetFiles(string path, string pattern) {
			IEnumerable<FileSystemInfo> fsysIter = null;
			try {
				fsysIter = new DirectoryInfo(path).EnumerateFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			if (fsysIter == null)
				yield break;

			foreach (var info in fsysIter) {
				if ((info.Attributes & System.IO.FileAttributes.Directory) != 0)
					continue;
				FileInfo fi = null;
				try {
					fi = new FileInfo(info.FullName);
				}
				catch (IOException) {
				}
				catch (UnauthorizedAccessException) {
				}
				catch (SecurityException) {
				}
				if (fi != null)
					yield return fi;
			}
		}

		static ProjectInfo OpenNetFile(string file) {
			try {
				file = Path.GetFullPath(file);
				if (!File.Exists(file))
					return null;
				string asmName = null;
				using (var mod = ModuleDefMD.Load(file)) {
					if (mod.Assembly != null)
						asmName = mod.Assembly.Name;
				}

				var projFileName = GetProjectFileName(file);
				projFileName = Path.Combine(GetProjectDir(GetLanguage(), file), projFileName);
				return new ProjectInfo {
					AssemblyFileName = file,
					AssemblySimpleName = asmName,
					ProjectFileName = projFileName,
					ProjectGuid = Guid.NewGuid(),
				};
			}
			catch {
			}
			return null;
		}

		static void DumpNetModule(ProjectInfo info, List<ProjectInfo> projectFiles) {
			var fileName = info.AssemblyFileName;
			if (string.IsNullOrEmpty(fileName))
				throw new Exception(".NET module filename is empty or null");

			var asmList = new AssemblyList("dnspc.exe");
			asmList.UseGAC = !noGac;
			asmList.AddSearchPath(Path.GetDirectoryName(fileName));
			foreach (var path in asmPaths)
				asmList.AddSearchPath(path);
			var lasm = new LoadedAssembly(asmList, fileName);
			var opts = new DecompilationOptions {
				FullDecompilation = true,
				CancellationToken = new CancellationToken(),
			};

			TextWriter writer = null;
			try {
				var lang = GetLanguage();

				if (useStdout)
					writer = System.Console.Out;
				else {
					var baseDir = GetProjectDir(lang, fileName);
					Directory.CreateDirectory(baseDir);
					writer = new StreamWriter(info.ProjectFileName, false, Encoding.UTF8);
					opts.SaveAsProjectDirectory = baseDir;
					opts.DontReferenceStdLib = noCorlibRef;
					opts.ProjectFiles = projectFiles;
					opts.ProjectGuid = info.ProjectGuid;
					opts.DontShowCreateMethodBodyExceptions = dontMaskErr;
					Console.WriteLine("Saving {0} to {1}", fileName, baseDir);
				}

				lang.DecompileAssembly(lasm, new PlainTextOutput(writer), opts);
			}
			finally {
				if (!useStdout && writer != null)
					writer.Dispose();
			}
		}

		static Language GetLanguage() {
			return Languages.GetLanguage(language);
		}

		static string GetProjectDir(Language lang, string fileName) {
			if (lang.ProjectFileExtension == null)
				return outputDir;
			var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
			if (string.IsNullOrEmpty(outputDir)) {
				var path = Path.GetDirectoryName(fileName);
				return Path.Combine(path, nameNoExt + projDirSuffix);
			}
			else
				return Path.Combine(outputDir, nameNoExt + projDirSuffix);
		}

		static string GetProjectFileName(string fileName) {
			var lang = GetLanguage();
			var ext = lang.ProjectFileExtension ?? lang.FileExtension;
			return Path.GetFileNameWithoutExtension(fileName) + ext;
		}
	}
}
