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
using System.Linq;
using System.Reflection;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	sealed class DocumentList {
		public const string DEFAULT_NAME = "(Default)";
		const string DOCUMENTLIST_NAME_ATTR = "name";
		const string DOCUMENT_SECTION = "File";

		public string Name { get; }
		public List<DsDocumentInfo> Documents { get; }

		public DocumentList(string name) {
			Documents = new List<DsDocumentInfo>();
			Name = name;
		}

		public DocumentList(DefaultDocumentList defaultList) {
			Documents = new List<DsDocumentInfo>(defaultList.Documents);
			Name = defaultList.Name;
		}

		public static DocumentList Create(ISettingsSection section) {
			var documentList = new DocumentList(section.Attribute<string>(DOCUMENTLIST_NAME_ATTR));
			foreach (var documentSect in section.SectionsWithName(DOCUMENT_SECTION)) {
				var info = DsDocumentInfoSerializer.TryLoad(documentSect);
				if (info != null)
					documentList.Documents.Add(info.Value);
			}
			return documentList;
		}

		public void Save(ISettingsSection section) {
			section.Attribute(DOCUMENTLIST_NAME_ATTR, Name);
			foreach (var info in Documents)
				DsDocumentInfoSerializer.Save(section.CreateSection(DOCUMENT_SECTION), info);
		}

		public void Update(IEnumerable<IDsDocument> documents) {
			Documents.Clear();
			foreach (var d in documents) {
				if (d.IsAutoLoaded)
					continue;
				var info = d.SerializedDocument;
				if (info != null)
					Documents.Add(info.Value);
			}
		}

		void AddGacDocument(string asmFullName) => Documents.Add(DsDocumentInfo.CreateGacDocument(asmFullName));
		void AddDocument(Assembly asm) => Documents.Add(DsDocumentInfo.CreateDocument(asm.Location));

		public void AddDefaultDocuments() {
			AddDocument(typeof(int).Assembly);
			AddDocument(typeof(Uri).Assembly);
			AddDocument(typeof(Enumerable).Assembly);
			AddDocument(typeof(System.Xml.XmlDocument).Assembly);
			AddDocument(typeof(System.Windows.Markup.MarkupExtension).Assembly);
			AddDocument(typeof(System.Windows.Rect).Assembly);
			AddDocument(typeof(System.Windows.UIElement).Assembly);
			AddDocument(typeof(System.Windows.FrameworkElement).Assembly);
			AddDocument(typeof(dnlib.DotNet.ModuleDefMD).Assembly);
			AddDocument(GetType().Assembly);
		}
	}
}
