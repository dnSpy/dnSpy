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
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Shows the methods that are used by this method.
	/// </summary>
	internal sealed class AnalyzedMethodUsesTreeNode : AnalyzerSearchTreeNode
	{
		private readonly MethodDefinition analyzedMethod;

		public AnalyzedMethodUsesTreeNode(MethodDefinition analyzedMethod)
		{
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
		}

		public override object Text
		{
			get { return "Uses"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			foreach (var f in GetUsedFields().Distinct()) {
				var node = new AnalyzedFieldTreeNode(f);
				node.Language = this.Language;
				yield return node;
			}
			foreach (var m in GetUsedMethods().Distinct()) {
				var node = new AnalyzedMethodTreeNode(m);
				node.Language = this.Language;
				yield return node;
			}
		}

		private IEnumerable<MethodDefinition> GetUsedMethods()
		{
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				MethodReference mr = instr.Operand as MethodReference;
				if (mr != null) {
					MethodDefinition def = mr.Resolve();
					if (def != null)
						yield return def;
				}
			}
		}

		private IEnumerable<FieldDefinition> GetUsedFields()
		{
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				FieldReference fr = instr.Operand as FieldReference;
				if (fr != null) {
					FieldDefinition def = fr.Resolve();
					if (def != null)
						yield return def;
				}
			}
		}
	}
}
