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
	sealed class MethodOverriddenNode : SearchNode {
		readonly MethodDef analyzedMethod;

		public MethodOverriddenNode(MethodDef analyzedMethod) {
			if (analyzedMethod == null)
				throw new ArgumentNullException(nameof(analyzedMethod));

			this.analyzedMethod = analyzedMethod;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			//note: only goes up 1 level
			AnalyzerTreeNodeData newNode = null;
			try {
				//get base type (if any)
				if (analyzedMethod.DeclaringType.BaseType == null) {
					yield break;
				}
				ITypeDefOrRef baseType = analyzedMethod.DeclaringType.BaseType;

				//only typedef has a Methods property
				if (baseType is TypeDef def) {
					foreach (var method in def.Methods) {
						if (TypesHierarchyHelpers.IsBaseMethod(method, analyzedMethod)) {
							bool hidesParent = !method.IsVirtual ^ method.IsNewSlot;
							newNode = new MethodNode(method, hidesParent) {Context = Context};
							break;	//there can be only one
						}
					}
				}
			}
			catch (ResolveException) {
				//ignored
			}
			if (newNode != null)
				yield return newNode;
		}

		public static bool CanShow(MethodDef method) =>
			method.DeclaringType.BaseType != null &&
			method.IsVirtual &&
			!method.DeclaringType.IsInterface;
	}
}
