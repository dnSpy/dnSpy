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
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using dnSpy.Images;
using dnSpy.TreeNodes;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ThreadVM;
			var s = parameter as string;
			if (vm == null || s == null)
				return null;

			if (StringComparer.OrdinalIgnoreCase.Equals(s, "CurrentImage")) {
				if (vm.IsCurrent)
					return ImageCache.Instance.GetImage(GetType().Assembly, "CurrentLine", BackgroundType.GridViewItem);
				if (vm.Type == ThreadType.Main)
					return ImageCache.Instance.GetImage(GetType().Assembly, "DraggedCurrentInstructionPointer", BackgroundType.GridViewItem);
				return null;
			}
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "CategoryImage")) {
				switch (vm.Type) {
				case ThreadType.Unknown:
				case ThreadType.Terminated:
					return ImageCache.Instance.GetImage(GetType().Assembly, "QuestionMark", BackgroundType.GridViewItem);
				case ThreadType.Main:
					return ImageCache.Instance.GetImage(GetType().Assembly, "Thread", BackgroundType.GridViewItem);
				case ThreadType.BGCOrFinalizer:
				case ThreadType.ThreadPool:
				case ThreadType.Worker:
					return ImageCache.Instance.GetImage(GetType().Assembly, "Process", BackgroundType.GridViewItem);
				default:
					Debug.Fail(string.Format("Unknown thread type: {0}", vm.Type));
					goto case ThreadType.Unknown;
				}
			}

			var gen = UISyntaxHighlighter.Create(DebuggerSettings.Instance.SyntaxHighlightThreads);
			var printer = new ThreadPrinter(gen.TextOutput, DebuggerSettings.Instance.UseHexadecimal);
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

			return gen.CreateTextBlock(true);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
