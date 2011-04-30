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
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedEventOverridesTreeNode : AnalyzerTreeNode
	{
		private readonly EventDefinition analyzedEvent;
		private readonly ThreadingSupport threading;

		public AnalyzedEventOverridesTreeNode(EventDefinition analyzedEvent)
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");

			this.analyzedEvent = analyzedEvent;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return "Overridden By"; }
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

		private IEnumerable<SharpTreeNode> FetchChildren(CancellationToken ct)
		{
			return FindReferences(MainWindow.Instance.CurrentAssemblyList.GetAssemblies(), ct);
		}

		private IEnumerable<SharpTreeNode> FindReferences(IEnumerable<LoadedAssembly> assemblies, CancellationToken ct)
		{
			assemblies = assemblies.Where(asm => asm.AssemblyDefinition != null);

			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany((LoadedAssembly asm) => FindReferences(asm, ct));
		}

		private IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly asm, CancellationToken ct)
		{
			string asmName = asm.AssemblyDefinition.Name.Name;
			string name = analyzedEvent.Name;
			string declTypeName = analyzedEvent.DeclaringType.FullName;
			foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.AssemblyDefinition.MainModule.Types, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();

				if (!TypesHierarchyHelpers.IsBaseType(analyzedEvent.DeclaringType, type, resolveTypeArguments: false))
					continue;

				foreach (EventDefinition eventDef in type.Events) {
					ct.ThrowIfCancellationRequested();

					if (TypesHierarchyHelpers.IsBaseEvent(analyzedEvent, eventDef)) {
						MethodDefinition anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
						bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
						yield return new AnalyzedEventTreeNode(eventDef, hidesParent ? "(hides) " : "");
					}
				}
			}
		}

		public static bool CanShow(EventDefinition property)
		{
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
