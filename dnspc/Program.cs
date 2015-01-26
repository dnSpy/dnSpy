// Simple hack to decompile code from the command line.

using System;
using System.IO;
using System.Collections.Generic;
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
		static string outputDir;
		static List<string> files;
		static List<string> asmPaths;
		static string language;
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

		static bool IsUnix() {
			// See http://mono-project.com/FAQ:_Technical#Mono_Platforms for platform detection.
			int p = (int)Environment.OSVersion.Platform;
			return p == 4 || p == 6 || p == 128;
		}

		static char PATHS_SEP = IsUnix() ? ':' : ';';

		static void PrintHelp() {
			var progName = GetProgramBaseName();
			Console.WriteLine("{0} [--stdout] [--asm-path path] [--no-gac] [--proj-dir-suffix suffix] [-r] [-o outdir] [-l lang] [fileOrDir1] [fileOrDir2] [...]", progName);
			Console.WriteLine("  --stdout     decompile to the screen");
			Console.WriteLine("  --proj-dir-suffix suffix   append 'suffix' to project dir name");
			Console.WriteLine("  --asm-path path    Asm search paths. Paths can be separated with '{0}'", PATHS_SEP);
			Console.WriteLine("  --no-gac     don't use the GAC to look up assemblies");
			Console.WriteLine("  -r           recursive search");
			Console.WriteLine("  -o outdir    output directory");
			Console.WriteLine("  -l lang      set language, default is C#");
			Console.WriteLine("Languages:");
			foreach (var lang in Languages.AllLanguages)
				Console.WriteLine("  {0}", lang.Name);
			Console.WriteLine(@"Examples:
  {0} --stdout C:\some\path\file.dll
      Decompile file.dll to the screen
  {0} C:\some\path
      Decompile all .NET files in the above directory
  {0} -r C:\some\path
      Decompile all .NET files in the above directory and all sub directories
  {0} -o C:\out\path C:\some\path
      Decompile all .NET files in the above directory and save files in C:\out\path
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
						outputDir = next;
						i++;
						break;

					case "l":
					case "-lang":
						language = next;
						i++;
						break;

					case "-proj-dir-suffix":
						projDirSuffix = next ?? string.Empty;
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

					default:
						throw new ErrorException(string.Format("Invalid option: {0}", arg));
					}
				}
				else
					files.Add(arg);
			}
		}

		static void DoIt() {
			foreach (var file in files) {
				if (File.Exists(file))
					DumpFile(file);
				else if (Directory.Exists(file))
					DumpDir(file, null);
				else {
					var path = Path.GetDirectoryName(file);
					var name = Path.GetFileName(file);
					if (Directory.Exists(path))
						DumpDir(path, name);
					else
						throw new ErrorException(string.Format("File/dir '{0}' doesn't exist", file));
				}
			}
		}

		static void DumpDir(string path, string pattern) {
			pattern = pattern ?? "*";
			DumpDir2(path, pattern);
			if (isRecursive) {
				foreach (var di in new DirectoryInfo(path).GetDirectories("*", SearchOption.AllDirectories))
					DumpDir2(di.FullName, pattern);
			}
		}

		static void DumpDir2(string path, string pattern) {
			pattern = pattern ?? "*";
			foreach (var fi in new DirectoryInfo(path).GetFiles(pattern)) {
				var fname = OpenNetFile(fi.FullName);
				if (fname == null)
					continue;
				DumpNetModule(fname);
			}
		}

		static string OpenNetFile(string file) {
			try {
				file = Path.GetFullPath(file);
				if (!File.Exists(file))
					return null;
				using (var mod = ModuleDefMD.Load(file)) {
				}
				return file;
			}
			catch {
			}
			return null;
		}

		static void DumpFile(string file) {
			var fname = OpenNetFile(file);
			if (fname == null)
				throw new Exception(string.Format("{0} is not a .NET file", file));
			DumpNetModule(fname);
		}

		static void DumpNetModule(string fileName) {
			if (string.IsNullOrEmpty(fileName))
				throw new Exception(".NET module filename is empty or null");

			var asmList = new AssemblyList("MyListName");
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
					var baseDir = GetProjectDir(fileName);
					Directory.CreateDirectory(baseDir);
					var projFileName = Path.GetFileNameWithoutExtension(fileName) + (lang.ProjectFileExtension ?? ".XXproj");
					var projFilePath = Path.Combine(baseDir, projFileName);
					writer = new StreamWriter(projFilePath, false, System.Text.Encoding.UTF8);
					opts.SaveAsProjectDirectory = baseDir;
					Console.WriteLine("Saving {0} to {1}", Path.GetFileName(fileName), baseDir);
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

		static string GetProjectDir(string fileName) {
			if (useStdout)
				throw new Exception("Shouldn't be here when --stdout was used");

			var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
			if (string.IsNullOrEmpty(outputDir)) {
				var path = Path.GetDirectoryName(fileName);
				return Path.Combine(path, nameNoExt + projDirSuffix);
			}
			else
				return Path.Combine(outputDir, nameNoExt + projDirSuffix);
		}
	}
}
