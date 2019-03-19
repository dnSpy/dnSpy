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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Internal {
	static class NativeMethods {
		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);
		public const uint MEM_COMMIT = 0x00001000;
		public const uint PAGE_READWRITE = 0x04;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, uint dwFreeType);
		public const uint MEM_RELEASE = 0x8000;
	}
}
