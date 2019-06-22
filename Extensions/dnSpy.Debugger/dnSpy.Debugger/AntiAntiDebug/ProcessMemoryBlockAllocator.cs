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
using System.Collections.Generic;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class ProcessMemoryBlockAllocator {
		public DbgProcess Process => process;

		readonly DbgProcess process;
		readonly IntPtr hProcess;
		readonly List<ProcessMemoryBlockImpl> allocatedMemory;

		// Must be a power of 2
		const uint ALLOC_SIZE = 0x10000;

		public ProcessMemoryBlockAllocator(DbgProcess process) {
			this.process = process;
			allocatedMemory = new List<ProcessMemoryBlockImpl>();
			hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, (uint)process.Id);
			if (hProcess == IntPtr.Zero || hProcess == (IntPtr)(-1))
				throw new DbgHookException($"Couldn't open process {process.Id} ({process.Name})");
		}

		ProcessMemoryBlockImpl? GetCloseBlock(ulong moduleAddress, ulong moduleEndAddress) {
			foreach (var mem in allocatedMemory) {
				if (process.Architecture != DbgArchitecture.X64)
					return mem;
				if (!IsClose64(moduleAddress, moduleEndAddress, mem.CurrentAddress))
					continue;
				if (!IsClose64(moduleAddress, moduleEndAddress, mem.EndAddress))
					continue;
				return mem;
			}

			return null;
		}

		static bool IsClose64(ulong start, ulong end, ulong addr) {
			long diff;

			diff = (long)(addr - start);
			if (!(int.MinValue <= diff && diff <= int.MaxValue))
				return false;

			diff = (long)(addr - end);
			if (!(int.MinValue <= diff && diff <= int.MaxValue))
				return false;

			return true;
		}

		static ulong AlignDownPage(ulong addr) => addr & ~((ulong)ALLOC_SIZE - 1);

		public ProcessMemoryBlock Allocate(ulong moduleAddress, ulong moduleEndAddress) {
			var mem = GetCloseBlock(moduleAddress, moduleEndAddress);
			if (!(mem is null))
				return mem;

			IntPtr memPtr;

			// Only needed if target process is X64 (because of rip-relative addressing).
			// Also, we can't allocate high mem if we're a 32-bit process so don't even try.
			if (IntPtr.Size == 8 && process.Architecture == DbgArchitecture.X64) {
				ulong addr_lo = AlignDownPage(moduleAddress) - ALLOC_SIZE;
				ulong addr_hi = AlignDownPage(moduleEndAddress) + ALLOC_SIZE;
				while (true) {
					bool lo = IsClose64(moduleAddress, moduleEndAddress, addr_lo) && IsClose64(moduleAddress, moduleEndAddress, addr_lo + ALLOC_SIZE);
					if (lo) {
						memPtr = NativeMethods.VirtualAllocEx(hProcess, (IntPtr)addr_lo, (IntPtr)ALLOC_SIZE, NativeMethods.MEM_RESERVE | NativeMethods.MEM_COMMIT, NativeMethods.PAGE_EXECUTE_READ);
						if (memPtr != IntPtr.Zero)
							return AddNewMemory(memPtr, ALLOC_SIZE);
						addr_lo -= ALLOC_SIZE;
					}

					bool hi = IsClose64(moduleAddress, moduleEndAddress, addr_hi) && IsClose64(moduleAddress, moduleEndAddress, addr_hi + ALLOC_SIZE);
					if (hi) {
						memPtr = NativeMethods.VirtualAllocEx(hProcess, (IntPtr)addr_hi, (IntPtr)ALLOC_SIZE, NativeMethods.MEM_RESERVE | NativeMethods.MEM_COMMIT, NativeMethods.PAGE_EXECUTE_READ);
						if (memPtr != IntPtr.Zero)
							return AddNewMemory(memPtr, ALLOC_SIZE);
						addr_hi += ALLOC_SIZE;
					}

					if (!lo && !hi)
						break;
				}
			}

			if (allocatedMemory.Count != 0)
				return allocatedMemory[0];

			memPtr = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)ALLOC_SIZE, NativeMethods.MEM_COMMIT, NativeMethods.PAGE_EXECUTE_READ);
			if (memPtr == IntPtr.Zero)
				throw new DbgHookException("Couldn't allocate memory");
			return AddNewMemory(memPtr, ALLOC_SIZE);
		}

		ProcessMemoryBlock AddNewMemory(IntPtr memPtr, uint size) {
			var mem = new ProcessMemoryBlockImpl((ulong)memPtr.ToInt64(), (int)size);
			allocatedMemory.Add(mem);
			return mem;
		}

		public void Write() {
			foreach (var mem in allocatedMemory)
				mem.WriteTo(process);
		}

		public void Dispose() => NativeMethods.CloseHandle(hProcess);
	}
}
