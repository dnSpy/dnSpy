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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using dndbg.COM.MetaHost;
using dndbg.Engine;
using dnlib.PE;

namespace dnSpy.Debugger.Dialogs {
	sealed class ManagedProcessesFinder {
		public sealed class Info {
			public int ProcessId;
			public Machine Machine;
			public string Title;
			public string FullPath;
			public CLRTypeAttachInfo Type;

			public Info(Process process, string clrVersion, CLRTypeAttachInfo type) {
				ProcessId = process.Id;
				Machine = IntPtr.Size == 4 ? Machine.I386 : Machine.AMD64;
				Title = string.Empty;
				FullPath = string.Empty;
				Type = type;
				try {
					Title = process.MainWindowTitle;
				} catch { }
				try {
					FullPath = process.MainModule.FileName;
				} catch { }
			}
		}

		public IEnumerable<Info> FindAll(CancellationToken cancellationToken) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			int ourPid = Process.GetCurrentProcess().Id;
			var processes = new List<Process>();
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
				using (var fh = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, process.Id)) {
					if (fh.IsInvalid)
						continue;
				}
				if (Environment.Is64BitOperatingSystem) {
					bool isWow64Process;
					if (NativeMethods.IsWow64Process(process.Handle, out isWow64Process)) {
						if (IntPtr.Size == 4 && !isWow64Process)
							continue;
					}
				}
				if (process.HasExited)
					continue;
				processes.Add(process);

				IEnumUnknown iter;
				int hr = mh.EnumerateLoadedRuntimes(process.Handle, out iter);
				if (hr >= 0) {
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

						var info = new Info(process, sb.ToString(), new DesktopCLRTypeAttachInfo(sb.ToString()));

						yield return info;
					}
				}
			}

			// Finding CoreCLR assemblies is much slower so do it last
			foreach (var process in processes) {
				if (process.HasExited)
					continue;
				ProcessModule[] modules;
				try {
					modules = process.Modules.Cast<ProcessModule>().ToArray();
				}
				catch {
					continue;
				}
				foreach (var module in modules) {
					var moduleFilename = module.FileName;
					var dllName = Path.GetFileName(moduleFilename);
					if (dllName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase)) {
						foreach (var info in TryGetCoreCLRInfos(process, moduleFilename))
							yield return info;
						break;
					}
				}
			}
		}

		IEnumerable<Info> TryGetCoreCLRInfos(Process process, string coreclrFilename) {
			foreach (var ccInfo in CoreCLRHelper.GetCoreCLRInfos(process.Id, coreclrFilename, null))
				yield return new Info(process, ccInfo.CoreCLRTypeInfo.Version, ccInfo.CoreCLRTypeInfo);
		}
	}
}
