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

using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// <see cref="ImageReference"/> tag
	/// </summary>
	public class HexImageReferenceTag : HexGlyphTag {
		/// <summary>
		/// Gets the image reference
		/// </summary>
		public ImageReference ImageReference { get; }

		/// <summary>
		/// Gets the Z-index, eg. <see cref="HexMarkerServiceZIndexes.CurrentValue"/>
		/// </summary>
		public int ZIndex { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageReference">Image reference</param>
		/// <param name="zIndex">Z-index, eg. <see cref="HexMarkerServiceZIndexes.CurrentValue"/></param>
		public HexImageReferenceTag(ImageReference imageReference, int zIndex) {
			ImageReference = imageReference;
			ZIndex = zIndex;
		}
	}
}
