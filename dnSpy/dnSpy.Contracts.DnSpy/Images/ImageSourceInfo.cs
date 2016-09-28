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

using System.Windows;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image source info
	/// </summary>
	public struct ImageSourceInfo {
		/// <summary>
		/// Any width
		/// </summary>
		public static readonly int AnyWidth = 0;

		/// <summary>
		/// Any size
		/// </summary>
		public static readonly int AnyHeight = 0;

		/// <summary>
		/// Any size
		/// </summary>
		public static readonly Size AnySize = new Size(AnyWidth, AnyHeight);

		/// <summary>
		/// URI of image
		/// </summary>
		public string Uri { get; set; }

		/// <summary>
		/// Size of image in pixels
		/// </summary>
		public Size Size { get; set; }
	}
}
