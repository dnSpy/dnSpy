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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Native;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Impl {
	unsafe sealed class DbgProcessImpl : DbgProcess {
		public override DbgManager DbgManager { get; }
		public override int Id { get; }
		public override int Bitness { get; }
		public override DbgMachine Machine { get; }
		public override string Filename { get; }

		struct EngineInfo {
			public DbgEngine Engine { get; }
			public DbgRuntime Runtime { get; }
			public EngineInfo(DbgEngine engine, DbgRuntime runtime) {
				Engine = engine;
				Runtime = runtime;
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

		public override DbgThread[] Threads {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		readonly object lockObj;
		readonly SafeFileHandle hProcess;

		public DbgProcessImpl(DbgManager owner, int pid) {
			lockObj = new object();
			engineInfos = new List<EngineInfo>();
			DbgManager = owner ?? throw new ArgumentNullException(nameof(owner));
			Id = pid;

			const int dwDesiredAccess = NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE;
			hProcess = NativeMethods.OpenProcess(dwDesiredAccess, false, pid);
			if (hProcess.IsInvalid)
				throw new InvalidOperationException($"Couldn't open process {pid}");

			Bitness = GetBitness(hProcess.DangerousGetHandle());
			Machine = GetMachine(Bitness);
			Filename = GetProcessFilename(pid) ?? string.Empty;
		}

		static string GetProcessFilename(int pid) {
			try {
				var p = Process.GetProcessById(pid);
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

		public override int ReadMemory(ulong address, byte* destination, int size) => throw new NotImplementedException();//TODO:
		public override int ReadMemory(ulong address, byte[] destination, int destinationIndex, int size) => throw new NotImplementedException();//TODO:
		public override int WriteMemory(ulong address, byte* source, int size) => throw new NotImplementedException();//TODO:
		public override int WriteMemory(ulong address, byte[] source, int sourceIndex, int size) => throw new NotImplementedException();//TODO:

		internal void Add(DbgEngine engine, DbgRuntime runtime) {
			lock (lockObj) {
				engineInfos.Add(new EngineInfo(engine, runtime));
				DbgManager.DispatcherThread.BeginInvoke(() => RuntimesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgRuntime>(runtime, added: true)));
			}
		}

		internal (DbgRuntime runtime, bool hasMoreRuntimes) Remove(DbgEngine engine) {
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
				if (runtime != null)
					DbgManager.DispatcherThread.BeginInvoke(() => RuntimesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgRuntime>(runtime, added: false)));
			}
			return (runtime, hasMoreRuntimes);
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
	}
}
