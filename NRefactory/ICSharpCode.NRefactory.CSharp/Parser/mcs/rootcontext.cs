//
// rootcontext.cs: keeps track of our tree representation, and assemblies loaded.
//
// Author: Miguel de Icaza (miguel@ximian.com)
//            Ravi Pratap  (ravi@ximian.com)
//            Marek Safar  (marek.safar@gmail.com)
//
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System;

namespace Mono.CSharp {

	public enum LanguageVersion
	{
		ISO_1 = 1,
		ISO_2 = 2,
		V_3 = 3,
		V_4 = 4,
		V_5 = 5,
		Future = 100,

		Default = LanguageVersion.V_5,
	}

	public enum RuntimeVersion
	{
		v1,
		v2,
		v4
	}

	public enum Target
	{
		Library, Exe, Module, WinExe
	}

	public enum Platform
	{
		AnyCPU, X86, X64, IA64
	}

	public class CompilerSettings
	{
		public Target Target;
		public Platform Platform;
		public string TargetExt;
		public bool VerifyClsCompliance;
		public bool Optimize;
		public LanguageVersion Version;
		public bool EnhancedWarnings;
		public bool LoadDefaultReferences;
		public string SdkVersion;

		public string StrongNameKeyFile;
		public string StrongNameKeyContainer;
		public bool StrongNameDelaySign;

		//
		// Assemblies references to be loaded
		//
		public List<string> AssemblyReferences;

		// 
		// External aliases for assemblies
		//
		public List<Tuple<string, string>> AssemblyReferencesAliases;

		//
		// Modules to be embedded
		//
		public List<string> Modules;

		//
		// Lookup paths for referenced assemblies
		//
		public List<string> ReferencesLookupPaths;

		//
		// Encoding.
		//
		public Encoding Encoding;

		//
		// If set, enable XML documentation generation
		//
		public string DocumentationFile;

		public string MainClass;

		//
		// Output file
		//
		public string OutputFile;

		// 
		// The default compiler checked state
		//
		public bool Checked;

		//
		// If true, the compiler is operating in statement mode,
		// this currently turns local variable declaration into
		// static variables of a class
		//
		public bool StatementMode;	// TODO: SUPER UGLY
		
		//
		// Whether to allow Unsafe code
		//
		public bool Unsafe;

		public string Win32ResourceFile;
		public string Win32IconFile;

		//
		// A list of resource files for embedding
		//
		public List<AssemblyResource> Resources;

		public bool GenerateDebugInfo;

		// Compiler debug flags only
		public bool ParseOnly, TokenizeOnly, Timestamps;
		public int DebugFlags;

		//
		// Whether we are being linked against the standard libraries.
		// This is only used to tell whether `System.Object' should
		// have a base class or not.
		//
		public bool StdLib;

		public RuntimeVersion StdLibRuntimeVersion;

		readonly List<string> conditional_symbols;

		readonly List<CompilationSourceFile> source_files;

		public CompilerSettings ()
		{
			StdLib = true;
			Target = Target.Exe;
			TargetExt = ".exe";
			Platform = Platform.AnyCPU;
			Version = LanguageVersion.Default;
			VerifyClsCompliance = true;
			Optimize = true;
			Encoding = Encoding.UTF8;
			LoadDefaultReferences = true;
			StdLibRuntimeVersion = RuntimeVersion.v4;

			AssemblyReferences = new List<string> ();
			AssemblyReferencesAliases = new List<Tuple<string, string>> ();
			Modules = new List<string> ();
			ReferencesLookupPaths = new List<string> ();

			conditional_symbols = new List<string> ();
			//
			// Add default mcs define
			//
			conditional_symbols.Add ("__MonoCS__");

			source_files = new List<CompilationSourceFile> ();
		}

		#region Properties

		public CompilationSourceFile FirstSourceFile {
			get {
				return source_files.Count > 0 ? source_files [0] : null;
			}
		}

		public bool HasKeyFileOrContainer {
			get {
				return StrongNameKeyFile != null || StrongNameKeyContainer != null;
			}
		}

		public bool NeedsEntryPoint {
			get {
				return Target == Target.Exe || Target == Target.WinExe;
			}
		}

		public List<CompilationSourceFile> SourceFiles {
			get {
				return source_files;
			}
		}

