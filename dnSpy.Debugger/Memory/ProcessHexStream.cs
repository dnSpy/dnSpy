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

using System;
using dnSpy.Shared.HexEditor;

namespace dnSpy.Debugger.Memory {
	sealed class ProcessHexStream : ISimpleHexStream {
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

		readonly IntPtr hProcess;

		public ProcessHexStream(IntPtr hProcess) {
			this.hProcess = hProcess;
		}

		public unsafe int Read(ulong offset, byte[] array, long index, int count) {
			if (IntPtr.Size == 4 && offset > uint.MaxValue)
				return 0;
			IntPtr sizeRead;
			bool b;
			fixed (void* p = &array[index])
				b = NativeMethods.ReadProcessMemory(hProcess, new IntPtr((void*)offset), new IntPtr(p), count, out sizeRead);
			return !b ? 0 : (int)sizeRead.ToInt64();
		}

		public unsafe int Write(ulong offset, byte[] array, long index, int count) {
			if (IntPtr.Size == 4 && offset > uint.MaxValue)
				return 0;
			uint oldProtect;
			bool restoreOldProtect = NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)offset), count, NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
			IntPtr sizeWritten;
			bool b;
			fixed (void* p = &array[index])
				b = NativeMethods.WriteProcessMemory(hProcess, new IntPtr((void*)offset), new IntPtr(p), count, out sizeWritten);
			if (restoreOldProtect)
				NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)offset), count, oldProtect, out oldProtect);
			return !b ? 0 : (int)sizeWritten.ToInt64();
		}
	}
}
