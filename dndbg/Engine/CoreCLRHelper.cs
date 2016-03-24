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
using System.Runtime.InteropServices;
using System.Text;
using dndbg.COM.CorDebug;
using Microsoft.Win32;

namespace dndbg.Engine {
	public struct CoreCLRInfo {
		public int ProcessId;
		public CoreCLRTypeAttachInfo CoreCLRTypeInfo;
		public string CoreCLRFilename;

		public CoreCLRInfo(int pid, string filename, string version, string dbgShimFilename) {
			this.ProcessId = pid;
			this.CoreCLRTypeInfo = new CoreCLRTypeAttachInfo(version, dbgShimFilename);
			this.CoreCLRFilename = filename;
		}
	}

	public static class CoreCLRHelper {
		const string DBGSHIM_FILENAME = "dbgshim.dll";
		delegate int GetStartupNotificationEvent(uint debuggeePID, out IntPtr phStartupEvent);
		delegate int CloseCLREnumeration(IntPtr pHandleArray, IntPtr pStringArray, uint dwArrayLength);
		delegate int EnumerateCLRs(uint debuggeePID, out IntPtr ppHandleArrayOut, out IntPtr ppStringArrayOut, out uint pdwArrayLengthOut);
		delegate int CreateVersionStringFromModule(uint pidDebuggee, [MarshalAs(UnmanagedType.LPWStr)] string szModuleName, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pBuffer, uint cchBuffer, out uint pdwLength);
		delegate int CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion iDebuggerVersion, [MarshalAs(UnmanagedType.LPWStr)] string szDebuggeeVersion, [MarshalAs(UnmanagedType.IUnknown)] out object ppCordb);
		delegate int CreateDebuggingInterfaceFromVersion([MarshalAs(UnmanagedType.LPWStr)] string szDebuggeeVersion, [MarshalAs(UnmanagedType.IUnknown)] out object ppCordb);

		/// <summary>
		/// Path to <c>dbgshim.dll</c> that will be used to initialize <c>dbgshim.dll</c> if it hasn't
		/// been initialized yet.
		/// </summary>
		public static string DbgShimPath { get; set; }

		/// <summary>
		/// Searches for CoreCLR runtimes in a process
		/// </summary>
		/// <param name="pid">Process ID</param>
		/// <param name="runtimePath">Path of CoreCLR.dll or path of the CoreCLR runtime. This is
		/// used to find <c>dbgshim.dll</c> if <paramref name="dbgshimPath"/> is null</param>
		/// <param name="dbgshimPath">Filename of dbgshim.dll or null if we should look in
		/// <paramref name="runtimePath"/></param>
		/// <returns></returns>
		public unsafe static CoreCLRInfo[] GetCoreCLRInfos(int pid, string runtimePath, string dbgshimPath) {
			var dbgShimState = GetOrCreateDbgShimState(runtimePath, dbgshimPath);
			if (dbgShimState == null)
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
					var version = GetVersionStringFromModule(dbgShimState, (uint)pid, psa[i], out moduleFilename);
					ary[i] = new CoreCLRInfo(pid, moduleFilename, version, dbgShimState.Filename);
				}

