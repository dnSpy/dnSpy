/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Types;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	sealed class ExistingTypeNodeUpdater {
		readonly TypeNode typeNode;
		readonly ModuleDef ownerModule;
		readonly TypeDef targetType;
		readonly TypeDefOptions origTypeDefOptions;
		readonly TypeDefOptions newTypeDefOptions;
		readonly ExistingTypeNodeUpdater[] nestedTypes1;
		readonly NestedTypeNodeCreator[] nestedTypes2;
		readonly FieldNodeCreator[] fields;
		readonly MethodNodeCreator[] methods;
		readonly EventNodeCreator[] events;
		readonly PropertyNodeCreator[] properties;
		readonly EditedFieldUpdater[] editedFields;
		readonly EditedMethodUpdater[] editedMethods;
		readonly EditedPropertyUpdater[] editedProperties;
		readonly EditedEventUpdater[] editedEvents;
		readonly DeletedTypeUpdater[] deletedTypes;
		readonly DeletedFieldUpdater[] deletedFields;
		readonly DeletedMethodUpdater[] deletedMethods;
		readonly DeletedPropertyUpdater[] deletedProperties;
		readonly DeletedEventUpdater[] deletedEvents;

		public ExistingTypeNodeUpdater(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, MergedImportedType type) {
			targetType = type.TargetType!;
			ownerModule = targetType.Module;
			origTypeDefOptions = new TypeDefOptions(targetType);
			newTypeDefOptions = type.NewTypeDefOptions;
			typeNode = modNode.Context.DocumentTreeView.FindNode(targetType) ?? throw new InvalidOperationException();
			nestedTypes1 = type.NewOrExistingNestedTypes.OfType<MergedImportedType>().Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			nestedTypes2 = type.NewOrExistingNestedTypes.OfType<NewImportedType>().Select(a => new NestedTypeNodeCreator(modNode, typeNode, a.TargetType!)).ToArray();
			if (nestedTypes1.Length + nestedTypes2.Length != type.NewOrExistingNestedTypes.Count)
				throw new InvalidOperationException();
			fields = type.NewFields.Select(a => new FieldNodeCreator(modNode, typeNode, a)).ToArray();
			var specialMethods = GetSpecialMethods(type);
			methods = type.NewMethods.Where(a => !specialMethods.Contains(a)).Select(a => new MethodNodeCreator(modNode, typeNode, a)).ToArray();
			events = type.NewEvents.Select(a => new EventNodeCreator(modNode, typeNode, a)).ToArray();
			properties = type.NewProperties.Select(a => new PropertyNodeCreator(modNode, typeNode, a)).ToArray();
			editedFields = type.EditedFields.Select(a => new EditedFieldUpdater(modNode, a.OriginalField, a.FieldDefOptions)).ToArray();
			editedMethods = type.EditedMethods.Select(a => new EditedMethodUpdater(methodAnnotations, modNode, a.OriginalMethod, a.NewBody, a.MethodDefOptions)).ToArray();
			editedProperties = type.EditedProperties.Select(a => new EditedPropertyUpdater(modNode, a.OriginalProperty, a.PropertyDefOptions)).ToArray();
			editedEvents = type.EditedEvents.Select(a => new EditedEventUpdater(modNode, a.OriginalEvent, a.EventDefOptions)).ToArray();
			deletedTypes = type.DeletedNestedTypes.Select(a => new DeletedTypeUpdater(modNode, a)).ToArray();
			deletedFields = type.DeletedFields.Select(a => new DeletedFieldUpdater(modNode, a)).ToArray();
			deletedMethods = type.DeletedMethods.Select(a => new DeletedMethodUpdater(modNode, a)).ToArray();
			deletedProperties = type.DeletedProperties.Select(a => new DeletedPropertyUpdater(modNode, a)).ToArray();
			deletedEvents = type.DeletedEvents.Select(a => new DeletedEventUpdater(modNode, a)).ToArray();
		}

		static HashSet<MethodDef> GetSpecialMethods(MergedImportedType type) {
			var specialMethods = new HashSet<MethodDef>();

			foreach (var p in type.NewProperties) {
				foreach (var m in p.GetMethods)
					specialMethods.Add(m);
				foreach (var m in p.SetMethods)
					specialMethods.Add(m);
				foreach (var m in p.OtherMethods)
					specialMethods.Add(m);
			}

			foreach (var e in type.NewEvents) {
				if (!(e.AddMethod is null))
					specialMethods.Add(e.AddMethod);
				if (!(e.RemoveMethod is null))
					specialMethods.Add(e.RemoveMethod);
				if (!(e.InvokeMethod is null))
					specialMethods.Add(e.InvokeMethod);
				foreach (var m in e.OtherMethods)
					specialMethods.Add(m);
			}

			return specialMethods;
		}

		public void Add() {
			newTypeDefOptions.CopyTo(targetType, ownerModule);
			for (int i = 0; i < deletedTypes.Length; i++)
				deletedTypes[i].Add();
			for (int i = 0; i < deletedFields.Length; i++)
				deletedFields[i].Add();
			for (int i = 0; i < deletedMethods.Length; i++)
				deletedMethods[i].Add();
			for (int i = 0; i < deletedProperties.Length; i++)
				deletedProperties[i].Add();
			for (int i = 0; i < deletedEvents.Length; i++)
				deletedEvents[i].Add();
			for (int i = 0; i < nestedTypes1.Length; i++)
				nestedTypes1[i].Add();
			for (int i = 0; i < nestedTypes2.Length; i++)
				nestedTypes2[i].Add();
			for (int i = 0; i < fields.Length; i++)
				fields[i].Add();
			for (int i = 0; i < methods.Length; i++)
				methods[i].Add();
			for (int i = 0; i < events.Length; i++)
				events[i].Add();
			for (int i = 0; i < properties.Length; i++)
				properties[i].Add();
			for (int i = 0; i < editedFields.Length; i++)
				editedFields[i].Add();
			for (int i = 0; i < editedMethods.Length; i++)
				editedMethods[i].Add();
			for (int i = 0; i < editedProperties.Length; i++)
				editedProperties[i].Add();
			for (int i = 0; i < editedEvents.Length; i++)
				editedEvents[i].Add();

			typeNode.TreeNode.RefreshUI();
			TypeDefSettingsCommand.InvalidateBaseTypeFolderNode(typeNode);
		}

		public void Remove() {
			for (int i = editedEvents.Length - 1; i >= 0; i--)
				editedEvents[i].Remove();
			for (int i = editedProperties.Length - 1; i >= 0; i--)
				editedProperties[i].Remove();
			for (int i = editedMethods.Length - 1; i >= 0; i--)
				editedMethods[i].Remove();
			for (int i = editedFields.Length - 1; i >= 0; i--)
				editedFields[i].Remove();
			for (int i = properties.Length - 1; i >= 0; i--)
				properties[i].Remove();
			for (int i = events.Length - 1; i >= 0; i--)
				events[i].Remove();
			for (int i = methods.Length - 1; i >= 0; i--)
				methods[i].Remove();
			for (int i = fields.Length - 1; i >= 0; i--)
				fields[i].Remove();
			for (int i = nestedTypes2.Length - 1; i >= 0; i--)
				nestedTypes2[i].Remove();
			for (int i = nestedTypes1.Length - 1; i >= 0; i--)
				nestedTypes1[i].Remove();
			for (int i = deletedEvents.Length - 1; i >= 0; i--)
				deletedEvents[i].Remove();
			for (int i = deletedProperties.Length - 1; i >= 0; i--)
				deletedProperties[i].Remove();
			for (int i = deletedMethods.Length - 1; i >= 0; i--)
				deletedMethods[i].Remove();
			for (int i = deletedFields.Length - 1; i >= 0; i--)
				deletedFields[i].Remove();
			for (int i = deletedTypes.Length - 1; i >= 0; i--)
				deletedTypes[i].Remove();
			origTypeDefOptions.CopyTo(targetType, ownerModule);

			typeNode.TreeNode.RefreshUI();
			TypeDefSettingsCommand.InvalidateBaseTypeFolderNode(typeNode);
		}
	}
}
