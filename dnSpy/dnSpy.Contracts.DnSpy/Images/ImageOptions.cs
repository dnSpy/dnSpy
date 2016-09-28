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
using System.Windows.Media;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image options
	/// </summary>
	public struct ImageOptions {
		/// <summary>
		/// Background color
		/// </summary>
		public Color? BackgroundColor { get; set; }

		/// <summary>
		/// Background type
		/// </summary>
		public BackgroundType? BackgroundType { get; set; }

		/// <summary>
		/// Image size in logical pixels. 16x16 is used if this is 0x0
		/// </summary>
		public Size LogicalSize { get; set; }
	}
}
