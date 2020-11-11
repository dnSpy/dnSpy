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
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventOverriddenNode : SearchNode {
		readonly EventDef analyzedEvent;
		readonly List<TypeDef> analyzedTypes;

		public EventOverriddenNode(EventDef analyzedEvent) {
			this.analyzedEvent = analyzedEvent ?? throw new ArgumentNullException(nameof(analyzedEvent));
			analyzedTypes = new List<TypeDef> { analyzedEvent.DeclaringType };
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.OverridesTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			AddTypeEquivalentTypes(Context.DocumentService, analyzedTypes[0], analyzedTypes);
			foreach (var declType in analyzedTypes) {
				var analyzedAccessor = GetVirtualAccessor(analyzedEvent.AddMethod) ?? GetVirtualAccessor(analyzedEvent.RemoveMethod) ?? GetVirtualAccessor(analyzedEvent.InvokeMethod);
				if (analyzedAccessor?.Overrides is IList<MethodOverride> overrides && overrides.Count > 0) {
					bool matched = false;
					foreach (var o in overrides) {
						if (o.MethodDeclaration.ResolveMethodDef() is MethodDef method && (method.IsVirtual || method.IsAbstract)) {
							if (method.DeclaringType.Events.FirstOrDefault(a => (object?)a.AddMethod == method || (object?)a.RemoveMethod == method || (object?)a.InvokeMethod == method) is EventDef eventDef) {
								matched = true;
								yield return new EventNode(eventDef) { Context = Context };
							}
						}
					}
					if (matched)
						yield break;
				}

				foreach (var eventDef in TypesHierarchyHelpers.FindBaseEvents(analyzedEvent, declType)) {
					var anyAccessor = GetVirtualAccessor(eventDef.AddMethod) ?? GetVirtualAccessor(eventDef.RemoveMethod) ?? GetVirtualAccessor(eventDef.InvokeMethod);
					if (anyAccessor is null || !(anyAccessor.IsVirtual || anyAccessor.IsAbstract))
						continue;
					yield return new EventNode(eventDef) { Context = Context };
					yield break;
				}
			}
		}

		public static bool CanShow(EventDef @event) =>
			(GetAccessor(@event.AddMethod) ?? GetAccessor(@event.RemoveMethod) ?? GetAccessor(@event.InvokeMethod)) is not null;

		static MethodDef? GetAccessor(MethodDef? accessor) {
			if (accessor is not null &&
				accessor.DeclaringType.BaseType is not null &&
				(accessor.IsVirtual || accessor.IsAbstract) && accessor.IsReuseSlot)
				return accessor;
			return null;
		}

		static MethodDef? GetVirtualAccessor(MethodDef? accessor) {
			if (accessor is not null && (accessor.IsVirtual || accessor.IsAbstract))
				return accessor;
			return null;
		}
	}
}
