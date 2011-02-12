//
// driver.cs: The compiler command line driver.
//
// Authors:
//   Miguel de Icaza (miguel@gnu.org)
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004, 2005, 2006, 2007, 2008 Novell, Inc
//

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace Mono.CSharp
{
	/// <summary>
	///    The compiler driver.
	/// </summary>
	class Driver
	{
		string first_source;

		internal int fatal_errors;
		
		internal readonly CompilerContext ctx;

		static readonly char[] argument_value_separator = new char [] { ';', ',' };

		private Driver (CompilerContext ctx)
		{
			this.ctx = ctx;
		}

		public static Driver Create (string[] args, bool require_files, Func<string [], int, int> unknown_option_parser, ReportPrinter printer)
		{
			Driver d = new Driver (new CompilerContext (new Report (printer)));

			if (!d.ParseArguments (args, require_files, unknown_option_parser))
				return null;

			return d;
		}

		Report Report {
			get { return ctx.Report; }
		}
       
		void tokenize_file (CompilationUnit file, CompilerContext ctx)
		{
			Stream input;

			try {
				input = File.OpenRead (file.Name);
			} catch {
				Report.Error (2001, "Source file `" + file.Name + "' could not be found");
				return;
			}

			using (input){
				SeekableStreamReader reader = new SeekableStreamReader (input, RootContext.Encoding);
				Tokenizer lexer = new Tokenizer (reader, file, ctx);
				int token, tokens = 0, errors = 0;

				while ((token = lexer.token ()) != Token.EOF){
					tokens++;
					if (token == Token.ERROR)
						errors++;
				}
				Console.WriteLine ("Tokenized: " + tokens + " found " + errors + " errors");
			}
			
			return;
		}

		void Parse (CompilationUnit file, ModuleContainer module)
		{
			Stream input;

			try {
				input = File.OpenRead (file.Name);
			} catch {
				Report.Error (2001, "Source file `{0}' could not be found", file.Name);
				return;
			}

			// Check 'MZ' header
			if (input.ReadByte () == 77 && input.ReadByte () == 90) {
				Report.Error (2015, "Source file `{0}' is a binary file and not a text file", file.Name);
				input.Close ();
				return;
			}

			input.Position = 0;
			SeekableStreamReader reader = new SeekableStreamReader (input, RootContext.Encoding);

			Parse (reader, file, module);
			reader.Dispose ();
			input.Close ();
		}	
		
		void Parse (SeekableStreamReader reader, CompilationUnit file, ModuleContainer module)
		{
			CSharpParser parser = new CSharpParser (reader, file, module);
			parser.parse ();
		}

		static void OtherFlags ()
		{
			Console.WriteLine (
				"Other flags in the compiler\n" +
				"   --fatal[=COUNT]    Makes errors after COUNT fatal\n" +
				"   --lint             Enhanced warnings\n" +
				"   --parse            Only parses the source file\n" +
				"   --runtime:VERSION  Sets mscorlib.dll metadata version: v1, v2, v4\n" +
				"   --stacktrace       Shows stack trace at error location\n" +
				"   --timestamp        Displays time stamps of various compiler events\n" +
				"   -v                 Verbose parsing (for debugging the parser)\n" + 
				"   --mcs-debug X      Sets MCS debugging level to X\n");
		}
		
		static void Usage ()
		{
			Console.WriteLine (
				"Mono C# compiler, Copyright 2001 - 2011 Novell, Inc.\n" +
				"mcs [options] source-files\n" +
				"   --about              About the Mono C# compiler\n" +
				"   -addmodule:M1[,Mn]   Adds the module to the generated assembly\n" + 
				"   -checked[+|-]        Sets default aritmetic overflow context\n" +
				"   -clscheck[+|-]       Disables CLS Compliance verifications\n" +
				"   -codepage:ID         Sets code page to the one in ID (number, utf8, reset)\n" +
				"   -define:S1[;S2]      Defines one or more conditional symbols (short: -d)\n" +
				"   -debug[+|-], -g      Generate debugging information\n" + 
				"   -delaysign[+|-]      Only insert the public key into the assembly (no signing)\n" +
				"   -doc:FILE            Process documentation comments to XML file\n" + 
				"   -fullpaths           Any issued error or warning uses absolute file path\n" +
				"   -help                Lists all compiler options (short: -?)\n" + 
				"   -keycontainer:NAME   The key pair container used to sign the output assembly\n" +
				"   -keyfile:FILE        The key file used to strongname the ouput assembly\n" +
				"   -langversion:TEXT    Specifies language version: ISO-1, ISO-2, 3, Default, or Future\n" + 
				"   -lib:PATH1[,PATHn]   Specifies the location of referenced assemblies\n" +
				"   -main:CLASS          Specifies the class with the Main method (short: -m)\n" +
				"   -noconfig            Disables implicitly referenced assemblies\n" +
				"   -nostdlib[+|-]       Does not reference mscorlib.dll library\n" +
				"   -nowarn:W1[,Wn]      Suppress one or more compiler warnings\n" + 
				"   -optimize[+|-]       Enables advanced compiler optimizations (short: -o)\n" + 
				"   -out:FILE            Specifies output assembly name\n" +
				"   -pkg:P1[,Pn]         References packages P1..Pn\n" + 
				"   -platform:ARCH       Specifies the target platform of the output assembly\n" +
				"                        ARCH can be one of: anycpu, x86, x64 or itanium\n" +
				"   -recurse:SPEC        Recursively compiles files according to SPEC pattern\n" + 
				"   -reference:A1[,An]   Imports metadata from the specified assembly (short: -r)\n" +
				"   -reference:ALIAS=A   Imports metadata using specified extern alias (short: -r)\n" +
				"   -sdk:VERSION         Specifies SDK version of referenced assemlies\n" +
				"                        VERSION can be one of: 2 (default), 4\n" +
				"   -target:KIND         Specifies the format of the output assembly (short: -t)\n" +
				"                        KIND can be one of: exe, winexe, library, module\n" +
				"   -unsafe[+|-]         Allows to compile code which uses unsafe keyword\n" +
				"   -warnaserror[+|-]    Treats all warnings as errors\n" +
				"   -warnaserror[+|-]:W1[,Wn] Treats one or more compiler warnings as errors\n" +
				"   -warn:0-4            Sets warning level, the default is 4 (short -w:)\n" +
				"   -helpinternal        Shows internal and advanced compiler options\n" + 
				"\n" +
				"Resources:\n" +
				"   -linkresource:FILE[,ID] Links FILE as a resource (short: -linkres)\n" +
				"   -resource:FILE[,ID]     Embed FILE as a resource (short: -res)\n" +
				"   -win32res:FILE          Specifies Win32 resource file (.res)\n" +
				"   -win32icon:FILE         Use this icon for the output\n" +
                                "   @file                   Read response file for more options\n\n" +
				"Options can be of the form -option or /option");
		}

		void TargetUsage ()
		{
			Report.Error (2019, "Invalid target type for -target. Valid options are `exe', `winexe', `library' or `module'");
		}
		
		static void About ()
		{
			Console.WriteLine (
				"The Mono C# compiler is Copyright 2001-2011, Novell, Inc.\n\n" +
				"The compiler source code is released under the terms of the \n"+
				"MIT X11 or GNU GPL licenses\n\n" +

				"For more information on Mono, visit the project Web site\n" +
				"   http://www.mono-project.com\n\n" +

				"The compiler was written by Miguel de Icaza, Ravi Pratap, Martin Baulig, Marek Safar, Raja R Harinath, Atushi Enomoto");
			Environment.Exit (0);
		}

		public static int Main (string[] args)
		{
			Location.InEmacs = Environment.GetEnvironmentVariable ("EMACS") == "t";
			var crp = new ConsoleReportPrinter ();
			Driver d = Driver.Create (args, true, null, crp);
			if (d == null)
				return 1;

			crp.Fatal = d.fatal_errors;

			if (d.Compile () && d.Report.Errors == 0) {
				if (d.Report.Warnings > 0) {
					Console.WriteLine ("Compilation succeeded - {0} warning(s)", d.Report.Warnings);
				}
				Environment.Exit (0);
				return 0;
			}
			
			
			Console.WriteLine("Compilation failed: {0} error(s), {1} warnings",
				d.Report.Errors, d.Report.Warnings);
			Environment.Exit (1);
			return 1;
		}

		static string [] LoadArgs (string file)
		{
			StreamReader f;
			var args = new List<string> ();
			string line;
			try {
				f = new StreamReader (file);
			} catch {
				return null;
			}

			StringBuilder sb = new StringBuilder ();
			
			while ((line = f.ReadLine ()) != null){
				int t = line.Length;

				for (int i = 0; i < t; i++){
					char c = line [i];
					
					if (c == '"' || c == '\''){
						char end = c;
						
						for (i++; i < t; i++){
							c = line [i];

							if (c == end)
								break;
							sb.Append (c);
						}
					} else if (c == ' '){
						if (sb.Length > 0){
							args.Add (sb.ToString ());
							sb.Length = 0;
						}
					} else
						sb.Append (c);
				}
				if (sb.Length > 0){
					args.Add (sb.ToString ());
					sb.Length = 0;
				}
			}

			return args.ToArray ();
		}

		//
		// Given a path specification, splits the path from the file/pattern
		//
		static void SplitPathAndPattern (string spec, out string path, out string pattern)
		{
			int p = spec.LastIndexOf ('/');
			if (p != -1){
				//
				// Windows does not like /file.cs, switch that to:
				// "\", "file.cs"
				//
				if (p == 0){
					path = "\\";
					pattern = spec.Substring (1);
				} else {
					path = spec.Substring (0, p);
					pattern = spec.Substring (p + 1);
				}
				return;
			}

			p = spec.LastIndexOf ('\\');
			if (p != -1){
				path = spec.Substring (0, p);
				pattern = spec.Substring (p + 1);
				return;
			}

			path = ".";
			pattern = spec;
		}

		void AddSourceFile (string f)
		{
			if (first_source == null)
				first_source = f;

			Location.AddFile (Report, f);
		}

		bool ParseArguments (string[] args, bool require_files, Func<string [], int, int> unknown_option_parser)
		{
			List<string> response_file_list = null;
			bool parsing_options = true;

			for (int i = 0; i < args.Length; i++) {
				string arg = args [i];
				if (arg.Length == 0)
					continue;

				if (arg [0] == '@') {
					string [] extra_args;
					string response_file = arg.Substring (1);

					if (response_file_list == null)
						response_file_list = new List<string> ();

					if (response_file_list.Contains (response_file)) {
						Report.Error (
							1515, "Response file `" + response_file +
							"' specified multiple times");
						return false;
					}

					response_file_list.Add (response_file);

					extra_args = LoadArgs (response_file);
					if (extra_args == null) {
						Report.Error (2011, "Unable to open response file: " +
								  response_file);
						return false;
					}

					args = AddArgs (args, extra_args);
					continue;
				}

				if (parsing_options) {
					if (arg == "--") {
						parsing_options = false;
						continue;
					}

					if (arg [0] == '-') {
						if (UnixParseOption (arg, ref args, ref i))
							continue;

						// Try a -CSCOPTION
						string csc_opt = "/" + arg.Substring (1);
						if (CSCParseOption (csc_opt, ref args))
							continue;

						if (unknown_option_parser != null){
							var ret = unknown_option_parser (args, i);
							if (ret != -1){
								i = ret;
								return true;
							}
						}
						
						Error_WrongOption (arg);
						return false;
					}
					if (arg [0] == '/') {
						if (CSCParseOption (arg, ref args))
							continue;

						// Need to skip `/home/test.cs' however /test.cs is considered as error
						if (arg.Length < 2 || arg.IndexOf ('/', 2) == -1) {
							Error_WrongOption (arg);
							return false;
						}
					}
				}

				ProcessSourceFiles (arg, false);
			}

			if (require_files == false)
				return true;
					
			//
			// If we are an exe, require a source file for the entry point
			//
			if (RootContext.Target == Target.Exe || RootContext.Target == Target.WinExe || RootContext.Target == Target.Module) {
				if (first_source == null) {
					Report.Error (2008, "No files to compile were specified");
					return false;
				}

			}

			//
			// If there is nothing to put in the assembly, and we are not a library
			//
			if (first_source == null && RootContext.Resources == null) {
				Report.Error (2008, "No files to compile were specified");
				return false;
			}

			return true;
		}

		public void Parse (ModuleContainer module)
		{
			Location.Initialize ();

			var cu = Location.SourceFiles;
			for (int i = 0; i < cu.Count; ++i) {
				if (RootContext.TokenizeOnly) {
					tokenize_file (cu [i], ctx);
				} else {
					Parse (cu [i], module);
				}
			}
		}

		void ProcessSourceFiles (string spec, bool recurse)
		{
			string path, pattern;

			SplitPathAndPattern (spec, out path, out pattern);
			if (pattern.IndexOf ('*') == -1){
				AddSourceFile (spec);
				return;
			}

			string [] files = null;
			try {
				files = Directory.GetFiles (path, pattern);
			} catch (System.IO.DirectoryNotFoundException) {
				Report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			} catch (System.IO.IOException){
				Report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			}
			foreach (string f in files) {
				AddSourceFile (f);
			}

			if (!recurse)
				return;
			
			string [] dirs = null;

			try {
				dirs = Directory.GetDirectories (path);
			} catch {
			}
			
			foreach (string d in dirs) {
					
				// Don't include path in this string, as each
				// directory entry already does
				ProcessSourceFiles (d + "/" + pattern, true);
			}
		}

		void SetWarningLevel (string s)
		{
			int level = -1;

			try {
				level = Int32.Parse (s);
			} catch {
			}
			if (level < 0 || level > 4){
				Report.Error (1900, "Warning level must be in the range 0-4");
				return;
			}
			Report.WarningLevel = level;
		}

		static void Version ()
		{
			string version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version.ToString ();
			Console.WriteLine ("Mono C# compiler version {0}", version);
			Environment.Exit (0);
		}
		
		//
		// Currently handles the Unix-like command line options, but will be
		// deprecated in favor of the CSCParseOption, which will also handle the
		// options that start with a dash in the future.
		//
		bool UnixParseOption (string arg, ref string [] args, ref int i)
		{
			switch (arg){
			case "-v":
				CSharpParser.yacc_verbose_flag++;
				return true;

			case "--version":
				Version ();
				return true;
				
			case "--parse":
				RootContext.ParseOnly = true;
				return true;
				
			case "--main": case "-m":
				Report.Warning (-29, 1, "Compatibility: Use -main:CLASS instead of --main CLASS or -m CLASS");
				if ((i + 1) >= args.Length){
					Usage ();
					Environment.Exit (1);
				}
				RootContext.MainClass = args [++i];
				return true;
				
			case "--unsafe":
				Report.Warning (-29, 1, "Compatibility: Use -unsafe instead of --unsafe");
				RootContext.Unsafe = true;
				return true;
				
			case "/?": case "/h": case "/help":
			case "--help":
				Usage ();
				Environment.Exit (0);
				return true;

			case "--define":
				Report.Warning (-29, 1, "Compatibility: Use -d:SYMBOL instead of --define SYMBOL");
				if ((i + 1) >= args.Length){
					Usage ();
					Environment.Exit (1);
				}
				RootContext.AddConditional (args [++i]);
				return true;

			case "--tokenize": 
				RootContext.TokenizeOnly = true;
				return true;
				
			case "-o": 
			case "--output":
				Report.Warning (-29, 1, "Compatibility: Use -out:FILE instead of --output FILE or -o FILE");
				if ((i + 1) >= args.Length){
					Usage ();
					Environment.Exit (1);
				}
				RootContext.OutputFile = args [++i];
				return true;

			case "--checked":
				Report.Warning (-29, 1, "Compatibility: Use -checked instead of --checked");
				RootContext.Checked = true;
				return true;
				
			case "--stacktrace":
				Report.Printer.Stacktrace = true;
				return true;
				
			case "--linkresource":
			case "--linkres":
				Report.Warning (-29, 1, "Compatibility: Use -linkres:VALUE instead of --linkres VALUE");
				if ((i + 1) >= args.Length){
					Usage ();
					Report.Error (5, "Missing argument to --linkres"); 
					Environment.Exit (1);
				}

				AddResource (new AssemblyResource (args[++i], args[i]));
				return true;
				
			case "--resource":
			case "--res":
				Report.Warning (-29, 1, "Compatibility: Use -res:VALUE instead of --res VALUE");
				if ((i + 1) >= args.Length){
					Usage ();
					Report.Error (5, "Missing argument to --resource"); 
					Environment.Exit (1);
				}

				AddResource (new AssemblyResource (args[++i], args[i], true));
				return true;
				
			case "--target":
				Report.Warning (-29, 1, "Compatibility: Use -target:KIND instead of --target KIND");
				if ((i + 1) >= args.Length){
					Environment.Exit (1);
					return true;
				}
				
				string type = args [++i];
				switch (type){
				case "library":
					RootContext.Target = Target.Library;
					RootContext.TargetExt = ".dll";
					break;
					
				case "exe":
					RootContext.Target = Target.Exe;
					break;
					
				case "winexe":
					RootContext.Target = Target.WinExe;
					break;
					
				case "module":
					RootContext.Target = Target.Module;
					RootContext.TargetExt = ".dll";
					break;
				default:
					TargetUsage ();
					break;
				}
				return true;
				
			case "-r":
				Report.Warning (-29, 1, "Compatibility: Use -r:LIBRARY instead of -r library");
				if ((i + 1) >= args.Length){
					Usage ();
					Environment.Exit (1);
				}
				
				string val = args [++i];
				int idx = val.IndexOf ('=');
				if (idx > -1) {
					string alias = val.Substring (0, idx);
					string assembly = val.Substring (idx + 1);
					AddAssemblyReference (alias, assembly);
					return true;
				}

				AddAssemblyReference (val);
				return true;
				
			case "-L":
				Report.Warning (-29, 1, "Compatibility: Use -lib:ARG instead of --L arg");
				if ((i + 1) >= args.Length){
					Usage ();	
					Environment.Exit (1);
				}
				RootContext.ReferencesLookupPaths.Add (args [++i]);
				return true;

			case "--lint":
				RootContext.EnhancedWarnings = true;
				return true;
				
			case "--nostdlib":
				Report.Warning (-29, 1, "Compatibility: Use -nostdlib instead of --nostdlib");
				RootContext.StdLib = false;
				return true;
				
			case "--nowarn":
				Report.Warning (-29, 1, "Compatibility: Use -nowarn instead of --nowarn");
				if ((i + 1) >= args.Length){
					Usage ();
					Environment.Exit (1);
				}
				int warn = 0;
				
				try {
					warn = Int32.Parse (args [++i]);
				} catch {
					Usage ();
					Environment.Exit (1);
				}
				Report.SetIgnoreWarning (warn);
				return true;

			case "--wlevel":
				Report.Warning (-29, 1, "Compatibility: Use -warn:LEVEL instead of --wlevel LEVEL");
				if ((i + 1) >= args.Length){
					Report.Error (
						1900,
						"--wlevel requires a value from 0 to 4");
					Environment.Exit (1);
				}

				SetWarningLevel (args [++i]);
				return true;

			case "--mcs-debug":
				if ((i + 1) >= args.Length){
					Report.Error (5, "--mcs-debug requires an argument");
					Environment.Exit (1);
				}

				try {
					Report.DebugFlags = Int32.Parse (args [++i]);
				} catch {
					Report.Error (5, "Invalid argument to --mcs-debug");
					Environment.Exit (1);
				}
				return true;
				
			case "--about":
				About ();
				return true;
				
			case "--recurse":
				Report.Warning (-29, 1, "Compatibility: Use -recurse:PATTERN option instead --recurse PATTERN");
				if ((i + 1) >= args.Length){
					Report.Error (5, "--recurse requires an argument");
					Environment.Exit (1);
				}
				ProcessSourceFiles (args [++i], true); 
				return true;
				
			case "--timestamp":
				RootContext.Timestamps = true;
				return true;

			case "--debug": case "-g":
				Report.Warning (-29, 1, "Compatibility: Use -debug option instead of -g or --debug");
				RootContext.GenerateDebugInfo = true;
				return true;
				
			case "--noconfig":
				Report.Warning (-29, 1, "Compatibility: Use -noconfig option instead of --noconfig");
				RootContext.LoadDefaultReferences = false;
				return true;

			default:
				if (arg.StartsWith ("--fatal")){
					if (arg.StartsWith ("--fatal=")){
						if (!Int32.TryParse (arg.Substring (8), out fatal_errors))
							fatal_errors = 1;
					} else
						fatal_errors = 1;
					return true;
				}
				if (arg.StartsWith ("--runtime:", StringComparison.Ordinal)) {
					string version = arg.Substring (10);

					switch (version) {
					case "v1":
					case "V1":
						RootContext.StdLibRuntimeVersion = RuntimeVersion.v1;
						break;
					case "v2":
					case "V2":
						RootContext.StdLibRuntimeVersion = RuntimeVersion.v2;
						return true;
					case "v4":
					case "V4":
						RootContext.StdLibRuntimeVersion = RuntimeVersion.v4;
						return true;
					}
					return true;
				}

				break;
			}

			return false;
		}

		public static string GetPackageFlags (string packages, bool fatal, Report report)
		{
			ProcessStartInfo pi = new ProcessStartInfo ();
			pi.FileName = "pkg-config";
			pi.RedirectStandardOutput = true;
			pi.UseShellExecute = false;
			pi.Arguments = "--libs " + packages;
			Process p = null;
			try {
				p = Process.Start (pi);
			} catch (Exception e) {
				report.Error (-27, "Couldn't run pkg-config: " + e.Message);
				if (fatal)
					Environment.Exit (1);
				p.Close ();
				return null;
			}
			
			if (p.StandardOutput == null){
				report.Warning (-27, 1, "Specified package did not return any information");
				p.Close ();
				return null;
			}
			string pkgout = p.StandardOutput.ReadToEnd ();
			p.WaitForExit ();
			if (p.ExitCode != 0) {
				report.Error (-27, "Error running pkg-config. Check the above output.");
				if (fatal)
					Environment.Exit (1);
				p.Close ();
				return null;
			}
			p.Close ();

			return pkgout;
		}

		//
		// This parses the -arg and /arg options to the compiler, even if the strings
		// in the following text use "/arg" on the strings.
		//
		bool CSCParseOption (string option, ref string [] args)
		{
			int idx = option.IndexOf (':');
			string arg, value;

			if (idx == -1){
				arg = option;
				value = "";
			} else {
				arg = option.Substring (0, idx);

				value = option.Substring (idx + 1);
			}

			switch (arg.ToLowerInvariant ()){
			case "/nologo":
				return true;

			case "/t":
			case "/target":
				switch (value){
				case "exe":
					RootContext.Target = Target.Exe;
					break;

				case "winexe":
					RootContext.Target = Target.WinExe;
					break;

				case "library":
					RootContext.Target = Target.Library;
					RootContext.TargetExt = ".dll";
					break;

				case "module":
					RootContext.Target = Target.Module;
					RootContext.TargetExt = ".netmodule";
					break;

				default:
					TargetUsage ();
					break;
				}
				return true;

			case "/out":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}
				RootContext.OutputFile = value;
				return true;

			case "/o":
			case "/o+":
			case "/optimize":
			case "/optimize+":
				RootContext.Optimize = true;
				return true;

			case "/o-":
			case "/optimize-":
				RootContext.Optimize = false;
				return true;

			// TODO: Not supported by csc 3.5+
			case "/incremental":
			case "/incremental+":
			case "/incremental-":
				// nothing.
				return true;

			case "/d":
			case "/define": {
				if (value.Length == 0){
					Usage ();
					Environment.Exit (1);
				}

				foreach (string d in value.Split (argument_value_separator)) {
					string conditional = d.Trim ();
					if (!Tokenizer.IsValidIdentifier (conditional)) {
						Report.Warning (2029, 1, "Invalid conditional define symbol `{0}'", conditional);
						continue;
					}
					RootContext.AddConditional (conditional);
				}
				return true;
			}

			case "/bugreport":
				//
				// We should collect data, runtime, etc and store in the file specified
				//
				Console.WriteLine ("To file bug reports, please visit: http://www.mono-project.com/Bugs");
				return true;

			case "/pkg": {
				string packages;

				if (value.Length == 0){
					Usage ();
					Environment.Exit (1);
				}
				packages = String.Join (" ", value.Split (new Char [] { ';', ',', '\n', '\r'}));
				string pkgout = GetPackageFlags (packages, true, Report);
				
				if (pkgout != null){
					string [] xargs = pkgout.Trim (new Char [] {' ', '\n', '\r', '\t'}).
						Split (new Char [] { ' ', '\t'});
					args = AddArgs (args, xargs);
				}
				
				return true;
			}

			case "/linkres":
			case "/linkresource":
			case "/res":
			case "/resource":
				AssemblyResource res = null;			
				string[] s = value.Split (argument_value_separator, StringSplitOptions.RemoveEmptyEntries);
				switch (s.Length) {
				case 1:
					if (s[0].Length == 0)
						goto default;
					res = new AssemblyResource (s [0], Path.GetFileName (s[0]));
					break;
				case 2:
					res = new AssemblyResource (s [0], s [1]);
					break;
				case 3:
					if (s [2] != "public" && s [2] != "private") {
						Report.Error (1906, "Invalid resource visibility option `{0}'. Use either `public' or `private' instead", s [2]);
						return true;
					}
					res = new AssemblyResource (s[0], s[1], s[2] == "private");
					break;
				default:
					Report.Error (-2005, "Wrong number of arguments for option `{0}'", option);
					break;
				}

				if (res != null) {
					res.IsEmbeded = arg [1] == 'r' || arg [1] == 'R';
					AddResource (res);
				}

				return true;
				
			case "/recurse":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}
				ProcessSourceFiles (value, true); 
				return true;

			case "/r":
			case "/reference": {
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				string[] refs = value.Split (argument_value_separator);
				foreach (string r in refs){
					if (r.Length == 0)
						continue;

					string val = r;
					int index = val.IndexOf ('=');
					if (index > -1) {
						string alias = r.Substring (0, index);
						string assembly = r.Substring (index + 1);
						AddAssemblyReference (alias, assembly);
						if (refs.Length != 1) {
							Report.Error (2034, "Cannot specify multiple aliases using single /reference option");
							break;
						}
					} else {
						AddAssemblyReference (val);
					}
				}
				return true;
			}
			case "/addmodule": {
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				string[] refs = value.Split (argument_value_separator);
				foreach (string r in refs){
					RootContext.Modules.Add (r);
				}
				return true;
			}
			case "/win32res": {
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}
				
				if (RootContext.Win32IconFile != null)
					Report.Error (1565, "Cannot specify the `win32res' and the `win32ico' compiler option at the same time");

				RootContext.Win32ResourceFile = value;
				return true;
			}
			case "/win32icon": {
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				if (RootContext.Win32ResourceFile != null)
					Report.Error (1565, "Cannot specify the `win32res' and the `win32ico' compiler option at the same time");

				RootContext.Win32IconFile = value;
				return true;
			}
			case "/doc": {
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				RootContext.Documentation = new Documentation (value);
				return true;
			}
			case "/lib": {
				string [] libdirs;
				
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				libdirs = value.Split (argument_value_separator);
				foreach (string dir in libdirs)
					RootContext.ReferencesLookupPaths.Add (dir);
				return true;
			}

			case "/debug-":
				RootContext.GenerateDebugInfo = false;
				return true;
				
			case "/debug":
				if (value == "full" || value == "")
					RootContext.GenerateDebugInfo = true;

				return true;
				
			case "/debug+":
				RootContext.GenerateDebugInfo = true;
				return true;

			case "/checked":
			case "/checked+":
				RootContext.Checked = true;
				return true;

			case "/checked-":
				RootContext.Checked = false;
				return true;

			case "/clscheck":
			case "/clscheck+":
				RootContext.VerifyClsCompliance = true;
				return true;

			case "/clscheck-":
				RootContext.VerifyClsCompliance = false;
				return true;

			case "/unsafe":
			case "/unsafe+":
				RootContext.Unsafe = true;
				return true;

			case "/unsafe-":
				RootContext.Unsafe = false;
				return true;

			case "/warnaserror":
			case "/warnaserror+":
				if (value.Length == 0) {
					Report.WarningsAreErrors = true;
				} else {
					foreach (string wid in value.Split (argument_value_separator))
						Report.AddWarningAsError (wid);
				}
				return true;

			case "/warnaserror-":
				if (value.Length == 0) {
					Report.WarningsAreErrors = false;
				} else {
					foreach (string wid in value.Split (argument_value_separator))
						Report.RemoveWarningAsError (wid);
				}
				return true;

			case "/warn":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				SetWarningLevel (value);
				return true;

			case "/nowarn": {
				if (value.Length == 0){
					Error_RequiresArgument (option);
					break;
				}

				var warns = value.Split (argument_value_separator);
				foreach (string wc in warns){
					try {
						if (wc.Trim ().Length == 0)
							continue;

						int warn = Int32.Parse (wc);
						if (warn < 1) {
							throw new ArgumentOutOfRangeException("warn");
						}
						Report.SetIgnoreWarning (warn);
					} catch {
						Report.Error (1904, "`{0}' is not a valid warning number", wc);
					}
				}
				return true;
			}

			case "/noconfig":
				RootContext.LoadDefaultReferences = false;
				return true;

			case "/platform":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				switch (value.ToLower (CultureInfo.InvariantCulture)) {
				case "anycpu":
					RootContext.Platform = Platform.AnyCPU;
					break;
				case "x86":
					RootContext.Platform = Platform.X86;
					break;
				case "x64":
					RootContext.Platform = Platform.X64;
					break;
				case "itanium":
					RootContext.Platform = Platform.IA64;
					break;
				default:
					Report.Error (1672, "Invalid platform type for -platform. Valid options are `anycpu', `x86', `x64' or `itanium'");
					break;
				}

				return true;

			case "/sdk":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				switch (value.ToLowerInvariant ()) {
					case "2":
						RootContext.SdkVersion = SdkVersion.v2;
						break;
					case "4":
						RootContext.SdkVersion = SdkVersion.v4;
						break;
					default:
						Report.Error (-26, "Invalid sdk version name");
						break;
				}

				return true;

				// We just ignore this.
			case "/errorreport":
			case "/filealign":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				return true;
				
			case "/helpinternal":
				OtherFlags ();
				Environment.Exit(0);
				return true;
				
			case "/help":
			case "/?":
				Usage ();
				Environment.Exit (0);
				return true;

			case "/main":
			case "/m":
				if (value.Length == 0){
					Error_RequiresArgument (option);
					break;
				}
				RootContext.MainClass = value;
				return true;

			case "/nostdlib":
			case "/nostdlib+":
				RootContext.StdLib = false;
				return true;

			case "/nostdlib-":
				RootContext.StdLib = true;
				return true;

			case "/fullpaths":
				RootContext.ShowFullPaths = true;
				return true;

			case "/keyfile":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					break;
				}

				RootContext.StrongNameKeyFile = value;
				return true;

			case "/keycontainer":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				RootContext.StrongNameKeyContainer = value;
				return true;
			case "/delaysign+":
			case "/delaysign":
				RootContext.StrongNameDelaySign = true;
				return true;
			case "/delaysign-":
				RootContext.StrongNameDelaySign = false;
				return true;

			case "/langversion":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				switch (value.ToLowerInvariant ()) {
				case "iso-1":
					RootContext.Version = LanguageVersion.ISO_1;
					return true;	
				case "default":
					RootContext.Version = LanguageVersion.Default;
					RootContext.AddConditional ("__V2__");
					return true;
				case "iso-2":
					RootContext.Version = LanguageVersion.ISO_2;
					return true;
				case "3":
					RootContext.Version = LanguageVersion.V_3;
					return true;
				case "future":
					RootContext.Version = LanguageVersion.Future;
					return true;
				}

				Report.Error (1617, "Invalid -langversion option `{0}'. It must be `ISO-1', `ISO-2', `3' or `Default'", value);
				return true;

			case "/codepage":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					break;
				}

				switch (value) {
				case "utf8":
					RootContext.Encoding = new UTF8Encoding();
					break;
				case "reset":
					RootContext.Encoding = Encoding.Default;
					break;
				default:
					try {
						RootContext.Encoding = Encoding.GetEncoding (Int32.Parse (value));
					} catch {
						Report.Error (2016, "Code page `{0}' is invalid or not installed", value);
					}
					break;
				}
				return true;

			default:
				return false;
			}

			return true;
		}

		void Error_WrongOption (string option)
		{
			Report.Error (2007, "Unrecognized command-line option: `{0}'", option);
		}

		void Error_RequiresFileName (string option)
		{
			Report.Error (2005, "Missing file specification for `{0}' option", option);
		}

		void Error_RequiresArgument (string option)
		{
			Report.Error (2006, "Missing argument for `{0}' option", option);
		}

		static string [] AddArgs (string [] args, string [] extra_args)
		{
			string [] new_args;
			new_args = new string [extra_args.Length + args.Length];

			// if args contains '--' we have to take that into account
			// split args into first half and second half based on '--'
			// and add the extra_args before --
			int split_position = Array.IndexOf (args, "--");
			if (split_position != -1)
			{
				Array.Copy (args, new_args, split_position);
				extra_args.CopyTo (new_args, split_position);
				Array.Copy (args, split_position, new_args, split_position + extra_args.Length, args.Length - split_position);
			}
			else
			{
				args.CopyTo (new_args, 0);
				extra_args.CopyTo (new_args, args.Length);
			}

			return new_args;
		}

		void AddAssemblyReference (string assembly)
		{
			RootContext.AssemblyReferences.Add (assembly);
		}

		void AddAssemblyReference (string alias, string assembly)
		{
			if (assembly.Length == 0) {
				Report.Error (1680, "Invalid reference alias `{0}='. Missing filename", alias);
				return;
			}

			if (!IsExternAliasValid (alias)) {
				Report.Error (1679, "Invalid extern alias for -reference. Alias `{0}' is not a valid identifier", alias);
				return;
			}

			RootContext.AssemblyReferencesAliases.Add (Tuple.Create (alias, assembly));
		}

		void AddResource (AssemblyResource res)
		{
			if (RootContext.Resources == null) {
				RootContext.Resources = new List<AssemblyResource> ();
				RootContext.Resources.Add (res);
				return;
			}

			if (RootContext.Resources.Contains (res)) {
				ctx.Report.Error (1508, "The resource identifier `{0}' has already been used in this assembly", res.Name);
				return;
			}

			RootContext.Resources.Add (res);
		}
		
		static bool IsExternAliasValid (string identifier)
		{
			if (identifier.Length == 0)
				return false;
			if (identifier [0] != '_' && !Char.IsLetter (identifier [0]))
				return false;

			for (int i = 1; i < identifier.Length; i++) {
				char c = identifier [i];
				if (Char.IsLetter (c) || Char.IsDigit (c))
					continue;

				UnicodeCategory category = Char.GetUnicodeCategory (c);
				if (category != UnicodeCategory.Format || category != UnicodeCategory.NonSpacingMark ||
						category != UnicodeCategory.SpacingCombiningMark ||
						category != UnicodeCategory.ConnectorPunctuation)
					return false;
			}
			
			return true;
		}

		//
		// Main compilation method
		//
		public bool Compile ()
		{
			TimeReporter tr = new TimeReporter (RootContext.Timestamps);
			ctx.TimeReporter = tr;
			tr.StartTotal ();

			var module = new ModuleContainer (ctx);
			RootContext.ToplevelTypes = module;

			tr.Start (TimeReporter.TimerType.ParseTotal);
			Parse (module);
			tr.Stop (TimeReporter.TimerType.ParseTotal);

			if (Report.Errors > 0)
				return false;

			if (RootContext.TokenizeOnly || RootContext.ParseOnly)
				return true;

			if (RootContext.ToplevelTypes.NamespaceEntry != null)
				throw new InternalErrorException ("who set it?");

			//
			// Quick hack
			//
			var output_file = RootContext.OutputFile;
			string output_file_name;
			if (output_file == null) {
				if (first_source == null) {
					Report.Error (1562, "If no source files are specified you must specify the output file with -out:");
					return false;
				}

				int pos = first_source.LastIndexOf ('.');

				if (pos > 0)
					output_file = first_source.Substring (0, pos) + RootContext.TargetExt;
				else
					output_file = first_source + RootContext.TargetExt;

				output_file_name = output_file;
			} else {
				output_file_name = Path.GetFileName (output_file);
			}

#if STATIC
			var importer = new StaticImporter ();
			var references_loader = new StaticLoader (importer, ctx);

			tr.Start (TimeReporter.TimerType.AssemblyBuilderSetup);
			var assembly = new AssemblyDefinitionStatic (module, references_loader, output_file_name, output_file);
			assembly.Create (references_loader.Domain);
			tr.Stop (TimeReporter.TimerType.AssemblyBuilderSetup);

			// Create compiler types first even before any referenced
			// assembly is loaded to allow forward referenced types from
			// loaded assembly into compiled builder to be resolved
			// correctly
			tr.Start (TimeReporter.TimerType.CreateTypeTotal);
			module.CreateType ();
			importer.AddCompiledAssembly (assembly);
			tr.Stop (TimeReporter.TimerType.CreateTypeTotal);

			references_loader.LoadReferences (module);

			tr.Start (TimeReporter.TimerType.PredefinedTypesInit);
			if (!ctx.BuildinTypes.CheckDefinitions (module))
				return false;

			tr.Stop (TimeReporter.TimerType.PredefinedTypesInit);

			references_loader.LoadModules (assembly, module.GlobalRootNamespace);
#else
			var assembly = new AssemblyDefinitionDynamic (module, output_file_name, output_file);
			module.SetDeclaringAssembly (assembly);

			var importer = new ReflectionImporter (ctx.BuildinTypes);
			assembly.Importer = importer;

			var loader = new DynamicLoader (importer, ctx);
			loader.LoadReferences (module);

			if (!ctx.BuildinTypes.CheckDefinitions (module))
				return false;

			if (!assembly.Create (AppDomain.CurrentDomain, AssemblyBuilderAccess.Save))
				return false;

			module.CreateType ();

			loader.LoadModules (assembly, module.GlobalRootNamespace);
#endif
			tr.Start (TimeReporter.TimerType.ModuleDefinitionTotal);
			module.Define ();
			tr.Stop (TimeReporter.TimerType.ModuleDefinitionTotal);

			if (Report.Errors > 0)
				return false;

			if (RootContext.Documentation != null &&
				!RootContext.Documentation.OutputDocComment (
					output_file, Report))
				return false;

			//
			// Verify using aliases now
			//
			tr.Start (TimeReporter.TimerType.UsingVerification);
			NamespaceEntry.VerifyAllUsing ();
			tr.Stop (TimeReporter.TimerType.UsingVerification);
			
			if (Report.Errors > 0){
				return false;
			}

			assembly.Resolve ();
			
			if (Report.Errors > 0)
				return false;


			tr.Start (TimeReporter.TimerType.EmitTotal);
			assembly.Emit ();
			tr.Stop (TimeReporter.TimerType.EmitTotal);

			if (Report.Errors > 0){
				return false;
			}

			tr.Start (TimeReporter.TimerType.CloseTypes);
			module.CloseType ();
			tr.Stop (TimeReporter.TimerType.CloseTypes);

			tr.Start (TimeReporter.TimerType.Resouces);
			assembly.EmbedResources ();
			tr.Stop (TimeReporter.TimerType.Resouces);

			if (Report.Errors > 0)
				return false;

			assembly.Save ();

#if STATIC
			references_loader.Dispose ();
#endif
			tr.StopTotal ();
			tr.ShowStats ();

			return (Report.Errors == 0);
		}
	}

	public class CompilerCompilationUnit {
		public ModuleContainer ModuleCompiled { get; set; }
		public LocationsBag LocationsBag { get; set; }
		public UsingsBag UsingsBag { get; set; }
		public SpecialsBag SpecialsBag { get; set; }
	}
	
	//
	// This is the only public entry point
	//
	public class CompilerCallableEntryPoint : MarshalByRefObject {
		
		public static bool InvokeCompiler (string [] args, TextWriter error)
		{
			try {
				StreamReportPrinter srp = new StreamReportPrinter (error);
				Driver d = Driver.Create (args, true, delegate (string[] a, int i) { System.Console.WriteLine ("Unknown option:" + a[i]); return 0; }, srp);
				if (d == null)
					return false;

				return d.Compile () && srp.ErrorsCount == 0;
			} finally {
				Reset ();
			}
		}

		public static int[] AllWarningNumbers {
			get {
				return Report.AllWarnings;
			}
		}

		public static void Reset ()
		{
			Reset (true);
		}

		public static void PartialReset ()
		{
			Reset (false);
		}
		
		public static void Reset (bool full_flag)
		{
			CSharpParser.yacc_verbose_flag = 0;
			Location.Reset ();
			
			if (!full_flag)
				return;

			RootContext.Reset (full_flag);
			TypeManager.Reset ();
			ArrayContainer.Reset ();
			ReferenceContainer.Reset ();
			PointerContainer.Reset ();
			Parameter.Reset ();

			Unary.Reset ();
			UnaryMutator.Reset ();
			Binary.Reset ();
			ConstantFold.Reset ();
			CastFromDecimal.Reset ();
			StringConcat.Reset ();
			
			NamespaceEntry.Reset ();
			Attribute.Reset ();
			AnonymousTypeClass.Reset ();
			AnonymousMethodBody.Reset ();
			AnonymousMethodStorey.Reset ();
			SymbolWriter.Reset ();
			Switch.Reset ();
			Linq.QueryBlock.TransparentParameter.Reset ();
			Convert.Reset ();
			TypeInfo.Reset ();
		}
		
		public static CompilerCompilationUnit ParseFile (string[] args, Stream input, string inputFile, TextWriter reportStream)
		{
			return ParseFile (args, input, inputFile, new StreamReportPrinter (reportStream));
		}
		
		internal static object parseLock = new object ();
		public static CompilerCompilationUnit ParseFile (string[] args, Stream input, string inputFile, ReportPrinter reportPrinter)
		{
			lock (parseLock) {
				try {
					Driver d = Driver.Create (args, false, null, reportPrinter);
					if (d == null)
						return null;
	
					Location.AddFile (null, inputFile);
					Location.Initialize ();
	
					// TODO: encoding from driver
					SeekableStreamReader reader = new SeekableStreamReader (input, Encoding.Default);
	
					CompilerContext ctx = new CompilerContext (new Report (reportPrinter));
					
					RootContext.ToplevelTypes = new ModuleContainer (ctx);
					CompilationUnit unit = null;
					try {
						unit = (CompilationUnit) Location.SourceFiles [0];
					} catch (Exception) {
						string path = Path.GetFullPath (inputFile);
						unit = new CompilationUnit (inputFile, path, 0);
					}
					CSharpParser parser = new CSharpParser (reader, unit, RootContext.ToplevelTypes);
					parser.Lexer.TabSize = 1;
					parser.Lexer.sbag = new SpecialsBag ();
					parser.LocationsBag = new LocationsBag ();
					parser.UsingsBag = new UsingsBag ();
					parser.parse ();
					
					return new CompilerCompilationUnit () { ModuleCompiled = RootContext.ToplevelTypes, LocationsBag = parser.LocationsBag, UsingsBag = parser.UsingsBag, SpecialsBag = parser.Lexer.sbag };
				} finally {
					Reset ();
				}
			}
		}
	}
}
