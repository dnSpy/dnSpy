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
	class AnalyzedPropertyOverridesTreeNode : AnalyzerTreeNode
	{
		readonly PropertyDefinition analyzedProperty;
		readonly ThreadingSupport threading;

		public AnalyzedPropertyOverridesTreeNode(PropertyDefinition analyzedProperty)
		{
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");

			this.analyzedProperty = analyzedProperty;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return "Overrided By"; }
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
			return FindReferences(MainWindow.Instance.AssemblyList.GetAssemblies(), ct);
		}

		IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly[] assemblies, CancellationToken ct)
		{
			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany((LoadedAssembly asm) => FindReferences(asm, ct));
		}

		IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly asm, CancellationToken ct)
		{
			string asmName = asm.AssemblyDefinition.Name.Name;
			string name = analyzedProperty.Name;
			string declTypeName = analyzedProperty.DeclaringType.FullName;
			foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.AssemblyDefinition.MainModule.Types, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();

				if (!TypesHierarchyHelpers.IsBaseType(analyzedProperty.DeclaringType, type, resolveTypeArguments: false))
					continue;

				foreach (PropertyDefinition property in type.Properties) {
					ct.ThrowIfCancellationRequested();

					if (TypesHierarchyHelpers.IsBaseProperty(analyzedProperty, property)) {
						MethodDefinition anyAccessor = property.GetMethod ?? property.SetMethod;
						bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
						yield return new AnalyzedPropertyTreeNode(property, hidesParent ? "(hides) " : "");
					}
				}
			}
		}

		public static bool CanShowAnalyzer(PropertyDefinition property)
		{
			var accessor = property.GetMethod ?? property.SetMethod;
			return accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
