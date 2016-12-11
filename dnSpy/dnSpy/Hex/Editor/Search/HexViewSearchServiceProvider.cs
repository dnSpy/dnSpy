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
using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Editor.Search {
	abstract class HexViewSearchServiceProvider {
		public abstract HexViewSearchService Get(WpfHexView wpfHexView);
	}

	[Export(typeof(HexViewSearchServiceProvider))]
	sealed class HexViewSearchServiceProviderImpl : HexViewSearchServiceProvider {
		readonly HexSearchServiceFactory hexSearchServiceFactory;
		readonly SearchSettings searchSettings;
		readonly IMessageBoxService messageBoxService;
		readonly HexEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		HexViewSearchServiceProviderImpl(HexSearchServiceFactory hexSearchServiceFactory, SearchSettings searchSettings, IMessageBoxService messageBoxService, HexEditorOperationsFactoryService editorOperationsFactoryService) {
			this.hexSearchServiceFactory = hexSearchServiceFactory;
			this.searchSettings = searchSettings;
			this.messageBoxService = messageBoxService;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public override HexViewSearchService Get(WpfHexView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(HexViewSearchService),
				() => new HexViewSearchServiceImpl(wpfTextView, hexSearchServiceFactory, searchSettings, messageBoxService, editorOperationsFactoryService));
		}
	}
}
