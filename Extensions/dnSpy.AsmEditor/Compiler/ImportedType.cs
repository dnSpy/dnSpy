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

using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.AsmEditor.Event;
using dnSpy.AsmEditor.Field;
using dnSpy.AsmEditor.Method;
using dnSpy.AsmEditor.Property;
using dnSpy.AsmEditor.Types;
using Emit = dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.Compiler {
	abstract class ImportedType {
		/// <summary>
		/// New or existing type in target module
		/// </summary>
		public TypeDef TargetType { get; protected set; }
	}

	/// <summary>
	/// This is a new type that got imported into the target module
	/// </summary>
	sealed class NewImportedType : ImportedType {
		public NewImportedType(TypeDef targetType) {
			TargetType = targetType;
		}
	}

	struct EditedProperty {
		public PropertyDef OriginalProperty { get; }
		public PropertyDefOptions PropertyDefOptions { get; }
		public EditedProperty(PropertyDef originalProperty, PropertyDefOptions propertyDefOptions) {
			OriginalProperty = originalProperty;
			PropertyDefOptions = propertyDefOptions;
		}
	}

	struct EditedEvent {
		public EventDef OriginalEvent { get; }
		public EventDefOptions EventDefOptions { get; }
		public EditedEvent(EventDef originalEvent, EventDefOptions eventDefOptions) {
			OriginalEvent = originalEvent;
			EventDefOptions = eventDefOptions;
		}
	}

	struct EditedMethod {
		public MethodDef OriginalMethod { get; }
		public Emit.MethodBody NewBody { get; }
		public MethodDefOptions MethodDefOptions { get; }

		public EditedMethod(MethodDef originalMethod, Emit.MethodBody newBody, MethodDefOptions methodDefOptions) {
			OriginalMethod = originalMethod;
			NewBody = newBody;
			MethodDefOptions = methodDefOptions;
		}
	}

	struct EditedField {
		public FieldDef OriginalField { get; }
		public FieldDefOptions FieldDefOptions { get; }
		public EditedField(FieldDef originalField, FieldDefOptions fieldDefOptions) {
			OriginalField = originalField;
			FieldDefOptions = fieldDefOptions;
		}
	}

	enum MergeKind {
		Rename,
		Merge,
		Edit,
	}

	/// <summary>
	/// This is a type that gets updated with more members. The user or the compiler added
	/// more members that must be included in the existing type
	/// </summary>
	sealed class MergedImportedType : ImportedType {
		internal MergeKind MergeKind { get; }

		public bool IsEmpty =>
			TargetType.IsGlobalModuleType &&
			NewOrExistingNestedTypes.Count == 0 &&
			NewProperties.Count == 0 &&
			NewEvents.Count == 0 &&
			NewMethods.Count == 0 &&
			NewFields.Count == 0 &&
			EditedProperties.Count == 0 &&
			EditedEvents.Count == 0 &&
			EditedMethods.Count == 0 &&
			EditedFields.Count == 0 &&
			DeletedNestedTypes.Count == 0 &&
			DeletedProperties.Count == 0 &&
			DeletedEvents.Count == 0 &&
			DeletedMethods.Count == 0 &&
			DeletedFields.Count == 0;

		/// <summary>
		/// New type properties
		/// </summary>
		public TypeDefOptions NewTypeDefOptions { get; }

		/// <summary>
		/// New or existing nested types that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<ImportedType> NewOrExistingNestedTypes { get; } = new List<ImportedType>();

		/// <summary>
		/// New properties that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<PropertyDef> NewProperties { get; } = new List<PropertyDef>();

		/// <summary>
		/// New events that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<EventDef> NewEvents { get; } = new List<EventDef>();

		/// <summary>
		/// New methods that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<MethodDef> NewMethods { get; } = new List<MethodDef>();

		/// <summary>
		/// New fields that must be added to <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<FieldDef> NewFields { get; } = new List<FieldDef>();

		/// <summary>
		/// Edited properties
		/// </summary>
		public List<EditedProperty> EditedProperties { get; } = new List<EditedProperty>();

		/// <summary>
		/// Edited events
		/// </summary>
		public List<EditedEvent> EditedEvents { get; } = new List<EditedEvent>();

		/// <summary>
		/// Edited methods
		/// </summary>
		public List<EditedMethod> EditedMethods { get; } = new List<EditedMethod>();

		/// <summary>
		/// Edited fields
		/// </summary>
		public List<EditedField> EditedFields { get; } = new List<EditedField>();

		/// <summary>
		/// Deleted nested types that must be removed from <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<TypeDef> DeletedNestedTypes { get; } = new List<TypeDef>();

		/// <summary>
		/// Deleted properties that must be removed from <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<PropertyDef> DeletedProperties { get; } = new List<PropertyDef>();

		/// <summary>
		/// Deleted events that must be removed from <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<EventDef> DeletedEvents { get; } = new List<EventDef>();

		/// <summary>
		/// Deleted methods that must be removed from <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<MethodDef> DeletedMethods { get; } = new List<MethodDef>();

		/// <summary>
		/// Deleted fields that must be removed from <see cref="ImportedType.TargetType"/>
		/// </summary>
		public List<FieldDef> DeletedFields { get; } = new List<FieldDef>();

		public MergedImportedType(TypeDef targetType, MergeKind mergeKind) {
			TargetType = targetType;
			MergeKind = mergeKind;
			NewTypeDefOptions = new TypeDefOptions();
		}
	}
}
