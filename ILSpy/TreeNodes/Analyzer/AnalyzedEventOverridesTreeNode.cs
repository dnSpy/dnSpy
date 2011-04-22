using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	class AnalyzedEventOverridesTreeNode : AnalyzerTreeNode
	{
		readonly EventDefinition analyzedEvent;
		readonly ThreadingSupport threading;

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
			get { return "Overriden By"; }
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
			return FindReferences(MainWindow.Instance.CurrentAssemblyList.GetAssemblies(), ct);
		}

		IEnumerable<SharpTreeNode> FindReferences(IEnumerable<LoadedAssembly> assemblies, CancellationToken ct)
		{
			assemblies = assemblies.Where(asm => asm.AssemblyDefinition != null);
			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany((LoadedAssembly asm) => FindReferences(asm, ct));
		}

		IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly asm, CancellationToken ct)
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

		public static bool CanShowAnalyzer(EventDefinition property)
		{
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
