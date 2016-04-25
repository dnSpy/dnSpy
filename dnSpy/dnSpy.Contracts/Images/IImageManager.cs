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

using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image manager
	/// </summary>
	public interface IImageManager {
		/// <summary>
		/// Returns an image
		/// </summary>
		/// <param name="asm">Assembly of image</param>
		/// <param name="icon">Name of image, without the .png extension. Must be in the images/ folder</param>
		/// <param name="bgType">Background type</param>
		/// <returns></returns>
		BitmapSource GetImage(Assembly asm, string icon, BackgroundType bgType);

		/// <summary>
		/// Returns an image
		/// </summary>
		/// <param name="asm">Assembly of image</param>
		/// <param name="icon">Name of image, without the .png extension. Must be in the images/ folder</param>
		/// <param name="bgColor">Background color</param>
		/// <returns></returns>
		BitmapSource GetImage(Assembly asm, string icon, Color bgColor);
	}
}
