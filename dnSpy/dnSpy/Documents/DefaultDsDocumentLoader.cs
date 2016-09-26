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

using System.Collections.Generic;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents {
	sealed class DefaultDsDocumentLoader : IDsDocumentLoader {
		readonly IDsDocumentService documentService;

		public DefaultDsDocumentLoader(IDsDocumentService documentService) {
			this.documentService = documentService;
		}

		public IDsDocument[] Load(IEnumerable<DocumentToLoad> documents) {
			var loadedDocuments = new List<IDsDocument>();
			var hash = new HashSet<IDsDocument>();
			foreach (var doc in documents) {
				if (doc.Info.Type == DocumentConstants.DOCUMENTTYPE_FILE && string.IsNullOrEmpty(doc.Info.Name))
					continue;
				var document = documentService.TryGetOrCreate(doc.Info, doc.IsAutoLoaded);
				if (document != null && !hash.Contains(document)) {
					hash.Add(document);
					loadedDocuments.Add(document);
				}
			}
			return loadedDocuments.ToArray();
		}
	}
}
