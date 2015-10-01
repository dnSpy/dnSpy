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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using dndbg.Engine.COM.CorDebug;
using Microsoft.Win32;

namespace dndbg.Engine {
	public struct CoreCLRInfo {
		public int ProcessId;
		public CoreCLRTypeAttachInfo CoreCLRTypeInfo;
		public string CoreCLRFilename;

		public CoreCLRInfo(int pid, string filename, string version) {
			this.ProcessId = pid;
			this.CoreCLRTypeInfo = new CoreCLRTypeAttachInfo(version);
			this.CoreCLRFilename = filename;
		}
	}

	public static class CoreCLRHelper {
		delegate int GetStartupNotificationEvent(uint debuggeePID, out IntPtr phStartupEvent);
		delegate int CloseCLREnumeration(IntPtr pHandleArray, IntPtr pStringArray, uint dwArrayLength);
		delegate int EnumerateCLRs(uint debuggeePID, out IntPtr ppHandleArrayOut, out IntPtr ppStringArrayOut, out uint pdwArrayLengthOut);
		delegate int CreateVersionStringFromModule(uint pidDebuggee, [MarshalAs(UnmanagedType.LPWStr)] string szModuleName, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pBuffer, uint cchBuffer, out uint pdwLength);
		delegate int CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion iDebuggerVersion, [MarshalAs(UnmanagedType.LPWStr)] string szDebuggeeVersion, [MarshalAs(UnmanagedType.IUnknown)] out object ppCordb);
		delegate int CreateDebuggingInterfaceFromVersion([MarshalAs(UnmanagedType.LPWStr)] string szDebuggeeVersion, [MarshalAs(UnmanagedType.IUnknown)] out object ppCordb);

		/// <summary>
		/// Path to <c>dbgshim.dll</c> that will be used to initialize <c>dbgshim.dll</c> if it hasn't
		/// been initialized yet. See also <see cref="CurrentDbgShimPath"/>
		/// </summary>
		public static string DbgShimPath { get; set; }

		/// <summary>
		/// Path to the loaded <c>dbgshim.dll</c>
		/// </summary>
		public static string CurrentDbgShimPath {
			get { return dbgShimState.Filename; }
		}

		public static bool DbgShimInitialized {
			get { return dbgShimState.Handle != IntPtr.Zero; }
		}

		public static bool TryInitializeDbgShim() {
			return GetDbgShimHandle(null) != IntPtr.Zero;
		}

		/// <summary>
		/// Searches for CoreCLR runtimes in a process
		/// </summary>
		/// <param name="pid">Process ID</param>
		/// <param name="runtimePath">Path of CoreCLR.dll or path of the CoreCLR runtime. This is
		/// used to find <c>dbgshim.dll</c> and is only needed if it hasn't been loaded yet. This
		/// value isn't needed if the path to <c>dbgshim.dll</c> exists in the registry or if
		/// <see cref="DbgShimPath"/> has been initialized.</param>
		/// <returns></returns>
		public unsafe static CoreCLRInfo[] GetCoreCLRInfos(int pid, string runtimePath) {
			var dbgshimHandle = GetDbgShimHandle(runtimePath);
			if (dbgshimHandle == IntPtr.Zero)
				return new CoreCLRInfo[0];

			IntPtr pHandleArray, pStringArray;
			uint dwArrayLength;
			int hr = dbgShimState.EnumerateCLRs((uint)pid, out pHandleArray, out pStringArray, out dwArrayLength);
			if (hr < 0 || dwArrayLength == 0)
				return new CoreCLRInfo[0];
			try {
				var ary = new CoreCLRInfo[dwArrayLength];
				var psa = (IntPtr*)pStringArray;
				for (int i = 0; i < ary.Length; i++) {
					string moduleFilename;
					var version = GetVersionStringFromModule((uint)pid, psa[i], out moduleFilename);
					ary[i] = new CoreCLRInfo(pid, moduleFilename, version);
				}

				return ary;
			}
			finally {
				hr = dbgShimState.CloseCLREnumeration(pHandleArray, pStringArray, dwArrayLength);
				Debug.Assert(hr >= 0);
			}
		}

