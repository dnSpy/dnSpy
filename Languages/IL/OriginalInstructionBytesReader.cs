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
using dnlib.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;

namespace dnSpy.Languages.IL {
	sealed class OriginalInstructionBytesReader : IInstructionBytesReader {
		readonly IImageStream stream;

		public OriginalInstructionBytesReader(MethodDef method) {
			//TODO: This fails and returns null if it's a CorMethodDef!
			this.stream = method.Module.GetImageStream((uint)method.RVA + method.Body.HeaderSize);
		}

		public int ReadByte() {
			if (stream != null)
				return stream.ReadByte();
			return -1;
		}

		public void SetInstruction(int index, uint offset) {
			if (stream != null)
				stream.Position = offset;
		}

		public void Dispose() {
			if (stream != null)
				stream.Dispose();
		}
	}
}
