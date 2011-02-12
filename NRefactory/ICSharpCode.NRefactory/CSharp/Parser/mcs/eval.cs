//
// eval.cs: Evaluation and Hosting API for the C# compiler
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004, 2005, 2006, 2007, 2008 Novell, Inc
//

using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Text;

namespace Mono.CSharp
{

	/// <summary>
	///   Evaluator: provides an API to evaluate C# statements and
	///   expressions dynamically.
	/// </summary>
	/// <remarks>
	///   This class exposes static methods to evaluate expressions in the
	///   current program.
	///
	///   To initialize the evaluator with a number of compiler
	///   options call the Init(string[]args) method with a set of
	///   command line options that the compiler recognizes.
	///
	///   To interrupt execution of a statement, you can invoke the
	///   Evaluator.Interrupt method.
	/// </remarks>
	public class Evaluator {

		enum ParseMode {
			// Parse silently, do not output any error messages
			Silent,

			// Report errors during parse
			ReportErrors,

			// Auto-complete, means that the tokenizer will start producing
			// GETCOMPLETIONS tokens when it reaches a certain point.
			GetCompletions
		}

		static object evaluator_lock = new object ();
		
		static string current_debug_name;
		static int count;
		static Thread invoke_thread;

		static List<NamespaceEntry.UsingAliasEntry> using_alias_list = new List<NamespaceEntry.UsingAliasEntry> ();
		internal static List<NamespaceEntry.UsingEntry> using_list = new List<NamespaceEntry.UsingEntry> ();
		static Dictionary<string, Tuple<FieldSpec, FieldInfo>> fields = new Dictionary<string, Tuple<FieldSpec, FieldInfo>> ();

		static TypeSpec interactive_base_class;
		static Driver driver;
		static bool inited;

		static CompilerContext ctx;
		static DynamicLoader loader;
		
		public static TextWriter MessageOutput = Console.Out;

		/// <summary>
		///   Optional initialization for the Evaluator.
		/// </summary>
		/// <remarks>
		///  Initializes the Evaluator with the command line options
		///  that would be processed by the command line compiler.  Only
		///  the first call to Init will work, any future invocations are
		///  ignored.
		///
		///  You can safely avoid calling this method if your application
		///  does not need any of the features exposed by the command line
		///  interface.
		/// </remarks>
		public static void Init (string [] args)
		{
			InitAndGetStartupFiles (args);
		}

		internal static ReportPrinter SetPrinter (ReportPrinter report_printer)
		{
			return ctx.Report.SetPrinter (report_printer);
		}				

		public static string [] InitAndGetStartupFiles (string [] args)
		{
			return InitAndGetStartupFiles (args, null);
		}

		/// <summary>
		///   Optional initialization for the Evaluator.
		/// </summary>
		/// <remarks>
		///  Initializes the Evaluator with the command line
		///  options that would be processed by the command
		///  line compiler.  Only the first call to
		///  InitAndGetStartupFiles or Init will work, any future
		///  invocations are ignored.
		///
		///  You can safely avoid calling this method if your application
		///  does not need any of the features exposed by the command line
		///  interface.
		///
		///  This method return an array of strings that contains any
		///  files that were specified in `args'.
		///
		///  If the unknownOptionParser is not null, this function is invoked
		///  with the current args array and the index of the option that is not
		///  known.  A value of true means that the value was processed, otherwise
		///  it will be reported as an error
		/// </remarks>
		public static string [] InitAndGetStartupFiles (string [] args, Func<string [], int, int> unknownOptionParser)
		{
			lock (evaluator_lock){
				if (inited)
					return new string [0];

				CompilerCallableEntryPoint.Reset ();
				var crp = new ConsoleReportPrinter ();
				driver = Driver.Create (args, false, unknownOptionParser, crp);
				if (driver == null)
					throw new Exception ("Failed to create compiler driver with the given arguments");

				crp.Fatal = driver.fatal_errors;
				ctx = driver.ctx;

				RootContext.ToplevelTypes = new ModuleContainer (ctx);
				
				var startup_files = new List<string> ();
				foreach (CompilationUnit file in Location.SourceFiles)
					startup_files.Add (file.Path);
				
				CompilerCallableEntryPoint.PartialReset ();

				var importer = new ReflectionImporter (ctx.BuildinTypes);
				loader = new DynamicLoader (importer, ctx);

				RootContext.ToplevelTypes.SetDeclaringAssembly (new AssemblyDefinitionDynamic (RootContext.ToplevelTypes, "temp"));

				loader.LoadReferences (RootContext.ToplevelTypes);
				ctx.BuildinTypes.CheckDefinitions (RootContext.ToplevelTypes);
				RootContext.ToplevelTypes.InitializePredefinedTypes ();

				RootContext.EvalMode = true;
				inited = true;

				return startup_files.ToArray ();
			}
		}

