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
// Copyright 2011 Xamarin Inc
//

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace Mono.CSharp
{
	/// <summary>
	///    The compiler driver.
	/// </summary>
	class Driver
	{
		readonly CompilerContext ctx;

		public Driver (CompilerContext ctx)
		{
			this.ctx = ctx;
		}

		Report Report {
			get {
				return ctx.Report;
			}
		}

		void tokenize_file (SourceFile sourceFile, ModuleContainer module, ParserSession session)
		{
			Stream input;

			try {
				input = File.OpenRead (sourceFile.Name);
			} catch {
				Report.Error (2001, "Source file `" + sourceFile.Name + "' could not be found");
				return;
			}

			using (input){
				SeekableStreamReader reader = new SeekableStreamReader (input, ctx.Settings.Encoding);
				var file = new CompilationSourceFile (module, sourceFile);

				Tokenizer lexer = new Tokenizer (reader, file, session, ctx.Report);
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

		void Parse (ModuleContainer module)
		{
			bool tokenize_only = module.Compiler.Settings.TokenizeOnly;
			var sources = module.Compiler.SourceFiles;

			Location.Initialize (sources);

			var session = new ParserSession {
				UseJayGlobalArrays = true,
				LocatedTokens = new LocatedToken[15000]
			};

			for (int i = 0; i < sources.Count; ++i) {
				if (tokenize_only) {
					tokenize_file (sources[i], module, session);
				} else {
					Parse (sources[i], module, session, Report);
				}
			}
		}

#if false
		void ParseParallel (ModuleContainer module)
		{
			var sources = module.Compiler.SourceFiles;

			Location.Initialize (sources);

			var pcount = Environment.ProcessorCount;
			var threads = new Thread[System.Math.Max (2, pcount - 1)];

			for (int i = 0; i < threads.Length; ++i) {
				var t = new Thread (l => {
					var session = new ParserSession () {
						//UseJayGlobalArrays = true,
					};

					var report = new Report (ctx, Report.Printer); // TODO: Implement flush at once printer

					for (int ii = (int) l; ii < sources.Count; ii += threads.Length) {
						Parse (sources[ii], module, session, report);
					}

					// TODO: Merge warning regions
				});

				t.Start (i);
				threads[i] = t;
			}

			for (int t = 0; t < threads.Length; ++t) {
				threads[t].Join ();
			}
		}
#endif

		public void Parse (SourceFile file, ModuleContainer module, ParserSession session, Report report)
		{
			Stream input;

			try {
				input = File.OpenRead (file.Name);
			} catch {
				report.Error (2001, "Source file `{0}' could not be found", file.Name);
				return;
			}

			// Check 'MZ' header
			if (input.ReadByte () == 77 && input.ReadByte () == 90) {

				report.Error (2015, "Source file `{0}' is a binary file and not a text file", file.Name);
				input.Close ();
				return;
			}

			input.Position = 0;
			SeekableStreamReader reader = new SeekableStreamReader (input, ctx.Settings.Encoding, session.StreamReaderBuffer);

			Parse (reader, file, module, session, report);

			if (ctx.Settings.GenerateDebugInfo && report.Errors == 0 && !file.HasChecksum) {
				input.Position = 0;
				var checksum = session.GetChecksumAlgorithm ();
				file.SetChecksum (checksum.ComputeHash (input));
			}

			reader.Dispose ();
			input.Close ();
		}

		public static CSharpParser Parse (SeekableStreamReader reader, SourceFile sourceFile, ModuleContainer module, ParserSession session, Report report, int lineModifier = 0, int colModifier = 0)
		{
			var file = new CompilationSourceFile (module, sourceFile);
			module.AddTypeContainer(file);

			CSharpParser parser = new CSharpParser (reader, file, report, session);
			parser.Lexer.Line += lineModifier;
			parser.Lexer.Column += colModifier;
			parser.Lexer.sbag = new SpecialsBag ();
			parser.parse ();
			return parser;
		}

		public static int Main (string[] args)
		{
			Location.InEmacs = Environment.GetEnvironmentVariable ("EMACS") == "t";

			CommandLineParser cmd = new CommandLineParser (Console.Out);
			var settings = cmd.ParseArguments (args);
			if (settings == null)
				return 1;

			if (cmd.HasBeenStopped)
				return 0;

			Driver d = new Driver (new CompilerContext (settings, new ConsoleReportPrinter ()));

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

		public static string GetPackageFlags (string packages, Report report)
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
				if (report == null)
					throw;

				report.Error (-27, "Couldn't run pkg-config: " + e.Message);
				return null;
			}
			
			if (p.StandardOutput == null) {
				if (report == null)
					throw new ApplicationException ("Specified package did not return any information");

				report.Warning (-27, 1, "Specified package did not return any information");
				p.Close ();
				return null;
			}

			string pkgout = p.StandardOutput.ReadToEnd ();
			p.WaitForExit ();
			if (p.ExitCode != 0) {
				if (report == null)
					throw new ApplicationException (pkgout);

				report.Error (-27, "Error running pkg-config. Check the above output.");
				p.Close ();
				return null;
			}

			p.Close ();
			return pkgout;
		}

		//
		// Main compilation method
		//
		public bool Compile ()
		{
			var settings = ctx.Settings;

			//
			// If we are an exe, require a source file for the entry point or
			// if there is nothing to put in the assembly, and we are not a library
			//
			if (settings.FirstSourceFile == null &&
				((settings.Target == Target.Exe || settings.Target == Target.WinExe || settings.Target == Target.Module) ||
				settings.Resources == null)) {
				Report.Error (2008, "No files to compile were specified");
				return false;
			}

			if (settings.Platform == Platform.AnyCPU32Preferred && (settings.Target == Target.Library || settings.Target == Target.Module)) {
				Report.Error (4023, "Platform option `anycpu32bitpreferred' is valid only for executables");
				return false;
			}

			TimeReporter tr = new TimeReporter (settings.Timestamps);
			ctx.TimeReporter = tr;
			tr.StartTotal ();

			var module = new ModuleContainer (ctx);
			RootContext.ToplevelTypes = module;

			tr.Start (TimeReporter.TimerType.ParseTotal);
			Parse (module);
			tr.Stop (TimeReporter.TimerType.ParseTotal);

			if (Report.Errors > 0)
				return false;

			if (settings.TokenizeOnly || settings.ParseOnly) {
				tr.StopTotal ();
				tr.ShowStats ();
				return true;
			}

			var output_file = settings.OutputFile;
			string output_file_name;
			if (output_file == null) {
				var source_file = settings.FirstSourceFile;

				if (source_file == null) {
					Report.Error (1562, "If no source files are specified you must specify the output file with -out:");
					return false;
				}

				output_file_name = source_file.Name;
				int pos = output_file_name.LastIndexOf ('.');

				if (pos > 0)
					output_file_name = output_file_name.Substring (0, pos);
				
				output_file_name += settings.TargetExt;
				output_file = output_file_name;
			} else {
				output_file_name = Path.GetFileName (output_file);

				if (string.IsNullOrEmpty (Path.GetFileNameWithoutExtension (output_file_name)) ||
					output_file_name.IndexOfAny (Path.GetInvalidFileNameChars ()) >= 0) {
					Report.Error (2021, "Output file name is not valid");
					return false;
				}
			}

#if STATIC
			var importer = new StaticImporter (module);
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
			module.CreateContainer ();
			importer.AddCompiledAssembly (assembly);
			references_loader.CompiledAssembly = assembly;
			tr.Stop (TimeReporter.TimerType.CreateTypeTotal);

			references_loader.LoadReferences (module);

			tr.Start (TimeReporter.TimerType.PredefinedTypesInit);
			if (!ctx.BuiltinTypes.CheckDefinitions (module))
				return false;

			tr.Stop (TimeReporter.TimerType.PredefinedTypesInit);

			references_loader.LoadModules (assembly, module.GlobalRootNamespace);
#else
			var assembly = new AssemblyDefinitionDynamic (module, output_file_name, output_file);
			module.SetDeclaringAssembly (assembly);

			var importer = new ReflectionImporter (module, ctx.BuiltinTypes);
			assembly.Importer = importer;

			var loader = new DynamicLoader (importer, ctx);
			loader.LoadReferences (module);

			if (!ctx.BuiltinTypes.CheckDefinitions (module))
				return false;

			if (!assembly.Create (AppDomain.CurrentDomain, AssemblyBuilderAccess.Save))
				return false;

			module.CreateContainer ();

			loader.LoadModules (assembly, module.GlobalRootNamespace);
#endif
			module.InitializePredefinedTypes ();

			tr.Start (TimeReporter.TimerType.ModuleDefinitionTotal);
			module.Define ();
			tr.Stop (TimeReporter.TimerType.ModuleDefinitionTotal);

			if (Report.Errors > 0)
				return false;

			if (settings.DocumentationFile != null) {
				var doc = new DocumentationBuilder (module);
				doc.OutputDocComment (output_file, settings.DocumentationFile);
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
			module.CloseContainer ();
			tr.Stop (TimeReporter.TimerType.CloseTypes);

			tr.Start (TimeReporter.TimerType.Resouces);
			if (!settings.WriteMetadataOnly)
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

			return Report.Errors == 0;
		}
	}

	public class CompilerCompilationUnit {
		public ModuleContainer ModuleCompiled { get; set; }
		public LocationsBag LocationsBag { get; set; }
		public SpecialsBag SpecialsBag { get; set; }
		public IDictionary<string, bool> Conditionals { get; set; }
		public object LastYYValue { get; set; }
	}

	//
	// This is the only public entry point
	//
	public class CompilerCallableEntryPoint : MarshalByRefObject
	{
		public static bool InvokeCompiler (string [] args, TextWriter error)
		{
			try {
				CommandLineParser cmd = new CommandLineParser (error);
				var setting = cmd.ParseArguments (args);
				if (setting == null)
					return false;

				var d = new Driver (new CompilerContext (setting, new StreamReportPrinter (error)));
				return d.Compile ();
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
			Location.Reset ();
			
			if (!full_flag)
				return;

			Linq.QueryBlock.TransparentParameter.Reset ();
			TypeInfo.Reset ();
		}
	}
	
}
