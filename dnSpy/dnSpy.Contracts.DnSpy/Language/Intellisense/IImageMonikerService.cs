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
using Microsoft.VisualStudio.Imaging.Interop;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Converts <see cref="ImageMoniker"/>s to and from <see cref="ImageReference"/>s
	/// </summary>
	public interface IImageMonikerService {
		/// <summary>
		/// Converts <paramref name="imageReference"/> to a <see cref="ImageMoniker"/>
		/// </summary>
		/// <param name="imageReference">Image reference</param>
		/// <returns></returns>
		ImageMoniker ToImageMoniker(ImageReference imageReference);

		/// <summary>
		/// Converts <paramref name="imageMoniker"/> to a <see cref="ImageReference"/>
		/// </summary>
		/// <param name="imageMoniker">Image moniker</param>
		/// <returns></returns>
		ImageReference ToImageReference(ImageMoniker imageMoniker);
	}
}
