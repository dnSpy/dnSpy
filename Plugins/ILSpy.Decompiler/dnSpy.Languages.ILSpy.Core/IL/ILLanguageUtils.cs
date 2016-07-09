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

using dnlib.DotNet;
using ICSharpCode.Decompiler.Disassembler;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Languages.ILSpy.Core.IL {
	static class ILLanguageUtils {
		public static bool Write(ITextOutput output, IMemberRef member) {
			var method = member as IMethod;
			if (method != null && method.IsMethod) {
				method.WriteMethodTo(output);
				return true;
			}

			var field = member as IField;
			if (field != null && field.IsField) {
				field.WriteFieldTo(output);
				return true;
			}

			var prop = member as PropertyDef;
			if (prop != null) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(new System.Threading.CancellationToken(), null));
				dis.DisassembleProperty(prop, false);
				return true;
			}

			var evt = member as EventDef;
			if (evt != null) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(new System.Threading.CancellationToken(), null));
				dis.DisassembleEvent(evt, false);
				return true;
			}

			var type = member as ITypeDefOrRef;
			if (type != null) {
				type.WriteTo(output, ILNameSyntax.TypeName);
				return true;
			}

			return false;
		}
	}
}