		static void Init ()
		{
			Init (new string [0]);
		}
		
		static void Reset ()
		{
			CompilerCallableEntryPoint.PartialReset ();
			
			Location.AddFile (null, "{interactive}");
			Location.Initialize ();

			current_debug_name = "interactive" + (count++) + ".dll";
		}

		/// <summary>
		///   The base class for the classes that host the user generated code
		/// </summary>
		/// <remarks>
		///
		///   This is the base class that will host the code
		///   executed by the Evaluator.  By default
		///   this is the Mono.CSharp.InteractiveBase class
		///   which is useful for interactive use.
		///
		///   By changing this property you can control the
		///   base class and the static members that are
		///   available to your evaluated code.
		/// </remarks>
		static public TypeSpec InteractiveBaseClass {
			get {
				if (interactive_base_class != null)
					return interactive_base_class;

				return loader.Importer.ImportType (typeof (InteractiveBase));
			}
		}

		public static void SetInteractiveBaseClass (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ();

			if (!inited)
				throw new Exception ("Evaluator has to be initiated before seting custom InteractiveBase class");

			lock (evaluator_lock)
				interactive_base_class = loader.Importer.ImportType (type);
		}

		/// <summary>
		///   Interrupts the evaluation of an expression executing in Evaluate.
		/// </summary>
		/// <remarks>
		///   Use this method to interrupt long-running invocations.
		/// </remarks>
		public static void Interrupt ()
		{
			if (!inited || !invoking)
				return;
			
			if (invoke_thread != null)
				invoke_thread.Abort ();
		}

		/// <summary>
		///   Compiles the input string and returns a delegate that represents the compiled code.
		/// </summary>
		/// <remarks>
		///
		///   Compiles the input string as a C# expression or
		///   statement, unlike the Evaluate method, the
		///   resulting delegate can be invoked multiple times
		///   without incurring in the compilation overhead.
		///
		///   If the return value of this function is null,
		///   this indicates that the parsing was complete.
		///   If the return value is a string it indicates
		///   that the input string was partial and that the
		///   invoking code should provide more code before
		///   the code can be successfully compiled.
		///
		///   If you know that you will always get full expressions or
		///   statements and do not care about partial input, you can use
		///   the other Compile overload. 
		///
		///   On success, in addition to returning null, the
		///   compiled parameter will be set to the delegate
		///   that can be invoked to execute the code.
		///
	        /// </remarks>
		static public string Compile (string input, out CompiledMethod compiled)
		{
			if (input == null || input.Length == 0){
				compiled = null;
				return null;
			}

			lock (evaluator_lock){
				if (!inited)
					Init ();
				else
					ctx.Report.Printer.Reset ();

			//	RootContext.ToplevelTypes = new ModuleContainer (ctx);

				bool partial_input;
				CSharpParser parser = ParseString (ParseMode.Silent, input, out partial_input);
				if (parser == null){
					compiled = null;
					if (partial_input)
						return input;
					
					ParseString (ParseMode.ReportErrors, input, out partial_input);
					return null;
				}
				
				object parser_result = parser.InteractiveResult;
				
				if (!(parser_result is Class)){
					int errors = ctx.Report.Errors;

					NamespaceEntry.VerifyAllUsing ();
					if (errors == ctx.Report.Errors)
						parser.CurrentNamespace.Extract (using_alias_list, using_list);
					else
						NamespaceEntry.Reset ();
				}

#if STATIC
				throw new NotSupportedException ();
#else
				compiled = CompileBlock (parser_result as Class, parser.undo, ctx.Report);
				return null;
#endif
			}
		}

