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
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class MethodBodyOptions {
		public MethodBodyType BodyType;
		public MethodImplAttributes CodeType;
		public NativeMethodBodyOptions NativeMethodBodyOptions = new NativeMethodBodyOptions();
		public CilBodyOptions CilBodyOptions = new CilBodyOptions();

		public MethodBodyOptions() {
		}

		public MethodBodyOptions(MethodDef method) {
			CodeType = method.CodeType;
			if (method.MethodBody is CilBody) {
				var headerRva = method.RVA;
				var headerFileOffset = (FileOffset)(method.Module.ToFileOffset((uint)headerRva) ?? (uint)headerRva);
				var rva = (RVA)((uint)headerRva + method.Body.HeaderSize);
				var fileOffset = (FileOffset)((long)headerFileOffset + method.Body.HeaderSize);
				CilBodyOptions = new CilBodyOptions((CilBody)method.MethodBody, headerRva, headerFileOffset, rva, fileOffset);
				BodyType = MethodBodyType.Cil;
			}
			else if (method.MethodBody is NativeMethodBody) {
				NativeMethodBodyOptions = new NativeMethodBodyOptions((NativeMethodBody)method.MethodBody);
				BodyType = MethodBodyType.Native;
			}
			else
				BodyType = MethodBodyType.None;
		}

		public MethodDef CopyTo(MethodDef method) {
			method.CodeType = CodeType;
			if (BodyType == MethodBodyType.Cil)
				method.MethodBody = CilBodyOptions.Create();
			else if (BodyType == MethodBodyType.Native)
				method.MethodBody = NativeMethodBodyOptions.Create();
			else {
				Debug.Assert(BodyType == MethodBodyType.None);
				if (BodyType != MethodBodyType.None)
					throw new InvalidOperationException();
				method.MethodBody = null;
			}
			return method;
		}
	}
}
