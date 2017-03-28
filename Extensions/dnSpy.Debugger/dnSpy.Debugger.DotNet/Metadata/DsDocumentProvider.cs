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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract class DsDocumentProvider {
		public abstract IEnumerable<DsDocumentInfo> DocumentInfos { get; }
	}

	struct DsDocumentInfo {
		public IDsDocument Document { get; }
		public ModuleId Id { get; }
		public bool IsActive { get; }
		public DsDocumentInfo(IDsDocument document, ModuleId id, bool isActive) {
			Document = document ?? throw new ArgumentNullException(nameof(document));
			Id = id;
			IsActive = isActive;
		}
	}

	[Export(typeof(DsDocumentProvider))]
	sealed class DsDocumentProviderImpl : DsDocumentProvider {
		readonly IDsDocumentService documentService;
		readonly Lazy<DbgDocumentInfoProvider>[] dbgDocumentInfoProviders;

		[ImportingConstructor]
		DsDocumentProviderImpl(IDsDocumentService documentService, [ImportMany] IEnumerable<Lazy<DbgDocumentInfoProvider>> dbgDocumentInfoProviders) {
			this.documentService = documentService;
			this.dbgDocumentInfoProviders = dbgDocumentInfoProviders.ToArray();
		}

		HashSet<IDsDocument> Documents {
			get {
				var hash = new HashSet<IDsDocument>();
				foreach (var d in documentService.GetDocuments()) {
					hash.Add(d);
					foreach (var c in d.Children)
						hash.Add(c);
				}

				return hash;
			}
		}

		public override IEnumerable<DsDocumentInfo> DocumentInfos {
			get {
				foreach (var doc in Documents) {
					var info = GetDocumentInfo(doc);
					if (info != null)
						yield return info.Value;
				}
			}
		}

		DsDocumentInfo? GetDocumentInfo(IDsDocument doc) {
			foreach (var lz in dbgDocumentInfoProviders) {
				var info = lz.Value.TryGetFileInfo(doc);
				if (info != null)
					return new DsDocumentInfo(doc, info.Value.Id, info.Value.IsActive);
			}
			var mod = doc.ModuleDef;
			if (mod != null && File.Exists(mod.Location))
				return new DsDocumentInfo(doc, ModuleId.CreateFromFile(mod), isActive: true);
			return null;
		}
	}
}