		/// <summary>
		///   Compiles the input string and returns a delegate that represents the compiled code.
		/// </summary>
		/// <remarks>
		///
		///   Compiles the input string as a C# expression or
		///   statement, unlike the Evaluate method, the
		///   resulting delegate can be invoked multiple times
		///   without incurring in the compilation overhead.
		///
		///   This method can only deal with fully formed input
		///   strings and does not provide a completion mechanism.
		///   If you must deal with partial input (for example for
		///   interactive use) use the other overload. 
		///
		///   On success, a delegate is returned that can be used
		///   to invoke the method.
		///
	        /// </remarks>
		static public CompiledMethod Compile (string input)
		{
			CompiledMethod compiled;

			// Ignore partial inputs
			if (Compile (input, out compiled) != null){
				// Error, the input was partial.
				return null;
			}

			// Either null (on error) or the compiled method.
			return compiled;
		}

		//
		// Todo: Should we handle errors, or expect the calling code to setup
		// the recording themselves?
		//

		/// <summary>
		///   Evaluates and expression or statement and returns any result values.
		/// </summary>
		/// <remarks>
		///   Evaluates the input string as a C# expression or
		///   statement.  If the input string is an expression
		///   the result will be stored in the result variable
		///   and the result_set variable will be set to true.
		///
		///   It is necessary to use the result/result_set
		///   pair to identify when a result was set (for
		///   example, execution of user-provided input can be
		///   an expression, a statement or others, and
		///   result_set would only be set if the input was an
		///   expression.
		///
		///   If the return value of this function is null,
		///   this indicates that the parsing was complete.
		///   If the return value is a string, it indicates
		///   that the input is partial and that the user
		///   should provide an updated string.
		/// </remarks>
		public static string Evaluate (string input, out object result, out bool result_set)
		{
			CompiledMethod compiled;

			result_set = false;
			result = null;

			input = Compile (input, out compiled);
			if (input != null)
				return input;
			
			if (compiled == null)
				return null;
				
			//
			// The code execution does not need to keep the compiler lock
			//
			object retval = typeof (NoValueSet);

			try {
				invoke_thread = System.Threading.Thread.CurrentThread;
				invoking = true;
				compiled (ref retval);
			} catch (ThreadAbortException e){
				Thread.ResetAbort ();
				Console.WriteLine ("Interrupted!\n{0}", e);
			} finally {
				invoking = false;
			}

			//
			// We use a reference to a compiler type, in this case
			// Driver as a flag to indicate that this was a statement
			//
			if (retval != typeof (NoValueSet)){
				result_set = true;
				result = retval; 
			}

			return null;
		}

		public static string [] GetCompletions (string input, out string prefix)
		{
			prefix = "";
			if (input == null || input.Length == 0)
				return null;
			
			lock (evaluator_lock){
				if (!inited)
					Init ();
				
				bool partial_input;
				CSharpParser parser = ParseString (ParseMode.GetCompletions, input, out partial_input);
				if (parser == null){
					if (CSharpParser.yacc_verbose_flag != 0)
						Console.WriteLine ("DEBUG: No completions available");
					return null;
				}
				
				Class parser_result = parser.InteractiveResult as Class;
				
				if (parser_result == null){
					if (CSharpParser.yacc_verbose_flag != 0)
						Console.WriteLine ("Do not know how to cope with !Class yet");
					return null;
				}

				try {
					var a = new AssemblyDefinitionDynamic (RootContext.ToplevelTypes, "temp");
					a.Create (AppDomain.CurrentDomain, AssemblyBuilderAccess.Run);
					RootContext.ToplevelTypes.SetDeclaringAssembly (a);
					RootContext.ToplevelTypes.CreateType ();
					RootContext.ToplevelTypes.Define ();
					if (ctx.Report.Errors != 0)
						return null;
					
					MethodOrOperator method = null;
					foreach (MemberCore member in parser_result.Methods){
						if (member.Name != "Host")
							continue;
						
						method = (MethodOrOperator) member;
						break;
					}
					if (method == null)
						throw new InternalErrorException ("did not find the the Host method");

					BlockContext bc = new BlockContext (method, method.Block, method.ReturnType);

					try {
						method.Block.Resolve (null, bc, method);
					} catch (CompletionResult cr){
						prefix = cr.BaseText;
						return cr.Result;
					} 
				} finally {
					parser.undo.ExecuteUndo ();
				}
				
			}
			return null;
		}

		/// <summary>
		///   Executes the given expression or statement.
		/// </summary>
		/// <remarks>
		///    Executes the provided statement, returns true
		///    on success, false on parsing errors.  Exceptions
		///    might be thrown by the called code.
		/// </remarks>
		public static bool Run (string statement)
		{
			if (!inited)
				Init ();

			object result;
			bool result_set;

			bool ok = Evaluate (statement, out result, out result_set) == null;
			
			return ok;
		}

