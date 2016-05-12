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

using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Locals {
	sealed class ValuePrinter {
		readonly IOutputColorWriter output;
		readonly bool useHex;

		public ValuePrinter(IOutputColorWriter output, bool useHex) {
			this.output = output;
			this.useHex = useHex;
		}

		public void WriteExpander(ValueVM vm) {
			if (vm.LazyLoading)
				output.Write(BoxedOutputColor.Text, "+");
			else if (vm.Children.Count == 0) {
				// VS prints nothing
			}
			else if (vm.IsExpanded)
				output.Write(BoxedOutputColor.Text, "-");
			else
				output.Write(BoxedOutputColor.Text, "+");
		}

		public void WriteName(ValueVM vm) => vm.WriteName(output);
		public void WriteValue(ValueVM vm) => Write(vm.CachedOutputValue);
		public void WriteType(ValueVM vm) => Write(vm.CachedOutputType);

		void Write(CachedOutput co) {
			var conv = new OutputConverter(output);
			foreach (var t in co.data)
				conv.Write(t.Item1, t.Item2);
		}
	}
}
