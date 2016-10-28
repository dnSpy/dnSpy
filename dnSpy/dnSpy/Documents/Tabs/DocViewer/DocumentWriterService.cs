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
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	[Export(typeof(IDocumentWriterService))]
	sealed class DocumentWriterService : IDocumentWriterService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<IDocumentWriterProvider, IOrderableContentTypeMetadata>[] documentWriterProviders;

		[ImportingConstructor]
		DocumentWriterService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<IDocumentWriterProvider, IOrderableContentTypeMetadata>> documentWriterProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.documentWriterProviders = Orderer.Order(documentWriterProviders).ToArray();
		}

		public void Write(IDecompilerOutput output, string text, string contentType) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			var ct = contentTypeRegistryService.GetContentType(contentType);
			if (ct == null)
				throw new ArgumentException($"Invalid content type: {contentType}");

			var writer = GetDocumentWriter(ct);
			if (writer != null)
				writer.Write(output, text);
			else
				output.Write(text, BoxedTextColor.Text);
		}

		IDocumentWriter GetDocumentWriter(IContentType contentType) {
			foreach (var lz in documentWriterProviders) {
				if (!contentType.IsOfAnyType(lz.Metadata.ContentTypes))
					continue;
				var writer = lz.Value.Create(contentType);
				if (writer != null)
					return writer;
			}
			return null;
		}
	}
}
