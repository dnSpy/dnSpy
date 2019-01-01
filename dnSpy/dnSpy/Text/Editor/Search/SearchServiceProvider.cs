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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor.Search {
	interface ISearchServiceProvider {
		ISearchService Get(IWpfTextView wpfTextView);
	}

	[Export(typeof(ISearchServiceProvider))]
	sealed class SearchServiceProvider : ISearchServiceProvider {
		readonly ITextSearchService2 textSearchService2;
		readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;
		readonly ISearchSettings searchSettings;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<IReplaceListenerProvider>[] replaceListenerProviders;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		SearchServiceProvider(ITextSearchService2 textSearchService2, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService, ISearchSettings searchSettings, IMessageBoxService messageBoxService, [ImportMany] IEnumerable<Lazy<IReplaceListenerProvider>> replaceListenerProviders, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.textSearchService2 = textSearchService2;
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
			this.searchSettings = searchSettings;
			this.messageBoxService = messageBoxService;
			this.replaceListenerProviders = replaceListenerProviders.ToArray();
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public ISearchService Get(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(SearchService),
				() => new SearchService(wpfTextView, textSearchService2, searchSettings, messageBoxService,
					textStructureNavigatorSelectorService.GetTextStructureNavigator(wpfTextView.TextBuffer),
					replaceListenerProviders, editorOperationsFactoryService));
		}
	}
}
