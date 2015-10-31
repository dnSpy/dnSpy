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
using dnSpy.Images;
using dnSpy.TreeNodes;

namespace dnSpy.Debugger.Modules {
	sealed class ModuleColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ModuleVM;
			var s = parameter as string;
			if (vm == null || s == null)
				return null;

			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Image"))
				return ImageCache.Instance.GetImage(vm.IsExe ? "AssemblyExe" : "AssemblyModule", BackgroundType.GridViewItem);

			var gen = UISyntaxHighlighter.Create(DebuggerSettings.Instance.SyntaxHighlightModules);
			var printer = new ModulePrinter(gen.TextOutput, DebuggerSettings.Instance.UseHexadecimal);
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Name"))
				printer.WriteName(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Path"))
				printer.WritePath(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Optimized"))
				printer.WriteOptimized(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Dynamic"))
				printer.WriteDynamic(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "InMemory"))
				printer.WriteInMemory(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Order"))
				printer.WriteOrder(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Version"))
				printer.WriteVersion(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Timestamp"))
				printer.WriteTimestamp(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Address"))
				printer.WriteAddress(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Process"))
				printer.WriteProcess(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "AppDomain"))
				printer.WriteAppDomain(vm);
			else
				return null;

			return gen.CreateTextBlock(true);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
