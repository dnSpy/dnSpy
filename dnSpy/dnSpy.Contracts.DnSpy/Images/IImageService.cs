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

using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image service
	/// </summary>
	public interface IImageService {
		/// <summary>
		/// Returns a 16x16 image
		/// </summary>
		/// <param name="imageReference">Image reference</param>
		/// <param name="bgType">Background type</param>
		/// <returns></returns>
		BitmapSource GetImage(ImageReference imageReference, BackgroundType bgType);

		/// <summary>
		/// Returns a 16x16 image
		/// </summary>
		/// <param name="imageReference">Image reference</param>
		/// <param name="bgColor">Background color or null to not modify the image</param>
		/// <returns></returns>
		BitmapSource GetImage(ImageReference imageReference, Color? bgColor);

		/// <summary>
		/// Returns an image
		/// </summary>
		/// <param name="imageReference">Image reference</param>
		/// <param name="options">Image options</param>
		/// <returns></returns>
		BitmapSource GetImage(ImageReference imageReference, ImageOptions options);
	}
}
