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
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Settings.AppearanceCategory;
using TC = dnSpy.Text.Classification;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexEditorFormatMapService))]
	sealed class HexEditorFormatMapServiceImpl : HexEditorFormatMapService {
		readonly TheEditorFormatMapService theEditorFormatMapService;

		[ImportingConstructor]
		HexEditorFormatMapServiceImpl(TheEditorFormatMapService theEditorFormatMapService) {
			this.theEditorFormatMapService = theEditorFormatMapService;
		}

		public override VSTC.IEditorFormatMap GetEditorFormatMap(HexView view) => theEditorFormatMapService.GetEditorFormatMap(view);
		public override VSTC.IEditorFormatMap GetEditorFormatMap(string category) => theEditorFormatMapService.GetEditorFormatMap(category);

		[Export(typeof(TheEditorFormatMapService))]
		sealed class TheEditorFormatMapService : TC.EditorFormatMapService {
			[ImportingConstructor]
			public TheEditorFormatMapService(IThemeService themeService, ITextAppearanceCategoryService textAppearanceCategoryService, TC.IEditorFormatDefinitionService editorFormatDefinitionService)
				: base(themeService, textAppearanceCategoryService, editorFormatDefinitionService) {
			}

			public VSTC.IEditorFormatMap GetEditorFormatMap(HexView view) {
				if (view == null)
					throw new ArgumentNullException(nameof(view));
				return view.Properties.GetOrCreateSingletonProperty(typeof(TC.ViewEditorFormatMap), () => CreateViewEditorFormatMap(view));
			}

			TC.ViewEditorFormatMap CreateViewEditorFormatMap(HexView hexView) {
				hexView.Closed += HexView_Closed;
				return new HexViewEditorFormatMap(hexView, this);
			}

			void HexView_Closed(object sender, EventArgs e) {
				var hexView = (HexView)sender;
				hexView.Closed -= HexView_Closed;
				var map = (TC.ViewEditorFormatMap)hexView.Properties[typeof(TC.ViewEditorFormatMap)];
				hexView.Properties.RemoveProperty(typeof(TC.ViewEditorFormatMap));
				map.Dispose();
			}
		}
	}
}
