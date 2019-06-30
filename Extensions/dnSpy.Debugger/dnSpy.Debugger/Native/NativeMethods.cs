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
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Native {
	static unsafe class NativeMethods {
		[DllImport("kernel32")]
		public static extern SafeProcessHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		public const int PROCESS_VM_OPERATION = 0x0008;
		public const int PROCESS_VM_READ = 0x0010;
		public const int PROCESS_VM_WRITE = 0x0020;
		public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

		[DllImport("kernel32", SetLastError = true)]
		public static extern SafeAccessTokenHandle OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
		public const int THREAD_TERMINATE = 0x0001;
		public const int THREAD_SUSPEND_RESUME = 0x0002;
		public const int THREAD_GET_CONTEXT = 0x0008;
		public const int THREAD_SET_CONTEXT = 0x0010;
		public const int THREAD_SET_INFORMATION = 0x0020;
		public const int THREAD_QUERY_INFORMATION = 0x0040;
		public const int THREAD_SET_THREAD_TOKEN = 0x0080;
		public const int THREAD_IMPERSONATE = 0x0100;
		public const int THREAD_DIRECT_IMPERSONATION = 0x0200;
		public const int THREAD_SET_LIMITED_INFORMATION = 0x0400;
		public const int THREAD_QUERY_LIMITED_INFORMATION = 0x0800;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool GetExitCodeThread(IntPtr hThread, out int lpExitCode);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);
		[DllImport("kernel32", SetLastError = true)]
		public static extern int GetThreadPriority(IntPtr hThread);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, void* lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool WriteProcessMemory(IntPtr hProcess, void* lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesWritten);
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtectEx(IntPtr hProcess, void* lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("user32")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		[DllImport("user32")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
