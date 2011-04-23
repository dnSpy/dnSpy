using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Searches for overrides of the analyzed method.
	/// </summary>
	class AnalyzerMethodOverridesTreeNode : AnalyzerTreeNode
	{
		readonly MethodDefinition analyzedMethod;
		readonly ThreadingSupport threading;

		public AnalyzerMethodOverridesTreeNode(MethodDefinition analyzedMethod)
		{
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
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
			if (threading.IsRunning)
			{
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
			string name = analyzedMethod.Name;
			string declTypeName = analyzedMethod.DeclaringType.FullName;
			foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.AssemblyDefinition.MainModule.Types, t => t.NestedTypes))
			{
				ct.ThrowIfCancellationRequested();
				SharpTreeNode newNode = null;
				try {
					if (!TypesHierarchyHelpers.IsBaseType(analyzedMethod.DeclaringType, type, resolveTypeArguments: false))
						continue;

					foreach (MethodDefinition method in type.Methods) {
						ct.ThrowIfCancellationRequested();

						if (TypesHierarchyHelpers.IsBaseMethod(analyzedMethod, method)) {
							bool hidesParent = !method.IsVirtual ^ method.IsNewSlot;
							newNode = new AnalyzedMethodTreeNode(method, hidesParent ? "(hides) " : "");
						}
					}
				}
				catch (ReferenceResolvingException) {
					// ignore this type definition. maybe add a notification about such cases.
				}
				if (newNode != null)
					yield return newNode;
			}
		}

		public static bool CanShowAnalyzer(MethodDefinition method)
		{
			return method.IsVirtual && !method.IsFinal && !method.DeclaringType.IsSealed && !method.DeclaringType.IsInterface;	// interfaces are temporarly disabled
		}
	}
}
