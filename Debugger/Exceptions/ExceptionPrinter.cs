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

using dnSpy.Decompiler;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionPrinter {
		readonly ITextOutput output;

		public ExceptionPrinter(ITextOutput output) {
			this.output = output;
		}

		public void WriteName(ExceptionVM vm) {
			if (vm.ExceptionInfo.IsOtherExceptions)
				output.Write(vm.Name, TextTokenType.Text);
			else
				WriteFullTypeName(vm.Name);
		}

		void WriteFullTypeName(string fullName) {
			string ns, name;
			SplitTypeName(fullName, out ns, out name);
			if (!string.IsNullOrEmpty(ns)) {
				output.WriteNamespace_OLD(ns);
				output.Write(".", TextTokenType.Operator);
			}
			output.Write(IdentifierEscaper.Escape(name), TextTokenType.Type);
		}

		static void SplitTypeName(string fullName, out string ns, out string name) {
			int i = fullName.LastIndexOf('.');
			if (i < 0) {
				ns = string.Empty;
				name = fullName;
			}
			else {
				ns = fullName.Substring(0, i);
				name = fullName.Substring(i + 1);
			}
		}
	}
}
