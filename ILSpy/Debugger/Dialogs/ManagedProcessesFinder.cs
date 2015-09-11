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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using dndbg.Engine.COM.MetaHost;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Dialogs {
	sealed class ManagedProcessesFinder {
		[DllImport("mscoree", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object CLRCreateInstance(ref Guid clsid, ref Guid riid);

		[DllImport("kernel32")]
		static extern SafeFileHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		const int SYNCHRONIZE = 0x00100000;
		const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFFF;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool Wow64Process);

		public sealed class Info {
			public int ProcessId;
			public string CLRVersion;
			public int IntPtrSize;
			public string Title;
			public string FullPath;
		}

		public IEnumerable<Info> FindAll(CancellationToken cancellationToken) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)CLRCreateInstance(ref clsid, ref riid);

			int ourPid = Process.GetCurrentProcess().Id;
			foreach (var process in Process.GetProcesses()) {
				cancellationToken.ThrowIfCancellationRequested();
				int pid;
				try {
					pid = process.Id;
				}
				catch {
					continue;
				}
				if (pid == ourPid)
					continue;
				// Prevent slow exceptions by filtering out processes we can't access
				using (var fh = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id)) {
					if (fh.IsInvalid)
						continue;
				}
				bool isWow64Process;
				if (IsWow64Process(process.Handle, out isWow64Process)) {
					if (IntPtr.Size == 4 && !isWow64Process)
						continue;
				}
				if (process.HasExited)
					continue;

				IEnumUnknown iter;
				int hr = mh.EnumerateLoadedRuntimes(process.Handle, out iter);
				if (hr < 0)
					continue;
				for (;;) {
					object obj;
					uint fetched;
					hr = iter.Next(1, out obj, out fetched);
					if (hr < 0 || fetched == 0)
						break;

					var rtInfo = (ICLRRuntimeInfo)obj;
					uint chBuffer = 0;
					var sb = new StringBuilder(300);
					hr = rtInfo.GetVersionString(sb, ref chBuffer);
					sb.EnsureCapacity((int)chBuffer);
					hr = rtInfo.GetVersionString(sb, ref chBuffer);

					var info = new Info();
					info.ProcessId = pid;
					info.IntPtrSize = IntPtr.Size;
					info.CLRVersion = sb.ToString();
					info.Title = string.Empty;
					info.FullPath = string.Empty;
					try {
						info.Title = process.MainWindowTitle;
					} catch { }
					try {
						info.FullPath = process.MainModule.FileName;
					} catch { }
					yield return info;
				}
			}
		}
	}
}
