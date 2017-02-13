/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using TC = dnSpy.Text.Classification;

namespace dnSpy.Hex.Classification {
	sealed class HexViewEditorFormatMap : TC.ViewEditorFormatMap {
		readonly HexView hexView;

		public HexViewEditorFormatMap(HexView hexView, TC.EditorFormatMapService editorFormatMapService)
			: base(editorFormatMapService, DefaultWpfHexViewOptions.AppearanceCategoryName) {
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			hexView.Options.OptionChanged += Options_OptionChanged;
			Initialize();
		}

		protected override string GetAppearanceCategory() => hexView.Options.AppearanceCategory();
		protected override void DisposeCore() => hexView.Options.OptionChanged -= Options_OptionChanged;
	}
}
