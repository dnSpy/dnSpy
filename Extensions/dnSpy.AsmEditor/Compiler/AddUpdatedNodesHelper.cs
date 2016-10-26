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
using dnSpy.AsmEditor.Commands;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AddUpdatedNodesHelper {
		readonly ModuleDocumentNode modNode;
		readonly TypeNodeCreator[] newTypeNodeCreators;
		readonly ExistingTypeNodeUpdater[] existingTypeNodeUpdaters;

		public AddUpdatedNodesHelper(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, ModuleImporter importer) {
			this.modNode = modNode;
			this.newTypeNodeCreators = importer.NewNonNestedTypes.Select(a => new TypeNodeCreator(modNode, a.TargetType)).ToArray();
			this.existingTypeNodeUpdaters = importer.MergedNonNestedTypes.Select(a => new ExistingTypeNodeUpdater(methodAnnotations, modNode, a)).ToArray();
			if (!importer.MergedNonNestedTypes.All(a => a.TargetType.Module == modNode.Document.ModuleDef))
				throw new InvalidOperationException();
		}

		public void Execute() {
			for (int i = 0; i < newTypeNodeCreators.Length; i++)
				newTypeNodeCreators[i].Add();
			for (int i = 0; i < existingTypeNodeUpdaters.Length; i++)
				existingTypeNodeUpdaters[i].Add();
		}

		public void Undo() {
			for (int i = existingTypeNodeUpdaters.Length - 1; i >= 0; i--)
				existingTypeNodeUpdaters[i].Remove();
			for (int i = newTypeNodeCreators.Length - 1; i >= 0; i--)
				newTypeNodeCreators[i].Remove();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
