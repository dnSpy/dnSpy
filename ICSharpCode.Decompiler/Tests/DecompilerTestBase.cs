using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Tests.Helpers;
using Microsoft.CSharp;
using Mono.Cecil;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	public abstract class DecompilerTestBase
	{
		protected static void ValidateFileRoundtrip(string samplesFileName)
		{
			var lines = File.ReadAllLines(Path.Combine(@"..\..\Tests", samplesFileName));
			var testCode = RemoveIgnorableLines(lines);
			var decompiledTestCode = RoundtripCode(testCode);
			CodeAssert.AreEqual(testCode, decompiledTestCode);
		}

		static string RemoveIgnorableLines(IEnumerable<string> lines)
		{
			return CodeSampleFileParser.ConcatLines(lines.Where(l => !CodeSampleFileParser.IsCommentOrBlank(l)));
		}

		/// <summary>
		/// Compiles and decompiles a source code.
		/// </summary>
		/// <param name="code">The source code to copile.</param>
		/// <returns>The decompilation result of compiled source code.</returns>
		static string RoundtripCode(string code)
		{
			DecompilerSettings settings = new DecompilerSettings();
			settings.FullyQualifyAmbiguousTypeNames = false;
			AssemblyDefinition assembly = Compile(code);
			AstBuilder decompiler = new AstBuilder(new DecompilerContext(assembly.MainModule) { Settings = settings });
			decompiler.AddAssembly(assembly);
			new Helpers.RemoveCompilerAttribute().Run(decompiler.CompilationUnit);
			StringWriter output = new StringWriter();
			decompiler.GenerateCode(new PlainTextOutput(output));
			return output.ToString();
		}

		static AssemblyDefinition Compile(string code)
		{
			CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			CompilerParameters options = new CompilerParameters();
			options.ReferencedAssemblies.Add("System.Core.dll");
			CompilerResults results = provider.CompileAssemblyFromSource(options, code);
			try
			{
				if (results.Errors.Count > 0)
				{
					StringBuilder b = new StringBuilder("Compiler error:");
					foreach (var error in results.Errors)
					{
						b.AppendLine(error.ToString());
					}
					throw new Exception(b.ToString());
				}
				return AssemblyDefinition.ReadAssembly(results.PathToAssembly);
			}
			finally
			{
				File.Delete(results.PathToAssembly);
				results.TempFiles.Delete();
			}
		}
	}
}
