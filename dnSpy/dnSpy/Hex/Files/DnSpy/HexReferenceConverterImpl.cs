/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.Hex.Files.DnSpy {
	[Export(typeof(HexReferenceConverter))]
	sealed class HexReferenceConverterImpl : HexReferenceConverter {
		readonly BufferToDocumentNodeService bufferToDocumentNodeService;

		[ImportingConstructor]
		HexReferenceConverterImpl(BufferToDocumentNodeService bufferToDocumentNodeService) => this.bufferToDocumentNodeService = bufferToDocumentNodeService;

		public override object Convert(HexView hexView, object reference) {
			if (reference is HexMethodReference methodRef)
				return ConvertMethodReference(methodRef);

			return reference;
		}

		MethodStatementReference ConvertMethodReference(HexMethodReference methodRef) {
			var docNode = bufferToDocumentNodeService.Find(methodRef.File);
			if (docNode == null)
				return null;
			var module = docNode.Document.ModuleDef;
			if (module == null)
				return null;
			var method = module.ResolveToken(methodRef.Token) as MethodDef;
			if (method == null)
				return null;

			return new MethodStatementReference(method, methodRef.Offset);
		}
	}

	sealed class HexMethodReference {
		public HexBufferFile File { get; }
		public uint Token { get; }
		public uint? Offset { get; }

		public HexMethodReference(HexBufferFile file, uint token, uint? offset) {
			File = file ?? throw new ArgumentNullException(nameof(file));
			Token = token;
			Offset = offset;
		}
	}

	abstract class BufferToDocumentNodeService {
		public abstract DsDocumentNode Find(HexBufferFile file);
	}

	[Export(typeof(BufferToDocumentNodeService))]
	sealed class BufferToDocumentNodeServiceImpl : BufferToDocumentNodeService {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		BufferToDocumentNodeServiceImpl(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		public override DsDocumentNode Find(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (file.Name == string.Empty)
				return null;
			var doc = documentTabService.DocumentTreeView.DocumentService.Find(new FilenameKey(file.Name));
			if (doc == null)
				return null;
			return documentTabService.DocumentTreeView.FindNode(doc);
		}
	}
}
