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

namespace dnSpy.Language.Intellisense {
	sealed class CompletionTextConverter : IMultiValueConverter {
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			var func = parameter as string;
			if (func == null)
				return null;
			var completion = (CompletionVM)values[0];
			var presenter = (CompletionPresenter)values[1];
			if (completion == null || presenter == null)
				return null;
			switch (func) {
			case "DisplayText":
				return presenter.GetDisplayText(completion);
			case "Suffix":
				return presenter.GetSuffix(completion);
			default:
				Debug.Fail($"Unknown value: {func}");
				return null;
			}
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
