// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	class AnalyzedFieldAccessNode : AnalyzerTreeNode
	{
		readonly bool showWrites; // true: show writes; false: show read access
		readonly FieldDefinition analyzedField;
		readonly ThreadingSupport threading;

		public AnalyzedFieldAccessNode(FieldDefinition analyzedField, bool showWrites)
		{
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");

			this.analyzedField = analyzedField;
			this.showWrites = showWrites;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return showWrites ? "Assigned By" : "Read By"; }
		}

		public override object Icon
		{
			get { return Images.Search; }
		}

		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}

		protected override void OnCollapsing()
		{
			if (threading.IsRunning) {
				this.LazyLoading = true;
				threading.Cancel();
				this.Children.Clear();
			}
		}

		IEnumerable<SharpTreeNode> FetchChildren(CancellationToken ct)
		{
			var analyzer = new ScopedWhereUsedScopeAnalyzer<SharpTreeNode>(analyzedField, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<SharpTreeNode> FindReferencesInType(TypeDefinition type)
		{
			string name = analyzedField.Name;
			string declTypeName = analyzedField.DeclaringType.FullName;

			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					if (CanBeReference(instr.OpCode.Code)) {
						FieldReference fr = instr.Operand as FieldReference;
						if (fr != null && fr.Name == name && Helpers.IsReferencedBy(analyzedField.DeclaringType, fr.DeclaringType) && fr.Resolve() == analyzedField) {
							found = true;
							break;
						}
					}
				}
				if (found)
					yield return new AnalyzedMethodTreeNode(method);
			}
		}

		bool CanBeReference(Code code)
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
	}
}
