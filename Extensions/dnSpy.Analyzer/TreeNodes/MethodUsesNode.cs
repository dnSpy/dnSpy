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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Shows the methods that are used by this method.
	/// </summary>
	sealed class MethodUsesNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodUsesNode(MethodDef analyzedMethod) {
			this.analyzedMethod = analyzedMethod ?? throw new ArgumentNullException(nameof(analyzedMethod));
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.UsesTreeNode);

		struct DefRef<T> where T : IDnlibDef {
			public readonly T Def;
			public readonly SourceRef SourceRef;
			public DefRef(T def, SourceRef sourceRef) {
				Def = def;
				SourceRef = sourceRef;
			}
		}

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foreach (var f in GetUsedFields().Distinct()) {
				yield return new FieldNode(f.Def) { Context = Context, SourceRef = f.SourceRef };
			}
			foreach (var m in GetUsedMethods().Distinct()) {
				yield return new MethodNode(m.Def) { Context = Context, SourceRef = m.SourceRef };
			}
		}

		IEnumerable<DefRef<MethodDef>> GetUsedMethods() {
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				IMethod mr = instr.Operand as IMethod;
				if (mr != null && !mr.IsField) {
					MethodDef def = mr.ResolveMethodDef();
					if (def != null)
						yield return new DefRef<MethodDef>(def, new SourceRef(analyzedMethod, instr.Offset, instr.Operand as IMDTokenProvider));
				}
			}
		}

		IEnumerable<DefRef<FieldDef>> GetUsedFields() {
			foreach (Instruction instr in analyzedMethod.Body.Instructions) {
				IField fr = instr.Operand as IField;
				if (fr != null && !fr.IsMethod) {
					FieldDef def = fr.ResolveFieldDef();
					if (def != null)
						yield return new DefRef<FieldDef>(def, new SourceRef(analyzedMethod, instr.Offset, instr.Operand as IMDTokenProvider));
				}
			}
		}
	}
}
