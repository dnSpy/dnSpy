using System;
using System.Collections.Generic;
using System.Threading;

using dnlib.DotNet;

using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventOverriddenNode : SearchNode {
		readonly EventDef analyzedEvent;

		public EventOverriddenNode(EventDef analyzedEvent) {
			if (analyzedEvent == null)
				throw new ArgumentNullException(nameof(analyzedEvent));

			this.analyzedEvent = analyzedEvent;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			//get base type (if any)
			if (analyzedEvent.DeclaringType.BaseType == null) {
				yield break;
			}
			ITypeDefOrRef baseType = analyzedEvent.DeclaringType.BaseType;

			//only typedef has a Events property
			if (baseType is TypeDef def) {
				foreach (EventDef eventDef in def.Events) {
					if (TypesHierarchyHelpers.IsBaseEvent(eventDef, analyzedEvent)) {
						MethodDef anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
						if (anyAccessor == null)
							continue;
						bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
						yield return new EventNode(eventDef, hidesParent) {Context = Context};
					}
				}
			}
		}

		public static bool CanShow(EventDef property) {
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor != null && accessor.IsVirtual && accessor.DeclaringType == null;
		}
	}
}
