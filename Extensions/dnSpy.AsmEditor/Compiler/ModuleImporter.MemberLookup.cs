/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Compiler {
	sealed partial class ModuleImporter {
		struct MemberLookup {
			readonly Dictionary<PropertyDef, PropertyDef> properties;
			readonly Dictionary<EventDef, EventDef> events;
			readonly Dictionary<MethodDef, MethodDef> methods;
			readonly Dictionary<FieldDef, FieldDef> fields;
			readonly Dictionary<IMethodDefOrRef, MethodAndOverride> methodOverrides;
			/*readonly*/ ImportSigComparer comparer;
			TypeDef targetType;

			struct MethodAndOverride {
				public MethodDef TargetMethod { get; }
				public IMethodDefOrRef MethodDeclaration { get; }

				public MethodAndOverride(MethodDef targetMethod, IMethodDefOrRef methodDeclaration) {
					TargetMethod = targetMethod;
					MethodDeclaration = methodDeclaration;
				}
			}

			public MemberLookup(ImportSigComparer comparer) {
				properties = new Dictionary<PropertyDef, PropertyDef>(new ImportPropertyEqualityComparer(comparer));
				events = new Dictionary<EventDef, EventDef>(new ImportEventEqualityComparer(comparer));
				methods = new Dictionary<MethodDef, MethodDef>(new ImportMethodEqualityComparer(comparer));
				fields = new Dictionary<FieldDef, FieldDef>(new ImportFieldEqualityComparer(comparer));
				methodOverrides = new Dictionary<IMethodDefOrRef, MethodAndOverride>(new ImportMethodEqualityComparer(comparer));
				this.comparer = comparer;
				targetType = null;
			}

			public void Initialize(TypeDef targetType) {
				this.targetType = targetType;
				properties.Clear();
				events.Clear();
				methods.Clear();
				fields.Clear();
				foreach (var p in targetType.Properties)
					properties[p] = p;
				foreach (var e in targetType.Events)
					events[e] = e;
				foreach (var m in targetType.Methods) {
					methods[m] = m;
					foreach (var o in m.Overrides)
						methodOverrides[o.MethodDeclaration] = new MethodAndOverride(m, o.MethodDeclaration);
				}
				foreach (var f in targetType.Fields)
					fields[f] = f;
			}

			MethodDef LookupOverride(MethodOverride o) {
				MethodAndOverride info;
				if (!methodOverrides.TryGetValue(o.MethodDeclaration, out info))
					return null;
				if (!comparer.Equals(info.MethodDeclaration.DeclaringType, o.MethodDeclaration.DeclaringType))
					return null;
				return info.TargetMethod;
			}

			public PropertyDef FindProperty(PropertyDef compiledProp) {
				PropertyDef targetProp;
				if (properties.TryGetValue(compiledProp, out targetProp))
					return targetProp;
				return FindPropertyOverride(compiledProp.GetMethod) ?? FindPropertyOverride(compiledProp.SetMethod);
			}

			PropertyDef FindPropertyOverride(MethodDef compiledMethod) {
				if (compiledMethod == null)
					return null;
				foreach (var o in compiledMethod.Overrides) {
					var targetMethod = LookupOverride(o);
					if (targetMethod != null)
						return targetMethod.DeclaringType.Properties.First(a => a.GetMethod == targetMethod || a.SetMethod == targetMethod);
				}
				return null;
			}

			public EventDef FindEvent(EventDef compiledEvent) {
				EventDef targetEvent;
				if (events.TryGetValue(compiledEvent, out targetEvent))
					return targetEvent;
				return FindEventOverride(compiledEvent.AddMethod) ?? FindEventOverride(compiledEvent.RemoveMethod) ?? FindEventOverride(compiledEvent.InvokeMethod);
			}

			EventDef FindEventOverride(MethodDef compiledMethod) {
				if (compiledMethod == null)
					return null;
				foreach (var o in compiledMethod.Overrides) {
					var targetMethod = LookupOverride(o);
					if (targetMethod != null)
						return targetMethod.DeclaringType.Events.First(a => a.AddMethod == targetMethod || a.RemoveMethod == targetMethod || a.InvokeMethod == targetMethod);
				}
				return null;
			}

			public MethodDef FindMethod(MethodDef compiledMethod) {
				MethodDef targetMethod;
				if (methods.TryGetValue(compiledMethod, out targetMethod))
					return targetMethod;
				foreach (var o in compiledMethod.Overrides) {
					targetMethod = LookupOverride(o);
					if (targetMethod != null)
						return targetMethod;
				}
				return null;
			}

			public FieldDef FindField(FieldDef compiledField) {
				FieldDef targetField;
				fields.TryGetValue(compiledField, out targetField);
				return targetField;
			}

			public void Remove(PropertyDef targetProp) {
				if (targetProp.Module != targetType.Module)
					throw new InvalidOperationException();
				bool b = properties.Remove(targetProp);
				Debug.Assert(b);
			}

			public void Remove(EventDef targetEvent) {
				if (targetEvent.Module != targetType.Module)
					throw new InvalidOperationException();
				bool b = events.Remove(targetEvent);
				Debug.Assert(b);
			}

			public void Remove(MethodDef targetMethod) {
				if (targetMethod.Module != targetType.Module)
					throw new InvalidOperationException();
				bool b = methods.Remove(targetMethod);
				Debug.Assert(b);
			}

			public void Remove(FieldDef targetField) {
				if (targetField.Module != targetType.Module)
					throw new InvalidOperationException();
				bool b = fields.Remove(targetField);
				Debug.Assert(b);
			}
		}
	}
}