		/// <summary>
		///   Evaluates and expression or statement and returns the result.
		/// </summary>
		/// <remarks>
		///   Evaluates the input string as a C# expression or
		///   statement and returns the value.   
		///
		///   This method will throw an exception if there is a syntax error,
		///   of if the provided input is not an expression but a statement.
		/// </remarks>
		public static object Evaluate (string input)
		{
			object result;
			bool result_set;
			
			string r = Evaluate (input, out result, out result_set);

			if (r != null)
				throw new ArgumentException ("Syntax error on input: partial input");
			
			if (result_set == false)
				throw new ArgumentException ("The expression did not set a result");

			return result;
		}

		enum InputKind {
			EOF,
			StatementOrExpression,
			CompilationUnit,
			Error
		}

		//
		// Deambiguates the input string to determine if we
		// want to process a statement or if we want to
		// process a compilation unit.
		//
		// This is done using a top-down predictive parser,
		// since the yacc/jay parser can not deambiguage this
		// without more than one lookahead token.   There are very
		// few ambiguities.
		//
		static InputKind ToplevelOrStatement (SeekableStreamReader seekable)
		{
			Tokenizer tokenizer = new Tokenizer (seekable, (CompilationUnit) Location.SourceFiles [0], ctx);
			
			int t = tokenizer.token ();
			switch (t){
			case Token.EOF:
				return InputKind.EOF;
				
			// These are toplevels
			case Token.EXTERN:
			case Token.OPEN_BRACKET:
			case Token.ABSTRACT:
			case Token.CLASS:
			case Token.ENUM:
			case Token.INTERFACE:
			case Token.INTERNAL:
			case Token.NAMESPACE:
			case Token.PRIVATE:
			case Token.PROTECTED:
			case Token.PUBLIC:
			case Token.SEALED:
			case Token.STATIC:
			case Token.STRUCT:
				return InputKind.CompilationUnit;
				
			// Definitely expression
			case Token.FIXED:
			case Token.BOOL:
			case Token.BYTE:
			case Token.CHAR:
			case Token.DECIMAL:
			case Token.DOUBLE:
			case Token.FLOAT:
			case Token.INT:
			case Token.LONG:
			case Token.NEW:
			case Token.OBJECT:
			case Token.SBYTE:
			case Token.SHORT:
			case Token.STRING:
			case Token.UINT:
			case Token.ULONG:
				return InputKind.StatementOrExpression;

			// These need deambiguation help
			case Token.USING:
				t = tokenizer.token ();
				if (t == Token.EOF)
					return InputKind.EOF;

				if (t == Token.IDENTIFIER)
					return InputKind.CompilationUnit;
				return InputKind.StatementOrExpression;


			// Distinguish between:
			//    delegate opt_anonymous_method_signature block
			//    delegate type 
			case Token.DELEGATE:
				t = tokenizer.token ();
				if (t == Token.EOF)
					return InputKind.EOF;
				if (t == Token.OPEN_PARENS || t == Token.OPEN_BRACE)
					return InputKind.StatementOrExpression;
				return InputKind.CompilationUnit;

			// Distinguih between:
			//    unsafe block
			//    unsafe as modifier of a type declaration
			case Token.UNSAFE:
				t = tokenizer.token ();
				if (t == Token.EOF)
					return InputKind.EOF;
				if (t == Token.OPEN_PARENS)
					return InputKind.StatementOrExpression;
				return InputKind.CompilationUnit;
				
		        // These are errors: we list explicitly what we had
			// from the grammar, ERROR and then everything else

			case Token.READONLY:
			case Token.OVERRIDE:
			case Token.ERROR:
				return InputKind.Error;

			// This catches everything else allowed by
			// expressions.  We could add one-by-one use cases
			// if needed.
			default:
				return InputKind.StatementOrExpression;
			}
		}
		
