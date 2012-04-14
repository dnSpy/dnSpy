// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	/// <summary>
	/// Tests parser + output visitor by roundtripping code.
	/// Everything but whitespace must be preserved.
	/// </summary>
	public class RoundtripTest
	{
		public static void RunTest(CSharpFile file)
		{
			string code = file.Content.Text.Replace("\r\n", "\n");
			Debug.Assert(code.IndexOf('\r') < 0);
			if (code.Contains("#pragma"))
				return; // skip code with preprocessor directives
			if (code.Contains("enum VarianceModifier") || file.FileName.EndsWith("ecore.cs") || file.FileName.EndsWith("method.cs"))
				return; // skip enum with ; at end (see TypeDeclarationTests.EnumWithSemicolonAtEnd)
			if (file.FileName.EndsWith("KnownTypeReference.cs") || file.FileName.EndsWith("typemanager.cs") || file.FileName.EndsWith("GetAllBaseTypesTest.cs") || file.FileName.EndsWith("Tokens.cs") || file.FileName.EndsWith("OpCode.cs") || file.FileName.EndsWith("MainWindow.cs"))
				return; // skip due to optional , at end of array initializer (see ArrayCreateExpressionTests.ArrayInitializerWithCommaAtEnd)
			if (file.FileName.EndsWith("cs-parser.cs"))
				return; // skip due to completely messed up comment locations
			if (file.FileName.Contains("FormattingTests") || file.FileName.Contains("ContextAction") || file.FileName.Contains("CodeCompletion"))
				return; // skip due to AttributeSectionTests.AttributeWithEmptyParenthesis
			if (file.FileName.EndsWith("TypeSystemTests.TestCase.cs") || file.FileName.EndsWith("AssemblyInfo.cs"))
				return; // skip due to AttributeSectionTests.AssemblyAttributeBeforeNamespace
			if (file.FileName.EndsWith("dynamic.cs") || file.FileName.EndsWith("expression.cs"))
				return; // skip due to PreprocessorDirectiveTests.NestedInactiveIf
			if (file.FileName.EndsWith("property.cs"))
				return; // skip due to PreprocessorDirectiveTests.CommentOnEndOfIfDirective
			if (file.FileName.EndsWith("DefaultResolvedTypeDefinition.cs"))
				return; // skip due to MethodDeclarationTests.GenericMethodWithMultipleConstraints
			
			Roundtrip(file.Project.CreateParser(), file.FileName, code);
			// After trying unix-style newlines, also try windows-style newlines:
			Roundtrip(file.Project.CreateParser(), file.FileName, code.Replace("\n", "\r\n"));
		}
		
		public static void Roundtrip(CSharpParser parser, string fileName, string code)
		{
			// 1. Parse
			CompilationUnit cu = parser.Parse(code, fileName);
			if (parser.HasErrors)
				throw new InvalidOperationException("There were parse errors.");
			
			// 2. Output
			StringWriter w = new StringWriter();
			cu.AcceptVisitor(new CSharpOutputVisitor(w, FormattingOptionsFactory.CreateMono ()));
			string generatedCode = w.ToString().TrimEnd();
			
			// 3. Compare output with original (modulo whitespaces)
			int pos2 = 0;
			for (int pos1 = 0; pos1 < code.Length; pos1++) {
				if (!char.IsWhiteSpace(code[pos1])) {
					while (pos2 < generatedCode.Length && char.IsWhiteSpace(generatedCode[pos2]))
						pos2++;
					if (pos2 >= generatedCode.Length || code[pos1] != generatedCode[pos2]) {
						ReadOnlyDocument doc = new ReadOnlyDocument(code);
						File.WriteAllText(Path.Combine(Program.TempPath, "roundtrip-error.cs"), generatedCode);
						throw new InvalidOperationException("Mismatch at " + doc.GetLocation(pos1) + " of file " + fileName);
					}
					pos2++;
				}
			}
			if (pos2 != generatedCode.Length)
				throw new InvalidOperationException("Mismatch at end of file " + fileName);
			
			// 3b - validate that there are no lone \r
			if (generatedCode.Replace(w.NewLine, "\n").IndexOf('\r') >= 0)
				throw new InvalidOperationException(@"Got lone \r in " + fileName);
			
			// 4. Parse generated output
			CompilationUnit generatedCU;
			try {
				generatedCU = parser.Parse(generatedCode, fileName);
			} catch {
				File.WriteAllText(Path.Combine(Program.TempPath, "roundtrip-error.cs"), generatedCode, Encoding.Unicode);
				throw;
			}
			
			if (parser.HasErrors) {
				File.WriteAllText(Path.Combine(Program.TempPath, "roundtrip-error.cs"), generatedCode);
				throw new InvalidOperationException("There were parse errors in the roundtripped " + fileName);
			}
			
			// 5. Compare AST1 with AST2
			if (!cu.IsMatch(generatedCU))
				throw new InvalidOperationException("AST match failed for " + fileName + ".");
		}
	}
}
