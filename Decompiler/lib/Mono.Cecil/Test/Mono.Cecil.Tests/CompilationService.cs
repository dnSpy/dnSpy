using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	struct CompilationResult {
		internal DateTime source_write_time;
		internal string result_file;

		public CompilationResult (DateTime write_time, string result_file)
		{
			this.source_write_time = write_time;
			this.result_file = result_file;
		}
	}

	abstract class CompilationService {

		Dictionary<string, CompilationResult> files = new Dictionary<string, CompilationResult> ();

		bool TryGetResult (string name, out string file_result)
		{
			file_result = null;
			CompilationResult result;
			if (!files.TryGetValue (name, out result))
				return false;

			if (result.source_write_time != File.GetLastWriteTime (name))
				return false;

			file_result = result.result_file;
			return true;
		}

		public string Compile (string name)
		{
			string result_file;
			if (TryGetResult (name, out result_file))
				return result_file;

			result_file = CompileFile (name);
			RegisterFile (name, result_file);
			return result_file;
		}

		void RegisterFile (string name, string result_file)
		{
			files [name] = new CompilationResult (File.GetLastWriteTime (name), result_file);
		}

		protected abstract string CompileFile (string name);

		public static string CompileResource (string name)
		{
			var extension = Path.GetExtension (name);
			if (extension == ".il")
				return IlasmCompilationService.Instance.Compile (name);

			if (extension == ".cs" || extension == ".vb")
				return CodeDomCompilationService.Instance.Compile (name);

			throw new NotSupportedException (extension);
		}

		protected static string GetCompiledFilePath (string file_name)
		{
			var tmp_cecil = Path.Combine (Path.GetTempPath (), "cecil");
			if (!Directory.Exists (tmp_cecil))
				Directory.CreateDirectory (tmp_cecil);

			return Path.Combine (tmp_cecil, Path.GetFileName (file_name) + ".dll");
		}

		static bool OnMono { get { return typeof (object).Assembly.GetType ("Mono.Runtime") != null; } }

		public static void Verify (string name)
		{
			var output = OnMono ? ShellService.PEDump (name) : ShellService.PEVerify (name);
			if (output.ExitCode != 0)
				Assert.Fail (output.ToString ());
		}
	}

	class IlasmCompilationService : CompilationService {

		public static readonly IlasmCompilationService Instance = new IlasmCompilationService ();

		protected override string CompileFile (string name)
		{
			string file = GetCompiledFilePath (name);

			var output = ShellService.ILAsm (name, file);

			AssertAssemblerResult (output);

			return file;
		}

		static void AssertAssemblerResult (ShellService.ProcessOutput output)
		{
			if (output.ExitCode != 0)
				Assert.Fail (output.ToString ());
		}
	}

	class CodeDomCompilationService : CompilationService {

		public static readonly CodeDomCompilationService Instance = new CodeDomCompilationService ();

		protected override string CompileFile (string name)
		{
			string file = GetCompiledFilePath (name);

			using (var provider = GetProvider (name)) {
				var parameters = GetDefaultParameters (name);
				parameters.IncludeDebugInformation = false;
				parameters.GenerateExecutable = false;
				parameters.OutputAssembly = file;

				var results = provider.CompileAssemblyFromFile (parameters, name);
				AssertCompilerResults (results);
			}

			return file;
		}

		static void AssertCompilerResults (CompilerResults results)
		{
			Assert.IsFalse (results.Errors.HasErrors, GetErrorMessage (results));
		}

		static string GetErrorMessage (CompilerResults results)
		{
			if (!results.Errors.HasErrors)
				return string.Empty;

			var builder = new StringBuilder ();
			foreach (CompilerError error in results.Errors)
				builder.AppendLine (error.ToString ());
			return builder.ToString ();
		}

		static CompilerParameters GetDefaultParameters (string name)
		{
			return GetCompilerInfo (name).CreateDefaultCompilerParameters ();
		}

		static CodeDomProvider GetProvider (string name)
		{
			return GetCompilerInfo (name).CreateProvider ();
		}

		static CompilerInfo GetCompilerInfo (string name)
		{
			return CodeDomProvider.GetCompilerInfo (
				CodeDomProvider.GetLanguageFromExtension (Path.GetExtension (name)));
		}
	}

	class ShellService {

		public class ProcessOutput {

			public int ExitCode;
			public string StdOut;
			public string StdErr;

			public ProcessOutput (int exitCode, string stdout, string stderr)
			{
				ExitCode = exitCode;
				StdOut = stdout;
				StdErr = stderr;
			}

			public override string ToString ()
			{
				return StdOut + StdErr;
			}
		}

		static ProcessOutput RunProcess (string target, params string [] arguments)
		{
			var stdout = new StringWriter ();
			var stderr = new StringWriter ();

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = target,
					Arguments = string.Join (" ", arguments),
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
				},
			};

			process.Start ();

			process.OutputDataReceived += (_, args) => stdout.Write (args.Data);
			process.ErrorDataReceived += (_, args) => stderr.Write (args.Data);

			process.BeginOutputReadLine ();
			process.BeginErrorReadLine ();

			process.WaitForExit ();

			return new ProcessOutput (process.ExitCode, stdout.ToString (), stderr.ToString ());
		}

		public static ProcessOutput ILAsm (string source, string output)
		{
			return RunProcess ("ilasm", "/nologo", "/dll", "/out:" + Quote (output), Quote (source));
		}

		static string Quote (string file)
		{
			return "\"" + file + "\"";
		}

		public static ProcessOutput PEVerify (string source)
		{
			return RunProcess ("peverify", "/nologo", Quote (source));
		}

		public static ProcessOutput PEDump (string source)
		{
			return RunProcess ("pedump", "--verify code,metadata", Quote (source));
		}
	}
}
