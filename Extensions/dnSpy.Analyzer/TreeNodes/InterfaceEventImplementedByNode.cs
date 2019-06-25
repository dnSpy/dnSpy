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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class InterfaceEventImplementedByNode : SearchNode {
		readonly EventDef analyzedEvent;
		readonly MethodDef? analyzedMethod;
		readonly AccessorKind accessorKind;

		enum AccessorKind {
			Adder,
			Remover,
			Invoker,
		}

		public InterfaceEventImplementedByNode(EventDef analyzedEvent) {
			this.analyzedEvent = analyzedEvent ?? throw new ArgumentNullException(nameof(analyzedEvent));
			if (!(this.analyzedEvent.AddMethod is null)) {
				analyzedMethod = this.analyzedEvent.AddMethod;
				accessorKind = AccessorKind.Adder;
			}
			else if (!(this.analyzedEvent.RemoveMethod is null)) {
				analyzedMethod = this.analyzedEvent.RemoveMethod;
				accessorKind = AccessorKind.Remover;
			}
			else {
				analyzedMethod = this.analyzedEvent.InvokeMethod;
				accessorKind = AccessorKind.Invoker;
			}
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.ImplementedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			if (analyzedMethod is null)
				yield break;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (analyzedMethod is null)
				yield break;
			if (type.IsInterface)
				yield break;
			var implementedInterfaceRef = InterfaceMethodImplementedByNode.GetInterface(type, analyzedMethod.DeclaringType);
			if (implementedInterfaceRef is null)
				yield break;

			foreach (EventDef ev in type.Events.Where(e => e.Name.EndsWith(analyzedEvent.Name))) {
				var accessor = GetAccessor(ev);
				// Don't include abstract accessors, they don't implement anything
				if (accessor is null || !accessor.IsVirtual || accessor.IsAbstract)
					continue;
				if (accessor.HasOverrides && accessor.Overrides.Any(m => CheckEquals(m.MethodDeclaration.ResolveMethodDef(), analyzedMethod))) {
					yield return new EventNode(ev) { Context = Context };
					yield break;
				}
			}

			foreach (EventDef ev in type.Events.Where(e => e.Name == analyzedEvent.Name)) {
				var accessor = GetAccessor(ev);
				// Don't include abstract accessors, they don't implement anything
				if (accessor is null || !accessor.IsVirtual || accessor.IsAbstract)
					continue;
				if (TypesHierarchyHelpers.MatchInterfaceMethod(accessor, analyzedMethod, implementedInterfaceRef)) {
					yield return new EventNode(ev) { Context = Context };
					yield break;
				}
			}
		}

		MethodDef? GetAccessor(EventDef ev) {
			switch (accessorKind) {
			case AccessorKind.Adder:	return ev.AddMethod;
			case AccessorKind.Remover:	return ev.RemoveMethod;
			case AccessorKind.Invoker:	return ev.InvokeMethod;
			default:					return null;
			}
		}

		public static bool CanShow(EventDef ev) => ev.DeclaringType.IsInterface && (ev.AddMethod ?? ev.RemoveMethod ?? ev.InvokeMethod) is MethodDef accessor && (accessor.IsVirtual || accessor.IsAbstract);
	}
}
