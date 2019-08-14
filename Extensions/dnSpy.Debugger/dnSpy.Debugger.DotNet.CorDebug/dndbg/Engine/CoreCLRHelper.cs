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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using dndbg.COM.CorDebug;
using dnSpy.Debugger.Shared;

namespace dndbg.Engine {
	readonly struct CoreCLRInfo {
		public int ProcessId { get; }
		public CoreCLRTypeAttachInfo CoreCLRTypeInfo { get; }

		public CoreCLRInfo(int pid, string coreclrFilename, string? version, string dbgShimFilename) {
			ProcessId = pid;
			CoreCLRTypeInfo = new CoreCLRTypeAttachInfo(version, dbgShimFilename, coreclrFilename);
		}
	}

	static class CoreCLRHelper {
		static readonly string dbgshimFilename = FileUtilities.GetNativeDllFilename("dbgshim");
		delegate int GetStartupNotificationEvent(uint debuggeePID, out IntPtr phStartupEvent);
		delegate int CloseCLREnumeration(IntPtr pHandleArray, IntPtr pStringArray, uint dwArrayLength);
		delegate int EnumerateCLRs(uint debuggeePID, out IntPtr ppHandleArrayOut, out IntPtr ppStringArrayOut, out uint pdwArrayLengthOut);
		delegate int CreateVersionStringFromModule(uint pidDebuggee, [MarshalAs(UnmanagedType.LPWStr)] string szModuleName, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pBuffer, uint cchBuffer, out uint pdwLength);
		delegate int CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion iDebuggerVersion, [MarshalAs(UnmanagedType.LPWStr)] string? szDebuggeeVersion, [MarshalAs(UnmanagedType.IUnknown)] out object ppCordb);

		/// <summary>
		/// Searches for CoreCLR runtimes in a process
		/// </summary>
		/// <param name="pid">Process ID</param>
		/// <param name="runtimePath">Path of CoreCLR.dll or path of the CoreCLR runtime. This is
		/// used to find <c>dbgshim.dll</c> if <paramref name="dbgshimPath"/> is null</param>
		/// <param name="dbgshimPath">Filename of dbgshim.dll or null if we should look in
		/// <paramref name="runtimePath"/></param>
		/// <returns></returns>
		public unsafe static CoreCLRInfo[] GetCoreCLRInfos(int pid, string? runtimePath, string? dbgshimPath) {
			var dbgShimState = GetOrCreateDbgShimState(runtimePath, dbgshimPath);
			if (dbgShimState is null)
				return Array.Empty<CoreCLRInfo>();
			int hr = dbgShimState.EnumerateCLRs!((uint)pid, out var pHandleArray, out var pStringArray, out uint dwArrayLength);
			if (hr < 0 || dwArrayLength == 0)
				return Array.Empty<CoreCLRInfo>();
			try {
				var ary = new CoreCLRInfo[dwArrayLength];
				var psa = (IntPtr*)pStringArray;
				for (int i = 0; i < ary.Length; i++) {
					var version = GetVersionStringFromModule(dbgShimState, (uint)pid, psa[i], out string coreclrFilename);
					ary[i] = new CoreCLRInfo(pid, coreclrFilename, version, dbgShimState.Filename!);
				}

				return ary;
			}
			finally {
				hr = dbgShimState.CloseCLREnumeration!(pHandleArray, pStringArray, dwArrayLength);
				Debug.Assert(hr >= 0);
			}
		}

		static string? GetVersionStringFromModule(DbgShimState dbgShimState, uint pid, IntPtr ps, out string coreclrFilename) {
			var sb = new StringBuilder(0x1000);
			coreclrFilename = Marshal.PtrToStringUni(ps)!;
			int hr = dbgShimState.CreateVersionStringFromModule!(pid, coreclrFilename, sb, (uint)sb.Capacity, out uint verLen);
			if (hr != 0) {
				sb.EnsureCapacity((int)verLen);
				hr = dbgShimState.CreateVersionStringFromModule(pid, coreclrFilename, sb, (uint)sb.Capacity, out verLen);
			}
			return hr < 0 ? null : sb.ToString();
		}

		static List<string> GetDbgShimPaths(string? runtimePath, string? dbgshimPath) {
			var list = new List<string>(3);
			if (File.Exists(dbgshimPath))
				list.Add(dbgshimPath!);
			if (!string2.IsNullOrEmpty(runtimePath)) {
				var dbgshimPathTemp = GetDbgShimPathFromRuntimePath(runtimePath);
				if (File.Exists(dbgshimPathTemp))
					list.Add(dbgshimPathTemp!);
			}
			return list;
		}

