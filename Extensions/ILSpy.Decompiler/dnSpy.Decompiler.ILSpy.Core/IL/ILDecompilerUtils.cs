/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;
using ICSharpCode.Decompiler.Disassembler;

namespace dnSpy.Decompiler.ILSpy.Core.IL {
	static class ILDecompilerUtils {
		public static bool Write(IDecompilerOutput output, IMemberRef member) {
			if (member is IMethod method && method.IsMethod) {
				method.WriteMethodTo(output);
				return true;
			}

			if (member is IField field && field.IsField) {
				field.WriteFieldTo(output);
				return true;
			}

			if (member is PropertyDef prop) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(0, new System.Threading.CancellationToken(), null));
				dis.DisassembleProperty(prop, false);
				return true;
			}

			if (member is EventDef evt) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(0, new System.Threading.CancellationToken(), null));
				dis.DisassembleEvent(evt, false);
				return true;
			}

			if (member is ITypeDefOrRef type) {
				type.WriteTo(output, ILNameSyntax.TypeName);
				return true;
			}

			return false;
		}
	}
}
