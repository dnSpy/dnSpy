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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;
using dnSpy.Contracts.Images;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexGlyphFactoryProvider))]
	[VSUTIL.Name(PredefinedHexGlyphFactoryProviderNames.HexImageReference)]
	[HexTagType(typeof(HexImageReferenceTag))]
	sealed class HexImageReferenceGlyphFactoryProvider : HexGlyphFactoryProvider {
		public override HexGlyphFactory GetGlyphFactory(WpfHexView view, WpfHexViewMargin margin) => new HexImageReferenceGlyphFactory();
	}

	sealed class HexImageReferenceGlyphFactory : HexGlyphFactory {
		public override UIElement GenerateGlyph(WpfHexViewLine line, HexGlyphTag tag) {
			var glyphTag = tag as HexImageReferenceTag;
			if (glyphTag == null)
				return null;

			const double DEFAULT_IMAGE_LENGTH = 16;
			const double EXTRA_LENGTH = 2;
			double imageLength = Math.Min(DEFAULT_IMAGE_LENGTH, line.Height + EXTRA_LENGTH);

			var image = new DsImage {
				Width = imageLength,
				Height = imageLength,
				ImageReference = glyphTag.ImageReference,
			};
			Panel.SetZIndex(image, glyphTag.ZIndex);
			return image;
		}
	}
}
