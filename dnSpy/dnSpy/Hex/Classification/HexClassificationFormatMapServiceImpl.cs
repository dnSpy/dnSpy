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
using DSTC = dnSpy.Text.Classification;
using TC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification {
	[Export(typeof(HexClassificationFormatMapService))]
	sealed class HexClassificationFormatMapServiceImpl : HexClassificationFormatMapService {
		readonly DSTC.IDsClassificationFormatMapService dsClassificationFormatMapService;

		[ImportingConstructor]
		HexClassificationFormatMapServiceImpl(DSTC.IDsClassificationFormatMapService dsClassificationFormatMapService) {
			this.dsClassificationFormatMapService = dsClassificationFormatMapService;
		}

		public override TC.IClassificationFormatMap GetClassificationFormatMap(string category) => dsClassificationFormatMapService.GetClassificationFormatMap(category);
		public override TC.IClassificationFormatMap GetClassificationFormatMap(HexView hexView) => dsClassificationFormatMapService.GetClassificationFormatMap(hexView);
	}
}
