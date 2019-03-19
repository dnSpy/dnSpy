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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.AsmEditor.Hex.PE {
	[Export(typeof(HexReferenceConverter))]
	sealed class HexReferenceConverterImpl : HexReferenceConverter {
		readonly BufferToDocumentNodeService bufferToDocumentNodeService;

		[ImportingConstructor]
		HexReferenceConverterImpl(BufferToDocumentNodeService bufferToDocumentNodeService) => this.bufferToDocumentNodeService = bufferToDocumentNodeService;

		public override object Convert(HexView hexView, object reference) {
			if (reference is HexFieldReference fieldRef)
				return ConvertFieldReference(fieldRef);

			return reference;
		}

		DocumentTreeNodeData ConvertFieldReference(HexFieldReference fieldRef) {
			var peNode = bufferToDocumentNodeService.FindPENode(fieldRef.File);
			if (peNode == null)
				return null;

			return peNode.FindNode(fieldRef.Structure, fieldRef.Field);
		}
	}
}
