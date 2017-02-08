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
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnSpy.AsmEditor.DnlibDialogs.Converters {
	/// <summary>
	/// Boolean to thickness converter
	/// </summary>
	public sealed class BooleanToThicknessConverter : IValueConverter {
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var ary = ((string)parameter).Split(sep);
			var s = (bool)value ? ary[0] : ary[1];
			var c = TypeDescriptor.GetConverter(typeof(Thickness));
			return c.ConvertFrom(null, culture, s);
		}
		static readonly char[] sep = new char[] { '|' };

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
