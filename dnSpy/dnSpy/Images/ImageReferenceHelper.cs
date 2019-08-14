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
using dnSpy.Contracts.Images;

namespace dnSpy.Images {
	static class ImageReferenceHelper {
		public static ImageReference? GetImageReference(object item, string? iconName) {
			if (string2.IsNullOrEmpty(iconName))
				return null;
			int colonIndex = iconName.IndexOf(':');
			if (colonIndex >= 0) {
				var type = iconName.Substring(0, colonIndex).Trim();
				iconName = iconName.Substring(colonIndex + 1).Trim();
				if (type.Equals("img", StringComparison.OrdinalIgnoreCase)) {
					if (ImageReference.TryParse(iconName, out var imageReference))
						return imageReference;
					return null;
				}
				return null;
			}
			return new ImageReference(item.GetType().Assembly, iconName);
		}
	}
}
