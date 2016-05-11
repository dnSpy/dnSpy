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
using System.Windows.Data;
using dnSpy.AsmEditor.MethodBody;
using dnSpy.Shared.Controls;

namespace dnSpy.AsmEditor.Converters {
	sealed class CilObjectConverter : IValueConverter {
		public static readonly CilObjectConverter Instance = new CilObjectConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			try {
				var flags = WriteObjectFlags.None;
				if (parameter != null) {
					foreach (var c in (string)parameter) {
						if (c == 's')
							flags |= WriteObjectFlags.ShortInstruction;
					}
				}

				var gen = ColorizedTextElementCreator.Create(true);
				BodyUtils.WriteObject(gen.Output, value, flags);
				return gen.CreateResult(true, true);
			}
			catch (Exception ex) {
				Debug.Fail(ex.ToString());
			}

			if (value == null)
				return string.Empty;
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
