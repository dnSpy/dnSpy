using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using dnlib.DotNet;

using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Searches methods that override by the analyzed method.
	/// </summary>
	sealed class MethodOverriddenByNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodOverriddenByNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException(nameof(analyzedMethod));

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverriddenByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			AnalyzerTreeNodeData newNode = null;
			try {
				if (!TypesHierarchyHelpers.IsBaseType(analyzedMethod.DeclaringType, type, resolveTypeArguments: false))
					yield break;

				foreach (MethodDef method in type.Methods) {
					if (TypesHierarchyHelpers.IsBaseMethod(analyzedMethod, method)) {
						bool hidesParent = !method.IsVirtual ^ method.IsNewSlot;
						newNode = new MethodNode(method, hidesParent) { Context = Context };
					}
				}
			}
			catch (ResolveException) {
				// ignore this type definition. maybe add a notification about such cases.
			}

			if (newNode != null)
				yield return newNode;
		}

		public static bool CanShow(MethodDef method) =>
			method.IsVirtual &&
			!method.IsFinal &&
			!method.DeclaringType.IsSealed &&
			!method.DeclaringType.IsInterface;  // interface methods are definitions not implementations - cannot be overridden
	}
}
