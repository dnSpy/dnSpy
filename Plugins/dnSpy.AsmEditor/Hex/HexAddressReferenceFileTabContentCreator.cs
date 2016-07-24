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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportReferenceFileTabContentCreator(Order = TabConstants.ORDER_CONTENTCREATOR_HEXADDRREF)]
	sealed class HexAddressReferenceFileTabContentCreator : IReferenceFileTabContentCreator {
		readonly Lazy<IHexBoxFileTabContentCreator> hexBoxFileTabContentCreator;

		[ImportingConstructor]
		HexAddressReferenceFileTabContentCreator(Lazy<IHexBoxFileTabContentCreator> hexBoxFileTabContentCreator, IFileTreeView fileTreeView) {
			this.hexBoxFileTabContentCreator = hexBoxFileTabContentCreator;
		}

		public FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref) {
			var addrRef = @ref as AddressReference;
			if (addrRef == null)
				addrRef = (@ref as TextReference)?.Reference as AddressReference;
			if (addrRef != null)
				return Create(addrRef, fileTabManager.FileTreeView);
			return null;
		}

		FileTabReferenceResult Create(AddressReference addrRef, IFileTreeView fileTreeView) {
			var content = hexBoxFileTabContentCreator.Value.TryCreate(addrRef.Filename);
			if (content == null)
				return null;
			ulong? fileOffset = GetFileOffset(addrRef, fileTreeView);
			return new FileTabReferenceResult(content, null, e => {
				if (e.Success) {
					Debug.Assert(e.Tab.Content == content);
					var uiContext = e.Tab.UIContext as HexBoxFileTabUIContext;
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

		ulong? GetFileOffset(AddressReference addrRef, IFileTreeView fileTreeView) {
			if (!addrRef.IsRVA)
				return addrRef.Address;
			if (string.IsNullOrEmpty(addrRef.Filename))
				return null;

			var file = fileTreeView.GetAllCreatedDnSpyFileNodes().FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.DnSpyFile.Filename, addrRef.Filename));
			if (file == null)
				return null;
			var pe = file.DnSpyFile.PEImage;
			if (pe == null)
				return null;
			return (ulong)pe.ToFileOffset((RVA)addrRef.Address);
		}
	}
}
