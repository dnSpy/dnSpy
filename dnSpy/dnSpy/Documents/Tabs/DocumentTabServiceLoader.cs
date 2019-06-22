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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	[ExportDsLoader(Order = LoaderConstants.ORDER_DOCUMENTTABMANAGER)]
	sealed class DocumentTabServiceLoader : IDsLoader {
		static readonly Guid SETTINGS_GUID = new Guid("53863C11-DF95-43F2-8F86-5E9DFCCE6893");
		const string DOCUMENT_LISTS_SECTION = "FileLists";
		const string TABGROUPWINDOW_SECTION = "TabGroupWindow";

		readonly DocumentTabSerializer documentTabSerializer;
		readonly DocumentTabService documentTabService;
		readonly IDocumentListLoader documentListLoader;

		[ImportingConstructor]
		DocumentTabServiceLoader(DocumentTabSerializer documentTabSerializer, DocumentTabService documentTabService, IDocumentListLoader documentListLoader) {
			this.documentTabSerializer = documentTabSerializer;
			this.documentTabService = documentTabService;
			this.documentListLoader = documentListLoader;
		}

		public IEnumerable<object?> Load(ISettingsService settingsService, IAppCommandLineArgs args) {
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);

			foreach (var o in documentListLoader.Load(section.GetOrCreateSection(DOCUMENT_LISTS_SECTION), args.LoadFiles))
				yield return o;

			if (args.LoadFiles) {
				var tgws = new List<SerializedTabGroupWindow>();
				var tgwsHash = new HashSet<string>(StringComparer.Ordinal);
				foreach (var tgwSection in section.SectionsWithName(TABGROUPWINDOW_SECTION)) {
					var tgw = SerializedTabGroupWindow.Load(tgwSection);
					yield return null;
					if (tgwsHash.Contains(tgw.Name))
						continue;
					tgws.Add(tgw);
				}

				// The documents are added to the treeview with a slight delay. Make sure the documents
				// have been added to the TV or the node lookup code will fail to find the nodes it needs.
				yield return LoaderConstants.Delay;

				foreach (var o in documentTabSerializer.Restore(tgws))
					yield return o;
			}

			documentTabService.OnTabsLoaded();
		}

		public void OnAppLoaded() { }

		public void Save(ISettingsService settingsService) {
			var section = settingsService.RecreateSection(SETTINGS_GUID);
			documentListLoader.Save(section.GetOrCreateSection(DOCUMENT_LISTS_SECTION));

			if (documentTabService.Settings.RestoreTabs) {
				foreach (var tgw in documentTabSerializer.SaveTabs())
					tgw.Save(section.CreateSection(TABGROUPWINDOW_SECTION));
			}
		}
	}
}
