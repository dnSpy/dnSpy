/*
    Copyright (C) 2017 HoLLy

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

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

			while (baseType != null) {
				//only typedef has a Events property
				if (baseType is TypeDef def) {
					foreach (EventDef eventDef in def.Events) {
						if (TypesHierarchyHelpers.IsBaseEvent(eventDef, analyzedEvent)) {
							MethodDef anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
							if (anyAccessor == null)
								continue;
							bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
							yield return new EventNode(eventDef, hidesParent) {Context = Context};
							yield break;
						}
					}
					baseType = def.BaseType;
				}
				else {
					//try to resolve the TypeRef
					//will be null if resolving failed
					baseType = baseType.Resolve();
				}
			}
		}

		public static bool CanShow(EventDef property) {
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor != null && accessor.IsVirtual && accessor.DeclaringType.BaseType != null;
		}
	}
}
