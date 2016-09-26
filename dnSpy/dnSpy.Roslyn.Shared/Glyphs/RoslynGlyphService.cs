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
using System.Windows.Media;
using dnSpy.Contracts.Images;
using dnSpy.Roslyn.Internal;

namespace dnSpy.Roslyn.Shared.Glyphs {
	interface IRoslynGlyphService {
		ImageSource GetImage(Glyph glyph, BackgroundType bgType);
		ImageSource GetImage(Glyph glyph, Color? color);
	}

	[Export(typeof(IRoslynGlyphService))]
	sealed class RoslynGlyphService : IRoslynGlyphService {
		readonly IImageService imageService;

		[ImportingConstructor]
		RoslynGlyphService(IImageService imageService) {
			this.imageService = imageService;
		}

		public ImageSource GetImage(Glyph glyph, BackgroundType bgType) {
			var imgRef = glyph.GetImageReference();
			return imgRef == null ? null : imageService.GetImage(imgRef.Value, bgType);
		}

		public ImageSource GetImage(Glyph glyph, Color? color) {
			var imgRef = glyph.GetImageReference();
			return imgRef == null ? null : imageService.GetImage(imgRef.Value, color);
		}
	}
}
