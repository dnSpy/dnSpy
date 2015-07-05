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
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.PE;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	sealed class MethodBodyOptions
	{
		public MethodBodyType BodyType;
		public MethodImplAttributes CodeType;
		public NativeMethodBodyOptions NativeMethodBodyOptions = new NativeMethodBodyOptions();
		public CilBodyOptions CilBodyOptions = new CilBodyOptions();

		public MethodBodyOptions()
		{
		}

		public MethodBodyOptions(MethodDef method)
		{
			this.CodeType = method.CodeType;
			if (method.MethodBody is CilBody) {
				var headerRva = method.RVA;
				var headerFileOffset = (FileOffset)method.Module.ToFileOffset((uint)headerRva);
				var rva = (RVA)((uint)headerRva + method.Body.HeaderSize);
				var fileOffset = (FileOffset)((long)headerFileOffset + method.Body.HeaderSize);
				this.CilBodyOptions = new CilBodyOptions((CilBody)method.MethodBody, headerRva, headerFileOffset, rva, fileOffset);
				this.BodyType = MethodBodyType.Cil;
			}
			else if (method.MethodBody is NativeMethodBody) {
				this.NativeMethodBodyOptions = new NativeMethodBodyOptions((NativeMethodBody)method.MethodBody);
				this.BodyType = MethodBodyType.Native;
			}
			else
				this.BodyType = MethodBodyType.None;
		}

		public MethodDef CopyTo(MethodDef method)
		{
			method.CodeType = this.CodeType;
			if (this.BodyType == MethodBodyType.Cil)
				method.MethodBody = CilBodyOptions.Create();
			else if (this.BodyType == MethodBodyType.Native)
				method.MethodBody = NativeMethodBodyOptions.Create();
			else {
				Debug.Assert(this.BodyType == MethodBodyType.None);
				if (this.BodyType != MethodBodyType.None)
					throw new InvalidOperationException();
				method.MethodBody = null;
			}
			return method;
		}
	}
}
