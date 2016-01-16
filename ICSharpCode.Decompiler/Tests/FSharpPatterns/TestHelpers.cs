using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.FSharpPatterns
{
	public class TestHelpers
	{
		public static string FuzzyReadResource(string resourceName)
		{
			var asm = Assembly.GetExecutingAssembly();
			var allResources = asm.GetManifestResourceNames();
			var fullResourceName = allResources.Single(r => r.ToLowerInvariant().EndsWith(resourceName.ToLowerInvariant()));
			return new StreamReader(asm.GetManifestResourceStream(fullResourceName)).ReadToEnd();
		}

		// see https://fsharp.github.io/FSharp.Compiler.Service/compiler.html
		public static string CompileFsToAssembly(string source, bool optimize)
		{
			var tmp = Path.GetTempFileName();
			File.Delete(tmp);
			var sourceFile = Path.ChangeExtension(tmp, ".fs");
			File.WriteAllText(sourceFile, source);
			var asmFile = Path.ChangeExtension(sourceFile, ".dll");
			var sscs = new Microsoft.FSharp.Compiler.SimpleSourceCodeServices.SimpleSourceCodeServices();
			var result = sscs.Compile(new[] { "fsc.exe", "--debug:full", $"--optimize{(optimize ? "+" : "-")}", "--target:library", "-o", asmFile, sourceFile });
			File.Delete(sourceFile);
			Assert.AreEqual(0, result.Item1.Length);
			Assert.AreEqual(0, result.Item2);
			Assert.True(File.Exists(asmFile), "Assembly File does not exist");
			return asmFile;
		}

		static Lazy<string> ilasm = new Lazy<string>(() => ToolLocator.FindTool("ilasm.exe"));
		static Lazy<string> ildasm = new Lazy<string>(() => ToolLocator.FindTool("ildasm.exe"));

		public static string CompileIL(string source)
		{
			if (ilasm.Value == null)
				Assert.NotNull(ilasm.Value, "Could not find ILASM.exe");
			var tmp = Path.GetTempFileName();
			File.Delete(tmp);
			var sourceFile = Path.ChangeExtension(tmp, ".il");
			File.WriteAllText(sourceFile, source);
			var asmFile = Path.ChangeExtension(sourceFile, ".dll");

			var args = $"{sourceFile} /dll /debug /output:{asmFile}";
			using (var proc = Process.Start(new ProcessStartInfo(ilasm.Value, args) { UseShellExecute = false,  }))
			{
				proc.WaitForExit();
				Assert.AreEqual(0, proc.ExitCode);
			}

			File.Delete(sourceFile);
			Assert.True(File.Exists(asmFile), "Assembly File does not exist");
			return asmFile;
		}

		public static void RunIL(string ilCode, string expectedCSharpCode)
		{
			var asmFilePath = CompileIL(ilCode);
			CompareAssemblyAgainstCSharp(expectedCSharpCode, asmFilePath);
		}

		public static void RunFSharp(string fsharpCode, string expectedCSharpCode, bool optimize)
		{
			var asmFilePath = CompileFsToAssembly(fsharpCode, optimize);
			CompareAssemblyAgainstCSharp(expectedCSharpCode, asmFilePath);
		}

		private static void CompareAssemblyAgainstCSharp(string expectedCSharpCode, string asmFilePath)
		{
			var module = ModuleDefinition.ReadModule(asmFilePath);
			try
			{
				try { module.ReadSymbols(); } catch { }
				AstBuilder decompiler = new AstBuilder(new DecompilerContext(module));
				decompiler.AddAssembly(module);
				new Helpers.RemoveCompilerAttribute().Run(decompiler.SyntaxTree);
				StringWriter output = new StringWriter();

				// the F# assembly contains a namespace `<StartupCode$tmp6D55>` where the part after tmp is randomly generated.
				// remove this from the ast to simplify the diff
				var startupCodeNode = decompiler.SyntaxTree.Children.SingleOrDefault(d => (d as NamespaceDeclaration)?.Name?.StartsWith("<StartupCode$") ?? false);
				startupCodeNode?.Remove();

				decompiler.GenerateCode(new PlainTextOutput(output));
				var fullCSharpCode = output.ToString();

				CodeAssert.AreEqual(expectedCSharpCode, output.ToString());
			}
			finally
			{
				File.Delete(asmFilePath);
				File.Delete(Path.ChangeExtension(asmFilePath, ".pdb"));
			}
		}
	}
}
