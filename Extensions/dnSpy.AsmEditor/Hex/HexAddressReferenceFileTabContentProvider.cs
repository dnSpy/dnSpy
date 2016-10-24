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
using System.Linq;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportReferenceDocumentTabContentProvider(Order = TabConstants.ORDER_CONTENTPROVIDER_HEXADDRREF)]
	sealed class HexAddressReferenceFileTabContentCreator : IReferenceDocumentTabContentProvider {
		readonly Lazy<IHexBoxDocumentTabContentCreator> hexBoxDocumentTabContentCreator;

		[ImportingConstructor]
		HexAddressReferenceFileTabContentCreator(Lazy<IHexBoxDocumentTabContentCreator> hexBoxDocumentTabContentCreator, IDocumentTreeView documentTreeView) {
			this.hexBoxDocumentTabContentCreator = hexBoxDocumentTabContentCreator;
		}

		public DocumentTabReferenceResult Create(IDocumentTabService documentTabService, DocumentTabContent sourceContent, object @ref) {
			var addrRef = @ref as AddressReference;
			if (addrRef == null)
				addrRef = (@ref as TextReference)?.Reference as AddressReference;
			if (addrRef != null)
				return Create(addrRef, documentTabService.DocumentTreeView);
			return null;
		}

		DocumentTabReferenceResult Create(AddressReference addrRef, IDocumentTreeView documentTreeView) {
			var content = hexBoxDocumentTabContentCreator.Value.TryCreate(addrRef.Filename);
			if (content == null)
				return null;
			ulong? fileOffset = GetFileOffset(addrRef, documentTreeView);
			return new DocumentTabReferenceResult(content, null, e => {
				if (e.Success) {
					Debug.Assert(e.Tab.Content == content);
					var uiContext = e.Tab.UIContext as HexBoxDocumentTabUIContext;
					Debug.Assert(uiContext != null);
					if (uiContext != null && fileOffset != null) {
						if (!IsVisible(uiContext.DnHexBox, fileOffset.Value, addrRef.Length))
							uiContext.DnHexBox.InitializeStartEndOffsetToDocument();
						if (!e.HasMovedCaret) {
							uiContext.DnHexBox.SelectAndMoveCaret(fileOffset.Value, addrRef.Length);
							e.HasMovedCaret = true;
						}
					}
				}
			});
		}

		static bool IsVisible(DnHexBox dnHexBox, ulong start, ulong length) {
			ulong end = length == 0 ? start : start + length - 1;
			if (end < start)
				return false;
			return start >= dnHexBox.StartOffset && end <= dnHexBox.EndOffset;
		}

		ulong? GetFileOffset(AddressReference addrRef, IDocumentTreeView documentTreeView) {
			if (!addrRef.IsRVA)
				return addrRef.Address;
			if (string.IsNullOrEmpty(addrRef.Filename))
				return null;

			var file = documentTreeView.GetAllCreatedDocumentNodes().FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Document.Filename, addrRef.Filename));
			if (file == null)
				return null;
			var pe = file.Document.PEImage;
			if (pe == null)
				return null;
			return (ulong)pe.ToFileOffset((RVA)addrRef.Address);
		}
	}
}
