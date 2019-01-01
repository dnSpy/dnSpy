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

using dnSpy.Contracts.Images;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Completion icon
	/// </summary>
	public class DsCompletionIcon : CompletionIcon {
		/// <summary>
		/// Gets the image
		/// </summary>
		public virtual ImageReference ImageReference { get; }

		/// <summary>
		/// true to theme the image by changing it so it matches the background color
		/// </summary>
		public virtual bool ThemeImage { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageReference">Image</param>
		/// <param name="themeImage">true to theme the image by changing it so it matches the background color</param>
		public DsCompletionIcon(ImageReference imageReference, bool themeImage = false) {
			ImageReference = imageReference;
			ThemeImage = themeImage;
		}
	}
}
