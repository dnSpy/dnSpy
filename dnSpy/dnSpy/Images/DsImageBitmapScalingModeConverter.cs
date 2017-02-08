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

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace dnSpy.Images {
	sealed class DsImageBitmapScalingModeConverter : IMultiValueConverter {
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			bool b = values.Length == 2;
			Debug.Assert(b);
			if (!b)
				return null;

			var zoom = (double)values[0];
			var dpi = (double)values[1];
			var scale = zoom * dpi / 96;
			if (scale % 1 < 0.05)
				return boxedBitmapScalingModeHighQuality;
			return boxedBitmapScalingModeNearestNeighbor;
		}
		static readonly object boxedBitmapScalingModeHighQuality = BitmapScalingMode.HighQuality;
		static readonly object boxedBitmapScalingModeNearestNeighbor = BitmapScalingMode.NearestNeighbor;

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
