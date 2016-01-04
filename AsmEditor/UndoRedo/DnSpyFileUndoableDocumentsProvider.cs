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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.UndoRedo {
	[Export(typeof(IUndoableDocumentsProvider))]
	sealed class DnSpyFileUndoableDocumentsProvider : IUndoableDocumentsProvider {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		DnSpyFileUndoableDocumentsProvider(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		IEnumerable<IUndoObject> IUndoableDocumentsProvider.GetObjects() {
			foreach (var file in GetAllDnSpyFileInstances())
				yield return GetUndoObject(file);
		}

		IEnumerable<IDnSpyFile> GetAllDnSpyFileInstances() {
			return fileTabManager.FileTreeView.GetAllCreatedDnSpyFileNodes().Select(a => a.DnSpyFile);
		}

		IUndoObject IUndoableDocumentsProvider.GetUndoObject(object obj) {
			var node = obj as IFileTreeNodeData;
			if (node != null) {
				var dnSpyFileNode = node.GetDnSpyFileNode();
				Debug.Assert(dnSpyFileNode != null);
				if (dnSpyFileNode != null) {
					// Need this check here since some commands (eg. create netmodule) create nodes
					// and they haven't yet been inserted into the treeview.
					if (dnSpyFileNode is IModuleFileNode)
						return GetUndoObjectNoChecks(dnSpyFileNode.DnSpyFile);
					if (dnSpyFileNode is IAssemblyFileNode) {
						var asmNode = (IAssemblyFileNode)dnSpyFileNode;
						asmNode.TreeNode.EnsureChildrenLoaded();
						var modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as IModuleFileNode;
						Debug.Assert(modNode != null);
						if (modNode != null)
							return GetUndoObjectNoChecks(modNode.DnSpyFile);
					}
					return GetUndoObject(dnSpyFileNode.DnSpyFile);
				}
			}
			var file = obj as IDnSpyFile;
			if (file != null)
				return GetUndoObject(file);

			return null;
		}

		bool IUndoableDocumentsProvider.OnExecutedOneCommand(IUndoObject obj) {
			var file = TryGetDnSpyFile(obj);
			if (file != null) {
				var module = file.ModuleDef;
				if (module != null)
					module.ResetTypeDefFindCache();
				fileTabManager.RefreshModifiedFile(file);
				return true;
			}

			return false;
		}

		object IUndoableDocumentsProvider.GetDocument(IUndoObject obj) {
			return TryGetDnSpyFile(obj);
		}

		IDnSpyFile GetDocumentFile(IDnSpyFile file) {
			var dnFile = file as IDnSpyDotNetFile;
			if (dnFile != null) {
				// Assemblies and manifest modules don't share a IDnSpyFile instance, but we must
				// use the same IUndoObject instance since they're part of the same file.
				var module = file.ModuleDef;
				Debug.Assert(module != null);
				if (module == null)
					throw new InvalidOperationException();
				var modFile = FindModule(module);
				Debug.Assert(modFile != null);
				if (modFile == null)
					throw new InvalidOperationException();
				return modFile;
			}

			return file;
		}

		IDnSpyFile FindModule(ModuleDef module) {
			var modNode = fileTabManager.FileTreeView.FindNode(module);
			return modNode == null ? null : modNode.DnSpyFile;
		}

		IUndoObject GetUndoObject(IDnSpyFile file) {
			return GetUndoObjectNoChecks(GetDocumentFile(file));
		}

		IUndoObject GetUndoObjectNoChecks(IDnSpyFile file) {
			var uo = file.Annotation<UndoObject>() ?? file.AddAnnotation(new UndoObject());
			uo.Value = file;
			return uo;
		}

		public static IDnSpyFile TryGetDnSpyFile(IUndoObject iuo) {
			var uo = iuo as UndoObject;
			return uo == null ? null : uo.Value as IDnSpyFile;
		}
	}
}
