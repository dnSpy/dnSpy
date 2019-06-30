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
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.BackgroundImage;

namespace dnSpy.BackgroundImage {
	static class DefaultRawSettings {
		public const Stretch DefaultStretch = Stretch.None;
		public const StretchDirection DefaultStretchDirection = StretchDirection.Both;
		public const double Opacity = 0.35;
		public const double HorizontalOffset = 0;
		public const double VerticalOffset = 0;
		public const double LeftMarginWidthPercent = 0;
		public const double RightMarginWidthPercent = 0;
		public const double TopMarginHeightPercent = 0;
		public const double BottomMarginHeightPercent = 0;
		public const double MaxHeight = 0;
		public const double MaxWidth = 0;
		public const double Zoom = 100;
		public const ImagePlacement DefaultImagePlacement = ImagePlacement.BottomRight;
		public const bool IsRandom = false;
		public const bool IsEnabled = true;
		public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
	}
}
