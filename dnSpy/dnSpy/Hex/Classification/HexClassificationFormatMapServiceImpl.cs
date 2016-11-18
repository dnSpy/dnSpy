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
using DSTC = dnSpy.Text.Classification;
using TC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexClassificationFormatMapService))]
	sealed class HexClassificationFormatMapServiceImpl : HexClassificationFormatMapService {
		readonly TheClassificationFormatMapService theClassificationFormatMapService;

		[ImportingConstructor]
		HexClassificationFormatMapServiceImpl(TheClassificationFormatMapService theClassificationFormatMapService) {
			this.theClassificationFormatMapService = theClassificationFormatMapService;
		}

		public override TC.IClassificationFormatMap GetClassificationFormatMap(string category) => theClassificationFormatMapService.GetClassificationFormatMap(category);
		public override TC.IClassificationFormatMap GetClassificationFormatMap(HexView hexView) => theClassificationFormatMapService.GetClassificationFormatMap(hexView);

		[Export(typeof(TheClassificationFormatMapService))]
		sealed class TheClassificationFormatMapService : DSTC.ClassificationFormatMapService {
			[ImportingConstructor]
			TheClassificationFormatMapService(IThemeService themeService, TC.IEditorFormatMapService editorFormatMapService, DSTC.IEditorFormatDefinitionService editorFormatDefinitionService, TC.IClassificationTypeRegistryService classificationTypeRegistryService)
			: base(themeService, editorFormatMapService, editorFormatDefinitionService, classificationTypeRegistryService) {
			}

			public TC.IClassificationFormatMap GetClassificationFormatMap(HexView hexView) {
				if (hexView == null)
					throw new ArgumentNullException(nameof(hexView));
				return hexView.Properties.GetOrCreateSingletonProperty(typeof(DSTC.ViewClassificationFormatMap), () => CreateViewClassificationFormatMap(hexView));
			}

			DSTC.ViewClassificationFormatMap CreateViewClassificationFormatMap(HexView hexView) {
				hexView.Closed += HexView_Closed;
				return new HexViewClassificationFormatMap(this, hexView);
			}

			static void HexView_Closed(object sender, EventArgs e) {
				var hexView = (HexView)sender;
				hexView.Closed -= HexView_Closed;
				var map = (DSTC.ViewClassificationFormatMap)hexView.Properties[typeof(DSTC.ViewClassificationFormatMap)];
				hexView.Properties.RemoveProperty(typeof(DSTC.ViewClassificationFormatMap));
				map.Dispose();
			}
		}
	}
}
