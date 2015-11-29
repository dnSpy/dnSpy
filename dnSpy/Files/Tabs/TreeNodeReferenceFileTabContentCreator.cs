/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Files.Tabs.TextEditor;

namespace dnSpy.Files.Tabs {
	[ExportReferenceFileTabContentCreator]
	sealed class TreeNodeReferenceFileTabContentCreator : IReferenceFileTabContentCreator {
		readonly DecompileFileTabContentFactory decompileFileTabContentFactory;
		readonly IFileTabManagerSettings fileTabManagerSettings;

		[ImportingConstructor]
		TreeNodeReferenceFileTabContentCreator(DecompileFileTabContentFactory decompileFileTabContentFactory, IFileTabManagerSettings fileTabManagerSettings) {
			this.decompileFileTabContentFactory = decompileFileTabContentFactory;
			this.fileTabManagerSettings = fileTabManagerSettings;
		}

		static IMemberDef ResolveMemberDef(IMemberRef @ref) {
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
			var def = ResolveMemberDef(@ref as IMemberRef);

			if (!fileTabManagerSettings.DecompileFullType || def == null)
				return def ?? @ref;

			const int MAX = 100;
			for (int i = 0; i < MAX && def.DeclaringType != null; i++)
				def = def.DeclaringType;
			return def;
		}

		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var codeRef = @ref as CodeReferenceSegment;
			if (codeRef != null) {
				var result = CreateMemberRefResult(fileTabManager, codeRef.Reference);
				if (result != null)
					return result;

				return CreateLocalRefResult(sourceContent, codeRef);
			}

			return CreateMemberRefResult(fileTabManager, @ref);
		}

		FileTabReferenceResult CreateLocalRefResult(IFileTabContent sourceContent, CodeReferenceSegment codeRef) {
			if (sourceContent == null)
				return null;
			var content = sourceContent.Clone();
			return new FileTabReferenceResult(content, null, a => {
				if (a.Success)
					GoToReference(content, codeRef);
			});
		}

		FileTabReferenceResult CreateMemberRefResult(IFileTabManager fileTabManager, object @ref) {
			var node = fileTabManager.FileTreeView.FindNode(GetReference(@ref));
			if (node == null)
				return null;

			var content = decompileFileTabContentFactory.Create(new IFileTreeNodeData[] { node });
			return new FileTabReferenceResult(content, null, a => {
				if (a.Success)
					GoToReference(content, ResolveMemberDef(@ref as IMemberRef));
			});
		}

		void GoToReference(IFileTabContent content, object @ref) {
			var uiCtx = content.FileTab.UIContext as ITextEditorUIContext;
			if (uiCtx == null)
				return;

			uiCtx.MoveCaretTo(@ref);
		}
	}
}
