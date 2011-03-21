// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.Decompiler.Ast;
using Microsoft.CSharp;
using Mono.Cecil;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture]
	public class TestRunner
	{
		[Test, Ignore("unncessary primitive casts")]
		public void CheckedUnchecked()
		{
			TestFile(@"..\..\Tests\CheckedUnchecked.cs");
		}
		
		[Test, Ignore("Missing cast on null")]
		public void DelegateConstruction()
		{
			TestFile(@"..\..\Tests\DelegateConstruction.cs");
		}
		
		[Test, Ignore("bug with variable-less catch")]
		public void ExceptionHandling()
		{
			TestFile(@"..\..\Tests\ExceptionHandling.cs");
		}
		
		[Test]
		public void Generics()
		{
			TestFile(@"..\..\Tests\Generics.cs");
		}
		
		[Test]
		public void IncrementDecrement()
		{
			TestFile(@"..\..\Tests\IncrementDecrement.cs");
		}
		
		[Test, Ignore("Formatting issues (array initializers not on single line)")]
		public void InitializerTests()
		{
			TestFile(@"..\..\Tests\InitializerTests.cs");
		}
		
		[Test, Ignore("ForEachOverArray not supported")]
		public void Loops()
		{
			TestFile(@"..\..\Tests\Loops.cs");
		}
		
		[Test]
		public void MultidimensionalArray()
		{
			TestFile(@"..\..\Tests\MultidimensionalArray.cs");
		}
		
		[Test]
		public void PropertiesAndEvents()
		{
			TestFile(@"..\..\Tests\PropertiesAndEvents.cs");
		}
		
		[Test, Ignore]
		public void Switch()
		{
			TestFile(@"..\..\Tests\Switch.cs");
		}
		
		[Test, Ignore("has incorrect casts to IntPtr")]
		public void UnsafeCode()
		{
			TestFile(@"..\..\Tests\UnsafeCode.cs");
		}
		
		[Test, Ignore("IncrementArrayLocation not yet supported")]
		public void ValueTypes()
		{
			TestFile(@"..\..\Tests\ValueTypes.cs");
		}
		
		[Test, Ignore("Redundant yield break; not removed")]
		public void YieldReturn()
		{
			TestFile(@"..\..\Tests\YieldReturn.cs");
		}
		
		static void TestFile(string fileName)
		{
			string code = File.ReadAllText(fileName);
			AssemblyDefinition assembly = Compile(code);
			AstBuilder decompiler = new AstBuilder(new DecompilerContext());
			decompiler.AddAssembly(assembly);
			decompiler.Transform(new Helpers.RemoveCompilerAttribute());
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
				if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal) | trimmed.StartsWith("#", StringComparison.Ordinal)) {
					diff.WriteLine(" " + line1);
					continue;
				}
				line2 = r2.ReadLine();
				while (line2 != null && string.IsNullOrWhiteSpace(line2))
					line2 = r2.ReadLine();
				if (line2 == null) {
					ok = false;
					diff.WriteLine("-" + line1);
					continue;
				}
				if (line1.Trim() != line2.Trim()) {
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
			CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			CompilerParameters options = new CompilerParameters();
			options.CompilerOptions = "/unsafe";
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
