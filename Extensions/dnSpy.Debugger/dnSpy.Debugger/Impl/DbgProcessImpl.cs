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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Native;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Impl {
	unsafe sealed class DbgProcessImpl : DbgProcess {
		public override DbgManager DbgManager => owner;
		public override int Id { get; }
		public override int Bitness { get; }
		public override DbgMachine Machine { get; }
		public override string Filename { get; }

		public override DbgProcessState State => state;
		DbgProcessState state;

		public override string Debugging {
			get {
				lock (lockObj)
					return debugging;
			}
		}
		string debugging = string.Empty;
		string CalculateDebugging_NoLock() {
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var sb = new StringBuilder();
			foreach (var info in engineInfos) {
				var desc = info.Debugging;
				if (seen.Contains(desc))
					continue;
				seen.Add(desc);
				if (sb.Length != 0)
					sb.Append(", ");
				sb.Append(desc);
			}
			return sb.ToString();
		}

		struct EngineInfo {
			public DbgEngine Engine { get; }
			public DbgRuntime Runtime { get; }
			public string Debugging { get; }
			public EngineInfo(DbgEngine engine, DbgRuntime runtime) {
				Engine = engine;
				Runtime = runtime;
				Debugging = engine.Debugging ?? "???";
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

		readonly object lockObj;
		readonly DbgManagerImpl owner;
		readonly SafeProcessHandle hProcess;

		public DbgProcessImpl(DbgManagerImpl owner, int pid, DbgProcessState state, bool shouldDetach) {
			lockObj = new object();
			engineInfos = new List<EngineInfo>();
			threads = new List<DbgThread>();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.state = state;
			Id = pid;
			ShouldDetach = shouldDetach;

			const int dwDesiredAccess = NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE;
			hProcess = NativeMethods.OpenProcess(dwDesiredAccess, false, pid);
			if (hProcess.IsInvalid)
				throw new InvalidOperationException($"Couldn't open process {pid}");

			Bitness = GetBitness(hProcess.DangerousGetHandle());
			Machine = GetMachine(Bitness);
			Filename = GetProcessFilename(pid) ?? string.Empty;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DbgManager.DispatcherThread.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		static string GetProcessFilename(int pid) {
			try {
				using (var p = Process.GetProcessById(pid))
					return p.MainModule.FileName;
			}
			catch {
			}
			return string.Empty;
		}

		static int GetBitness(IntPtr hProcess) {
			if (!Environment.Is64BitOperatingSystem) {
				Debug.Assert(IntPtr.Size == 4);
				return IntPtr.Size * 8;
			}
			if (NativeMethods.IsWow64Process(hProcess, out bool isWow64Process)) {
				if (isWow64Process)
					return 32;
				return 64;
			}
			Debug.Fail("IsWow64Process failed");
			return IntPtr.Size * 8;
		}

		static DbgMachine GetMachine(int bitness) {
			// We only allow debugging on the same computer and this is x86 or x64
			switch (bitness) {
			case 32: return DbgMachine.X86;
			case 64: return DbgMachine.X64;
			default: throw new ArgumentOutOfRangeException(nameof(bitness));
			}
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

		public override void ReadMemory(ulong address, byte* destination, int size) {
			if (hProcess.IsClosed || (Bitness == 32 && address > uint.MaxValue)) {
				Clear(destination, size);
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

				bool b = NativeMethods.ReadProcessMemory(hProcessLocal, (void*)address, destination, new IntPtr(len), out var sizeRead);

				int read = !b ? 0 : (int)sizeRead.ToInt64();
				Debug.Assert(read <= len);
				Debug.Assert(read == 0 || read == len);
				if (read != len)
					Clear(destination + read, len - read);

				address += (ulong)len;
				count -= (uint)len;
				destination += len;

				if (address == endAddr) {
					Clear(destination, (int)count);
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

		public override void WriteMemory(ulong address, byte* source, int size) {
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
				NativeMethods.WriteProcessMemory(hProcessLocal, (void*)address, source, new IntPtr(len), out var sizeWritten);
				if (restoreOldProtect)
					NativeMethods.VirtualProtectEx(hProcessLocal, (void*)address, new IntPtr(len), oldProtect, out oldProtect);

				address += (ulong)len;
				count -= (uint)len;
				source += len;

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

		internal void Add_DbgThread(DbgEngine engine, DbgRuntime runtime, DbgProcessState newState) {
			bool raiseStateChanged, raiseDebuggingChanged;
			DbgThread[] addedThreads;
			lock (lockObj) {
				engineInfos.Add(new EngineInfo(engine, runtime));
				var newDebugging = CalculateDebugging_NoLock();
				raiseStateChanged = state != newState;
				raiseDebuggingChanged = debugging != newDebugging;
				state = newState;
				debugging = newDebugging;
				addedThreads = runtime.Threads;
				runtime.ThreadsChanged += DbgRuntime_ThreadsChanged;
			}
			RuntimesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgRuntime>(runtime, added: true));
			if (addedThreads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(addedThreads, added: true));
			if (raiseStateChanged)
				OnPropertyChanged(nameof(State));
			if (raiseDebuggingChanged)
				OnPropertyChanged(nameof(Debugging));
		}

		internal void UpdateState_DbgThread(DbgProcessState newState) {
			bool raiseStateChanged;
			lock (lockObj) {
				raiseStateChanged = state != newState;
				state = newState;
			}
			if (raiseStateChanged)
				OnPropertyChanged(nameof(State));
		}

		internal (DbgRuntime runtime, bool hasMoreRuntimes) Remove_DbgThread(DbgEngine engine) {
			DbgRuntime runtime = null;
			bool hasMoreRuntimes;
			lock (lockObj) {
				for (int i = 0; i < engineInfos.Count; i++) {
					var info = engineInfos[i];
					if (info.Engine == engine) {
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
				raiseDebuggingChanged = debugging != newDebugging;
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

		protected override void CloseCore() {
			VerifyHasNoRuntimes();
			hProcess.Dispose();
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
					DbgManager.DispatcherThread.BeginInvoke(() => OnPropertyChanged(nameof(ShouldDetach)));
			}
		}
		bool shouldDetach;

		public override void Detach() => owner.Detach(this);
		public override void Terminate() => owner.Terminate(this);
	}
}
