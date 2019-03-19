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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.Shared;
using dnSpy.Debugger.Utilities;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Impl {
	unsafe sealed class DbgProcessImpl : DbgProcess, IIsRunningProvider {
		public override DbgManager DbgManager => owner;
		public override int Id { get; }
		public override int Bitness { get; }
		public override DbgArchitecture Architecture { get; }
		public override DbgOperatingSystem OperatingSystem { get; }
		public override string Filename { get; }
		public override string Name { get; }

		public override DbgProcessState State => state;
		DbgProcessState state;

		public override event EventHandler IsRunningChanged;
		public override event EventHandler DelayedIsRunningChanged;
		public override bool IsRunning {
			get {
				lock (lockObj)
					return cachedIsRunning;
			}
		}
		bool CalculateIsRunning_NoLock() => state == DbgProcessState.Running;
		bool cachedIsRunning;

		public override ReadOnlyCollection<string> Debugging {
			get {
				lock (lockObj)
					return debugging;
			}
		}
		ReadOnlyCollection<string> debugging;
		ReadOnlyCollection<string> CalculateDebugging_NoLock() {
			var list = new List<string>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var sb = new StringBuilder();
			foreach (var info in engineInfos) {
				foreach (var s in info.Debugging) {
					if (!list.Contains(s))
						list.Add(s);
				}
			}
			return new ReadOnlyCollection<string>(list.ToArray());
		}
		static bool StringArrayEquals(IList<string> a, IList<string> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!StringComparer.Ordinal.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		sealed class EngineInfo {
			public DbgEngine Engine { get; }
			public DbgRuntimeImpl Runtime { get; }
			public string[] Debugging { get; }
			public bool IsPaused { get; set; }
			public EngineInfo(DbgEngine engine, DbgRuntimeImpl runtime) {
				Engine = engine;
				Runtime = runtime;
				Debugging = engine.Debugging ?? Array.Empty<string>();
				IsPaused = false;
			}
		}
		readonly List<EngineInfo> engineInfos;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgRuntime>> RuntimesChanged;
		public override DbgRuntime[] Runtimes {
			get {
				lock (lockObj) {
					if (engineInfos.Count == 0)
						return Array.Empty<DbgRuntime>();
					var res = new DbgRuntime[engineInfos.Count];
					for (int i = 0; i < res.Length; i++)
						res[i] = engineInfos[i].Runtime;
					return res;
				}
			}
		}

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;
		public override DbgThread[] Threads {
			get {
				lock (lockObj)
					return threads.ToArray();
			}
		}
		readonly List<DbgThread> threads;// Owned by the runtimes

		internal CurrentObject<DbgRuntimeImpl> CurrentRuntime => currentRuntime;

		readonly object lockObj;
		readonly DbgManagerImpl owner;
		readonly SafeProcessHandle hProcess;
		CurrentObject<DbgRuntimeImpl> currentRuntime;

		public DbgProcessImpl(DbgManagerImpl owner, Dispatcher dispatcher, int pid, DbgProcessState state, bool shouldDetach) {
			lockObj = new object();
			engineInfos = new List<EngineInfo>();
			threads = new List<DbgThread>();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.state = state;
			cachedIsRunning = CalculateIsRunning_NoLock();
			Id = pid;
			ShouldDetach = shouldDetach;

			const int dwDesiredAccess = NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ |
				NativeMethods.PROCESS_VM_WRITE | NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION;
			hProcess = NativeMethods.OpenProcess(dwDesiredAccess, false, pid);
			if (hProcess.IsInvalid)
				throw new InvalidOperationException($"Couldn't open process {pid}, error: 0x{Marshal.GetLastWin32Error():X8}");

			Bitness = ProcessUtilities.GetBitness(hProcess.DangerousGetHandle());
			Architecture = GetArchitecture(Bitness);
			OperatingSystem = GetOperatingSystem();
			var info = GetProcessName(pid);
			Filename = info.filename ?? string.Empty;
			Name = info.name ?? string.Empty;
			debugging = CalculateDebugging_NoLock();

			new DelayedIsRunningHelper(this, dispatcher, RaiseDelayedIsRunningChanged_DbgThread);
		}

		// DbgManager thread
		internal void SetCurrentRuntime_DbgThread(DbgRuntimeImpl runtime) {
			owner.Dispatcher.VerifyAccess();
			currentRuntime = new CurrentObject<DbgRuntimeImpl>(runtime, currentRuntime.Break);
		}

		// DbgManager thread
		void UpdateRuntime_DbgThread(DbgRuntimeImpl runtime) {
			owner.Dispatcher.VerifyAccess();
			lock (lockObj) {
				var newCurrent = GetRuntime_NoLock(currentRuntime.Current, runtime);
				var newBreak = GetRuntime_NoLock(currentRuntime.Break, runtime);
				currentRuntime = new CurrentObject<DbgRuntimeImpl>(newCurrent, newBreak);
			}
		}

		DbgRuntimeImpl GetRuntime_NoLock(DbgRuntimeImpl runtime, DbgRuntimeImpl defaultRuntime) {
			if (runtime != null) {
				var info = engineInfos.First(a => a.Runtime == runtime);
				if (info.IsPaused || owner.GetDelayedIsRunning_DbgThread(info.Engine) == false)
					return runtime;
			}
			return defaultRuntime ?? engineInfos.FirstOrDefault(a => a.IsPaused)?.Runtime;
		}

		// DbgManager thread
		internal void SetPaused_DbgThread(DbgRuntimeImpl runtime) {
			owner.Dispatcher.VerifyAccess();
			if (runtime == null)
				return;
			lock (lockObj) {
				var info = engineInfos.First(a => a.Runtime == runtime);
				info.IsPaused = true;
				UpdateRuntime_DbgThread(runtime);
			}
		}

		// DbgManager thread
		internal void SetRunning_DbgThread(DbgRuntimeImpl runtime) {
			owner.Dispatcher.VerifyAccess();
			if (runtime == null)
				return;
			lock (lockObj) {
				var info = engineInfos.First(a => a.Runtime == runtime);
				info.IsPaused = false;
				UpdateRuntime_DbgThread(null);
			}
		}

		// DbgManager thread
		internal void RaiseDelayedIsRunningChanged_DbgThread() {
			owner.Dispatcher.VerifyAccess();
			if (IsRunning) {
				DbgEngine[] engines;
				lock (lockObj)
					engines = engineInfos.Select(a => a.Engine).ToArray();
				owner.SetDelayedIsRunning_DbgThread(engines);
				DelayedIsRunningChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		internal DbgEngine TryGetEngine(DbgRuntime runtime) {
			lock (lockObj) {
				foreach (var info in engineInfos) {
					if (info.Runtime == runtime)
						return info.Engine;
				}
			}
			return null;
		}

		bool IIsRunningProvider.IsDebugging => State != DbgProcessState.Terminated;
		public event EventHandler IsDebuggingChanged;

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DbgManager.Dispatcher.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		static (string filename, string name) GetProcessName(int pid) {
			string name = null;
			string filename = null;
			try {
				using (var p = Process.GetProcessById(pid)) {
					name = p.ProcessName;
					// Could throw
					filename = p.MainModule.FileName;
					name = Path.GetFileName(filename);
				}
			}
			catch {
			}
			return (filename, name);
		}

		static DbgArchitecture GetArchitecture(int bitness) {
			// We only allow debugging on the same computer
			switch (RuntimeInformation.ProcessArchitecture) {
			case System.Runtime.InteropServices.Architecture.X86:
			case System.Runtime.InteropServices.Architecture.X64:
				if (bitness == 32)
					return DbgArchitecture.X86;
				if (bitness == 64)
					return DbgArchitecture.X64;
				throw new ArgumentOutOfRangeException(nameof(bitness));

			case System.Runtime.InteropServices.Architecture.Arm:
			case System.Runtime.InteropServices.Architecture.Arm64:
				if (bitness == 32)
					return DbgArchitecture.Arm;
				if (bitness == 64)
					return DbgArchitecture.Arm64;
				throw new ArgumentOutOfRangeException(nameof(bitness));

			default:
				throw new InvalidOperationException($"Unknown CPU arch: {RuntimeInformation.ProcessArchitecture}");
			}
		}

		static DbgOperatingSystem GetOperatingSystem() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return DbgOperatingSystem.Windows;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return DbgOperatingSystem.MacOS;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return DbgOperatingSystem.Linux;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
				return DbgOperatingSystem.FreeBSD;
			throw new InvalidOperationException("Unknown operating system");
		}

		public unsafe override void ReadMemory(ulong address, byte[] destination, int destinationIndex, int size) {
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if (destinationIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(destinationIndex));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if ((uint)(destinationIndex + size) > (uint)destination.Length)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (size == 0)
				return;
			fixed (byte* p = &destination[destinationIndex])
				ReadMemory(address, p, size);
		}

		public override void ReadMemory(ulong address, void* destination, int size) {
			if (destination == null && size != 0)
				throw new ArgumentNullException(nameof(destination));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			var dest = (byte*)destination;
			if (hProcess.IsClosed || (Bitness == 32 && address > uint.MaxValue)) {
				Clear(dest, size);
				return;
			}

			ulong endAddr = Bitness == 32 ? (ulong)uint.MaxValue + 1 : 0UL;
			uint count = (uint)size;
			var hProcessLocal = hProcess.DangerousGetHandle();
			ulong pageSize = (ulong)Environment.SystemPageSize;
			while (count != 0) {
				int len = (int)Math.Min((uint)pageSize, count);

				ulong nextPage = (address + pageSize) & ~(pageSize - 1);
				ulong pageSizeLeft = nextPage - address;
				if ((ulong)len > pageSizeLeft)
					len = (int)pageSizeLeft;

				bool b = NativeMethods.ReadProcessMemory(hProcessLocal, (void*)address, dest, new IntPtr(len), out var sizeRead);

				int read = !b ? 0 : (int)sizeRead.ToInt64();
				Debug.Assert(read <= len);
				Debug.Assert(read == 0 || read == len);
				if (read != len)
					Clear(dest + read, len - read);

				address += (ulong)len;
				count -= (uint)len;
				dest += len;

				if (address == endAddr) {
					Clear(dest, (int)count);
					break;
				}
			}
		}

		unsafe void Clear(byte* destination, int size) => Memset.Clear(destination, 0, size);

		public unsafe override void WriteMemory(ulong address, byte[] source, int sourceIndex, int size) {
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(sourceIndex));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if ((uint)(sourceIndex + size) > (uint)source.Length)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (size == 0)
				return;
			fixed (byte* p = &source[sourceIndex])
				WriteMemory(address, p, size);
		}

		public override void WriteMemory(ulong address, void* source, int size) {
			var src = (byte*)source;
			if (hProcess.IsClosed || (Bitness == 32 && address > uint.MaxValue))
				return;

			ulong endAddr = Bitness == 32 ? (ulong)uint.MaxValue + 1 : 0UL;
			uint count = (uint)size;
			var hProcessLocal = hProcess.DangerousGetHandle();
			ulong pageSize = (ulong)Environment.SystemPageSize;
			while (count != 0) {
				int len = (int)Math.Min((uint)pageSize, count);

				ulong nextPage = (address + pageSize) & ~(pageSize - 1);
				ulong pageSizeLeft = nextPage - address;
				if ((ulong)len > pageSizeLeft)
					len = (int)pageSizeLeft;

				bool restoreOldProtect = NativeMethods.VirtualProtectEx(hProcessLocal, (void*)address, new IntPtr(len), NativeMethods.PAGE_EXECUTE_READWRITE, out uint oldProtect);
				NativeMethods.WriteProcessMemory(hProcessLocal, (void*)address, src, new IntPtr(len), out var sizeWritten);
				if (restoreOldProtect)
					NativeMethods.VirtualProtectEx(hProcessLocal, (void*)address, new IntPtr(len), oldProtect, out oldProtect);

				address += (ulong)len;
				count -= (uint)len;
				src += len;

				if (address == endAddr)
					break;
			}
		}

		void DbgRuntime_ThreadsChanged(object sender, DbgCollectionChangedEventArgs<DbgThread> e) {
			lock (lockObj) {
				if (e.Added)
					threads.AddRange(e.Objects);
				else {
					foreach (var thread in e.Objects) {
						bool b = threads.Remove(thread);
						Debug.Assert(b);
					}
				}
			}
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(e.Objects, added: e.Added));
		}

		internal void Add_DbgThread(DbgEngine engine, DbgRuntimeImpl runtime, DbgProcessState newState) {
			bool raiseStateChanged, raiseDebuggingChanged, raiseIsRunningChanged;
			DbgThread[] addedThreads;
			lock (lockObj) {
				engineInfos.Add(new EngineInfo(engine, runtime));
				var newDebugging = CalculateDebugging_NoLock();
				raiseStateChanged = state != newState;
				raiseDebuggingChanged = !StringArrayEquals(debugging, newDebugging);
				state = newState;
				if (raiseDebuggingChanged)
					debugging = newDebugging;
				var newIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = cachedIsRunning != newIsRunning;
				cachedIsRunning = newIsRunning;
				addedThreads = runtime.Threads;
				runtime.ThreadsChanged += DbgRuntime_ThreadsChanged;
			}
			RuntimesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgRuntime>(runtime, added: true));
			if (addedThreads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(addedThreads, added: true));
			if (raiseStateChanged)
				OnPropertyChanged(nameof(State));
			if (raiseIsRunningChanged)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
			if (raiseDebuggingChanged)
				OnPropertyChanged(nameof(Debugging));
		}

		internal void UpdateState_DbgThread(DbgProcessState newState) {
			bool raiseStateChanged, raiseIsRunningChanged, raiseIsDebuggingChanged;
			lock (lockObj) {
				raiseStateChanged = state != newState;
				state = newState;
				var newIsRunning = CalculateIsRunning_NoLock();
				raiseIsRunningChanged = cachedIsRunning != newIsRunning;
				cachedIsRunning = newIsRunning;
				raiseIsDebuggingChanged = state == DbgProcessState.Terminated;
			}
			if (raiseStateChanged)
				OnPropertyChanged(nameof(State));
			if (raiseIsRunningChanged)
				IsRunningChanged?.Invoke(this, EventArgs.Empty);
			if (raiseIsDebuggingChanged)
				IsDebuggingChanged?.Invoke(this, EventArgs.Empty);
		}

		internal (DbgRuntimeImpl runtime, bool hasMoreRuntimes) Remove_DbgThread(DbgEngine engine) {
			DbgRuntimeImpl runtime = null;
			bool hasMoreRuntimes;
			lock (lockObj) {
				for (int i = 0; i < engineInfos.Count; i++) {
					var info = engineInfos[i];
					if (info.Engine == engine) {
						UpdateRuntime_DbgThread(null);
						runtime = info.Runtime;
						engineInfos.RemoveAt(i);
						break;
					}
				}
				hasMoreRuntimes = engineInfos.Count > 0;
			}
			return (runtime, hasMoreRuntimes);
		}

		internal void NotifyRuntimesChanged_DbgThread(DbgRuntime runtime) {
			bool raiseDebuggingChanged;
			DbgThread[] removedThreads;
			lock (lockObj) {
				var newDebugging = CalculateDebugging_NoLock();
				raiseDebuggingChanged = !StringArrayEquals(debugging, newDebugging);
				if (raiseDebuggingChanged)
					debugging = newDebugging;
				runtime.ThreadsChanged -= DbgRuntime_ThreadsChanged;
				var threadsList = new List<DbgThread>();
				for (int i = threads.Count - 1; i >= 0; i--) {
					var thread = threads[i];
					if (thread.Runtime == runtime) {
						threadsList.Add(thread);
						threads.RemoveAt(i);
					}
				}
				removedThreads = threadsList.ToArray();
			}
			if (removedThreads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(removedThreads, added: false));
			if (raiseDebuggingChanged)
				OnPropertyChanged(nameof(Debugging));
			if (runtime != null)
				RuntimesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgRuntime>(runtime, added: false));
		}

		internal bool ExecuteLockedIfNoMoreRuntimes(Func<bool> funcIfNoMoreRuntimes, bool defaultValue) {
			lock (lockObj) {
				if (engineInfos.Count == 0)
					return funcIfNoMoreRuntimes();
				else
					return defaultValue;
			}
		}

		[Conditional("DEBUG")]
		void VerifyHasNoRuntimes() {
			lock (lockObj)
				Debug.Assert(engineInfos.Count == 0);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			VerifyHasNoRuntimes();
			hProcess.Dispose();
			currentRuntime = default;
		}

		public override bool ShouldDetach {
			get {
				lock (lockObj)
					return shouldDetach;
			}
			set {
				bool raiseEvent;
				lock (lockObj) {
					raiseEvent = shouldDetach != value;
					shouldDetach = value;
				}
				if (raiseEvent)
					DbgManager.Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(ShouldDetach)));
			}
		}
		bool shouldDetach;

		internal int GetExitCode() {
			if (NativeMethods.GetExitCodeProcess(hProcess.DangerousGetHandle(), out int processExitCode))
				return processExitCode;
			return -1;
		}

		public override void Detach() => owner.Detach(this);
		public override void Terminate() => owner.Terminate(this);
		public override void Break() => owner.Break(this);
		public override void Run() => owner.Run(this);
	}
}
