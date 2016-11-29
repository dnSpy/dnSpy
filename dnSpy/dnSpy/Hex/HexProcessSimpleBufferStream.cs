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
using System.Diagnostics;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	sealed class HexProcessSimpleBufferStream : HexSimpleBufferStream {
		public override bool IsVolatile => isVolatile;
		public override bool IsReadOnly => isReadOnly;
		public override HexSpan Span { get; }
		public override string Name { get; }
		public override ulong PageSize { get; }

		readonly IntPtr hProcess;
		readonly bool isReadOnly;
		readonly bool isVolatile;

		public HexProcessSimpleBufferStream(IntPtr hProcess, string name, bool isReadOnly, bool isVolatile) {
			this.hProcess = hProcess;
			Span = GetDefaultSpan(hProcess);
			Name = name ?? GetDefaultName(hProcess);
			PageSize = GetDefaultPageSize(hProcess);
			this.isReadOnly = isReadOnly;
			this.isVolatile = isVolatile;
		}

		static string GetDefaultName(IntPtr hProcess) {
			uint pid = NativeMethods.GetProcessId(hProcess);
			return $"<MEMORY: pid {pid}>";
		}

		static HexSpan GetDefaultSpan(IntPtr hProcess) {
			int bitSize = GetProcessAddressBitSize(hProcess);
			if (bitSize == 32)
				return HexSpan.FromBounds(0, uint.MaxValue + 1UL);
			if (bitSize == 64) {
				if (IntPtr.Size != 8)
					return HexSpan.FromBounds(0, new HexPosition(ulong.MaxValue) + 1);
				NativeMethods.SYSTEM_INFO info;
				NativeMethods.GetSystemInfo(out info);
				return HexSpan.FromBounds(0, new HexPosition((ulong)info.lpMaximumApplicationAddress.ToInt64()) + 1);
			}
			Debug.Fail($"Unsupported bit size: {bitSize}");
			return HexSpan.FromBounds(0, HexPosition.MaxEndPosition);
		}

		static ulong GetDefaultPageSize(IntPtr hProcess) => (ulong)Environment.SystemPageSize;

		static int GetProcessAddressBitSize(IntPtr hProcess) {
			if (!Environment.Is64BitOperatingSystem) {
				Debug.Assert(IntPtr.Size == 4);
				return IntPtr.Size * 8;
			}
			bool isWow64Process;
			if (NativeMethods.IsWow64Process(hProcess, out isWow64Process)) {
				if (isWow64Process)
					return 32;
				return 64;
			}
			Debug.Fail("IsWow64Process failed");
			return IntPtr.Size * 8;
		}

		public unsafe override HexPosition Read(HexPosition position, byte[] destination, long destinationIndex, long length) {
			if (position >= Span.End)
				return 0;
			int bytesToRead = (int)Math.Min(length, int.MaxValue);
			IntPtr sizeRead;
			bool b;
			fixed (void* p = &destination[destinationIndex])
				b = NativeMethods.ReadProcessMemory(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(p), bytesToRead, out sizeRead);
			return !b ? 0 : sizeRead.ToInt64();
		}

		public unsafe override HexPosition Write(HexPosition position, byte[] source, long sourceIndex, long length) {
			if (isReadOnly)
				return 0;
			if (position >= Span.End)
				return 0;
			int bytesToWrite = (int)Math.Min(length, int.MaxValue);
			uint oldProtect;
			bool restoreOldProtect = NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)position.ToUInt64()), bytesToWrite, NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
			IntPtr sizeWritten;
			bool b;
			fixed (void* p = &source[sourceIndex])
				b = NativeMethods.WriteProcessMemory(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(p), bytesToWrite, out sizeWritten);
			if (restoreOldProtect)
				NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)position.ToUInt64()), bytesToWrite, oldProtect, out oldProtect);
			return !b ? 0 : (int)sizeWritten.ToInt64();
		}
	}
}
