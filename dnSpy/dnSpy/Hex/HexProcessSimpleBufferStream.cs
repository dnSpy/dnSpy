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

		IntPtr hProcess;
		readonly bool isReadOnly;
		readonly bool isVolatile;
		readonly HexPosition endAddress;

		public HexProcessSimpleBufferStream(IntPtr hProcess, string name, bool isReadOnly, bool isVolatile) {
			this.hProcess = hProcess;
			Span = GetDefaultSpan(hProcess);
			Name = name ?? GetDefaultName(hProcess);
			PageSize = GetDefaultPageSize(hProcess);
			this.isReadOnly = isReadOnly;
			this.isVolatile = isVolatile;
			endAddress = GetEndAddress(hProcess);
		}

		public unsafe override HexSpanInfo GetSpanInfo(HexPosition position) {
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (position >= endAddress)
				return new HexSpanInfo(HexSpan.FromBounds(endAddress, HexPosition.MaxEndPosition), HexSpanInfoFlags.None);

			ulong baseAddress, regionSize;
			uint state, protect;
			int res;
			if (IntPtr.Size == 4) {
				NativeMethods.MEMORY_BASIC_INFORMATION32 info;
				res = NativeMethods.VirtualQueryEx32(hProcess, new IntPtr((void*)position.ToUInt64()), out info, NativeMethods.MEMORY_BASIC_INFORMATION32_SIZE);
				baseAddress = info.BaseAddress;
				regionSize = info.RegionSize;
				state = info.State;
				protect = info.Protect;
				Debug.Assert(res == 0 || res == NativeMethods.MEMORY_BASIC_INFORMATION32_SIZE);
			}
			else {
				NativeMethods.MEMORY_BASIC_INFORMATION64 info;
				res = NativeMethods.VirtualQueryEx64(hProcess, new IntPtr((void*)position.ToUInt64()), out info, NativeMethods.MEMORY_BASIC_INFORMATION64_SIZE);
				baseAddress = info.BaseAddress;
				regionSize = info.RegionSize;
				state = info.State;
				protect = info.Protect;
				Debug.Assert(res == 0 || res == NativeMethods.MEMORY_BASIC_INFORMATION64_SIZE);
			}

			// Could fail if eg. the process has exited
			if (res == 0)
				return new HexSpanInfo(HexSpan.FromBounds(HexPosition.Zero, endAddress), HexSpanInfoFlags.None);

			var flags = HexSpanInfoFlags.None;
			if (state == NativeMethods.MEM_COMMIT) {
				uint access = protect & 0xFF;
				if (access != NativeMethods.PAGE_NOACCESS && (protect & NativeMethods.PAGE_GUARD) == 0)
					flags |= HexSpanInfoFlags.HasData;
			}
			return new HexSpanInfo(new HexSpan(baseAddress, regionSize), flags);
		}

		static HexPosition GetEndAddress(IntPtr hProcess) {
			NativeMethods.SYSTEM_INFO info;
			NativeMethods.GetSystemInfo(out info);
			ulong mask = IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue;
			return new HexPosition((ulong)info.lpMaximumApplicationAddress.ToInt64() & mask) + 1;
		}

		static string GetDefaultName(IntPtr hProcess) {
			uint pid = NativeMethods.GetProcessId(hProcess);
			return $"<MEMORY: pid {pid}>";
		}

		static HexSpan GetDefaultSpan(IntPtr hProcess) {
			int bitness = GetBitness(hProcess);
			if (bitness == 32)
				return HexSpan.FromBounds(0, uint.MaxValue + 1UL);
			if (bitness == 64) {
				// If we're a 32-bit process, we can't read anything >= 2^32 from the other process
				// so return span [0,2^32)
				if (IntPtr.Size != 8)
					return HexSpan.FromBounds(0, new HexPosition(uint.MaxValue + 1UL));
				NativeMethods.SYSTEM_INFO info;
				NativeMethods.GetSystemInfo(out info);
				var lastAddr = (ulong)info.lpMaximumApplicationAddress.ToInt64();
				// Include the last part so we get a nice even address
				if ((lastAddr & 0xFFFFF) == 0xEFFFF)
					lastAddr += 0x10000;
				return HexSpan.FromBounds(0, new HexPosition(lastAddr) + 1);
			}
			Debug.Fail($"Unsupported bitness: {bitness}");
			return HexSpan.FromBounds(0, HexPosition.MaxEndPosition);
		}

		static ulong GetDefaultPageSize(IntPtr hProcess) => (ulong)Environment.SystemPageSize;

		static int GetBitness(IntPtr hProcess) {
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
				b = NativeMethods.ReadProcessMemory(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(p), new IntPtr(bytesToRead), out sizeRead);
			return !b ? 0 : sizeRead.ToInt64();
		}

		public unsafe override HexPosition Write(HexPosition position, byte[] source, long sourceIndex, long length) {
			if (isReadOnly)
				return 0;
			if (position >= Span.End)
				return 0;
			int bytesToWrite = (int)Math.Min(length, int.MaxValue);
			uint oldProtect;
			bool restoreOldProtect = NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(bytesToWrite), NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
			IntPtr sizeWritten;
			bool b;
			fixed (void* p = &source[sourceIndex])
				b = NativeMethods.WriteProcessMemory(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(p), new IntPtr(bytesToWrite), out sizeWritten);
			if (restoreOldProtect)
				NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)position.ToUInt64()), new IntPtr(bytesToWrite), oldProtect, out oldProtect);
			return !b ? 0 : (int)sizeWritten.ToInt64();
		}

		protected override void DisposeCore() => hProcess = IntPtr.Zero;
	}
}
