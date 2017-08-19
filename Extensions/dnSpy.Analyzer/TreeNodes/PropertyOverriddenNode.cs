using System;
using System.Collections.Generic;
using System.Threading;

using dnlib.DotNet;

using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class PropertyOverriddenNode : SearchNode {
		readonly PropertyDef analyzedProperty;

		public PropertyOverriddenNode(PropertyDef analyzedProperty) {
			if (analyzedProperty == null)
				throw new ArgumentNullException(nameof(analyzedProperty));

			this.analyzedProperty = analyzedProperty;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			//get base type (if any)
			if (analyzedProperty.DeclaringType.BaseType == null) {
				yield break;
			}
			ITypeDefOrRef baseType = analyzedProperty.DeclaringType.BaseType;

			//only typedef has a Events property
			if (baseType is TypeDef def) {
				foreach (PropertyDef property in def.Properties) {
					if (TypesHierarchyHelpers.IsBaseProperty(property, analyzedProperty)) {
						MethodDef anyAccessor = property.GetMethod ?? property.SetMethod;
						if (anyAccessor == null)
							continue;
						bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
						yield return new PropertyNode(property, hidesParent) {Context = Context};
					}
				}
			}
		}

		public static bool CanShow(PropertyDef property) {
			var accessor = property.GetMethod ?? property.SetMethod;
			return accessor != null && accessor.IsVirtual && accessor.DeclaringType != null;
		}
	}
}