		#endregion

		public void AddConditionalSymbol (string symbol)
		{
			if (!conditional_symbols.Contains (symbol))
				conditional_symbols.Add (symbol);
		}

		public bool IsConditionalSymbolDefined (string symbol)
		{
			return conditional_symbols.Contains (symbol);
		}
	}

	public class CommandLineParser
	{
		enum ParseResult
		{
			Success,
			Error,
			Stop,
			UnknownOption
		}

		static readonly char[] argument_value_separator = new char[] { ';', ',' };
		static readonly char[] numeric_value_separator = new char[] { ';', ',', ' ' };

		readonly Report report;
		readonly TextWriter output;
		bool stop_argument;

		Dictionary<string, int> source_file_index;

		public event Func<string[], int, int> UnknownOptionHandler;

		public CommandLineParser (Report report)
			: this (report, Console.Out)
		{
		}

		public CommandLineParser (Report report, TextWriter messagesOutput)
		{
			this.report = report;
			this.output = messagesOutput;
		}

		public bool HasBeenStopped {
			get {
				return stop_argument;
			}
		}

		void About ()
		{
			output.WriteLine (
				"The Mono C# compiler is Copyright 2001-2011, Novell, Inc.\n\n" +
				"The compiler source code is released under the terms of the \n" +
				"MIT X11 or GNU GPL licenses\n\n" +

				"For more information on Mono, visit the project Web site\n" +
				"   http://www.mono-project.com\n\n" +

				"The compiler was written by Miguel de Icaza, Ravi Pratap, Martin Baulig, Marek Safar, Raja R Harinath, Atushi Enomoto");
		}

		public CompilerSettings ParseArguments (string[] args)
		{
			CompilerSettings settings = new CompilerSettings ();
			List<string> response_file_list = null;
			bool parsing_options = true;
			stop_argument = false;
			source_file_index = new Dictionary<string, int> ();

			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				if (arg.Length == 0)
					continue;

				if (arg[0] == '@') {
					string[] extra_args;
					string response_file = arg.Substring (1);

					if (response_file_list == null)
						response_file_list = new List<string> ();

					if (response_file_list.Contains (response_file)) {
						report.Error (1515, "Response file `{0}' specified multiple times", response_file);
						return null;
					}

					response_file_list.Add (response_file);

					extra_args = LoadArgs (response_file);
					if (extra_args == null) {
						report.Error (2011, "Unable to open response file: " + response_file);
						return null;
					}

					args = AddArgs (args, extra_args);
					continue;
				}

				if (parsing_options) {
					if (arg == "--") {
						parsing_options = false;
						continue;
					}

					bool dash_opt = arg[0] == '-';
					bool slash_opt = arg[0] == '/';
					if (dash_opt) {
						switch (ParseOptionUnix (arg, ref args, ref i, settings)) {
						case ParseResult.Error:
						case ParseResult.Success:
							continue;
						case ParseResult.Stop:
							stop_argument = true;
							return settings;
						case ParseResult.UnknownOption:
							if (UnknownOptionHandler != null) {
								var ret = UnknownOptionHandler (args, i);
								if (ret != -1) {
									i = ret;
									continue;
								}
							}
							break;
						}
					}

					if (dash_opt || slash_opt) {
						// Try a -CSCOPTION
						string csc_opt = dash_opt ? "/" + arg.Substring (1) : arg;
						switch (ParseOption (csc_opt, ref args, settings)) {
						case ParseResult.Error:
						case ParseResult.Success:
							continue;
						case ParseResult.UnknownOption:
							// Need to skip `/home/test.cs' however /test.cs is considered as error
							if ((slash_opt && arg.Length > 3 && arg.IndexOf ('/', 2) > 0))
								break;

							if (UnknownOptionHandler != null) {
								var ret = UnknownOptionHandler (args, i);
								if (ret != -1) {
									i = ret;
									continue;
								}
							}

							Error_WrongOption (arg);
							return null;

						case ParseResult.Stop:
							stop_argument = true;
							return settings;
						}
					}
				}

				ProcessSourceFiles (arg, false, settings.SourceFiles);
			}

			return settings;
		}

