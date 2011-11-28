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
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedFieldAccessTreeNode : AnalyzerSearchTreeNode
	{
		private readonly bool showWrites; // true: show writes; false: show read access
		private readonly FieldDefinition analyzedField;
		private Lazy<Hashtable> foundMethods;
		private readonly object hashLock = new object();

		public AnalyzedFieldAccessTreeNode(FieldDefinition analyzedField, bool showWrites)
		{
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");

			this.analyzedField = analyzedField;
			this.showWrites = showWrites;
		}

		public override object Text
		{
			get { return showWrites ? "Assigned By" : "Read By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			foundMethods = new Lazy<Hashtable>(LazyThreadSafetyMode.ExecutionAndPublication);

			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedField, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct).OrderBy(n => n.Text)) {
				yield return child;
			}

			foundMethods = null;
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDefinition type)
		{
			string name = analyzedField.Name;

			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					if (CanBeReference(instr.OpCode.Code)) {
						FieldReference fr = instr.Operand as FieldReference;
						if (fr != null && fr.Name == name &&
							Helpers.IsReferencedBy(analyzedField.DeclaringType, fr.DeclaringType) &&
							fr.Resolve() == analyzedField) {
							found = true;
							break;
						}
					}
				}

				method.Body = null;

				if (found) {
					MethodDefinition codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDefinition;
					if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
						var node = new AnalyzedMethodTreeNode(codeLocation);
						node.Language = this.Language;
						yield return node;
					}
				}
			}
		}

		private bool CanBeReference(Code code)
		{
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

		private bool HasAlreadyBeenFound(MethodDefinition method)
		{
			Hashtable hashtable = foundMethods.Value;
			lock (hashLock) {
				if (hashtable.Contains(method)) {
					return true;
				} else {
					hashtable.Add(method, null);
					return false;
				}
			}
		}
	}
}
