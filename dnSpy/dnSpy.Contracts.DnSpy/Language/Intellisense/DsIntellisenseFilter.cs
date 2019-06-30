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

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Completion filter
	/// </summary>
	public class DsIntellisenseFilter {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageReference">Image</param>
		/// <param name="toolTip"></param>
		/// <param name="accessKey"></param>
		/// <param name="isChecked"></param>
		/// <param name="isEnabled"></param>
		public DsIntellisenseFilter(ImageReference imageReference, string? toolTip, string? accessKey, bool isChecked, bool isEnabled) {
			ImageReference = imageReference;
			ToolTip = toolTip;
			AccessKey = accessKey;
			IsChecked = isChecked;
			IsEnabled = isEnabled;
		}

		/// <summary>
		/// Gets the image if any
		/// </summary>
		public ImageReference ImageReference { get; }

		/// <summary>
		/// Tooltip or null
		/// </summary>
		public string? ToolTip { get; }

		/// <summary>
		/// Access key or null
		/// </summary>
		public string? AccessKey { get; }

		/// <summary>
		/// true if it's checked
		/// </summary>
		public bool IsChecked { get; set; }

		/// <summary>
		/// true if it's enabled
		/// </summary>
		public bool IsEnabled { get; set; }
	}
}
