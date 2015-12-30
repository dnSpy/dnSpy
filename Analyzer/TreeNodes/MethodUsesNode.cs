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
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Shows the methods that are used by this method.
	/// </summary>
	sealed class MethodUsesNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodUsesNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(dnSpy_Analyzer_Resources.UsesTreeNode, TextTokenType.Text);
		}

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foreach (var f in GetUsedFields().Distinct()) {
				yield return new FieldNode(f) { Context = Context };
			}
			foreach (var m in GetUsedMethods().Distinct()) {
				yield return new MethodNode(m) { Context = Context };
			}
		}

		IEnumerable<MethodDef> GetUsedMethods() {
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				IMethod mr = instr.Operand as IMethod;
				if (mr != null && !mr.IsField) {
					MethodDef def = DnlibExtensions.Resolve(mr);
					if (def != null)
						yield return def;
				}
			}
		}

		IEnumerable<FieldDef> GetUsedFields() {
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				IField fr = instr.Operand as IField;
				if (fr != null && !fr.IsMethod) {
					FieldDef def = DnlibExtensions.Resolve(fr);
					if (def != null)
						yield return def;
				}
			}
		}
	}
}
