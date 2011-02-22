// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Decompiler;
using Microsoft.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Tests
{
	public class TestRunner
	{
		public static void Main()
		{
			Test(@"..\..\Tests\DelegateConstruction.cs");
			
			Console.ReadKey();
		}
		
		
		
		static void Test(string fileName)
		{
			string code = File.ReadAllText(fileName);
			AssemblyDefinition assembly = Compile(code);
			AstBuilder decompiler = new AstBuilder(new DecompilerContext());
			decompiler.AddAssembly(assembly);
			StringWriter output = new StringWriter();
			decompiler.GenerateCode(new PlainTextOutput(output));
			StringWriter diff = new StringWriter();
			if (!Compare(code, output.ToString(), diff)) {
				throw new Exception("Test failure." + Environment.NewLine + diff.ToString());
			}
		}
		
		static bool Compare(string input1, string input2, StringWriter diff)
		{
			bool ok = true;
			int numberOfContinuousMistakes = 0;
			StringReader r1 = new StringReader(input1);
			StringReader r2 = new StringReader(input2);
			string line1, line2;
			while ((line1 = r1.ReadLine()) != null) {
				string trimmed = line1.Trim();
				if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal) || line1.StartsWith("using ", StringComparison.Ordinal)) {
					diff.WriteLine(" " + line1);
					continue;
				}
				line2 = r2.ReadLine();
				while (line2 != null && (line2.StartsWith("using ", StringComparison.Ordinal) || line2.Trim().Length == 0))
					line2 = r2.ReadLine();
				if (line2 == null) {
					ok = false;
					diff.WriteLine("-" + line1);
					continue;
				}
				if (line1 != line2) {
					ok = false;
					if (numberOfContinuousMistakes++ > 5)
						return false;
					diff.WriteLine("-" + line1);
					diff.WriteLine("+" + line2);
				} else {
					if (numberOfContinuousMistakes > 0)
						numberOfContinuousMistakes--;
					diff.WriteLine(" " + line1);
				}
			}
			while ((line2 = r2.ReadLine()) != null) {
				ok = false;
				diff.WriteLine("+" + line2);
			}
			return ok;
		}
		
		static AssemblyDefinition Compile(string code)
		{
			CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string> {{ "CompilerVersion", "v4.0" }});
			CompilerParameters options = new CompilerParameters();
			options.ReferencedAssemblies.Add("System.Core.dll");
			CompilerResults results = provider.CompileAssemblyFromSource(options, code);
			try {
				if (results.Errors.Count > 0) {
					StringBuilder b = new StringBuilder("Compiler error:");
					foreach (var error in results.Errors) {
						b.AppendLine(error.ToString());
					}
					throw new Exception(b.ToString());
				}
				return AssemblyDefinition.ReadAssembly(results.PathToAssembly);
			} finally {
				File.Delete(results.PathToAssembly);
				results.TempFiles.Delete();
			}
		}
	}
}