				return ary;
			}
			finally {
				hr = dbgShimState.CloseCLREnumeration(pHandleArray, pStringArray, dwArrayLength);
				Debug.Assert(hr >= 0);
			}
		}

		static string GetVersionStringFromModule(DbgShimState dbgShimState, uint pid, IntPtr ps, out string moduleFilename) {
			var sb = new StringBuilder(0x1000);
			moduleFilename = Marshal.PtrToStringUni(ps);
			uint verLen;
			int hr = dbgShimState.CreateVersionStringFromModule(pid, moduleFilename, sb, (uint)sb.Capacity, out verLen);
			if (hr != 0) {
				sb.EnsureCapacity((int)verLen);
				hr = dbgShimState.CreateVersionStringFromModule(pid, moduleFilename, sb, (uint)sb.Capacity, out verLen);
			}
			return hr < 0 ? null : sb.ToString();
		}

		static List<string> GetDbgShimPaths(string runtimePath, string dbgshimPath) {
			var list = new List<string>(3);
			if (File.Exists(dbgshimPath))
				list.Add(dbgshimPath);
			if (!string.IsNullOrEmpty(runtimePath)) {
				var dbgshimPathTemp = GetDbgShimPathFromRuntimePath(runtimePath);
				if (File.Exists(dbgshimPathTemp))
					list.Add(dbgshimPathTemp);
			}
			if (File.Exists(DbgShimPath))
				list.Add(DbgShimPath);
			var s = GetDbgShimPathFromRegistry();
			if (File.Exists(s))
				list.Add(s);
			return list;
		}

		static DbgShimState GetOrCreateDbgShimState(string runtimePath, string dbgshimPath) {
			var paths = GetDbgShimPaths(runtimePath, dbgshimPath);
			DbgShimState dbgShimState;
			foreach (var path in paths) {
				if (dbgShimStateDict.TryGetValue(path, out dbgShimState))
					return dbgShimState;
			}

			if (paths.Count == 0)
				return null;
			dbgshimPath = paths[0];

			// Use the same flags as dbgshim.dll uses when it loads mscordbi.dll
			var handle = NativeMethods.LoadLibraryEx(dbgshimPath, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | NativeMethods.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
			if (handle == IntPtr.Zero)
				return null;	// eg. it's x86 but we're x64 or vice versa, or it's not a valid PE file

			dbgShimState = new DbgShimState();
			dbgShimState.Filename = dbgshimPath;
			dbgShimState.Handle = handle;
			dbgShimState.GetStartupNotificationEvent = GetDelegate<GetStartupNotificationEvent>(handle, "GetStartupNotificationEvent");
			dbgShimState.CloseCLREnumeration = GetDelegate<CloseCLREnumeration>(handle, "CloseCLREnumeration");
			dbgShimState.EnumerateCLRs = GetDelegate<EnumerateCLRs>(handle, "EnumerateCLRs");
			dbgShimState.CreateVersionStringFromModule = GetDelegate<CreateVersionStringFromModule>(handle, "CreateVersionStringFromModule");
			dbgShimState.CreateDebuggingInterfaceFromVersionEx = GetDelegate<CreateDebuggingInterfaceFromVersionEx>(handle, "CreateDebuggingInterfaceFromVersionEx");
			if (dbgShimState.GetStartupNotificationEvent == null ||
				dbgShimState.CloseCLREnumeration == null ||
				dbgShimState.EnumerateCLRs == null ||
				dbgShimState.CreateVersionStringFromModule == null ||
				dbgShimState.CreateDebuggingInterfaceFromVersionEx == null) {
				NativeMethods.FreeLibrary(handle);
				return null;
			}

			dbgShimStateDict.Add(dbgShimState.Filename, dbgShimState);
			return dbgShimState;
		}
		static readonly Dictionary<string, DbgShimState> dbgShimStateDict = new Dictionary<string, DbgShimState>(StringComparer.OrdinalIgnoreCase);
		sealed class DbgShimState {
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

		// We'd most likely find the Silverlight dbgshim.dll in the registry (check the Wow6432Node
		// path), so disable this method.
		static readonly bool enable_GetDbgShimPathFromRegistry = false;
		static string GetDbgShimPathFromRegistry() {
			if (!enable_GetDbgShimPathFromRegistry)
				return null;
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
				return Path.Combine(Path.GetDirectoryName(path), DBGSHIM_FILENAME);
			}
			catch {
			}
			return null;
		}

		public static ICorDebug CreateCorDebug(CoreCLRTypeAttachInfo info) {
			var dbgShimState = GetOrCreateDbgShimState(null, info.DbgShimFilename);
			if (dbgShimState == null)
				return null;

			object obj;
			int hr = dbgShimState.CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion.CorDebugVersion_4_0, info.Version, out obj);
			return obj as ICorDebug;
		}

		public unsafe static DnDebugger CreateDnDebugger(DebugProcessOptions options, CoreCLRTypeDebugInfo info, Func<bool> keepWaiting, Func<ICorDebug, uint, DnDebugger> createDnDebugger) {
			var dbgShimState = GetOrCreateDbgShimState(info.HostFilename, info.DbgShimFilename);
			if (dbgShimState == null)
				throw new Exception(string.Format("Could not load dbgshim.dll: '{0}' . Make sure you use the {1}-bit version", info.DbgShimFilename, IntPtr.Size * 8));

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
				version = GetVersionStringFromModule(dbgShimState, pi.dwProcessId, psa[index], out moduleFilename);
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