		static DbgShimState? GetOrCreateDbgShimState(string? runtimePath, string? dbgshimPath) {
			var paths = GetDbgShimPaths(runtimePath, dbgshimPath);
			DbgShimState? dbgShimState;
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
			if (dbgShimState.GetStartupNotificationEvent is null ||
				dbgShimState.CloseCLREnumeration is null ||
				dbgShimState.EnumerateCLRs is null ||
				dbgShimState.CreateVersionStringFromModule is null ||
				dbgShimState.CreateDebuggingInterfaceFromVersionEx is null) {
				NativeMethods.FreeLibrary(handle);
				return null;
			}

			dbgShimStateDict.Add(dbgShimState.Filename, dbgShimState);
			return dbgShimState;
		}
		static readonly Dictionary<string, DbgShimState> dbgShimStateDict = new Dictionary<string, DbgShimState>(StringComparer.OrdinalIgnoreCase);
		sealed class DbgShimState {
			public string? Filename;
			public IntPtr Handle;
			public GetStartupNotificationEvent? GetStartupNotificationEvent;
			public CloseCLREnumeration? CloseCLREnumeration;
			public EnumerateCLRs? EnumerateCLRs;
			public CreateVersionStringFromModule? CreateVersionStringFromModule;
			public CreateDebuggingInterfaceFromVersionEx? CreateDebuggingInterfaceFromVersionEx;
		}

		static T? GetDelegate<T>(IntPtr handle, string funcName) where T : class {
			var addr = NativeMethods.GetProcAddress(handle, funcName);
			if (addr == IntPtr.Zero)
				return null;
			return (T)(object)Marshal.GetDelegateForFunctionPointer(addr, typeof(T));
		}

		static string? GetDbgShimPathFromRuntimePath(string path) {
			try {
				return Path.Combine(Path.GetDirectoryName(path)!, dbgshimFilename);
			}
			catch {
			}
			return null;
		}

		public static ICorDebug? CreateCorDebug(int pid, CoreCLRTypeAttachInfo info, out string coreclrFilename, out string? otherVersion) {
			string? clrPath = info.CoreCLRFilename;
			otherVersion = info.Version;

			if (clrPath is null || otherVersion is null) {
				var infos = GetCoreCLRInfos(pid, runtimePath: null, dbgshimPath: info.DbgShimFilename);
				if (infos.Length != 0) {
					clrPath ??= infos[0].CoreCLRTypeInfo.CoreCLRFilename;
					otherVersion ??= infos[0].CoreCLRTypeInfo.Version;
				}
				else
					throw new ArgumentException("Couldn't find a CoreCLR process");
			}
			coreclrFilename = clrPath ?? throw new ArgumentException($"Couldn't get the CLR path");

			var dbgShimState = GetOrCreateDbgShimState(null, info.DbgShimFilename);
			if (dbgShimState is null)
				return null;

			int hr = dbgShimState.CreateDebuggingInterfaceFromVersionEx!(CorDebugInterfaceVersion.CorDebugVersion_4_0, otherVersion, out object obj);
			return obj as ICorDebug;
		}

