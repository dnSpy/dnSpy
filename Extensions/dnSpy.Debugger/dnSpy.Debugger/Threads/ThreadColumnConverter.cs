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
using dnSpy.Contracts.Controls;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ThreadVM;
			var s = parameter as string;
			if (vm == null || s == null)
				return null;

			var gen = ColorizedTextElementProvider.Create(vm.Context.SyntaxHighlight);
			var printer = new ThreadPrinter(gen.Output, vm.Context.UseHexadecimal, vm.Context.TheDebugger.Debugger);
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Id"))
				printer.WriteId(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "ManagedId"))
				printer.WriteManagedId(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "CategoryText"))
				printer.WriteCategory(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Name"))
				printer.WriteName(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Location"))
				printer.WriteLocation(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Priority"))
				printer.WritePriority(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "AffinityMask"))
				printer.WriteAffinityMask(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Suspended"))
				printer.WriteSuspended(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "Process"))
				printer.WriteProcess(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "AppDomain"))
				printer.WriteAppDomain(vm);
			else if (StringComparer.OrdinalIgnoreCase.Equals(s, "UserState"))
				printer.WriteUserState(vm);
			else
				return null;

			return gen.CreateResult(true);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
