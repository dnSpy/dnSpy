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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.NRefactory;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class FieldAccessNode : SearchNode {
		readonly bool showWrites; // true: show writes; false: show read access
		readonly FieldDef analyzedField;
		Lazy<Hashtable> foundMethods;
		readonly object hashLock = new object();

		public FieldAccessNode(FieldDef analyzedField, bool showWrites) {
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");

			this.analyzedField = analyzedField;
			this.showWrites = showWrites;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(showWrites ? "Assigned By" : "Read By", TextTokenType.Text);
		}

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foundMethods = new Lazy<Hashtable>(LazyThreadSafetyMode.ExecutionAndPublication);

			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.FileManager, analyzedField, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}

			foundMethods = null;
		}

		IEnumerable<IAnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			foreach (MethodDef method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					if (CanBeReference(instr.OpCode.Code)) {
						IField fr = instr.Operand as IField;
						if (fr != null && new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(fr, analyzedField) &&
							Helpers.IsReferencedBy(analyzedField.DeclaringType, fr.DeclaringType)) {
							found = true;
							break;
						}
					}
				}

				Helpers.FreeMethodBody(method);

				if (found) {
					MethodDef codeLocation = GetOriginalCodeLocation(method) as MethodDef;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						yield return new MethodNode(codeLocation) { Context = Context };
					}
				}
			}
		}

		bool CanBeReference(Code code) {
			switch (code) {
			case Code.Ldfld:
			case Code.Ldsfld:
				return !showWrites;
			case Code.Stfld:
			case Code.Stsfld:
				return showWrites;
			case Code.Ldflda:
			case Code.Ldsflda:
				return true; // always show address-loading
			default:
				return false;
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) {
			Hashtable hashtable = foundMethods.Value;
			lock (hashLock) {
				if (hashtable.Contains(method)) {
					return true;
				}
				else {
					hashtable.Add(method, null);
					return false;
				}
			}
		}
	}
}
