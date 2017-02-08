/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Controls {
	static class ZoomSelector {
		public const double MinZoomLevel = 20;
		public const double MaxZoomLevel = 400;
		static readonly double[] zoomLevels = new double[] {
			// These are rounded from 100 * 1.1^(-x), the algorithm used by the text editor
			MinZoomLevel,
			22,
			24,
			26,
			29,
			32,
			35,
			39,
			42,
			47,
			50,
			56,
			62,
			70,
			75,
			83,
			91,
			100,
			// Similar to the original text editor values: 100 * 1.1^x but trying to use
			// better values so images aren't blurry (eg. instead of 146.41%, use 150%)
			110,
			125,
			140,
			150,
			175,
			200,
			225,
			250,
			275,
			300,
			350,
			MaxZoomLevel,
		};

		/// <summary>
		/// Returns the next zoomed in zoom level
		/// </summary>
		/// <param name="zoomLevel">Zoom level (100 if 100% zoom)</param>
		/// <returns></returns>
		public static double ZoomIn(double zoomLevel) => zoomLevels[GetNextZoomInIndex(zoomLevel)];

		static int GetNextZoomInIndex(double zoomLevel) {
			if (zoomLevel < MinZoomLevel)
				return 0;
			for (int i = 1; i < zoomLevels.Length; i++) {
				if (zoomLevels[i - 1] <= zoomLevel && zoomLevel < zoomLevels[i])
					return i;
			}
			return zoomLevels.Length - 1;
		}

		/// <summary>
		/// Returns the next zoomed out zoom level
		/// </summary>
		/// <param name="zoomLevel">Zoom level (100 if 100% zoom)</param>
		/// <returns></returns>
		public static double ZoomOut(double zoomLevel) => zoomLevels[GetNextZoomOutIndex(zoomLevel)];

		static int GetNextZoomOutIndex(double zoomLevel) {
			if (zoomLevel > MaxZoomLevel)
				return zoomLevels.Length - 1;
			for (int i = zoomLevels.Length - 2; i >= 0; i--) {
				if (zoomLevels[i] < zoomLevel && zoomLevel <= zoomLevels[i + 1])
					return i;
			}
			return 0;
		}
	}
}
