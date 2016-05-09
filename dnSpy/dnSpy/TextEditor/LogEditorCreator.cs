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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.TextEditor {
	[Export, Export(typeof(ILogEditorCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class LogEditorCreator : ILogEditorCreator {
		readonly IThemeManager themeManager;
		readonly IWpfCommandManager wpfCommandManager;
		readonly IMenuManager menuManager;
		readonly ITextEditorSettings textEditorSettings;
		readonly ITextSnapshotColorizerCreator textBufferColorizerCreator;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextBufferFactoryService textBufferFactoryService;

		[ImportingConstructor]
		LogEditorCreator(IThemeManager themeManager, IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorSettings textEditorSettings, ITextSnapshotColorizerCreator textBufferColorizerCreator, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService) {
			this.themeManager = themeManager;
			this.wpfCommandManager = wpfCommandManager;
			this.menuManager = menuManager;
			this.textEditorSettings = textEditorSettings;
			this.textBufferColorizerCreator = textBufferColorizerCreator;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textBufferFactoryService = textBufferFactoryService;
		}

		public ILogEditorUI Create(LogEditorOptions options) =>
			new LogEditorUI(options, themeManager, wpfCommandManager, menuManager, textEditorSettings, textBufferColorizerCreator, contentTypeRegistryService, textBufferFactoryService);
	}
}
