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
using System.Globalization;
using System.Windows.Data;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Debugger.Modules {
	sealed class ModuleColumnConverter : IValueConverter {
		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) { writer.Clear(); }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ModuleVM;
			var tag = parameter as string;
			if (vm == null || tag == null)
				return null;

			var writer = Cache.GetWriter();
			try {
				var printer = new ModulePrinter(writer, vm.Context.UseHexadecimal, vm.Context.TheDebugger.Debugger);
				if (tag == PredefinedTextClassifierTags.ModulesWindowName)
					printer.WriteName(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowPath)
					printer.WritePath(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowOptimized)
					printer.WriteOptimized(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowDynamic)
					printer.WriteDynamic(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowInMemory)
					printer.WriteInMemory(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowOrder)
					printer.WriteOrder(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowVersion)
					printer.WriteVersion(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowTimestamp)
					printer.WriteTimestamp(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowAddress)
					printer.WriteAddress(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowProcess)
					printer.WriteProcess(vm);
				else if (tag == PredefinedTextClassifierTags.ModulesWindowAppDomain)
					printer.WriteAppDomain(vm);
				else
					return null;

				var context = new TextClassifierContext(writer.Text, tag, vm.Context.SyntaxHighlight, writer.Colors);
				return vm.Context.TextElementProvider.CreateTextElement(vm.Context.ClassificationFormatMap, context, ContentTypes.ModulesWindow, TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis);
			}
			finally {
				Cache.FreeWriter(writer);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
