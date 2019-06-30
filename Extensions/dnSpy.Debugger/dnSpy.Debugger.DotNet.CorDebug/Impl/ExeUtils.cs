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

using System;
using System.IO;
using System.Text;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class ExeUtils {
		public static bool TryGetTextSectionInfo(BinaryReader exe, out int textOffset, out int textSize) {
			try {
				uint sig = exe.ReadUInt32();
				exe.BaseStream.Position -= 4;
				if (PEExeUtils.CheckMagic((ushort)sig))
					return PEExeUtils.TryGetTextSectionInfo(exe, out textOffset, out textSize);
				if (ElfExeUtils.CheckMagic(sig))
					return ElfExeUtils.TryGetTextSectionInfo(exe, out textOffset, out textSize);
				if (MachOExeUtils.CheckMagic(sig))
					return MachOExeUtils.TryGetTextSectionInfo(exe, out textOffset, out textSize);
			}
			catch (IOException) {
			}

			textOffset = 0;
			textSize = 0;
			return false;
		}

		internal static ushort ReadUInt16(BinaryReader reader, bool isLittleEndian) {
			if (isLittleEndian)
				return reader.ReadUInt16();
			return (ushort)(((uint)reader.ReadByte() << 8) | reader.ReadByte());
		}

		internal static uint ReadUInt32(BinaryReader reader, bool isLittleEndian) {
			if (isLittleEndian)
				return reader.ReadUInt32();
			return ((uint)reader.ReadByte() << 24) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 8) | (uint)reader.ReadByte();
		}

		internal static ulong ReadUInt64(BinaryReader reader, bool isLittleEndian) {
			if (isLittleEndian)
				return reader.ReadUInt64();
			return ((ulong)ReadUInt32(reader, isLittleEndian) << 32) | ReadUInt32(reader, isLittleEndian);
		}
	}

	static class PEExeUtils {
		public static bool CheckMagic(ushort magic) => magic == 0x5A4D;

		public static bool TryGetTextSectionInfo(BinaryReader exe, out int textOffset, out int textSize) {
			textOffset = 0;
			textSize = 0;

			try {
				var f = exe.BaseStream;
				if (!CheckMagic(exe.ReadUInt16()))
					return false;
				f.Position += 0x3A;
				f.Position = exe.ReadUInt32();
				if (exe.ReadUInt32() != 0x4550)
					return false;
				f.Position += 2;
				var numSections = exe.ReadUInt16();
				if (numSections < 1)
					return false;
				f.Position += 0x0C;
				var sizeOfOptionalHeader = exe.ReadUInt16();
				f.Position += 2 + sizeOfOptionalHeader + 8 + 4 + 4;
				var sizeOfRawData = exe.ReadUInt32();
				var pointerToRawData = exe.ReadUInt32();
				if ((long)sizeOfRawData + pointerToRawData > f.Length)
					return false;

				textOffset = (int)pointerToRawData;
				textSize = (int)sizeOfRawData;
				return true;
			}
			catch (IOException) {
			}

			return false;
		}
	}

	static class ElfExeUtils {
		public static bool CheckMagic(uint magic) => magic == 0x464C457F;

		public static bool TryGetTextSectionInfo(BinaryReader exe, out int textOffset, out int textSize) {
			textOffset = 0;
			textSize = 0;

			try {
				var f = exe.BaseStream;
				if (!CheckMagic(exe.ReadUInt32()))
					return false;
				var eiClass = exe.ReadByte();
				if (eiClass != 1 && eiClass != 2)
					return false;
				var eiData = exe.ReadByte();
				if (eiData != 1 && eiData != 2)
					return false;
				bool isLittleEndian = eiData == 1;
				if (exe.ReadByte() != 1)
					return false;
				f.Position += 1 + 1 + 7;
				f.Position += 2;// e_type
				f.Position += 2;// e_machine
				if (ExeUtils.ReadUInt32(exe, isLittleEndian) != 1)
					return false;
				f.Position += 4 * eiClass;
				var ePhoff = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
				var sShoff = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
				f.Position += 4 + 2 + 2 + 2;
				var eShentsize = ExeUtils.ReadUInt16(exe, isLittleEndian);
				if (eShentsize < (eiClass == 1 ? 0x28 : 0x40))
					return false;
				var eShnum = ExeUtils.ReadUInt16(exe, isLittleEndian);
				var eShstrndx = ExeUtils.ReadUInt16(exe, isLittleEndian);
				if (eShstrndx >= eShnum)
					return false;

				f.Position = (long)(sShoff + (uint)eShstrndx * (uint)eShentsize);
				f.Position += eiClass == 1 ? 0x10 : 0x18;
				var stringOffset = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
				f.Position = (long)sShoff;
				var sb = new StringBuilder();
				for (int i = 0; i < eShnum; i++) {
					var sectPos = f.Position;

					var nameOffset = stringOffset + ExeUtils.ReadUInt32(exe, isLittleEndian);
					ExeUtils.ReadUInt32(exe, isLittleEndian);
					var flags = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
					_ = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
					var offset = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);
					var size = eiClass == 1 ? ExeUtils.ReadUInt32(exe, isLittleEndian) : ExeUtils.ReadUInt64(exe, isLittleEndian);

					if (size != 0 && (flags & 6) == 6 && ReadName(sb, exe, nameOffset) == ".text") {
						textOffset = (int)offset;
						textSize = (int)size;
						return true;
					}

					f.Position = sectPos + eShentsize;
				}
			}
			catch (IOException) {
			}

			return false;
		}

		static string ReadName(StringBuilder sb, BinaryReader exe, ulong offset) {
			var f = exe.BaseStream;
			var pos = f.Position;
			f.Position = (long)offset;
			sb.Clear();
			for (;;) {
				byte b = exe.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			f.Position = pos;
			return sb.ToString();
		}
	}

	static class MachOExeUtils {
		public static bool CheckMagic(uint magic) => magic == 0xFEEDFACE || magic == 0xFEEDFACF;

		public static bool TryGetTextSectionInfo(BinaryReader exe, out int textOffset, out int textSize) {
			textOffset = 0;
			textSize = 0;

			try {
				var f = exe.BaseStream;
				var magic = exe.ReadUInt32();
				if (!CheckMagic(magic))
					return false;
				bool is32 = magic == 0xFEEDFACE;
				bool isLittleEndian = true;
				var cputype = ExeUtils.ReadUInt32(exe, isLittleEndian);
				var cpusubtype = ExeUtils.ReadUInt32(exe, isLittleEndian);
				var filetype = ExeUtils.ReadUInt32(exe, isLittleEndian);
				var ncmds = ExeUtils.ReadUInt32(exe, isLittleEndian);
				var sizeofcmds = ExeUtils.ReadUInt32(exe, isLittleEndian);
				var flags = ExeUtils.ReadUInt32(exe, isLittleEndian);
				if (!is32)
					ExeUtils.ReadUInt32(exe, isLittleEndian);

				for (uint i = 0; i < ncmds; i++) {
					var pos = f.Position;
					var cmd = ExeUtils.ReadUInt32(exe, isLittleEndian);
					var cmdsize = ExeUtils.ReadUInt32(exe, isLittleEndian);

					if (cmd == 0x19) {
						// LC_SEGMENT_64
						var segname = GetStringZ(exe.ReadBytes(16));
						var vmaddr = ExeUtils.ReadUInt64(exe, isLittleEndian);
						var vmsize = ExeUtils.ReadUInt64(exe, isLittleEndian);
						var fileoff = ExeUtils.ReadUInt64(exe, isLittleEndian);
						var filesize = ExeUtils.ReadUInt64(exe, isLittleEndian);
						var maxprot = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var initprot = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var nsects = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var seg_flags = ExeUtils.ReadUInt32(exe, isLittleEndian);

						for (uint j = 0; j < nsects; j++) {
							var sect_name = GetStringZ(exe.ReadBytes(16));
							var sect_segname = GetStringZ(exe.ReadBytes(16));
							var sect_addr = ExeUtils.ReadUInt64(exe, isLittleEndian);
							var sect_size = ExeUtils.ReadUInt64(exe, isLittleEndian);
							var sect_offset = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_align = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_reloff = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_nreloc = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_flags = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_res1 = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_res2 = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_res3 = ExeUtils.ReadUInt32(exe, isLittleEndian);
							if (sect_name == "__text" && sect_segname == "__TEXT") {
								textOffset = (int)sect_offset;
								textSize = (int)sect_size;
								return true;
							}
						}
					}
					else if (cmd == 1) {
						// LC_SEGMENT
						var segname = GetStringZ(exe.ReadBytes(16));
						var vmaddr = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var vmsize = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var fileoff = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var filesize = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var maxprot = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var initprot = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var nsects = ExeUtils.ReadUInt32(exe, isLittleEndian);
						var seg_flags = ExeUtils.ReadUInt32(exe, isLittleEndian);

						for (uint j = 0; j < nsects; j++) {
							var sect_name = GetStringZ(exe.ReadBytes(16));
							var sect_segname = GetStringZ(exe.ReadBytes(16));
							var sect_addr = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_size = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_offset = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_align = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_reloff = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_nreloc = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_flags = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_res1 = ExeUtils.ReadUInt32(exe, isLittleEndian);
							var sect_res2 = ExeUtils.ReadUInt32(exe, isLittleEndian);
							if (sect_name == "__text" && sect_segname == "__TEXT") {
								textOffset = (int)sect_offset;
								textSize = (int)sect_size;
								return true;
							}
						}
					}

					f.Position = pos + cmdsize;
				}
			}
			catch (IOException) {
			}

			return false;
		}

		static string GetStringZ(byte[] data) {
			int zeroIndex = Array.IndexOf(data, (byte)0);
			if (zeroIndex < 0)
				zeroIndex = data.Length;
			return Encoding.ASCII.GetString(data, 0, zeroIndex);
		}
	}
}
