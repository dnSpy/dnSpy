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
	sealed class AddUpdatedNodesHelper {
		readonly ModuleDocumentNode modNode;
		readonly TypeNodeCreator[] newTypeNodeCreators;
		readonly ExistingTypeNodeUpdater[] existingTypeNodeUpdaters;
		readonly CustomAttribute[] newAssemblyCustomAttributes;
		readonly CustomAttribute[] newModuleCustomAttributes;
		readonly CustomAttribute[] origAssemblyCustomAttributes;
		readonly CustomAttribute[] origModuleCustomAttributes;

		public AddUpdatedNodesHelper(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, ModuleImporter importer) {
			this.modNode = modNode;
			this.newTypeNodeCreators = importer.NewNonNestedTypes.Select(a => new TypeNodeCreator(modNode, a.TargetType)).ToArray();
			this.existingTypeNodeUpdaters = importer.MergedNonNestedTypes.Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			if (!importer.MergedNonNestedTypes.All(a => a.TargetType.Module == modNode.Document.ModuleDef))
				throw new InvalidOperationException();
			this.newAssemblyCustomAttributes = importer.NewAssemblyCustomAttributes;
			this.newModuleCustomAttributes = importer.NewModuleCustomAttributes;
			if (newAssemblyCustomAttributes != null)
				origAssemblyCustomAttributes = modNode.Document.AssemblyDef?.CustomAttributes.ToArray();
			if (newModuleCustomAttributes != null)
				origModuleCustomAttributes = modNode.Document.ModuleDef.CustomAttributes.ToArray();
		}

		public void Execute() {
			for (int i = 0; i < newTypeNodeCreators.Length; i++)
				newTypeNodeCreators[i].Add();
			for (int i = 0; i < existingTypeNodeUpdaters.Length; i++)
				existingTypeNodeUpdaters[i].Add();
			if (origAssemblyCustomAttributes != null && newAssemblyCustomAttributes != null) {
				modNode.Document.AssemblyDef.CustomAttributes.Clear();
				foreach (var ca in newAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			if (origModuleCustomAttributes != null && newModuleCustomAttributes != null) {
				modNode.Document.ModuleDef.CustomAttributes.Clear();
				foreach (var ca in newModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
		}

		public void Undo() {
			for (int i = existingTypeNodeUpdaters.Length - 1; i >= 0; i--)
				existingTypeNodeUpdaters[i].Remove();
			for (int i = newTypeNodeCreators.Length - 1; i >= 0; i--)
				newTypeNodeCreators[i].Remove();
			if (origAssemblyCustomAttributes != null && newAssemblyCustomAttributes != null) {
				modNode.Document.AssemblyDef.CustomAttributes.Clear();
				foreach (var ca in origAssemblyCustomAttributes)
					modNode.Document.AssemblyDef.CustomAttributes.Add(ca);
			}
			if (origModuleCustomAttributes != null && newModuleCustomAttributes != null) {
				modNode.Document.ModuleDef.CustomAttributes.Clear();
				foreach (var ca in origModuleCustomAttributes)
					modNode.Document.ModuleDef.CustomAttributes.Add(ca);
			}
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
