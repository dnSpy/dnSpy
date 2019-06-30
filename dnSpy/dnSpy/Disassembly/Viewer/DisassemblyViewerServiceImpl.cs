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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Text;
using dnSpy.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Disassembly.Viewer {
	[Export(typeof(DisassemblyViewerService))]
	sealed class DisassemblyViewerServiceImpl : DisassemblyViewerService {
		readonly IDocumentTabService documentTabService;
		readonly IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider;
		readonly IContentType asmContentType;
		readonly DisassemblyViewerServiceSettingsImpl settings;

		public override DisassemblyViewerServiceSettings Settings => settings;

		[ImportingConstructor]
		DisassemblyViewerServiceImpl(IDocumentTabService documentTabService, IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IContentTypeRegistryService contentTypeRegistryService) {
			this.documentTabService = documentTabService;
			this.documentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			asmContentType = contentTypeRegistryService.GetContentType(ContentTypes.Assembler);
			settings = new DisassemblyViewerServiceSettingsImpl();
		}

		public override void Show(DisassemblyContentProvider contentProvider, bool newTab) {
			if (contentProvider is null)
				throw new ArgumentNullException(nameof(contentProvider));

			IDocumentTab tab;
			if (newTab)
				tab = documentTabService.OpenEmptyTab();
			else
				tab = documentTabService.GetOrCreateActiveTab();
			var tabContent = new DisassemblyDocumentTabContent(documentViewerContentFactoryProvider, asmContentType, contentProvider);
			tab.Show(tabContent, null, null);
		}
	}
}
