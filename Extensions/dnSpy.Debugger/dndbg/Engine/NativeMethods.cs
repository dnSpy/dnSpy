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
using System.Runtime.InteropServices;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	static class NativeMethods {
		[DllImport("mscoree", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object CLRCreateInstance([In] ref Guid clsid, [In] ref Guid riid);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle([In] IntPtr hObject);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr LoadLibraryEx([In, MarshalAs(UnmanagedType.LPStr)] string lpFileName, IntPtr hFile, uint dwFlags);
		public const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
		public const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr GetProcAddress([In] IntPtr hModule, [In, MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool FreeLibrary([In] IntPtr hModule);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CreateProcess([In] string lpApplicationName, [In, Out] string lpCommandLine, [In] IntPtr lpProcessAttributes, [In] IntPtr lpThreadAttributes, [In] bool bInheritHandles, [In] ProcessCreationFlags dwCreationFlags, [In] IntPtr lpEnvironment, [In] string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool TerminateProcess([In] IntPtr hProcess, [In] uint uExitCode);

		[DllImport("kernel32", SetLastError = true)]
		public static extern uint ResumeThread(IntPtr hThread);

		[DllImport("kernel32", SetLastError = true)]
		public static extern uint WaitForSingleObject([In] IntPtr hHandle, [In] uint dwMilliseconds);
		public const uint WAIT_FAILED = 0xFFFFFFFF;
		public const uint WAIT_TIMEOUT = 0x00000102;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool SetEvent([In] IntPtr hEvent);
	}
}
