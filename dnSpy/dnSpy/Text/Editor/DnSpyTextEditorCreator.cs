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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Editor {
	/// <summary>
	/// Creates <see cref="DnSpyTextEditor"/> instances
	/// </summary>
	interface IDnSpyTextEditorCreator {
		/// <summary>
		/// Create new <see cref="DnSpyTextEditor"/> instances
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		DnSpyTextEditor Create(DnSpyTextEditorOptions options);
	}

	[Export(typeof(IDnSpyTextEditorCreator))]
	sealed class DnSpyTextEditorCreator : IDnSpyTextEditorCreator {
		readonly IThemeManager themeManager;
		readonly IWpfCommandManager wpfCommandManager;
		readonly IMenuManager menuManager;
		readonly ITextEditorSettings textEditorSettings;
		readonly ITextSnapshotColorizerCreator textBufferColorizerCreator;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IContentTypeRegistryService contentTypeRegistryService;

		[ImportingConstructor]
		DnSpyTextEditorCreator(IThemeManager themeManager, IWpfCommandManager wpfCommandManager, IMenuManager menuManager, ITextEditorSettings textEditorSettings, ITextSnapshotColorizerCreator textBufferColorizerCreator, ITextBufferFactoryService textBufferFactoryService, IContentTypeRegistryService contentTypeRegistryService) {
			this.themeManager = themeManager;
			this.wpfCommandManager = wpfCommandManager;
			this.menuManager = menuManager;
			this.textEditorSettings = textEditorSettings;
			this.textBufferColorizerCreator = textBufferColorizerCreator;
			this.textBufferFactoryService = textBufferFactoryService;
			this.contentTypeRegistryService = contentTypeRegistryService;
		}

		public DnSpyTextEditor Create(DnSpyTextEditorOptions options) {
			var textBuffer = options.TextBuffer ?? textBufferFactoryService.CreateTextBuffer(contentTypeRegistryService.GetContentType((object)options.ContentType ?? options.ContentTypeGuid) ?? textBufferFactoryService.TextContentType);
			var textEditor = new DnSpyTextEditor(themeManager, textEditorSettings, textBufferColorizerCreator, textBuffer);

			if (options.MenuGuid != null)
				menuManager.InitializeContextMenu(textEditor, options.MenuGuid.Value, options.CreateGuidObjectsCreator?.Invoke(), new ContextMenuInitializer(textEditor, textEditor));

			return textEditor;
		}
	}
}