		void ProcessSourceFiles (string spec, bool recurse, List<CompilationSourceFile> sourceFiles)
		{
			string path, pattern;

			SplitPathAndPattern (spec, out path, out pattern);
			if (pattern.IndexOf ('*') == -1) {
				AddSourceFile (spec, sourceFiles);
				return;
			}

			string[] files = null;
			try {
				files = Directory.GetFiles (path, pattern);
			} catch (System.IO.DirectoryNotFoundException) {
				report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			} catch (System.IO.IOException) {
				report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			}
			foreach (string f in files) {
				AddSourceFile (f, sourceFiles);
			}

			if (!recurse)
				return;

			string[] dirs = null;

			try {
				dirs = Directory.GetDirectories (path);
			} catch {
			}

			foreach (string d in dirs) {

				// Don't include path in this string, as each
				// directory entry already does
				ProcessSourceFiles (d + "/" + pattern, true, sourceFiles);
			}
		}

		static string[] AddArgs (string[] args, string[] extra_args)
		{
			string[] new_args;
			new_args = new string[extra_args.Length + args.Length];

			// if args contains '--' we have to take that into account
			// split args into first half and second half based on '--'
			// and add the extra_args before --
			int split_position = Array.IndexOf (args, "--");
			if (split_position != -1) {
				Array.Copy (args, new_args, split_position);
				extra_args.CopyTo (new_args, split_position);
				Array.Copy (args, split_position, new_args, split_position + extra_args.Length, args.Length - split_position);
			} else {
				args.CopyTo (new_args, 0);
				extra_args.CopyTo (new_args, args.Length);
			}

			return new_args;
		}

		void AddAssemblyReference (string alias, string assembly, CompilerSettings settings)
		{
			if (assembly.Length == 0) {
				report.Error (1680, "Invalid reference alias `{0}='. Missing filename", alias);
				return;
			}

			if (!IsExternAliasValid (alias)) {
				report.Error (1679, "Invalid extern alias for -reference. Alias `{0}' is not a valid identifier", alias);
				return;
			}

			settings.AssemblyReferencesAliases.Add (Tuple.Create (alias, assembly));
		}

		void AddResource (AssemblyResource res, CompilerSettings settings)
		{
			if (settings.Resources == null) {
				settings.Resources = new List<AssemblyResource> ();
				settings.Resources.Add (res);
				return;
			}

			if (settings.Resources.Contains (res)) {
				report.Error (1508, "The resource identifier `{0}' has already been used in this assembly", res.Name);
				return;
			}

			settings.Resources.Add (res);
		}

		void AddSourceFile (string fileName, List<CompilationSourceFile> sourceFiles)
		{
			string path = Path.GetFullPath (fileName);

			int index;
			if (source_file_index.TryGetValue (path, out index)) {
				string other_name = sourceFiles[index - 1].Name;
				if (fileName.Equals (other_name))
					report.Warning (2002, 1, "Source file `{0}' specified multiple times", other_name);
				else
					report.Warning (2002, 1, "Source filenames `{0}' and `{1}' both refer to the same file: {2}", fileName, other_name, path);

				return;
			}

			var unit = new CompilationSourceFile (fileName, path, sourceFiles.Count + 1);
			sourceFiles.Add (unit);
			source_file_index.Add (path, unit.Index);
		}

		void Error_RequiresArgument (string option)
		{
			report.Error (2006, "Missing argument for `{0}' option", option);
		}

		void Error_RequiresFileName (string option)
		{
			report.Error (2005, "Missing file specification for `{0}' option", option);
		}

		void Error_WrongOption (string option)
		{
			report.Error (2007, "Unrecognized command-line option: `{0}'", option);
		}

		static bool IsExternAliasValid (string identifier)
		{
			if (identifier.Length == 0)
				return false;
			if (identifier[0] != '_' && !char.IsLetter (identifier[0]))
				return false;

			for (int i = 1; i < identifier.Length; i++) {
				char c = identifier[i];
				if (char.IsLetter (c) || char.IsDigit (c))
					continue;

				UnicodeCategory category = char.GetUnicodeCategory (c);
				if (category != UnicodeCategory.Format || category != UnicodeCategory.NonSpacingMark ||
						category != UnicodeCategory.SpacingCombiningMark ||
						category != UnicodeCategory.ConnectorPunctuation)
					return false;
			}

			return true;
		}