		static string GetVersionStringFromModule(uint pid, IntPtr ps, out string moduleFilename) {
			var sb = new StringBuilder(0x1000);
			moduleFilename = Marshal.PtrToStringUni(ps);
			uint verLen;
			int hr = dbgShimState.CreateVersionStringFromModule(pid, moduleFilename, sb, (uint)sb.MaxCapacity, out verLen);
			if (hr != 0) {
				sb.EnsureCapacity((int)verLen);
				hr = dbgShimState.CreateVersionStringFromModule(pid, moduleFilename, sb, (uint)sb.MaxCapacity, out verLen);
			}
			return hr < 0 ? null : sb.ToString();
		}

		static IntPtr GetDbgShimHandle(string runtimePath, bool useFullPath = false) {
			if (dbgShimState.Handle != IntPtr.Zero)
				return dbgShimState.Handle;

			var path = DbgShimPath;
			if (!File.Exists(path)) {
				path = GetDbgShimPathFromRegistry();
				if (!File.Exists(path)) {
					path = useFullPath ? runtimePath : GetDbgShimPathFromRuntimePath(runtimePath);
					if (!File.Exists(path))
						return IntPtr.Zero;
				}
			}

			// Use the same flags as dbgshim.dll uses when it loads mscordbi.dll
			var handle = NativeMethods.LoadLibraryEx(path, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | NativeMethods.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
			Debug.Assert(handle != IntPtr.Zero);

			var newState = new DbgShimState();
			newState.Filename = path;
			newState.Handle = handle;
			newState.GetStartupNotificationEvent = GetDelegate<GetStartupNotificationEvent>(handle, "GetStartupNotificationEvent");
			newState.CloseCLREnumeration = GetDelegate<CloseCLREnumeration>(handle, "CloseCLREnumeration");
			newState.EnumerateCLRs = GetDelegate<EnumerateCLRs>(handle, "EnumerateCLRs");
			newState.CreateVersionStringFromModule = GetDelegate<CreateVersionStringFromModule>(handle, "CreateVersionStringFromModule");
			newState.CreateDebuggingInterfaceFromVersionEx = GetDelegate<CreateDebuggingInterfaceFromVersionEx>(handle, "CreateDebuggingInterfaceFromVersionEx");
			if (newState.GetStartupNotificationEvent == null ||
				newState.CloseCLREnumeration == null ||
				newState.EnumerateCLRs == null ||
				newState.CreateVersionStringFromModule == null ||
				newState.CreateDebuggingInterfaceFromVersionEx == null) {
				NativeMethods.FreeLibrary(handle);
				return IntPtr.Zero;
			}

			dbgShimState = newState;
			return dbgShimState.Handle;
		}
		static DbgShimState dbgShimState;
		struct DbgShimState {
			public string Filename;
			public IntPtr Handle;
			public GetStartupNotificationEvent GetStartupNotificationEvent;
			public CloseCLREnumeration CloseCLREnumeration;
			public EnumerateCLRs EnumerateCLRs;
			public CreateVersionStringFromModule CreateVersionStringFromModule;
			public CreateDebuggingInterfaceFromVersionEx CreateDebuggingInterfaceFromVersionEx;
		}

		static T GetDelegate<T>(IntPtr handle, string funcName) where T : class {
			var addr = NativeMethods.GetProcAddress(handle, funcName);
			if (addr == null)
				return null;
			return (T)(object)Marshal.GetDelegateForFunctionPointer(addr, typeof(T));
		}

		static string GetDbgShimPathFromRegistry() {
			try {
				using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework"))
					return key.GetValue("DbgPackShimPath") as string;
			}
			catch {
			}
			return null;
		}

		static string GetDbgShimPathFromRuntimePath(string path) {
			try {
				return Path.GetDirectoryName(path) + @"\dbgshim.dll";
			}
			catch {
			}
			return null;
		}

		public static ICorDebug CreateCorDebug(CoreCLRTypeAttachInfo info) {
			// If it's null, we haven't created a CoreCLRTypeInfo...
			if (dbgShimState.Handle == null)
				return null;

			object obj;
			int hr = dbgShimState.CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion.CorDebugVersion_4_0, info.Version, out obj);
			return obj as ICorDebug;
		}

