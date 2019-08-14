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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.UndoRedo {
	[Export(typeof(IUndoableDocumentsProvider))]
	sealed class DsDocumentUndoableDocumentsProvider : IUndoableDocumentsProvider {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		DsDocumentUndoableDocumentsProvider(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		IEnumerable<IUndoObject> IUndoableDocumentsProvider.GetObjects() {
			foreach (var file in GetAllDocuments())
				yield return GetUndoObject(file);
		}

		IEnumerable<IDsDocument> GetAllDocuments() => documentTabService.DocumentTreeView.GetAllCreatedDocumentNodes().Select(a => a.Document);

		IUndoObject? IUndoableDocumentsProvider.GetUndoObject(object obj) {
			if (obj is DocumentTreeNodeData node) {
				var documentNode = node.GetDocumentNode();
				Debug2.Assert(!(documentNode is null));
				if (!(documentNode is null)) {
					// Need this check here since some commands (eg. create netmodule) create nodes
					// and they haven't yet been inserted into the treeview.
					if (documentNode is ModuleDocumentNode)
						return GetUndoObjectNoChecks(documentNode.Document);
					if (documentNode is AssemblyDocumentNode asmNode) {
						asmNode.TreeNode.EnsureChildrenLoaded();
						var modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
						Debug2.Assert(!(modNode is null));
						if (!(modNode is null))
							return GetUndoObjectNoChecks(modNode.Document);
					}
					return GetUndoObject(documentNode.Document);
				}
			}
			if (obj is IDsDocument document)
				return GetUndoObject(document);

			return null;
		}

		bool IUndoableDocumentsProvider.OnExecutedOneCommand(IUndoObject obj) {
			var file = TryGetDocument(obj);
			if (!(file is null)) {
				var module = file.ModuleDef;
				if (!(module is null))
					module.ResetTypeDefFindCache();
				documentTabService.RefreshModifiedDocument(file);
				return true;
			}

			return false;
		}

		object? IUndoableDocumentsProvider.GetDocument(IUndoObject obj) => TryGetDocument(obj);

		IDsDocument GetDocumentFile(IDsDocument document) {
			if (document is IDsDotNetDocument dnDocument) {
				// Assemblies and manifest modules don't share a IDsDocument instance, but we must
				// use the same IUndoObject instance since they're part of the same file.
				var module = document.ModuleDef;
				Debug2.Assert(!(module is null));
				if (module is null)
					throw new InvalidOperationException();
				var modFile = FindModule(module);
				// It could've been removed but some menu item handler could still have a reference
				// to the module.
				return modFile ?? document;
			}

			return document;
		}

		IDsDocument? FindModule(ModuleDef module) => documentTabService.DocumentTreeView.FindNode(module)?.Document;
		IUndoObject GetUndoObject(IDsDocument document) => GetUndoObjectNoChecks(GetDocumentFile(document));

		IUndoObject GetUndoObjectNoChecks(IDsDocument document) {
			var uo = document.Annotation<UndoObject>() ?? document.AddAnnotation(new UndoObject())!;
			uo.Value = document;
			return uo;
		}

		public static IDsDocument? TryGetDocument(IUndoObject iuo) => (iuo as UndoObject)?.Value as IDsDocument;
	}
}
