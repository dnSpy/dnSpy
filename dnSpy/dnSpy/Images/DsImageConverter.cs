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
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using dnSpy.Contracts.Images;

namespace dnSpy.Images {
	sealed class DsImageConverter : IMultiValueConverter {
		internal static IImageService imageService;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			bool b = values.Length == 7;
			Debug.Assert(b);
			if (!b)
				return null;

			var width = (double)values[0];
			var height = (double)values[1];
			var imageReference = (ImageReference)values[2];
			var backgroundColor = (Color?)values[3];
			var backgroundBrush = (Brush)values[4];
			var zoom = (double)values[5];
			var dpi = (double)values[6];

			var options = new ImageOptions {
				BackgroundColor = backgroundColor,
				BackgroundBrush = backgroundBrush,
				LogicalSize = new Size(width, height),
				Zoom = new Size(zoom, zoom),
				Dpi = new Size(dpi, dpi),
			};

			return imageService.GetImage(imageReference, options);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
