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
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Used by taggers to declare which tag types they support
	/// </summary>
	public sealed class HexTagTypeAttribute : VSUTIL.MultipleBaseMetadataAttribute {
		/// <summary>
		/// Gets the tag type
		/// </summary>
		public Type TagTypes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tagType">Tag type; it must derive from <see cref="HexTag"/></param>
		public HexTagTypeAttribute(Type tagType) {
			if (tagType == null)
				throw new ArgumentNullException(nameof(tagType));
			if (!typeof(HexTag).IsAssignableFrom(tagType))
				throw new ArgumentOutOfRangeException(nameof(tagType));
			TagTypes = tagType;
		}
	}
}