		public unsafe static DnDebugger CreateDnDebugger(DebugProcessOptions options, CoreCLRTypeDebugInfo info, IntPtr outputHandle, IntPtr errorHandle, Func<bool> keepWaiting, Func<ICorDebug, string, uint, string?, DnDebugger> createDnDebugger) {
			var dbgShimState = GetOrCreateDbgShimState(info.HostFilename, info.DbgShimFilename);
			if (dbgShimState is null)
				throw new Exception($"Could not load {dbgshimFilename}: '{info.DbgShimFilename}' . Make sure you use the {IntPtr.Size * 8}-bit version");

			var startupEvent = IntPtr.Zero;
			var hThread = IntPtr.Zero;
			IntPtr pHandleArray = IntPtr.Zero, pStringArray = IntPtr.Zero;
			uint dwArrayLength = 0;

			bool useHost = !(info.HostFilename is null);
			var pi = new PROCESS_INFORMATION();
			bool error = true, calledSetEvent = false;
			try {
				bool inheritHandles = options.InheritHandles;
				var dwCreationFlags = options.ProcessCreationFlags ?? DebugProcessOptions.DefaultProcessCreationFlags;
				dwCreationFlags |= ProcessCreationFlags.CREATE_SUSPENDED;
				var si = new STARTUPINFO();
				si.hStdOutput = outputHandle;
				si.hStdError = errorHandle;
				if (si.hStdOutput != IntPtr.Zero || si.hStdError != IntPtr.Zero) {
					si.dwFlags |= STARTUPINFO.STARTF_USESTDHANDLES;
					inheritHandles = true;
				}
				si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
				string cmdline;
				if (useHost)
					cmdline = "\"" + info.HostFilename + "\" " + info.HostCommandLine + " \"" + options.Filename + "\"" + (string.IsNullOrEmpty(options.CommandLine) ? string.Empty : " " + options.CommandLine);
				else
					cmdline = "\"" + options.Filename + "\"" + (string.IsNullOrEmpty(options.CommandLine) ? string.Empty : " " + options.CommandLine);
				var env = Win32EnvironmentStringBuilder.CreateEnvironmentUnicodeString(options.Environment!);
				dwCreationFlags |= ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT;
				var appName = useHost ? info.HostFilename : options.Filename;
				bool b = NativeMethods.CreateProcess(appName ?? string.Empty, cmdline, IntPtr.Zero, IntPtr.Zero,
							inheritHandles, dwCreationFlags, env, options.CurrentDirectory,
							ref si, out pi);
				hThread = pi.hThread;
				if (!b)
					throw new Exception($"Could not execute '{options.Filename}'");

				int hr = dbgShimState.GetStartupNotificationEvent!(pi.dwProcessId, out startupEvent);
				if (hr < 0)
					throw new Exception($"GetStartupNotificationEvent failed: 0x{hr:X8}");

				NativeMethods.ResumeThread(hThread);

				const uint WAIT_MS = 5000;
				for (;;) {
					uint res = NativeMethods.WaitForSingleObject(startupEvent, WAIT_MS);
					if (res == 0)
						break;

					if (res == NativeMethods.WAIT_FAILED)
						throw new Exception($"Error waiting for startup event: 0x{Marshal.GetLastWin32Error():X8}");
					if (res == NativeMethods.WAIT_TIMEOUT) {
						if (keepWaiting())
							continue;
						throw new TimeoutException("Waiting for CoreCLR timed out. Debug 32-bit .NET Core apps with 32-bit dnSpy (dnSpy-x86.exe), and 64-bit .NET Core apps with 64-bit dnSpy (dnSpy.exe).");
					}
					Debug.Fail($"Unknown result from WaitForMultipleObjects: 0x{res:X8}");
					throw new Exception("Error waiting for startup event");
				}

				hr = dbgShimState.EnumerateCLRs!(pi.dwProcessId, out pHandleArray, out pStringArray, out dwArrayLength);
				if (hr < 0 || dwArrayLength == 0) {
					// CoreCLR doesn't give us a good error code if we try to debug a .NET Core app
					// with an incompatible bitness:
					//		x86 tries to debug x64: hr == 0x8007012B (ERROR_PARTIAL_COPY)
					//		x64 tries to debug x86: hr == 0x00000000 && dwArrayLength == 0x00000000
					if (IntPtr.Size == 4 && (uint)hr == 0x8007012B)
						throw new StartDebuggerException(StartDebuggerError.UnsupportedBitness);
					if (IntPtr.Size == 8 && hr == 0 && dwArrayLength == 0)
						throw new StartDebuggerException(StartDebuggerError.UnsupportedBitness);
					throw new Exception("Process started but no CoreCLR found");
				}
				var psa = (IntPtr*)pStringArray;
				var pha = (IntPtr*)pHandleArray;
				const int index = 0;
				var version = GetVersionStringFromModule(dbgShimState, pi.dwProcessId, psa[index], out string coreclrFilename);
				hr = dbgShimState.CreateDebuggingInterfaceFromVersionEx!(CorDebugInterfaceVersion.CorDebugVersion_4_0, version, out object obj);
				var corDebug = obj as ICorDebug;
				if (corDebug is null)
					throw new Exception($"Could not create a ICorDebug: hr=0x{hr:X8}");
				var dbg = createDnDebugger(corDebug, coreclrFilename, pi.dwProcessId, version);
				for (uint i = 0; i < dwArrayLength; i++)
					NativeMethods.SetEvent(pha[i]);
				calledSetEvent = true;
				error = false;
				return dbg;
			}
			finally {
				if (!calledSetEvent && pHandleArray != IntPtr.Zero && dwArrayLength != 0) {
					var pha = (IntPtr*)pHandleArray;
					for (uint i = 0; i < dwArrayLength; i++)
						NativeMethods.SetEvent(pha[i]);
				}
				if (startupEvent != IntPtr.Zero)
					NativeMethods.CloseHandle(startupEvent);
				if (hThread != IntPtr.Zero)
					NativeMethods.CloseHandle(hThread);
				if (pHandleArray != IntPtr.Zero)
					dbgShimState.CloseCLREnumeration!(pHandleArray, pStringArray, dwArrayLength);
				if (error)
					NativeMethods.TerminateProcess(pi.hProcess, uint.MaxValue);
				if (pi.hProcess != IntPtr.Zero)
					NativeMethods.CloseHandle(pi.hProcess);
			}
		}
	}
}
