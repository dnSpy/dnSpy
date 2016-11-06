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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.IO;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Compiler {
	[DebuggerDisplay("{Description}")]
	sealed class MergeWithAssemblyCommand : EditCodeCommandBase {
		[ExportMenuItem(Header = "res:MergeWithAssemblyCommand", Icon = DsImagesAttribute.Assembly, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 19.999)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly IPickFilename pickFilename;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, IPickFilename pickFilename) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.pickFilename = pickFilename;
			}

			public override bool IsVisible(AsmEditorContext context) => MergeWithAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MergeWithAssemblyCommand.Execute(pickFilename, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:MergeWithAssemblyCommand", Icon = DsImagesAttribute.Assembly, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 49.999)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly IPickFilename pickFilename;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, IPickFilename pickFilename)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.pickFilename = pickFilename;
			}

			public override bool IsVisible(AsmEditorContext context) => MergeWithAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MergeWithAssemblyCommand.Execute(pickFilename, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:MergeWithAssemblyCommand", Icon = DsImagesAttribute.Assembly, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 19.999)]
		sealed class CodeCommand : NodesCodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly IPickFilename pickFilename;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, IPickFilename pickFilename)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.pickFilename = pickFilename;
			}

			public override bool IsEnabled(CodeContext context) => MergeWithAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => MergeWithAssemblyCommand.Execute(pickFilename, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1;

		static void Execute(IPickFilename pickFilename, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = nodes[0] as AssemblyDocumentNode;
			ModuleDocumentNode modNode;
			if (asmNode != null) {
				asmNode.TreeNode.EnsureChildrenLoaded();
				modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
			}
			else
				modNode = nodes[0].GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				return;
			var module = modNode.Document.ModuleDef;
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var filename = pickFilename.GetFilename(null, "dll", PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			var result = GetModuleBytes(filename);
			if (result == null)
				return;

			// This is a basic assembly merger, we don't support merging dependencies. It would require
			// fixing all refs to the dep and redirect them to the new defs that now exist in 'module'.
			var asm = module.Assembly;
			if (asm != null && result.Value.Assembly != null) {
				if (IsNonSupportedAssembly(module, asm, result.Value.Assembly)) {
					Contracts.App.MsgBox.Instance.Show($"Can't merge with {result.Value.Assembly} because it's a dependency");
					return;
				}
			}

			var importer = new ModuleImporter(module, EditCodeVM.makeEverythingPublic);
			try {
				importer.Import(result.Value.RawBytes, result.Value.DebugFile, ModuleImporterOptions.None);
			}
			catch (Exception ex) {
				Contracts.App.MsgBox.Instance.Show(ex);
				return;
			}

			undoCommandService.Value.Add(new MergeWithAssemblyCommand(addUpdatedNodesHelperProvider, modNode, importer));
		}

		static bool IsNonSupportedAssembly(ModuleDef module, AssemblyDef asm, IAssembly assembly) {
			if (AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(asm, assembly))
				return true;
			foreach (var asmRef in module.GetAssemblyRefs()) {
				if (AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(asmRef, assembly))
					return true;
			}
			return false;
		}

		struct ModuleResult {
			public IAssembly Assembly { get; }
			public byte[] RawBytes { get; }
			public DebugFileResult DebugFile { get; }
			public ModuleResult(IAssembly assembly, byte[] bytes, DebugFileResult debugFile) {
				Assembly = assembly;
				RawBytes = bytes;
				DebugFile = debugFile;
			}
		}

		static ModuleResult? GetModuleBytes(string filename) {
			if (!File.Exists(filename))
				return null;
			try {
				using (var module = ModuleDefMD.Load(filename)) {
					// It's a .NET file, return all bytes
					var bytes = module.MetaData.PEImage.CreateFullStream().ReadAllBytes();
					var asm = module.Assembly?.ToAssemblyRef();
					var debugFile = GetDebugFile(module);
					return new ModuleResult(asm, bytes, debugFile);
				}
			}
			catch {
			}
			return null;
		}

		static DebugFileResult GetDebugFile(ModuleDef module) {
			var pdbFilename = Path.ChangeExtension(module.Location, "pdb");
			try {
				var pdbBytes = File.ReadAllBytes(pdbFilename);
				string pdbMagic = "Microsoft C/C++ MSF 7.00\r\n";
				if (pdbBytes.Length > pdbMagic.Length && Encoding.ASCII.GetString(pdbBytes, 0, pdbMagic.Length) == pdbMagic)
					return new DebugFileResult(DebugFileFormat.Pdb, pdbBytes);

				//TODO: Support portable pdb and embedded pdb
			}
			catch {
			}

			return new DebugFileResult();
		}

		MergeWithAssemblyCommand(Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, ModuleDocumentNode modNode, ModuleImporter importer)
			: base(addUpdatedNodesHelperProvider, modNode, importer) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.MergeWithAssemblyCommand2;
	}
}
