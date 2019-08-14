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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Compiler {
	[DebuggerDisplay("{Description}")]
	sealed class EditAssemblyCommand : EditCodeCommandBase {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 11)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditAssembly);
			public override string? GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditAssembly);
			public override bool IsVisible(AsmEditorContext context) => EditAssemblyCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditAssemblyCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 41)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditAssembly);
			public override string? GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditAssembly);
			public override bool IsVisible(AsmEditorContext context) => EditAssemblyCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditAssemblyCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 11)]
		sealed class CodeCommand : NodesCodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(CodeContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditAssembly);
			public override string? GetHeader(CodeContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditAssembly);
			public override bool IsEnabled(CodeContext context) => EditAssemblyCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(CodeContext context) => EditAssemblyCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(EditCodeVMCreator editCodeVMCreator, DocumentTreeNodeData[] nodes) =>
			editCodeVMCreator.CanCreate(CompilationKind.EditAssembly) && nodes.Length == 1 && (nodes[0] is AssemblyDocumentNode || nodes[0] is ModuleDocumentNode);

		static void Execute(EditCodeVMCreator editCodeVMCreator, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(editCodeVMCreator, nodes))
				return;

			var asmNode = nodes[0] as AssemblyDocumentNode;
			ModuleDocumentNode? modNode;
			if (!(asmNode is null)) {
				asmNode.TreeNode.EnsureChildrenLoaded();
				modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
			}
			else
				modNode = nodes[0] as ModuleDocumentNode;
			Debug2.Assert(!(modNode is null));
			if (modNode is null)
				return;
			var module = modNode.Document.ModuleDef;
			Debug2.Assert(!(module is null));
			if (module is null)
				throw new InvalidOperationException();

			if (module.IsManifestModule)
				asmNode = modNode.TreeNode.Parent?.Data as AssemblyDocumentNode;
			else
				asmNode = null;

			var vm = editCodeVMCreator.CreateEditAssembly(module);
			var win = new EditCodeDlg();
			win.DataContext = vm;
			win.Owner = appService.MainWindow;
			win.Title = $"{dnSpy_AsmEditor_Resources.EditAssemblyCode} - {asmNode?.ToString() ?? modNode.ToString()}";

			if (win.ShowDialog() != true) {
				vm.Dispose();
				return;
			}
			Debug2.Assert(!(vm.Result is null));

			undoCommandService.Value.Add(new EditAssemblyCommand(addUpdatedNodesHelperProvider, modNode, vm.Result));
			vm.Dispose();
		}

		EditAssemblyCommand(Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, ModuleDocumentNode modNode, ModuleImporter importer)
			: base(addUpdatedNodesHelperProvider, modNode, importer) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditAssemblyCode;
	}
}
