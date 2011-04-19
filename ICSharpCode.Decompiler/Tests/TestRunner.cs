// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiffLib;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Tests.Helpers;
using Microsoft.CSharp;
using Mono.Cecil;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture]
	public class TestRunner
	{
		[Test, Ignore("disambiguating overloads is not yet implemented")]
		public void CallOverloadedMethod()
		{
			TestFile(@"..\..\Tests\CallOverloadedMethod.cs");
		}
		
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
		
		[Test, Ignore("arg-Variables in catch clauses")]
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
		
		[Test]
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
		
		[Test, Ignore("Formatting differences in anonymous method create expressions")]
		public void QueryExpressions()
		{
			TestFile(@"..\..\Tests\QueryExpressions.cs");
		}
		
		[Test, Ignore("switch transform doesn't recreate the exact original switch")]
		public void Switch()
		{
			TestFile(@"..\..\Tests\Switch.cs");
		}
		
		[Test]
		public void UndocumentedExpressions()
		{
			TestFile(@"..\..\Tests\UndocumentedExpressions.cs");
		}
		
		[Test, Ignore("has incorrect casts to IntPtr")]
		public void UnsafeCode()
		{
			TestFile(@"..\..\Tests\UnsafeCode.cs");
		}
		
		[Test]
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
			AstBuilder decompiler = new AstBuilder(new DecompilerContext(assembly.MainModule));
			decompiler.AddAssembly(assembly);
			new Helpers.RemoveCompilerAttribute().Run(decompiler.CompilationUnit);
			StringWriter output = new StringWriter();
			decompiler.GenerateCode(new PlainTextOutput(output));
			CodeAssert.AreEqual(code, output.ToString());
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
