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

using System;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Line transform
	/// </summary>
	public struct LineTransform {
		/// <summary>
		/// Gets the amount of space required above the text of the line before applying the <see cref="VerticalScale"/> factor.
		/// </summary>
		public double TopSpace { get; }

		/// <summary>
		/// Gets the amount of space required below the text of the line before applying the <see cref="VerticalScale"/> factor.
		/// </summary>
		public double BottomSpace { get; }

		/// <summary>
		/// Gets the x-coordinate of the effective right edge of the line.
		/// </summary>
		public double Right { get; }

		/// <summary>
		/// Gets the vertical scale factor to be applied to the text of the line and the space above and below the line.
		/// </summary>
		public double VerticalScale { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="verticalScale">Vertical scale</param>
		public LineTransform(double verticalScale) {
			if (verticalScale <= 0 || double.IsNaN(verticalScale))
				throw new System.ArgumentOutOfRangeException(nameof(verticalScale));
			TopSpace = 0;
			BottomSpace = 0;
			Right = 0;
			VerticalScale = verticalScale;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="topSpace">Top space</param>
		/// <param name="bottomSpace">Bottom space</param>
		/// <param name="verticalScale">Vertical scale</param>
		public LineTransform(double topSpace, double bottomSpace, double verticalScale) {
			if (double.IsNaN(topSpace))
				throw new ArgumentOutOfRangeException(nameof(topSpace));
			if (double.IsNaN(bottomSpace))
				throw new ArgumentOutOfRangeException(nameof(bottomSpace));
			if (verticalScale <= 0 || double.IsNaN(verticalScale))
				throw new ArgumentOutOfRangeException(nameof(verticalScale));
			TopSpace = topSpace;
			BottomSpace = bottomSpace;
			Right = 0;
			VerticalScale = verticalScale;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="topSpace">Top space</param>
		/// <param name="bottomSpace">Bottom space</param>
		/// <param name="verticalScale">Vertical scale</param>
		/// <param name="right">X coordinate of the right edge</param>
		public LineTransform(double topSpace, double bottomSpace, double verticalScale, double right) {
			if (double.IsNaN(topSpace))
				throw new ArgumentOutOfRangeException(nameof(topSpace));
			if (double.IsNaN(bottomSpace))
				throw new ArgumentOutOfRangeException(nameof(bottomSpace));
			if (verticalScale <= 0 || double.IsNaN(verticalScale))
				throw new ArgumentOutOfRangeException(nameof(verticalScale));
			if (right < 0 || double.IsNaN(right))
				throw new ArgumentOutOfRangeException(nameof(bottomSpace));
			TopSpace = topSpace;
			BottomSpace = bottomSpace;
			Right = right;
			VerticalScale = verticalScale;
		}
	}
}
