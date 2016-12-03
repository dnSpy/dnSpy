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
using System.Diagnostics;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	[Export]
	sealed class DocumentListService {
		const string CURRENT_LIST_ATTR = "name";
		const string DOCUMENT_LIST_SECTION = "FileList";

		public DocumentList SelectedDocumentList {
			get {
				Debug.Assert(hasLoaded);
				if (documentsList.Count == 0)
					CreateDefaultList();
				if ((uint)selectedIndex >= (uint)documentsList.Count)
					selectedIndex = 0;
				return documentsList[selectedIndex];
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				int index = documentsList.IndexOf(value);
				if (index < 0)
					throw new InvalidOperationException();
				selectedIndex = index;
			}
		}
		int selectedIndex;

		public DocumentList[] DocumentLists => documentsList.ToArray();
		readonly List<DocumentList> documentsList;

		DocumentListService() {
			documentsList = new List<DocumentList>();
			selectedIndex = -1;
			hasLoaded = false;
		}

		void CreateDefaultList() {
			var fl = new DocumentList(DocumentList.DEFAULT_NAME);
			fl.AddDefaultDocuments();
			documentsList.Add(fl);
		}

		int IndexOf(string name) {
			for (int i = 0; i < documentsList.Count; i++) {
				if (StringComparer.Ordinal.Equals(documentsList[i].Name, name))
					return i;
			}
			return -1;
		}

		void SelectList(string name) => selectedIndex = IndexOf(name);

		public void Load(ISettingsSection section) {
			var listName = section.Attribute<string>(CURRENT_LIST_ATTR);
			var names = new HashSet<string>(StringComparer.Ordinal);
			foreach (var listSection in section.SectionsWithName(DOCUMENT_LIST_SECTION)) {
				var documentList = DocumentList.Create(listSection);
				if (names.Contains(documentList.Name))
					continue;
				documentsList.Add(documentList);
			}
			hasLoaded = true;

			SelectList(listName);
		}
		bool hasLoaded;

		public void Save(ISettingsSection section) {
			section.Attribute(CURRENT_LIST_ATTR, SelectedDocumentList.Name);
			foreach (var documentList in documentsList)
				documentList.Save(section.CreateSection(DOCUMENT_LIST_SECTION));
		}

		public bool Remove(DocumentList documentList) {
			if (documentList == SelectedDocumentList)
				return false;
			var selected = SelectedDocumentList;
			documentsList.Remove(documentList);
			selectedIndex = documentsList.IndexOf(selected);
			Debug.Assert(selectedIndex >= 0);
			Debug.Assert(SelectedDocumentList == selected);
			return true;
		}

		public void Add(DocumentList documentList) {
			if (documentsList.Contains(documentList))
				return;
			documentsList.Add(documentList);
		}
	}
}
