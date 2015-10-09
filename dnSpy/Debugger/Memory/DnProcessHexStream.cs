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
using System.Runtime.InteropServices;
using dndbg.Engine;
using dnSpy.HexEditor;

namespace dnSpy.Debugger.Memory {
	sealed class DnProcessHexStream : ISimpleHexStream {
		public ulong StartOffset {
			get { return 0; }
		}

		public ulong EndOffset {
			get { return IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue; }
		}

		public ulong Size {
			get { return IntPtr.Size == 4 ? uint.MaxValue + 1UL : ulong.MaxValue; }
		}

		public ulong PageSize {
			get { return (ulong)Environment.SystemPageSize; }
		}

		readonly DnProcess process;

		public DnProcessHexStream(DnProcess process) {
			this.process = process;
		}

		public int Read(ulong offset, byte[] array, long index, int count) {
			try {
				int sizeRead;
				int hr = process.CorProcess.ReadMemory(offset, array, index, count, out sizeRead);
				return hr != 0 ? 0 : sizeRead;
			}
			catch {
				return 0;
			}
		}

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtectEx([In] IntPtr hProcess, [In] IntPtr lpAddress, [In] int dwSize, [In] uint flNewProtect, out uint lpflOldProtect);
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		public int Write(ulong offset, byte[] array, long index, int count) {
			var hProcess = process.CorProcess.Handle;
			uint oldProtect;
			bool restoreOldProtect = VirtualProtectEx(hProcess, new IntPtr((long)offset), array.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
			try {
				int sizeWritten;
				int hr = process.CorProcess.WriteMemory(offset, array, index, count, out sizeWritten);
				return hr != 0 ? 0 : sizeWritten;
			}
			catch {
				return 0;
			}
			finally {
				if (restoreOldProtect)
					VirtualProtectEx(hProcess, new IntPtr((long)offset), array.Length, oldProtect, out oldProtect);
			}
		}
	}
}
