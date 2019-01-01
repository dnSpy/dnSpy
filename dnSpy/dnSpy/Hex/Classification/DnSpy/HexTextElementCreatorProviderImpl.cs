/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Classification.DnSpy;
using dnSpy.Contracts.Settings.AppearanceCategory;
using CT = dnSpy.Contracts.Text;
using CTC = dnSpy.Contracts.Text.Classification;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification.DnSpy {
	[Export(typeof(HexTextElementCreatorProvider))]
	sealed class HexTextElementCreatorProviderImpl : HexTextElementCreatorProvider {
		readonly CTC.ITextElementProvider textElementProvider;
		readonly VSTC.IClassificationFormatMapService classificationFormatMapService;

		[ImportingConstructor]
		HexTextElementCreatorProviderImpl(CTC.ITextElementProvider textElementProvider, VSTC.IClassificationFormatMapService classificationFormatMapService) {
			this.textElementProvider = textElementProvider;
			this.classificationFormatMapService = classificationFormatMapService;
		}

		public override HexTextElementCreator Create() => Create(CT.ContentTypes.DefaultHexToolTip);
		public override HexTextElementCreator Create(string contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			return new HexTextElementCreatorImpl(textElementProvider, classificationFormatMap, contentType);
		}
	}
}
