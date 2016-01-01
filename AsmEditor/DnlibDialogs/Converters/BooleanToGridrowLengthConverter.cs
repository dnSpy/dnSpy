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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnSpy.AsmEditor.DnlibDialogs.Converters {
	/// <summary>
	/// Converts a <see cref="bool"/> to a <see cref="GridLength"/>. If the value is true, it's
	/// converted to a "1*" or a "<user-parameter>*" value, else to a 0px length. The user can set
	/// ConverterParameter to the desired value. 1 is default.
	/// </summary>
	sealed class BooleanToGridrowLengthConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			double starValue = 1;
			if (parameter != null)
				starValue = System.Convert.ToDouble(parameter, culture);
			return (bool)value ? new GridLength(starValue, GridUnitType.Star) : new GridLength(0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
