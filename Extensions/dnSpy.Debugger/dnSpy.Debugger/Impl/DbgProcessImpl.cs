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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.Native;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Impl {
	sealed class DbgProcessImpl : DbgProcess {
		public override DbgManager DbgManager { get; }
		public override int Id { get; }
		public override int Bitness { get; }

		struct EngineInfo {
			public DbgEngine Engine { get; }
			public DbgRuntime Runtime { get; }
			public EngineInfo(DbgEngine engine, DbgRuntime runtime) {
				Engine = engine;
				Runtime = runtime;
			}
		}
		readonly List<EngineInfo> engineInfos;

		public override event EventHandler<RuntimesChangedEventArgs> RuntimesChanged;
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
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			lockObj = new object();
			engineInfos = new List<EngineInfo>();
			DbgManager = owner;
			Id = pid;
			Bitness = GetBitness();

			const int dwDesiredAccess = NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE;
			hProcess = NativeMethods.OpenProcess(dwDesiredAccess, false, pid);
			if (hProcess.IsInvalid)
				throw new InvalidOperationException($"Couldn't open process {pid}");
		}

		static int GetBitness() {
			// Identical to this process. We don't create a new process to debug anything
			// so our bitness must equal the debugged process' bitness.
			return IntPtr.Size * 8;
		}

		public override int ReadMemory(ulong address, IntPtr destination, int size) {
			throw new NotImplementedException();//TODO:
		}

		public override int ReadMemory(ulong address, byte[] destination, int destinationIndex, int size) {
			throw new NotImplementedException();//TODO:
		}

		public override int WriteMemory(ulong address, IntPtr source, int size) {
			throw new NotImplementedException();//TODO:
		}

		public override int WriteMemory(ulong address, byte[] source, int sourceIndex, int size) {
			throw new NotImplementedException();//TODO:
		}

		internal void Add(DbgEngine engine, DbgRuntime runtime) {
			lock (lockObj)
				engineInfos.Add(new EngineInfo(engine, runtime));
			RuntimesChanged?.Invoke(this, new RuntimesChangedEventArgs(runtime, added: true));
		}

		//TODO: Call this method
		public void Dispose() => hProcess.Dispose();
	}
}