		public unsafe static DnDebugger CreateDnDebugger(DebugProcessOptions options, CoreCLRTypeDebugInfo info, Func<bool> keepWaiting, Func<ICorDebug, uint, DnDebugger> createDnDebugger) {
			var f = info.DbgShimFilename;
			bool useFullPath = true;
			if (string.IsNullOrEmpty(f)) {
				f = info.HostFilename;
				useFullPath = false;
			}
			if (GetDbgShimHandle(f, useFullPath) == IntPtr.Zero)
				throw new Exception(string.Format("Could not load dbgshim.dll: '{0}'", f));

			IntPtr startupEvent = IntPtr.Zero;
			IntPtr hThread = IntPtr.Zero;
			IntPtr pHandleArray = IntPtr.Zero, pStringArray = IntPtr.Zero;
			uint dwArrayLength = 0;
			string version = null;

			var pi = new PROCESS_INFORMATION();
			bool error = true;
			try {
				var dwCreationFlags = options.ProcessCreationFlags ?? DebugProcessOptions.DefaultProcessCreationFlags;
				dwCreationFlags |= ProcessCreationFlags.CREATE_SUSPENDED;
				var si = new STARTUPINFO();
				si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
				var cmdline = " " + (info.HostCommandLine ?? string.Empty) + " \"" + options.Filename + "\" " + (options.CommandLine ?? string.Empty);
				bool b = NativeMethods.CreateProcess(info.HostFilename ?? string.Empty, cmdline, IntPtr.Zero, IntPtr.Zero,
							options.InheritHandles, dwCreationFlags, IntPtr.Zero, options.CurrentDirectory,
							ref si, out pi);
				hThread = pi.hThread;
				if (!b)
					throw new Exception(string.Format("Could not execute '{0}'", options.Filename));

				int hr = dbgShimState.GetStartupNotificationEvent(pi.dwProcessId, out startupEvent);
				if (hr < 0)
					throw new Exception(string.Format("GetStartupNotificationEvent failed: 0x{0:X8}", hr));

				NativeMethods.ResumeThread(hThread);

				const uint WAIT_MS = 1000;
				for (;;) {
					uint res = NativeMethods.WaitForSingleObject(startupEvent, WAIT_MS);
					if (res == 0)
						break;

					if (res == NativeMethods.WAIT_FAILED)
						throw new Exception(string.Format("Error waiting for startup event: 0x{0:X8}", Marshal.GetLastWin32Error()));
					if (res == NativeMethods.WAIT_TIMEOUT) {
						if (keepWaiting())
							continue;
						throw new TimeoutException("Waiting for CoreCLR timed out");
					}
					Debug.Fail(string.Format("Unknown result from WaitForMultipleObjects: 0x{0:X8}", res));
					throw new Exception("Error waiting for startup event");
				}

				hr = dbgShimState.EnumerateCLRs(pi.dwProcessId, out pHandleArray, out pStringArray, out dwArrayLength);
				if (hr < 0 || dwArrayLength == 0)
					throw new Exception("Process started but no CoreCLR found");
				var psa = (IntPtr*)pStringArray;
				var pha = (IntPtr*)pHandleArray;
				string moduleFilename;
				const int index = 0;
				version = GetVersionStringFromModule(pi.dwProcessId, psa[index], out moduleFilename);
				object obj;
				hr = dbgShimState.CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion.CorDebugVersion_4_0, version, out obj);
				var corDebug = obj as ICorDebug;
				if (corDebug == null)
					throw new Exception(string.Format("Could not create a ICorDebug: hr=0x{0:X8}", hr));
				var dbg = createDnDebugger(corDebug, pi.dwProcessId);
				for (uint i = 0; i < dwArrayLength; i++)
					NativeMethods.SetEvent(pha[i]);
				error = false;
				return dbg;
			}
			finally {
				if (startupEvent != IntPtr.Zero)
					NativeMethods.CloseHandle(startupEvent);
				if (hThread != IntPtr.Zero)
					NativeMethods.CloseHandle(hThread);
				if (pHandleArray != IntPtr.Zero)
					dbgShimState.CloseCLREnumeration(pHandleArray, pStringArray, dwArrayLength);
				if (error)
					NativeMethods.TerminateProcess(pi.hProcess, uint.MaxValue);
				if (pi.hProcess != IntPtr.Zero)
					NativeMethods.CloseHandle(pi.hProcess);
			}
		}
	}
}
