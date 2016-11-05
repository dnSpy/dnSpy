/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	sealed class ExistingTypeNodeUpdater {
		readonly TypeNode typeNode;
		readonly ExistingTypeNodeUpdater[] nestedTypes1;
		readonly NestedTypeNodeCreator[] nestedTypes2;
		readonly FieldNodeCreator[] fields;
		readonly MethodNodeCreator[] methods;
		readonly EventNodeCreator[] events;
		readonly PropertyNodeCreator[] properties;
		readonly EditedMethodBodyUpdater[] editedMethods;

		public ExistingTypeNodeUpdater(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, MergedImportedType type) {
			this.typeNode = modNode.Context.DocumentTreeView.FindNode(type.TargetType);
			if (this.typeNode == null)
				throw new InvalidOperationException();
			this.nestedTypes1 = type.NewNestedTypes.OfType<MergedImportedType>().Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			this.nestedTypes2 = type.NewNestedTypes.OfType<NewImportedType>().Select(a => new NestedTypeNodeCreator(modNode, typeNode, a.TargetType)).ToArray();
			if (nestedTypes1.Length + nestedTypes2.Length != type.NewNestedTypes.Count)
				throw new InvalidOperationException();
			this.fields = type.NewFields.Select(a => new FieldNodeCreator(modNode, typeNode, a)).ToArray();
			var specialMethods = GetSpecialMethods(type);
			this.methods = type.NewMethods.Where(a => !specialMethods.Contains(a)).Select(a => new MethodNodeCreator(modNode, typeNode, a)).ToArray();
			this.events = type.NewEvents.Select(a => new EventNodeCreator(modNode, typeNode, a)).ToArray();
			this.properties = type.NewProperties.Select(a => new PropertyNodeCreator(modNode, typeNode, a)).ToArray();
			this.editedMethods = type.EditedMethodBodies.Select(a => new EditedMethodBodyUpdater(methodAnnotations, modNode, a.OriginalMethod, a.NewBody, a.ImplAttributes, a.CustomAttributes, a.DeclSecurities)).ToArray();
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
				if (e.AddMethod != null)
					specialMethods.Add(e.AddMethod);
				if (e.RemoveMethod != null)
					specialMethods.Add(e.RemoveMethod);
				if (e.InvokeMethod != null)
					specialMethods.Add(e.InvokeMethod);
				foreach (var m in e.OtherMethods)
					specialMethods.Add(m);
			}

			return specialMethods;
		}

		public void Add() {
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
			for (int i = 0; i < editedMethods.Length; i++)
				editedMethods[i].Add();
		}

		public void Remove() {
			for (int i = editedMethods.Length - 1; i >= 0; i--)
				editedMethods[i].Remove();
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
		}
	}
}
