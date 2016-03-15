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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger {
	static class NativeMethods {
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtectEx([In] IntPtr hProcess, [In] IntPtr lpAddress, [In] int dwSize, [In] uint flNewProtect, out uint lpflOldProtect);
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("mscoree", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object CLRCreateInstance(ref Guid clsid, ref Guid riid);

		[DllImport("kernel32")]
		public static extern SafeFileHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		const int SYNCHRONIZE = 0x00100000;
		public const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFFF;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool Wow64Process);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);
		[DllImport("kernel32", SetLastError = true)]
		public static extern int GetThreadPriority(IntPtr hThread);

		[DllImport("user32")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