		//
		// Parses the string @input and returns a CSharpParser if succeeful.
		//
		// if @silent is set to true then no errors are
		// reported to the user.  This is used to do various calls to the
		// parser and check if the expression is parsable.
		//
		// @partial_input: if @silent is true, then it returns whether the
		// parsed expression was partial, and more data is needed
		//
		static CSharpParser ParseString (ParseMode mode, string input, out bool partial_input)
		{
			partial_input = false;
			Reset ();
			queued_fields.Clear ();
			Tokenizer.LocatedToken.Initialize ();

			Stream s = new MemoryStream (Encoding.Default.GetBytes (input));
			SeekableStreamReader seekable = new SeekableStreamReader (s, Encoding.Default);

			InputKind kind = ToplevelOrStatement (seekable);
			if (kind == InputKind.Error){
				if (mode == ParseMode.ReportErrors)
					ctx.Report.Error (-25, "Detection Parsing Error");
				partial_input = false;
				return null;
			}

			if (kind == InputKind.EOF){
				if (mode == ParseMode.ReportErrors)
					Console.Error.WriteLine ("Internal error: EOF condition should have been detected in a previous call with silent=true");
				partial_input = true;
				return null;
				
			}
			seekable.Position = 0;

			CSharpParser parser = new CSharpParser (seekable, Location.SourceFiles [0], RootContext.ToplevelTypes);

			if (kind == InputKind.StatementOrExpression){
				parser.Lexer.putback_char = Tokenizer.EvalStatementParserCharacter;
				RootContext.StatementMode = true;
			} else {
				//
				// Do not activate EvalCompilationUnitParserCharacter until
				// I have figured out all the limitations to invoke methods
				// in the generated classes.  See repl.txt
				//
				parser.Lexer.putback_char = Tokenizer.EvalUsingDeclarationsParserCharacter;
				//parser.Lexer.putback_char = Tokenizer.EvalCompilationUnitParserCharacter;
				RootContext.StatementMode = false;
			}

			if (mode == ParseMode.GetCompletions)
				parser.Lexer.CompleteOnEOF = true;

			ReportPrinter old_printer = null;
			if ((mode == ParseMode.Silent || mode == ParseMode.GetCompletions) && CSharpParser.yacc_verbose_flag == 0)
				old_printer = SetPrinter (new StreamReportPrinter (TextWriter.Null));

			try {
				parser.parse ();
			} finally {
				if (ctx.Report.Errors != 0){
					if (mode != ParseMode.ReportErrors  && parser.UnexpectedEOF)
						partial_input = true;

					parser.undo.ExecuteUndo ();
					parser = null;
				}

				if (old_printer != null)
					SetPrinter (old_printer);
			}
			return parser;
		}

		//
		// Queue all the fields that we use, as we need to then go from FieldBuilder to FieldInfo
		// or reflection gets confused (it basically gets confused, and variables override each
		// other).
		//
		static List<Field> queued_fields = new List<Field> ();
		
		//static ArrayList types = new ArrayList ();

		static volatile bool invoking;
#if !STATIC		
		static CompiledMethod CompileBlock (Class host, Undo undo, Report Report)
		{
			AssemblyDefinitionDynamic assembly;

			if (Environment.GetEnvironmentVariable ("SAVE") != null) {
				assembly = new AssemblyDefinitionDynamic (RootContext.ToplevelTypes, current_debug_name, current_debug_name);
				assembly.Importer = loader.Importer;
			} else {
				assembly = new AssemblyDefinitionDynamic (RootContext.ToplevelTypes, current_debug_name);
			}

			assembly.Create (AppDomain.CurrentDomain, AssemblyBuilderAccess.RunAndSave);
			RootContext.ToplevelTypes.CreateType ();
			RootContext.ToplevelTypes.Define ();

			if (Report.Errors != 0){
				undo.ExecuteUndo ();
				return null;
			}

			TypeBuilder tb = null;
			MethodBuilder mb = null;
				
			if (host != null){
				tb = host.TypeBuilder;
				mb = null;
				foreach (MemberCore member in host.Methods){
					if (member.Name != "Host")
						continue;
					
					MethodOrOperator method = (MethodOrOperator) member;
					mb = method.MethodBuilder;
					break;
				}

				if (mb == null)
					throw new Exception ("Internal error: did not find the method builder for the generated method");
			}
			
			RootContext.ToplevelTypes.Emit ();
			if (Report.Errors != 0){
				undo.ExecuteUndo ();
				return null;
			}

			RootContext.ToplevelTypes.CloseType ();

			if (Environment.GetEnvironmentVariable ("SAVE") != null)
				assembly.Save ();

			if (host == null)
				return null;
			
			//
			// Unlike Mono, .NET requires that the MethodInfo is fetched, it cant
			// work from MethodBuilders.   Retarded, I know.
			//
			var tt = assembly.Builder.GetType (tb.Name);
			MethodInfo mi = tt.GetMethod (mb.Name);
			
			// Pull the FieldInfos from the type, and keep track of them
			foreach (Field field in queued_fields){
				FieldInfo fi = tt.GetField (field.Name);

				Tuple<FieldSpec, FieldInfo> old;
				
				// If a previous value was set, nullify it, so that we do
				// not leak memory
				if (fields.TryGetValue (field.Name, out old)) {
					if (old.Item1.MemberType.IsStruct) {
						//
						// TODO: Clear fields for structs
						//
					} else {
						try {
							old.Item2.SetValue (null, null);
						} catch {
						}
					}

					fields [field.Name] = Tuple.Create (field.Spec, fi);
				} else {
					fields.Add (field.Name, Tuple.Create (field.Spec, fi));
				}
			}
			//types.Add (tb);

			queued_fields.Clear ();
			
			return (CompiledMethod) System.Delegate.CreateDelegate (typeof (CompiledMethod), mi);
		}
#endif
		static internal void LoadAliases (NamespaceEntry ns)
		{
			ns.Populate (using_alias_list, using_list);
		}
		
