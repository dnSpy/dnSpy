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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Zoom constants
	/// </summary>
	public static class ZoomConstants {
		/// <summary>
		/// Default zoom
		/// </summary>
		public const double DefaultZoom = 100.0;

		/// <summary>
		/// Minimum zoom
		/// </summary>
		public const double MinZoom = 20.0;

		/// <summary>
		/// Maximum zoom
		/// </summary>
		public const double MaxZoom = 400.0;

		/// <summary>
		/// Zoom scaling factor
		/// </summary>
		public const double ScalingFactor = 1.1;
	}
}
