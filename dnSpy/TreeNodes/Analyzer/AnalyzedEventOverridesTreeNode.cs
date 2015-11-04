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
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	internal sealed class AnalyzedEventOverridesTreeNode : AnalyzerSearchTreeNode {
		private readonly EventDef analyzedEvent;

		public AnalyzedEventOverridesTreeNode(EventDef analyzedEvent) {
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");

			this.analyzedEvent = analyzedEvent;
		}

		protected override void Write(ITextOutput output, Language language) {
			output.Write("Overridden By", TextTokenType.Text);
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedEvent, FindReferencesInType);
			return analyzer.PerformAnalysis(ct).OrderBy(n => n.ToString(Language));
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDef type) {
			if (!TypesHierarchyHelpers.IsBaseType(analyzedEvent.DeclaringType, type, resolveTypeArguments: false))
				yield break;

			foreach (EventDef eventDef in type.Events) {
				if (TypesHierarchyHelpers.IsBaseEvent(analyzedEvent, eventDef)) {
					MethodDef anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
					if (anyAccessor == null)
						continue;
					bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
					var node = new AnalyzedEventTreeNode(eventDef, hidesParent);
					node.Language = this.Language;
					yield return node;
				}
			}
		}

		public static bool CanShow(EventDef property) {
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor != null && accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
