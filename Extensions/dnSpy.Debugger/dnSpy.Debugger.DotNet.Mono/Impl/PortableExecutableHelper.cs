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
using System.Diagnostics;
using System.IO;
using dnlib.PE;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class PortableExecutableHelper {
		public static bool TryGetSizeOfImage(DbgProcess process, ulong address, bool isFileLayout, out uint imageSize) {
			imageSize = 0;
			try {
				var buffer = new byte[0x2000];
				process.ReadMemory(address, buffer, 0, buffer.Length);
				if (BitConverter.ToUInt16(buffer, 0) != 0x5A4D)
					return false;

				using (var peImage = new PEImage(buffer, null, isFileLayout ? ImageLayout.File : ImageLayout.Memory, verify: true)) {
					ulong length = GetImageSize(peImage);
					Debug.Assert(length <= uint.MaxValue);
					if (length > uint.MaxValue)
						return false;
					imageSize = (uint)length;
					return true;
				}
			}
			catch (BadImageFormatException) {
				return false;
			}
			catch (IOException) {
				return false;
			}
			catch (Exception ex) {
				Debug.Fail(ex.ToString());
				return false;
			}
		}

		static ulong AlignUp(ulong val, uint alignment) => (val + alignment - 1) & ~(ulong)(alignment - 1);

		static ulong GetImageSize(IPEImage peImage) {
			var optHdr = peImage.ImageNTHeaders.OptionalHeader;
			uint alignment = peImage.IsFileImageLayout ? optHdr.FileAlignment : optHdr.SectionAlignment;
			ulong len = AlignUp(optHdr.SizeOfHeaders, alignment);
			foreach (var section in peImage.ImageSectionHeaders) {
				ulong len2 = peImage.IsFileImageLayout ?
					AlignUp((ulong)section.PointerToRawData + section.SizeOfRawData, alignment) :
					AlignUp((ulong)section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData), alignment);
				if (len2 > len)
					len = len2;
			}
			return len;
		}

		public static bool TryGetModuleAddressAndSize(DbgProcess process, string moduleFilename, out ulong imageAddr, out uint imageSize) {
			//TODO: mono maps the files as data, use Win32 funcs to find all mapped files
			imageAddr = 0;
			imageSize = 0;
			return false;
		}
	}
}