		/// <summary>
		///   A sentinel value used to indicate that no value was
		///   was set by the compiled function.   This is used to
		///   differentiate between a function not returning a
		///   value and null.
		/// </summary>
		public class NoValueSet {
		}

		static internal Tuple<FieldSpec, FieldInfo> LookupField (string name)
		{
			Tuple<FieldSpec, FieldInfo> fi;
			fields.TryGetValue (name, out fi);
			return fi;
		}

		//
		// Puts the FieldBuilder into a queue of names that will be
		// registered.   We can not register FieldBuilders directly
		// we need to fetch the FieldInfo after Reflection cooks the
		// types, or bad things happen (bad means: FieldBuilders behave
		// incorrectly across multiple assemblies, causing assignments to
		// invalid areas
		//
		// This also serves for the parser to register Field classes
		// that should be exposed as global variables
		//
		static internal void QueueField (Field f)
		{
			queued_fields.Add (f);
		}

		static string Quote (string s)
		{
			if (s.IndexOf ('"') != -1)
				s = s.Replace ("\"", "\\\"");
			
			return "\"" + s + "\"";
		}

		static public string GetUsing ()
		{
			lock (evaluator_lock){
				StringBuilder sb = new StringBuilder ();
				
				foreach (object x in using_alias_list)
					sb.Append (String.Format ("using {0};\n", x));
				
				foreach (object x in using_list)
					sb.Append (String.Format ("using {0};\n", x));
				
				return sb.ToString ();
			}
		}

		static internal ICollection<string> GetUsingList ()
		{
			var res = new List<string> (using_list.Count);
			foreach (object ue in using_list)
				res.Add (ue.ToString ());
			return res;
		}
		
		static internal string [] GetVarNames ()
		{
			lock (evaluator_lock){
				return new List<string> (fields.Keys).ToArray ();
			}
		}
		
		static public string GetVars ()
		{
			lock (evaluator_lock){
				StringBuilder sb = new StringBuilder ();
				
				foreach (var de in fields){
					var fi = LookupField (de.Key);
					object value;
					try {
						value = fi.Item2.GetValue (null);
						if (value is string)
							value = Quote ((string)value);
					} catch {
						value = "<error reading value>";
					}

					sb.AppendFormat ("{0} {1} = {2}", fi.Item1.MemberType.GetSignatureForError (), de.Key, value);
					sb.AppendLine ();
				}
				
				return sb.ToString ();
			}
		}

		/// <summary>
		///    Loads the given assembly and exposes the API to the user.
		/// </summary>
		static public void LoadAssembly (string file)
		{
			lock (evaluator_lock){
				var a = loader.LoadAssemblyFile (file);
				if (a != null)
					loader.Importer.ImportAssembly (a, RootContext.ToplevelTypes.GlobalRootNamespace);
			}
		}

		/// <summary>
		///    Exposes the API of the given assembly to the Evaluator
		/// </summary>
		static public void ReferenceAssembly (Assembly a)
		{
			lock (evaluator_lock){
				loader.Importer.ImportAssembly (a, RootContext.ToplevelTypes.GlobalRootNamespace);
			}
		}

