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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract class DsDocumentProvider {
		public abstract IAssemblyResolver AssemblyResolver { get; }
		public abstract IEnumerable<IDsDocument> Documents { get; }
		public abstract IEnumerable<DocumentInfo> DocumentInfos { get; }
		public abstract IDsDocument Find(IDsDocumentNameKey key);
		public abstract IDsDocument GetOrAdd(IDsDocument document);
	}

	readonly struct DocumentInfo {
		public IDsDocument Document { get; }
		public ModuleId Id { get; }
		public bool IsActive { get; }
		public DocumentInfo(IDsDocument document, ModuleId id, bool isActive) {
			Document = document ?? throw new ArgumentNullException(nameof(document));
			Id = id;
			IsActive = isActive;
		}
	}

	[Export(typeof(DsDocumentProvider))]
	sealed class DsDocumentProviderImpl : DsDocumentProvider {
		readonly IDsDocumentService documentService;

		[ImportingConstructor]
		DsDocumentProviderImpl(IDsDocumentService documentService) => this.documentService = documentService;

		public override IAssemblyResolver AssemblyResolver => documentService.AssemblyResolver;

		public override IEnumerable<IDsDocument> Documents {
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

		public override IEnumerable<DocumentInfo> DocumentInfos {
			get {
				foreach (var doc in Documents) {
					var info = GetDocumentInfo(doc);
					if (info != null)
						yield return info.Value;
				}
			}
		}

		DocumentInfo? GetDocumentInfo(IDsDocument doc) {
			var dnDoc = doc as DsDotNetDocumentBase;
			if (dnDoc is IModuleIdHolder idHolder)
				return new DocumentInfo(doc, idHolder.ModuleId, dnDoc.IsActive);
			var mod = doc.ModuleDef;
			if (mod != null && File.Exists(mod.Location))
				return new DocumentInfo(doc, ModuleId.CreateFromFile(mod), isActive: dnDoc?.IsActive ?? true);
			return null;
		}

		public override IDsDocument Find(IDsDocumentNameKey key) => documentService.Find(key);
		public override IDsDocument GetOrAdd(IDsDocument document) => documentService.GetOrAdd(document);
	}
}
