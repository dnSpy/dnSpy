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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.Attach {
	// MS forces us to use an undocumented function and structures to get the command line.
	// It's also the same company that decided that a 32-bit VS was a great idea.
	unsafe static class Win32CommandLineProvider {
		[DllImport("ntdll", EntryPoint = "NtQueryInformationProcess", SetLastError = true)]
		static extern int NtQueryInformationProcess32(IntPtr ProcessHandle, int ProcessInformationClass, [In] ref PROCESS_BASIC_INFORMATION32 ProcessInformation, int ProcessInformationLength, out int ReturnLength);
		[DllImport("ntdll", EntryPoint = "NtQueryInformationProcess", SetLastError = true)]
		static extern int NtQueryInformationProcess64(IntPtr ProcessHandle, int ProcessInformationClass, [In] ref PROCESS_BASIC_INFORMATION64 ProcessInformation, int ProcessInformationLength, out int ReturnLength);
		const int ProcessBasicInformation = 0;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct PROCESS_BASIC_INFORMATION32 {
			public uint Reserved1;
			public uint PebBaseAddress;
			public uint Reserved2a;
			public uint Reserved2b;
			public uint UniqueProcessId;
			public uint Reserved3;
			public static readonly int SIZE = 6 * 4;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct PROCESS_BASIC_INFORMATION64 {
			public ulong Reserved1;
			public ulong PebBaseAddress;
			public ulong Reserved2a;
			public ulong Reserved2b;
			public ulong UniqueProcessId;
			public ulong Reserved3;
			public static readonly int SIZE = 6 * 8;
		}

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, void* lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct UNICODE_STRING32 {
			public ushort Length;
			public ushort MaximumLength;
			public uint Buffer;
			public const int SIZE = 8;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct UNICODE_STRING64 {
			public ushort Length;
			public ushort MaximumLength;
			public uint Padding;
			public ulong Buffer;
			public const int SIZE = 16;
		}

		public static string? TryGetCommandLine(IntPtr hProcess) {
			try {
				return TryGetCommandLineCore(hProcess);
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			catch {
				// WTF, I blame MS, there should be a public API to get the command line of another process
			}
			Debug.Fail("Couldn't get the command line of a process");
			return null;
		}

		const int ProcessParametersOffset32 = 0x10;
		const int ProcessParametersOffset64 = 0x20;

		const int CommandLineOffset32 = 0x40;
		const int CommandLineOffset64 = 0x70;

		static string? TryGetCommandLineCore(IntPtr hProcess) {
			int ptrSize = IntPtr.Size;

			ushort cmdlineLength;
			IntPtr cmdlineBuffer;
			bool b;
			if (ptrSize == 4) {
				PROCESS_BASIC_INFORMATION32 pbi = default;
				int hr = NtQueryInformationProcess32(hProcess, ProcessBasicInformation, ref pbi, PROCESS_BASIC_INFORMATION32.SIZE, out int returnLength);
				if (hr != 0 || pbi.PebBaseAddress == 0)
					return null;

				IntPtr userProcessParamsAddr;
				b = ReadProcessMemory(hProcess, (byte*)pbi.PebBaseAddress + ProcessParametersOffset32, &userProcessParamsAddr, new IntPtr(ptrSize), out var bytesRead);
				if (!b || bytesRead.ToInt64() != ptrSize)
					return null;

				UNICODE_STRING32 unicodeString;
				b = ReadProcessMemory(hProcess, (byte*)userProcessParamsAddr + CommandLineOffset32, &unicodeString, new IntPtr(UNICODE_STRING32.SIZE), out bytesRead);
				if (!b || bytesRead.ToInt64() != UNICODE_STRING32.SIZE)
					return null;
				cmdlineLength = unicodeString.Length;
				cmdlineBuffer = IntPtr.Size == 4 ? new IntPtr((int)unicodeString.Buffer) : new IntPtr(unicodeString.Buffer);
			}
			else {
				PROCESS_BASIC_INFORMATION64 pbi = default;
				int hr = NtQueryInformationProcess64(hProcess, ProcessBasicInformation, ref pbi, PROCESS_BASIC_INFORMATION64.SIZE, out int returnLength);
				if (hr != 0 || pbi.PebBaseAddress == 0)
					return null;

				IntPtr userProcessParamsAddr;
				b = ReadProcessMemory(hProcess, (byte*)pbi.PebBaseAddress + ProcessParametersOffset64, &userProcessParamsAddr, new IntPtr(ptrSize), out var bytesRead);
				if (!b || bytesRead.ToInt64() != ptrSize)
					return null;

				UNICODE_STRING64 unicodeString;
				b = ReadProcessMemory(hProcess, (byte*)userProcessParamsAddr + CommandLineOffset64, &unicodeString, new IntPtr(UNICODE_STRING64.SIZE), out bytesRead);
				if (!b || bytesRead.ToInt64() != UNICODE_STRING64.SIZE)
					return null;
				cmdlineLength = unicodeString.Length;
				cmdlineBuffer = new IntPtr((long)unicodeString.Buffer);
			}

			if (cmdlineLength <= 0 || cmdlineBuffer == IntPtr.Zero)
				return string.Empty;
			cmdlineLength &= 0xFFFE;
			var cmdLineChars = new char[cmdlineLength / 2];
			fixed (void* p = cmdLineChars) {
				b = ReadProcessMemory(hProcess, cmdlineBuffer.ToPointer(), p, new IntPtr(cmdlineLength), out var bytesRead);
				if (!b || bytesRead.ToInt64() != cmdlineLength)
					return null;
			}

			return new string(cmdLineChars);
		}
	}
}