		/// <summary>
		///   If true, turns type expressions into valid expressions
		///   and calls the describe method on it
		/// </summary>
		public static bool DescribeTypeExpressions;
	}

	
	/// <summary>
	///   A delegate that can be used to invoke the
	///   compiled expression or statement.
	/// </summary>
	/// <remarks>
	///   Since the Compile methods will compile
	///   statements and expressions into the same
	///   delegate, you can tell if a value was returned
	///   by checking whether the returned value is of type
	///   NoValueSet.   
	/// </remarks>
	
	public delegate void CompiledMethod (ref object retvalue);

	/// <summary>
	///   The default base class for every interaction line
	/// </summary>
	/// <remarks>
	///   The expressions and statements behave as if they were
	///   a static method of this class.   The InteractiveBase class
	///   contains a number of useful methods, but can be overwritten
	///   by setting the InteractiveBaseType property in the Evaluator
	/// </remarks>
	public class InteractiveBase {
		/// <summary>
		///   Determines where the standard output of methods in this class will go. 
		/// </summary>
		public static TextWriter Output = Console.Out;

		/// <summary>
		///   Determines where the standard error of methods in this class will go. 
		/// </summary>
		public static TextWriter Error = Console.Error;

		/// <summary>
		///   The primary prompt used for interactive use.
		/// </summary>
		public static string Prompt             = "csharp> ";

		/// <summary>
		///   The secondary prompt used for interactive use (used when
		///   an expression is incomplete).
		/// </summary>
		public static string ContinuationPrompt = "      > ";

		/// <summary>
		///   Used to signal that the user has invoked the  `quit' statement.
		/// </summary>
		public static bool QuitRequested;
		
		/// <summary>
		///   Shows all the variables defined so far.
		/// </summary>
		static public void ShowVars ()
		{
			Output.Write (Evaluator.GetVars ());
			Output.Flush ();
		}

		/// <summary>
		///   Displays the using statements in effect at this point. 
		/// </summary>
		static public void ShowUsing ()
		{
			Output.Write (Evaluator.GetUsing ());
			Output.Flush ();
		}

		public delegate void Simple ();
		
		/// <summary>
		///   Times the execution of the given delegate
		/// </summary>
		static public TimeSpan Time (Simple a)
		{
			DateTime start = DateTime.Now;
			a ();
			return DateTime.Now - start;
		}
		
#if !STATIC
		/// <summary>
		///   Loads the assemblies from a package
		/// </summary>
		/// <remarks>
		///   Loads the assemblies from a package.   This is equivalent
		///   to passing the -pkg: command line flag to the C# compiler
		///   on the command line. 
		/// </remarks>
		static public void LoadPackage (string pkg)
		{
			if (pkg == null){
				Error.WriteLine ("Invalid package specified");
				return;
			}

			string pkgout = Driver.GetPackageFlags (pkg, false, RootContext.ToplevelTypes.Compiler.Report);
			if (pkgout == null)
				return;

			string [] xargs = pkgout.Trim (new Char [] {' ', '\n', '\r', '\t'}).
				Split (new Char [] { ' ', '\t'});

			foreach (string s in xargs){
				if (s.StartsWith ("-r:") || s.StartsWith ("/r:") || s.StartsWith ("/reference:")){
					string lib = s.Substring (s.IndexOf (':')+1);

					Evaluator.LoadAssembly (lib);
					continue;
				}
			}
		}
#endif

#if !STATIC
		/// <summary>
		///   Loads the assembly
		/// </summary>
		/// <remarks>
		///   Loads the specified assembly and makes its types
		///   available to the evaluator.  This is equivalent
		///   to passing the -pkg: command line flag to the C#
		///   compiler on the command line.
		/// </remarks>
		static public void LoadAssembly (string assembly)
		{
			Evaluator.LoadAssembly (assembly);
		}

		static public void print (string obj)
		{
			Output.WriteLine (obj);
		}

		static public void print (string fmt, params object [] args)
		{
			Output.WriteLine (fmt, args);
		}
#endif
		
