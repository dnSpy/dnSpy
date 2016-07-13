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
		readonly ITextEditorSettings textEditorSettings;

		[ImportingConstructor]
		DnSpyTextEditorCreator(IThemeManager themeManager, ITextEditorSettings textEditorSettings) {
			this.themeManager = themeManager;
			this.textEditorSettings = textEditorSettings;
		}

		public DnSpyTextEditor Create(DnSpyTextEditorOptions options) =>
			new DnSpyTextEditor(themeManager, textEditorSettings);
	}
}
