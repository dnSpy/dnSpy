// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	#if DEBUG
	/// <summary>
	/// Represents the ILAst "language" used for debugging purposes.
	/// </summary>
	sealed class ILAstLanguage : Language
	{
		string name;
		bool inlineVariables = true;
		ILAstOptimizationStep? abortBeforeStep;
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			if (!method.HasBody) {
				return;
			}
			
			ILAstBuilder astBuilder = new ILAstBuilder();
			ILBlock ilMethod = new ILBlock();
			DecompilerContext context = new DecompilerContext(method.Module) { CurrentType = method.DeclaringType, CurrentMethod = method };
			ilMethod.Body = astBuilder.Build(method, inlineVariables, context);
			
			if (abortBeforeStep != null) {
				new ILAstOptimizer().Optimize(context, ilMethod, abortBeforeStep.Value);
			}
			
			if (context.CurrentMethodIsAsync)
				output.WriteLine("async/await");
			
			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v != null && !v.IsParameter).Distinct();
			foreach (ILVariable v in allVariables) {
				output.WriteDefinition(v.Name, v);
				if (v.Type != null) {
					output.Write(" : ");
					if (v.IsPinned)
						output.Write("pinned ");
					v.Type.WriteTo(output, ILNameSyntax.ShortTypeName);
				}
				if (v.IsGenerated) {
					output.Write(" [generated]");
				}
				output.WriteLine();
			}
			output.WriteLine();
			
			foreach (ILNode node in ilMethod.Body) {
				node.WriteTo(output);
				output.WriteLine();
			}
		}
		
		internal static IEnumerable<ILAstLanguage> GetDebugLanguages()
		{
			yield return new ILAstLanguage { name = "ILAst (unoptimized)", inlineVariables = false };
			string nextName = "ILAst (variable splitting)";
			foreach (ILAstOptimizationStep step in Enum.GetValues(typeof(ILAstOptimizationStep))) {
				yield return new ILAstLanguage { name = nextName, abortBeforeStep = step };
				nextName = "ILAst (after " + step + ")";
				
			}
		}
		
		public override string FileExtension {
			get {
				return ".il";
			}
		}
		
		public override string TypeToString(TypeReference t, bool includeNamespace, ICustomAttributeProvider attributeProvider = null)
		{
			PlainTextOutput output = new PlainTextOutput();
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
			return output.ToString();
		}
	}
	#endif
}