		static string[] LoadArgs (string file)
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

			while ((line = f.ReadLine ()) != null) {
				int t = line.Length;

				for (int i = 0; i < t; i++) {
					char c = line[i];

					if (c == '"' || c == '\'') {
						char end = c;

						for (i++; i < t; i++) {
							c = line[i];

							if (c == end)
								break;
							sb.Append (c);
						}
					} else if (c == ' ') {
						if (sb.Length > 0) {
							args.Add (sb.ToString ());
							sb.Length = 0;
						}
					} else
						sb.Append (c);
				}
				if (sb.Length > 0) {
					args.Add (sb.ToString ());
					sb.Length = 0;
				}
			}

			return args.ToArray ();
		}

		void OtherFlags ()
		{
			output.WriteLine (
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

		//
		// This parses the -arg and /arg options to the compiler, even if the strings
		// in the following text use "/arg" on the strings.
		//
		ParseResult ParseOption (string option, ref string[] args, CompilerSettings settings)
		{
			int idx = option.IndexOf (':');
			string arg, value;

			if (idx == -1) {
				arg = option;
				value = "";
			} else {
				arg = option.Substring (0, idx);

				value = option.Substring (idx + 1);
			}

			switch (arg.ToLowerInvariant ()) {
			case "/nologo":
				return ParseResult.Success;

			case "/t":
			case "/target":
				switch (value) {
				case "exe":
					settings.Target = Target.Exe;
					break;

				case "winexe":
					settings.Target = Target.WinExe;
					break;

				case "library":
					settings.Target = Target.Library;
					settings.TargetExt = ".dll";
					break;

				case "module":
					settings.Target = Target.Module;
					settings.TargetExt = ".netmodule";
					break;

				default:
					report.Error (2019, "Invalid target type for -target. Valid options are `exe', `winexe', `library' or `module'");
					return ParseResult.Error;
				}
				return ParseResult.Success;

			case "/out":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					return ParseResult.Error;
				}
				settings.OutputFile = value;
				return ParseResult.Success;

			case "/o":
			case "/o+":
			case "/optimize":
			case "/optimize+":
				settings.Optimize = true;
				return ParseResult.Success;

			case "/o-":
			case "/optimize-":
				settings.Optimize = false;
				return ParseResult.Success;

			// TODO: Not supported by csc 3.5+
			case "/incremental":
			case "/incremental+":
			case "/incremental-":
				// nothing.
				return ParseResult.Success;

			case "/d":
			case "/define": {
					if (value.Length == 0) {
						Error_RequiresArgument (option);
						return ParseResult.Error;
					}

					foreach (string d in value.Split (argument_value_separator)) {
						string conditional = d.Trim ();
						if (!Tokenizer.IsValidIdentifier (conditional)) {
							report.Warning (2029, 1, "Invalid conditional define symbol `{0}'", conditional);
							continue;
						}

						settings.AddConditionalSymbol (conditional);
					}
					return ParseResult.Success;
				}

			case "/bugreport":
				//
				// We should collect data, runtime, etc and store in the file specified
				//
				output.WriteLine ("To file bug reports, please visit: http://www.mono-project.com/Bugs");
				return ParseResult.Success;

			case "/pkg": {
					string packages;

					if (value.Length == 0) {
						Error_RequiresArgument (option);
						return ParseResult.Error;
					}
					packages = String.Join (" ", value.Split (new Char[] { ';', ',', '\n', '\r' }));
					string pkgout = Driver.GetPackageFlags (packages, report);

					if (pkgout == null)
						return ParseResult.Error;

					string[] xargs = pkgout.Trim (new Char[] { ' ', '\n', '\r', '\t' }).Split (new Char[] { ' ', '\t' });
					args = AddArgs (args, xargs);
					return ParseResult.Success;
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
					res = new AssemblyResource (s[0], Path.GetFileName (s[0]));
					break;
				case 2:
					res = new AssemblyResource (s[0], s[1]);
					break;
				case 3:
					if (s[2] != "public" && s[2] != "private") {
						report.Error (1906, "Invalid resource visibility option `{0}'. Use either `public' or `private' instead", s[2]);
						return ParseResult.Error;
					}
					res = new AssemblyResource (s[0], s[1], s[2] == "private");
					break;
				default:
					report.Error (-2005, "Wrong number of arguments for option `{0}'", option);
					return ParseResult.Error;
				}

				if (res != null) {
					res.IsEmbeded = arg[1] == 'r' || arg[1] == 'R';
					AddResource (res, settings);
				}

				return ParseResult.Success;

			case "/recurse":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					return ParseResult.Error;
				}
				ProcessSourceFiles (value, true, settings.SourceFiles);
				return ParseResult.Success;

			case "/r":
			case "/reference": {
					if (value.Length == 0) {
						Error_RequiresFileName (option);
						return ParseResult.Error;
					}

					string[] refs = value.Split (argument_value_separator);
					foreach (string r in refs) {
						if (r.Length == 0)
							continue;

						string val = r;
						int index = val.IndexOf ('=');
						if (index > -1) {
							string alias = r.Substring (0, index);
							string assembly = r.Substring (index + 1);
							AddAssemblyReference (alias, assembly, settings);
							if (refs.Length != 1) {
								report.Error (2034, "Cannot specify multiple aliases using single /reference option");
								return ParseResult.Error;
							}
						} else {
							settings.AssemblyReferences.Add (val);
						}
					}
					return ParseResult.Success;
				}
			case "/addmodule": {
					if (value.Length == 0) {
						Error_RequiresFileName (option);
						return ParseResult.Error;
					}

					string[] refs = value.Split (argument_value_separator);
					foreach (string r in refs) {
						settings.Modules.Add (r);
					}
					return ParseResult.Success;
				}
			case "/win32res": {
					if (value.Length == 0) {
						Error_RequiresFileName (option);
						return ParseResult.Error;
					}

					if (settings.Win32IconFile != null)
						report.Error (1565, "Cannot specify the `win32res' and the `win32ico' compiler option at the same time");

					settings.Win32ResourceFile = value;
					return ParseResult.Success;
				}
			case "/win32icon": {
					if (value.Length == 0) {
						Error_RequiresFileName (option);
						return ParseResult.Error;
					}

					if (settings.Win32ResourceFile != null)
						report.Error (1565, "Cannot specify the `win32res' and the `win32ico' compiler option at the same time");

					settings.Win32IconFile = value;
					return ParseResult.Success;
				}
			case "/doc": {
					if (value.Length == 0) {
						Error_RequiresFileName (option);
						return ParseResult.Error;
					}

					settings.DocumentationFile = value;
					return ParseResult.Success;
				}
			case "/lib": {
					string[] libdirs;

					if (value.Length == 0) {
						return ParseResult.Error;
					}

					libdirs = value.Split (argument_value_separator);
					foreach (string dir in libdirs)
						settings.ReferencesLookupPaths.Add (dir);
					return ParseResult.Success;
				}

			case "/debug-":
				settings.GenerateDebugInfo = false;
				return ParseResult.Success;

			case "/debug":
				if (value == "full" || value == "")
					settings.GenerateDebugInfo = true;

				return ParseResult.Success;

			case "/debug+":
				settings.GenerateDebugInfo = true;
				return ParseResult.Success;

			case "/checked":
			case "/checked+":
				settings.Checked = true;
				return ParseResult.Success;

			case "/checked-":
				settings.Checked = false;
				return ParseResult.Success;

			case "/clscheck":
			case "/clscheck+":
				settings.VerifyClsCompliance = true;
				return ParseResult.Success;

			case "/clscheck-":
				settings.VerifyClsCompliance = false;
				return ParseResult.Success;

			case "/unsafe":
			case "/unsafe+":
				settings.Unsafe = true;
				return ParseResult.Success;

			case "/unsafe-":
				settings.Unsafe = false;
				return ParseResult.Success;

			case "/warnaserror":
			case "/warnaserror+":
				if (value.Length == 0) {
					report.WarningsAreErrors = true;
				} else {
					foreach (string wid in value.Split (numeric_value_separator))
						report.AddWarningAsError (wid);
				}
				return ParseResult.Success;

			case "/warnaserror-":
				if (value.Length == 0) {
					report.WarningsAreErrors = false;
				} else {
					foreach (string wid in value.Split (numeric_value_separator))
						report.RemoveWarningAsError (wid);
				}
				return ParseResult.Success;

			case "/warn":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				SetWarningLevel (value);
				return ParseResult.Success;

			case "/nowarn":
					if (value.Length == 0) {
						Error_RequiresArgument (option);
						return ParseResult.Error;
					}

					var warns = value.Split (numeric_value_separator);
					foreach (string wc in warns) {
						try {
							if (wc.Trim ().Length == 0)
								continue;

							int warn = Int32.Parse (wc);
							if (warn < 1) {
								throw new ArgumentOutOfRangeException ("warn");
							}
							report.SetIgnoreWarning (warn);
						} catch {
							report.Error (1904, "`{0}' is not a valid warning number", wc);
							return ParseResult.Error;
						}
					}
					return ParseResult.Success;

			case "/noconfig":
				settings.LoadDefaultReferences = false;
				return ParseResult.Success;

			case "/platform":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				switch (value.ToLower (CultureInfo.InvariantCulture)) {
				case "anycpu":
					settings.Platform = Platform.AnyCPU;
					break;
				case "x86":
					settings.Platform = Platform.X86;
					break;
				case "x64":
					settings.Platform = Platform.X64;
					break;
				case "itanium":
					settings.Platform = Platform.IA64;
					break;
				default:
					report.Error (1672, "Invalid platform type for -platform. Valid options are `anycpu', `x86', `x64' or `itanium'");
					return ParseResult.Error;
				}

				return ParseResult.Success;

			case "/sdk":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				settings.SdkVersion = value;
				return ParseResult.Success;

			// We just ignore this.
			case "/errorreport":
			case "/filealign":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				return ParseResult.Success;

			case "/helpinternal":
				OtherFlags ();
				return ParseResult.Stop;

			case "/help":
			case "/?":
				Usage ();
				return ParseResult.Stop;

			case "/main":
			case "/m":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}
				settings.MainClass = value;
				return ParseResult.Success;

			case "/nostdlib":
			case "/nostdlib+":
				settings.StdLib = false;
				return ParseResult.Success;

			case "/nostdlib-":
				settings.StdLib = true;
				return ParseResult.Success;

			case "/fullpaths":
				report.Printer.ShowFullPaths = true;
				return ParseResult.Success;

			case "/keyfile":
				if (value.Length == 0) {
					Error_RequiresFileName (option);
					return ParseResult.Error;
				}

				settings.StrongNameKeyFile = value;
				return ParseResult.Success;

			case "/keycontainer":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				settings.StrongNameKeyContainer = value;
				return ParseResult.Success;

			case "/delaysign+":
			case "/delaysign":
				settings.StrongNameDelaySign = true;
				return ParseResult.Success;

			case "/delaysign-":
				settings.StrongNameDelaySign = false;
				return ParseResult.Success;

			case "/langversion":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				switch (value.ToLowerInvariant ()) {
				case "iso-1":
					settings.Version = LanguageVersion.ISO_1;
					return ParseResult.Success;
				case "default":
					settings.Version = LanguageVersion.Default;
					return ParseResult.Success;
				case "iso-2":
					settings.Version = LanguageVersion.ISO_2;
					return ParseResult.Success;
				case "3":
					settings.Version = LanguageVersion.V_3;
					return ParseResult.Success;
				case "4":
					settings.Version = LanguageVersion.V_4;
					return ParseResult.Success;
				case "5":
					settings.Version = LanguageVersion.V_5;
					return ParseResult.Success;
				case "future":
					settings.Version = LanguageVersion.Future;
					return ParseResult.Success;
				}

				report.Error (1617, "Invalid -langversion option `{0}'. It must be `ISO-1', `ISO-2', `3', `4', `5', `Default' or `Future'", value);
				return ParseResult.Error;

			case "/codepage":
				if (value.Length == 0) {
					Error_RequiresArgument (option);
					return ParseResult.Error;
				}

				switch (value) {
				case "utf8":
					settings.Encoding = Encoding.UTF8;
					break;
				case "reset":
					settings.Encoding = Encoding.Default;
					break;
				default:
					try {
						settings.Encoding = Encoding.GetEncoding (int.Parse (value));
					} catch {
						report.Error (2016, "Code page `{0}' is invalid or not installed", value);
					}
					return ParseResult.Error;
				}
				return ParseResult.Success;

			default:
				return ParseResult.UnknownOption;
			}
		}

		//
		// Currently handles the Unix-like command line options, but will be
		// deprecated in favor of the CSCParseOption, which will also handle the
		// options that start with a dash in the future.
		//
		ParseResult ParseOptionUnix (string arg, ref string[] args, ref int i, CompilerSettings settings)
		{
			switch (arg){
			case "-v":
				CSharpParser.yacc_verbose_flag++;
				return ParseResult.Success;

			case "--version":
				Version ();
				return ParseResult.Stop;
				
			case "--parse":
				settings.ParseOnly = true;
				return ParseResult.Success;
				
			case "--main": case "-m":
				report.Warning (-29, 1, "Compatibility: Use -main:CLASS instead of --main CLASS or -m CLASS");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				settings.MainClass = args[++i];
				return ParseResult.Success;
				
			case "--unsafe":
				report.Warning (-29, 1, "Compatibility: Use -unsafe instead of --unsafe");
				settings.Unsafe = true;
				return ParseResult.Success;
				
			case "/?": case "/h": case "/help":
			case "--help":
				Usage ();
				return ParseResult.Stop;

			case "--define":
				report.Warning (-29, 1, "Compatibility: Use -d:SYMBOL instead of --define SYMBOL");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				settings.AddConditionalSymbol (args [++i]);
				return ParseResult.Success;

			case "--tokenize":
				settings.TokenizeOnly = true;
				return ParseResult.Success;
				
			case "-o": 
			case "--output":
				report.Warning (-29, 1, "Compatibility: Use -out:FILE instead of --output FILE or -o FILE");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				settings.OutputFile = args[++i];
				return ParseResult.Success;

			case "--checked":
				report.Warning (-29, 1, "Compatibility: Use -checked instead of --checked");
				settings.Checked = true;
				return ParseResult.Success;
				
			case "--stacktrace":
				report.Printer.Stacktrace = true;
				return ParseResult.Success;
				
			case "--linkresource":
			case "--linkres":
				report.Warning (-29, 1, "Compatibility: Use -linkres:VALUE instead of --linkres VALUE");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				AddResource (new AssemblyResource (args[++i], args[i]), settings);
				return ParseResult.Success;
				
			case "--resource":
			case "--res":
				report.Warning (-29, 1, "Compatibility: Use -res:VALUE instead of --res VALUE");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				AddResource (new AssemblyResource (args[++i], args[i], true), settings);
				return ParseResult.Success;
				
			case "--target":
				report.Warning (-29, 1, "Compatibility: Use -target:KIND instead of --target KIND");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				
				string type = args [++i];
				switch (type){
				case "library":
					settings.Target = Target.Library;
					settings.TargetExt = ".dll";
					break;
					
				case "exe":
					settings.Target = Target.Exe;
					break;
					
				case "winexe":
					settings.Target = Target.WinExe;
					break;
					
				case "module":
					settings.Target = Target.Module;
					settings.TargetExt = ".dll";
					break;
				default:
					report.Error (2019, "Invalid target type for -target. Valid options are `exe', `winexe', `library' or `module'");
					break;
				}
				return ParseResult.Success;
				
			case "-r":
				report.Warning (-29, 1, "Compatibility: Use -r:LIBRARY instead of -r library");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				
				string val = args [++i];
				int idx = val.IndexOf ('=');
				if (idx > -1) {
					string alias = val.Substring (0, idx);
					string assembly = val.Substring (idx + 1);
					AddAssemblyReference (alias, assembly, settings);
					return ParseResult.Success;
				}

				settings.AssemblyReferences.Add (val);
				return ParseResult.Success;
				
			case "-L":
				report.Warning (-29, 1, "Compatibility: Use -lib:ARG instead of --L arg");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				settings.ReferencesLookupPaths.Add (args [++i]);
				return ParseResult.Success;

			case "--lint":
				settings.EnhancedWarnings = true;
				return ParseResult.Success;
				
			case "--nostdlib":
				report.Warning (-29, 1, "Compatibility: Use -nostdlib instead of --nostdlib");
				settings.StdLib = false;
				return ParseResult.Success;
				
			case "--nowarn":
				report.Warning (-29, 1, "Compatibility: Use -nowarn instead of --nowarn");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				int warn = 0;
				
				try {
					warn = int.Parse (args [++i]);
				} catch {
					Usage ();
					Environment.Exit (1);
				}
				report.SetIgnoreWarning (warn);
				return ParseResult.Success;

			case "--wlevel":
				report.Warning (-29, 1, "Compatibility: Use -warn:LEVEL instead of --wlevel LEVEL");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				SetWarningLevel (args [++i]);
				return ParseResult.Success;

			case "--mcs-debug":
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				try {
					settings.DebugFlags = int.Parse (args [++i]);
				} catch {
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}

				return ParseResult.Success;
				
			case "--about":
				About ();
				return ParseResult.Stop;
				
			case "--recurse":
				report.Warning (-29, 1, "Compatibility: Use -recurse:PATTERN option instead --recurse PATTERN");
				if ((i + 1) >= args.Length){
					Error_RequiresArgument (arg);
					return ParseResult.Error;
				}
				ProcessSourceFiles (args [++i], true, settings.SourceFiles);
				return ParseResult.Success;
				
			case "--timestamp":
				settings.Timestamps = true;
				return ParseResult.Success;

			case "--debug": case "-g":
				report.Warning (-29, 1, "Compatibility: Use -debug option instead of -g or --debug");
				settings.GenerateDebugInfo = true;
				return ParseResult.Success;
				
			case "--noconfig":
				report.Warning (-29, 1, "Compatibility: Use -noconfig option instead of --noconfig");
				settings.LoadDefaultReferences = false;
				return ParseResult.Success;

			default:
				if (arg.StartsWith ("--fatal")){
					int fatal = 1;
					if (arg.StartsWith ("--fatal="))
						int.TryParse (arg.Substring (8), out fatal);

					report.Printer.FatalCounter = fatal;
					return ParseResult.Success;
				}
				if (arg.StartsWith ("--runtime:", StringComparison.Ordinal)) {
					string version = arg.Substring (10);

					switch (version) {
					case "v1":
					case "V1":
						settings.StdLibRuntimeVersion = RuntimeVersion.v1;
						break;
					case "v2":
					case "V2":
						settings.StdLibRuntimeVersion = RuntimeVersion.v2;
						break;
					case "v4":
					case "V4":
						settings.StdLibRuntimeVersion = RuntimeVersion.v4;
						break;
					}
					return ParseResult.Success;
				}

				return ParseResult.UnknownOption;
			}
		}

		void SetWarningLevel (string s)
		{
			int level = -1;

			try {
				level = int.Parse (s);
			} catch {
			}
			if (level < 0 || level > 4) {
				report.Error (1900, "Warning level must be in the range 0-4");
				return;
			}
			report.WarningLevel = level;
		}

		//
		// Given a path specification, splits the path from the file/pattern
		//
		static void SplitPathAndPattern (string spec, out string path, out string pattern)
		{
			int p = spec.LastIndexOf ('/');
			if (p != -1) {
				//
				// Windows does not like /file.cs, switch that to:
				// "\", "file.cs"
				//
				if (p == 0) {
					path = "\\";
					pattern = spec.Substring (1);
				} else {
					path = spec.Substring (0, p);
					pattern = spec.Substring (p + 1);
				}
				return;
			}

			p = spec.LastIndexOf ('\\');
			if (p != -1) {
				path = spec.Substring (0, p);
				pattern = spec.Substring (p + 1);
				return;
			}

			path = ".";
			pattern = spec;
		}

		void Usage ()
		{
			output.WriteLine (
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
				"   -langversion:TEXT    Specifies language version: ISO-1, ISO-2, 3, 4, 5, Default or Future\n" +
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
				"   -sdk:VERSION         Specifies SDK version of referenced assemblies\n" +
				"                        VERSION can be one of: 2, 4 (default) or custom value\n" +
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

		void Version ()
		{
			string version = System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType.Assembly.GetName ().Version.ToString ();
			output.WriteLine ("Mono C# compiler version {0}", version);
		}
	}

	public class RootContext
	{
		//
		// Contains the parsed tree
		//
		static ModuleContainer root;

		static public ModuleContainer ToplevelTypes {
			get { return root; }
			set { root = value; }
		}
	}
}
