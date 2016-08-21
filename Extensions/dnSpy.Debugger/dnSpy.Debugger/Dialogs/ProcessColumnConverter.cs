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
using dnSpy.Contracts.Controls;

namespace dnSpy.Debugger.Dialogs {
	sealed class ProcessColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ProcessVM;
			var s = parameter as string;
			if (vm == null || s == null)
				return null;

			var gen = ColorizedTextElementProvider.Create(vm.Context.SyntaxHighlight);
			var printer = new ProcessPrinter(gen.Output, false);
			HorizontalAlignment? horizAlign = null;
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "FullPath"))
				printer.WriteFullPath(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Filename"))
				printer.WriteFilename(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "PID")) {
				printer.WritePID(vm);
				horizAlign = HorizontalAlignment.Right;
			}
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "CLRVersion"))
				printer.WriteCLRVersion(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Type"))
				printer.WriteType(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Machine"))
				printer.WriteMachine(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Title"))
				printer.WriteTitle(vm);
			else
				return null;

			var tb = gen.CreateResult(true);
			if (horizAlign != null)
				tb.HorizontalAlignment = horizAlign.Value;
			return tb;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
