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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Files.Tabs.TextEditor;

namespace dnSpy.Files.Tabs {
	[ExportReferenceFileTabContentCreator(Order = TabConstants.ORDER_CONTENTCREATOR_CODEREF)]
	sealed class TreeNodeReferenceFileTabContentCreator : IReferenceFileTabContentCreator {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTabManagerSettings fileTabManagerSettings;

		[ImportingConstructor]
		TreeNodeReferenceFileTabContentCreator(DecompileFileTabContentFactory decompileFileTabContentFactory, IFileTabManagerSettings fileTabManagerSettings) {
			this.decompileFileTabContentFactory = decompileFileTabContentFactory;
			this.fileTabManagerSettings = fileTabManagerSettings;
		}

		static object ResolveMemberDef(object @ref) {
			if (@ref is ParamDef)
				return @ref;

			if (@ref is ITypeDefOrRef)
				return ((ITypeDefOrRef)@ref).ResolveTypeDef();

			if (@ref is IMethod && ((IMethod)@ref).MethodSig != null) {
				var m = (IMethod)@ref;
				if (m is MethodSpec)
					m = ((MethodSpec)m).Method;
				if (m is MemberRef)
					return ((MemberRef)m).ResolveMethod();
				return m as MethodDef;
			}

			if (@ref is IField) {
				var f = (IField)@ref;
				if (f is MemberRef)
					return ((MemberRef)f).ResolveField();
				return f as FieldDef;
			}

			if (@ref is PropertyDef)
				return (PropertyDef)@ref;

			if (@ref is EventDef)
				return (EventDef)@ref;

			return null;
		}

		object GetReference(object @ref) {
			var @ref2 = ResolveMemberDef(@ref);
			var def = @ref2 as IMemberDef ?? (@ref2 as ParamDef)?.DeclaringMethod;

			if (!fileTabManagerSettings.DecompileFullType || @ref2 == null || def == null)
				return @ref2 ?? @ref;

			const int MAX = 100;
			for (int i = 0; i < MAX && def.DeclaringType != null; i++)
				def = def.DeclaringType;
			return def;
		}

		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var codeRef = @ref as CodeReference;
			if (codeRef != null) {
				var result = CreateMemberRefResult(fileTabManager, codeRef.Reference);
				if (result != null)
					return result;

				return CreateLocalRefResult(sourceContent, codeRef);
			}

			return CreateMemberRefResult(fileTabManager, @ref);
		}

		FileTabReferenceResult CreateLocalRefResult(IFileTabContent sourceContent, CodeReference codeRef) {
			Debug.Assert(IsSupportedReference(codeRef));
			if (sourceContent == null)
				return null;
			var content = sourceContent.Clone();
			return new FileTabReferenceResult(content, null, a => {
				if (a.Success && !a.HasMovedCaret) {
					GoToReference(content, codeRef);
					a.HasMovedCaret = true;
				}
			});
		}

		FileTabReferenceResult CreateMemberRefResult(IFileTabManager fileTabManager, object @ref) {
			var resolvedRef = ResolveMemberDef(@ref);
			if (!IsSupportedReference(resolvedRef))
				return null;
			var newRef = GetReference(@ref);
			var node = fileTabManager.FileTreeView.FindNode(newRef);
			if (node == null) {
				// If it's eg. a TypeDef, its assembly has been removed from the file list or it
				// was never inserted because adding an assembly had been temporarily disabled.
				// Add the assembly to the list again. Next time the user clicks on the link,
				// FindNode() above will succeed.
				var def = @ref as IMemberDef ?? (@ref as ParamDef)?.DeclaringMethod;
				if (def != null) {
					DnSpyFile file = null;
					var mod = def.Module;
					if (mod != null && mod.Assembly != null)
						file = DnSpyDotNetFile.CreateAssembly(DnSpyFileInfo.CreateFile(mod.Location), mod, false);
					else if (mod != null)
						file = DnSpyDotNetFile.CreateModule(DnSpyFileInfo.CreateFile(mod.Location), mod, false);
					if (file != null) {
						var existingFile = fileTabManager.FileTreeView.FileManager.GetOrAdd(file);
						if (existingFile != file)
							fileTabManager.FileTreeView.FileManager.ForceAdd(file, true, null);
					}
				}

				return null;
			}

			var content = decompileFileTabContentFactory.Create(new IFileTreeNodeData[] { node });
			return new FileTabReferenceResult(content, null, a => {
				if (a.Success && !a.HasMovedCaret) {
					GoToReference(content, resolvedRef);
					a.HasMovedCaret = true;
				}
			});
		}

		static bool IsSupportedReference(object @ref) => @ref is CodeReference || @ref is IMemberDef || @ref is ParamDef;

		void GoToReference(IFileTabContent content, object @ref) {
			Debug.Assert(IsSupportedReference(@ref));
			var uiCtx = content.FileTab.UIContext as ITextEditorUIContext;
			if (uiCtx == null)
				return;

			uiCtx.MoveCaretTo(@ref);
		}
	}
}
