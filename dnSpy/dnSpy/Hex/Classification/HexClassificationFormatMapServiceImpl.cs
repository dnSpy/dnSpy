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
using TC = dnSpy.Text.Classification;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexClassificationFormatMapService))]
	sealed class HexClassificationFormatMapServiceImpl : HexClassificationFormatMapService {
		readonly TheClassificationFormatMapService theClassificationFormatMapService;

		[ImportingConstructor]
		HexClassificationFormatMapServiceImpl(TheClassificationFormatMapService theClassificationFormatMapService) {
			this.theClassificationFormatMapService = theClassificationFormatMapService;
		}

		public override VSTC.IClassificationFormatMap GetClassificationFormatMap(string category) => theClassificationFormatMapService.GetClassificationFormatMap(category);
		public override VSTC.IClassificationFormatMap GetClassificationFormatMap(HexView hexView) => theClassificationFormatMapService.GetClassificationFormatMap(hexView);

		[Export(typeof(TheClassificationFormatMapService))]
		sealed class TheClassificationFormatMapService : TC.ClassificationFormatMapService {
			[ImportingConstructor]
			TheClassificationFormatMapService(IThemeService themeService, VSTC.IEditorFormatMapService editorFormatMapService, TC.IEditorFormatDefinitionService editorFormatDefinitionService, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService)
			: base(themeService, editorFormatMapService, editorFormatDefinitionService, classificationTypeRegistryService) {
			}

			public VSTC.IClassificationFormatMap GetClassificationFormatMap(HexView hexView) {
				if (hexView == null)
					throw new ArgumentNullException(nameof(hexView));
				return hexView.Properties.GetOrCreateSingletonProperty(typeof(TC.ViewClassificationFormatMap), () => CreateViewClassificationFormatMap(hexView));
			}

			TC.ViewClassificationFormatMap CreateViewClassificationFormatMap(HexView hexView) {
				hexView.Closed += HexView_Closed;
				return new HexViewClassificationFormatMap(this, hexView);
			}

			static void HexView_Closed(object sender, EventArgs e) {
				var hexView = (HexView)sender;
				hexView.Closed -= HexView_Closed;
				var map = (TC.ViewClassificationFormatMap)hexView.Properties[typeof(TC.ViewClassificationFormatMap)];
				hexView.Properties.RemoveProperty(typeof(TC.ViewClassificationFormatMap));
				map.Dispose();
			}
		}
	}
}
