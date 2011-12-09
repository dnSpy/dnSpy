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

		void tokenize_file (CompilationSourceFile file)
		{
			Stream input;

			try {
				input = File.OpenRead (file.Name);
			} catch {
				Report.Error (2001, "Source file `" + file.Name + "' could not be found");
				return;
			}

			using (input){
				SeekableStreamReader reader = new SeekableStreamReader (input, ctx.Settings.Encoding);
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

		void Parse (ModuleContainer module)
		{
			Location.Initialize (module.Compiler.SourceFiles);

			bool tokenize_only = module.Compiler.Settings.TokenizeOnly;
			var sources = module.Compiler.SourceFiles;
			for (int i = 0; i < sources.Count; ++i) {
				if (tokenize_only) {
					tokenize_file (sources[i]);
				} else {
					Parse (sources[i], module);
				}
			}
		}

		public void Parse (CompilationSourceFile file, ModuleContainer module)
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
			SeekableStreamReader reader = new SeekableStreamReader (input, ctx.Settings.Encoding);

			Parse (reader, file, module);
			reader.Dispose ();
			input.Close ();
		}	
		
		public void Parse (SeekableStreamReader reader, CompilationSourceFile file, ModuleContainer module)
		{
			file.NamespaceContainer = new NamespaceContainer (null, module, null, file);

			CSharpParser parser = new CSharpParser (reader, file);
			parser.parse ();
		}
		
		public static int Main (string[] args)
		{
			Location.InEmacs = Environment.GetEnvironmentVariable ("EMACS") == "t";

			var r = new Report (new ConsoleReportPrinter ());
			CommandLineParser cmd = new CommandLineParser (r);
			var settings = cmd.ParseArguments (args);
			if (settings == null || r.Errors > 0)
				return 1;

			if (cmd.HasBeenStopped)
				return 0;

			Driver d = new Driver (new CompilerContext (settings, r));

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
			module.CreateType ();
			importer.AddCompiledAssembly (assembly);
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

			module.CreateType ();

			loader.LoadModules (assembly, module.GlobalRootNamespace);
#endif
			module.InitializePredefinedTypes ();

			tr.Start (TimeReporter.TimerType.UsingResolve);
			foreach (var source_file in ctx.SourceFiles) {
				source_file.NamespaceContainer.Resolve ();
			}
			tr.Stop (TimeReporter.TimerType.UsingResolve);

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

			return Report.Errors == 0;
		}
	}

	public class CompilerCompilationUnit {
		public ModuleContainer ModuleCompiled { get; set; }
		public LocationsBag LocationsBag { get; set; }
		public UsingsBag UsingsBag { get; set; }
		public SpecialsBag SpecialsBag { get; set; }
		public object LastYYValue { get; set; }
	}
	
	//
	// This is the only public entry point
	//
	public class CompilerCallableEntryPoint : MarshalByRefObject {
		
		public static bool InvokeCompiler (string [] args, TextWriter error)
		{
			try {
				var r = new Report (new StreamReportPrinter (error));
				CommandLineParser cmd = new CommandLineParser (r, error);
				var setting = cmd.ParseArguments (args);
				if (setting == null || r.Errors > 0)
					return false;

				var d = new Driver (new CompilerContext (setting, r));
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
			CSharpParser.yacc_verbose_flag = 0;
			Location.Reset ();
			
			if (!full_flag)
				return;

			AnonymousTypeClass.Reset ();
			AnonymousMethodBody.Reset ();
			AnonymousMethodStorey.Reset ();
			SymbolWriter.Reset ();
			Switch.Reset ();
			Linq.QueryBlock.TransparentParameter.Reset ();
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
					//                                     Driver d = Driver.Create (args, false, null, reportPrinter);
					//                                     if (d == null)
					//                                             return null;
       
					var r = new Report (reportPrinter);
					CommandLineParser cmd = new CommandLineParser (r, Console.Out);
					var setting = cmd.ParseArguments (args);
					if (setting == null || r.Errors > 0)
						return null;
					setting.Version = LanguageVersion.V_5;
					
					CompilerContext ctx = new CompilerContext (setting, r);
					
					var files = new List<CompilationSourceFile> ();
					var unit = new CompilationSourceFile (inputFile, inputFile, 0);
					var module = new ModuleContainer (ctx);
					unit.NamespaceContainer = new NamespaceContainer (null, module, null, unit);
					files.Add (unit);
					Location.Initialize (files);

					// TODO: encoding from driver
					SeekableStreamReader reader = new SeekableStreamReader (input, Encoding.UTF8);
				
					RootContext.ToplevelTypes = module;
					
					CSharpParser parser = new CSharpParser (reader, unit);
					parser.Lexer.TabSize = 1;
					parser.Lexer.sbag = new SpecialsBag ();
					parser.LocationsBag = new LocationsBag ();
					parser.UsingsBag = new UsingsBag ();
					parser.parse ();
					
					return new CompilerCompilationUnit () { 
						ModuleCompiled = RootContext.ToplevelTypes,
						LocationsBag = parser.LocationsBag, 
						UsingsBag = parser.UsingsBag, 
						SpecialsBag = parser.Lexer.sbag
					};
				} finally {
					Reset ();
				}
			}
		}
	}
}
