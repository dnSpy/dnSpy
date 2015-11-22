/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Files.Tabs.TextEditor {
	[ExportFileTabUIContextCreator(Order = TabsConstants.ORDER_TEXTEDITORUICONTEXTCREATOR)]
	sealed class TextEditorUIContextCreator : IFileTabUIContextCreator {
		readonly IThemeManager themeManager;

		[ImportingConstructor]
		TextEditorUIContextCreator(IThemeManager themeManager) {
			this.themeManager = themeManager;
		}

		public T Create<T>() where T : class, IFileTabUIContext {
			if (typeof(T) == typeof(ITextEditorUIContext)) {
				var tec = new TextEditorControl(themeManager);
				return (T)(IFileTabUIContext)new TextEditorUIContext(tec);
			}
			return null;
		}
	}
}
