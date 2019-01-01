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

namespace dnSpy.Debugger.DotNet.CorDebug.Native {
	static class NativeMethods {
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

		[DllImport("kernel32", SetLastError = true)]
		public static extern SafeProcessHandle OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
		public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
		const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		const uint SYNCHRONIZE = 0x00100000;
		public const uint PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFFF;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

		[DllImport("mscoree", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object CLRCreateInstance(ref Guid clsid, ref Guid riid);
	}
}
