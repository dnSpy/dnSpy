/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Globalization;
using System.Windows.Data;
using dnSpy.Shared.UI.Controls;

namespace dnSpy.Shared.UI.MVVM.Converters {
	public sealed class FontSizeConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return Math.Round((double)value * 3 / 4);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var s = (string)value;
			double v;
			if (double.TryParse(s, out v))
				return v * 4 / 3;
			return FontUtils.DEFAULT_FONT_SIZE;
		}
	}
}