		/// <summary>
		///   Returns a list of available static methods. 
		/// </summary>
		static public string help {
			get {
				return "Static methods:\n" +
					"  Describe (object)       - Describes the object's type\n" +
					"  LoadPackage (package);  - Loads the given Package (like -pkg:FILE)\n" +
					"  LoadAssembly (assembly) - Loads the given assembly (like -r:ASSEMBLY)\n" +
					"  ShowVars ();            - Shows defined local variables.\n" +
					"  ShowUsing ();           - Show active using declarations.\n" +
					"  Prompt                  - The prompt used by the C# shell\n" +
					"  ContinuationPrompt      - The prompt for partial input\n" +
					"  Time(() -> { })         - Times the specified code\n" +
					"  print (obj)             - Shorthand for Console.WriteLine\n" +
					"  quit;                   - You'll never believe it - this quits the repl!\n" +
					"  help;                   - This help text\n";
			}
		}

		/// <summary>
		///   Indicates to the read-eval-print-loop that the interaction should be finished. 
		/// </summary>
		static public object quit {
			get {
				QuitRequested = true;

				// To avoid print null at the exit
				return typeof (Evaluator.NoValueSet);
			}
		}

#if !NET_2_1
		/// <summary>
		///   Describes an object or a type.
		/// </summary>
		/// <remarks>
		///   This method will show a textual representation
		///   of the object's type.  If the object is a
		///   System.Type it renders the type directly,
		///   otherwise it renders the type returned by
		///   invoking GetType on the object.
		/// </remarks>
		static public string Describe (object x)
		{
			if (x == null)
				return "<null>";

			var type = x as Type ?? x.GetType ();

			StringWriter sw = new StringWriter ();
			new Outline (type, sw, true, false, false).OutlineType ();
			return sw.ToString ();
		}
#endif
	}

	class HoistedEvaluatorVariable : HoistedVariable
	{
		public HoistedEvaluatorVariable (Field field)
			: base (null, field)
		{
		}

		public override void EmitSymbolInfo ()
		{
		}

		protected override FieldExpr GetFieldExpression (EmitContext ec)
		{
			return new FieldExpr (field, field.Location);
		}
	}

	/// <summary>
	///    A class used to assign values if the source expression is not void
	///
	///    Used by the interactive shell to allow it to call this code to set
	///    the return value for an invocation.
	/// </summary>
	class OptionalAssign : SimpleAssign {
		public OptionalAssign (Expression t, Expression s, Location loc)
			: base (t, s, loc)
		{
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			CloneContext cc = new CloneContext ();
			Expression clone = source.Clone (cc);

			//
			// A useful feature for the REPL: if we can resolve the expression
			// as a type, Describe the type;
			//
			if (Evaluator.DescribeTypeExpressions){
				var old_printer = Evaluator.SetPrinter (new StreamReportPrinter (TextWriter.Null));
				clone = clone.Resolve (ec);
				if (clone == null){
					clone = source.Clone (cc);
					clone = clone.Resolve (ec, ResolveFlags.Type);
					if (clone == null){
						Evaluator.SetPrinter (old_printer);
						clone = source.Clone (cc);
						clone = clone.Resolve (ec);
						return null;
					}
					
					Arguments args = new Arguments (1);
					args.Add (new Argument (new TypeOf ((TypeExpr) clone, Location)));
					source = new Invocation (new SimpleName ("Describe", Location), args).Resolve (ec);
				}
				Evaluator.SetPrinter (old_printer);
			} else {
				clone = clone.Resolve (ec);
				if (clone == null)
					return null;
			}
	
			// This means its really a statement.
			if (clone.Type == TypeManager.void_type || clone is DynamicInvocation || clone is Assign) {
				return clone;
			}

			return base.DoResolve (ec);
		}
	}

	public class Undo {
		List<KeyValuePair<TypeContainer, TypeContainer>> undo_types;
		
		public Undo ()
		{
			undo_types = new List<KeyValuePair<TypeContainer, TypeContainer>> ();
		}

		public void AddTypeContainer (TypeContainer current_container, TypeContainer tc)
		{
			if (current_container == tc){
				Console.Error.WriteLine ("Internal error: inserting container into itself");
				return;
			}

			if (undo_types == null)
				undo_types = new List<KeyValuePair<TypeContainer, TypeContainer>> ();

			undo_types.Add (new KeyValuePair<TypeContainer, TypeContainer> (current_container, tc));
		}

		public void ExecuteUndo ()
		{
			if (undo_types == null)
				return;

			foreach (var p in undo_types){
				TypeContainer current_container = p.Key;

				current_container.RemoveTypeContainer (p.Value);
			}
			undo_types = null;
		}
	}
	
}
