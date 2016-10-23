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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Documents.Tabs.DocViewer;

namespace dnSpy.Documents.Tabs {
	[ExportReferenceDocumentTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_TEXTREF)]
	sealed class TreeNodeReferenceDocumentTabContentProvider : IReferenceDocumentTabContentProvider {
		readonly DecompileDocumentTabContentFactory decompileDocumentTabContentFactory;
		readonly IDocumentTabServiceSettings documentTabServiceSettings;

		[ImportingConstructor]
		TreeNodeReferenceDocumentTabContentProvider(DecompileDocumentTabContentFactory decompileDocumentTabContentFactory, IDocumentTabServiceSettings documentTabServiceSettings) {
			this.decompileDocumentTabContentFactory = decompileDocumentTabContentFactory;
			this.documentTabServiceSettings = documentTabServiceSettings;
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

			if (!documentTabServiceSettings.DecompileFullType || @ref2 == null || def == null)
				return @ref2 ?? @ref;

			const int MAX = 100;
			for (int i = 0; i < MAX && def.DeclaringType != null; i++)
				def = def.DeclaringType;
			return def;
		}

		public DocumentTabReferenceResult Create(IDocumentTabService documentTabService, IDocumentTabContent sourceContent, object @ref) {
			var textRef = @ref as TextReference;
			if (textRef != null) {
				if (textRef.Reference is IAssembly || textRef.Reference is ModuleDef || textRef.Reference is ModuleRef || textRef.Reference is NamespaceReference)
					return null;
				var result = CreateMemberRefResult(documentTabService, textRef.Reference);
				if (result != null)
					return result;

				return CreateLocalRefResult(sourceContent, textRef);
			}

			return CreateMemberRefResult(documentTabService, @ref);
		}

		DocumentTabReferenceResult CreateLocalRefResult(IDocumentTabContent sourceContent, TextReference textRef) {
			Debug.Assert(IsSupportedReference(textRef));
			if (sourceContent == null)
				return null;
			if (!sourceContent.CanClone)
				return null;
			var content = sourceContent.Clone();
			return new DocumentTabReferenceResult(content, null, a => {
				if (a.Success && !a.HasMovedCaret) {
					GoToReference(content, textRef, false);
					a.HasMovedCaret = true;
				}
			});
		}

		DocumentTabReferenceResult CreateMemberRefResult(IDocumentTabService documentTabService, object @ref) {
			var resolvedRef = ResolveMemberDef(@ref);
			if (!IsSupportedReference(resolvedRef))
				return null;
			var newRef = GetReference(@ref);
			var node = documentTabService.DocumentTreeView.FindNode(newRef);
			if (node == null) {
				// If it's eg. a TypeDef, its assembly has been removed from the document list or it
				// was never inserted because adding an assembly had been temporarily disabled.
				// Add the assembly to the list again. Next time the user clicks on the link,
				// FindNode() above will succeed.
				var def = @ref as IMemberDef ?? (@ref as ParamDef)?.DeclaringMethod;
				if (def != null) {
					DsDocument document = null;
					var mod = def.Module;
					if (mod != null && mod.Assembly != null)
						document = DsDotNetDocument.CreateAssembly(DsDocumentInfo.CreateDocument(mod.Location), mod, false);
					else if (mod != null)
						document = DsDotNetDocument.CreateModule(DsDocumentInfo.CreateDocument(mod.Location), mod, false);
					if (document != null) {
						var existingDocument = documentTabService.DocumentTreeView.DocumentService.GetOrAdd(document);
						if (existingDocument != document)
							documentTabService.DocumentTreeView.DocumentService.ForceAdd(document, true, null);
					}
				}

				return null;
			}

			var content = decompileDocumentTabContentFactory.Create(new IDocumentTreeNodeData[] { node });
			return new DocumentTabReferenceResult(content, null, a => {
				if (a.Success && !a.HasMovedCaret) {
					GoToReference(content, resolvedRef, content.WasNewContent);
					a.HasMovedCaret = true;
				}
			});
		}

		static bool IsSupportedReference(object @ref) => @ref is TextReference || @ref is IMemberDef || @ref is ParamDef;

		void GoToReference(IDocumentTabContent content, object @ref, bool center) {
			Debug.Assert(IsSupportedReference(@ref));
			var uiCtx = content.DocumentTab.UIContext as IDocumentViewer;
			if (uiCtx == null)
				return;

			var options = MoveCaretOptions.Select | MoveCaretOptions.Focus;
			if (center)
				options |= MoveCaretOptions.Center;
			uiCtx.MoveCaretToReference(@ref, options);
		}
	}
}
