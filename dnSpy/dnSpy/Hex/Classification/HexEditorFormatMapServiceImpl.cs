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
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using Microsoft.VisualStudio.Text.Classification;
using TC = dnSpy.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexEditorFormatMapService))]
	sealed class HexEditorFormatMapServiceImpl : HexEditorFormatMapService {
		readonly TC.IDsEditorFormatMapService dsEditorFormatMapService;

		[ImportingConstructor]
		HexEditorFormatMapServiceImpl(TC.IDsEditorFormatMapService dsEditorFormatMapService) {
			this.dsEditorFormatMapService = dsEditorFormatMapService;
		}

		public override IEditorFormatMap GetEditorFormatMap(HexView view) => dsEditorFormatMapService.GetEditorFormatMap(view);
		public override IEditorFormatMap GetEditorFormatMap(string category) => dsEditorFormatMapService.GetEditorFormatMap(category);
	}
}
