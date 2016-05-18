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
using System.Windows.Data;
using dnSpy.Contracts.Images;
using dnSpy.Shared.Controls;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as BreakpointVM;
			if (vm == null)
				return null;
			var s = parameter as string;
			if (s == null)
				return null;

			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Image")) {
				string img = vm.IsEnabled ? "Breakpoint" : "DisabledBreakpoint";
				return vm.Context.ImageManager.GetImage(new ImageReference(GetType().Assembly, img), BackgroundType.GridViewItem);
			}

			var gen = ColorizedTextElementCreator.Create(vm.Context.SyntaxHighlight);
			var printer = new BreakpointPrinter(gen.Output, vm.Context.UseHexadecimal, vm.Context.Language);
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Name"))
				printer.WriteName(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Assembly"))
				printer.WriteAssembly(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Module"))
				printer.WriteModule(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "File"))
				printer.WriteFile(vm);
			else
				return null;

			return gen.CreateResult(true);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
