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

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using dnlib.DotNet;
using dnlib.IO;

namespace ICSharpCode.ILSpy
{
	sealed class OriginalInstructionBytesReader : IInstructionBytesReader
	{
		readonly IImageStream stream;

		public OriginalInstructionBytesReader(MethodDef method)
		{
			this.stream = method.Module.GetImageStream((uint)method.RVA + method.Body.HeaderSize);
		}

		public int ReadByte()
		{
			return stream.ReadByte();
		}

		public void SetInstruction(int index, uint offset)
		{
			stream.Position = offset;
		}

		public void Dispose()
		{
			stream.Dispose();
		}
	}
}
