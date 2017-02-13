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

using dnlib.DotNet;
using dnlib.IO;

namespace dnSpy.AsmEditor.Hex {
	static class InstructionUtils {
		public static ulong GetTotalMethodBodyLength(MethodDef md) {
			if (md == null || md.RVA == 0)
				return 0;
			var mod = md.Module as ModuleDefMD;//TODO: Support CorModuleDef
			if (mod == null)
				return 0;

			try {
				using (var reader = mod.MetaData.PEImage.CreateFullStream()) {
					reader.Position = (long)mod.MetaData.PEImage.ToFileOffset(md.RVA);
					var start = reader.Position;
					if (!ReadHeader(reader, out ushort flags, out uint codeSize))
						return 0;

					reader.Position += codeSize;

					if ((flags & 8) != 0) {
						reader.Position = (reader.Position + 3) & ~3;
						byte b = reader.ReadByte();
						if ((b & 0x3F) != 1)
							reader.Position--;
						else if ((b & 0x40) != 0) {
							reader.Position--;
							int num = (ushort)((reader.ReadUInt32() >> 8) / 24);
							reader.Position += num * 24;
						}
						else {
							int num = (ushort)((uint)reader.ReadByte() / 12);
							reader.Position += 2 + num * 12;
						}
					}

					return (ulong)(reader.Position - start);
				}
			}
			catch {
				return 0;
			}
		}

		static bool ReadHeader(IBinaryReader reader, out ushort flags, out uint codeSize) {
			byte b = reader.ReadByte();
			switch (b & 7) {
			case 2:
			case 6:
				flags = 2;
				codeSize = (uint)(b >> 2);
				return true;

			case 3:
				flags = (ushort)((reader.ReadByte() << 8) | b);
				uint headerSize = (byte)(flags >> 12);
				ushort maxStack = reader.ReadUInt16();
				codeSize = reader.ReadUInt32();
				uint localVarSigTok = reader.ReadUInt32();

				reader.Position += -12 + headerSize * 4;
				if (headerSize < 3)
					flags &= 0xFFF7;
				return true;

			default:
				flags = 0;
				codeSize = 0;
				return false;
			}
		}
	}
}
