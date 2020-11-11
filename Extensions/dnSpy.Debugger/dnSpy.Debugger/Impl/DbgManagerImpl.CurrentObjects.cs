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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		public override event EventHandler<DbgCurrentObjectChangedEventArgs<DbgProcess>>? CurrentProcessChanged;
		public override DbgCurrentObject<DbgProcess> CurrentProcess => dbgCurrentProcess;
		public override event EventHandler<DbgCurrentObjectChangedEventArgs<DbgRuntime>>? CurrentRuntimeChanged;
		public override DbgCurrentObject<DbgRuntime> CurrentRuntime => dbgCurrentRuntime;
		public override event EventHandler<DbgCurrentObjectChangedEventArgs<DbgThread>>? CurrentThreadChanged;
		public override DbgCurrentObject<DbgThread> CurrentThread => dbgCurrentThread;

		readonly DbgCurrentProcess dbgCurrentProcess;
		readonly DbgCurrentRuntime dbgCurrentRuntime;
		readonly DbgCurrentThread dbgCurrentThread;

		sealed class DbgCurrentProcess : DbgCurrentObject<DbgProcess> {
			public override DbgProcess? Current {
				get {
					lock (owner.lockObj)
						return currentProcess.Current;
				}
				set {
					if (value is null)
						throw new ArgumentNullException(nameof(value));
					var process = value as DbgProcessImpl;
					if (process is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					owner.DbgThread(() => owner.SetCurrentProcess_DbgThread(process));
				}
			}
			public override DbgProcess? Break {
				get {
					lock (owner.lockObj)
						return currentProcess.Break;
				}
			}
			internal CurrentObject<DbgProcessImpl> currentProcess;
			readonly DbgManagerImpl owner;
			public DbgCurrentProcess(DbgManagerImpl owner) => this.owner = owner;
		}

		sealed class DbgCurrentRuntime : DbgCurrentObject<DbgRuntime> {
			public override DbgRuntime? Current {
				get {
					lock (owner.lockObj)
						return currentRuntime.Current;
				}
				set {
					if (value is null)
						throw new ArgumentNullException(nameof(value));
					var runtime = value as DbgRuntimeImpl;
					if (runtime is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					owner.DbgThread(() => owner.SetCurrentRuntime_DbgThread(runtime));
				}
			}
			public override DbgRuntime? Break {
				get {
					lock (owner.lockObj)
						return currentRuntime.Break;
				}
			}
			internal CurrentObject<DbgRuntimeImpl> currentRuntime;
			readonly DbgManagerImpl owner;
			public DbgCurrentRuntime(DbgManagerImpl owner) => this.owner = owner;
		}

		sealed class DbgCurrentThread : DbgCurrentObject<DbgThread> {
			public override DbgThread? Current {
				get {
					lock (owner.lockObj)
						return currentThread.Current;
				}
				set {
					if (value is null)
						throw new ArgumentNullException(nameof(value));
					var thread = value as DbgThreadImpl;
					if (thread is null)
						throw new ArgumentOutOfRangeException(nameof(value));
					owner.DbgThread(() => owner.SetCurrentThread_DbgThread(thread));
				}
			}
			public override DbgThread? Break {
				get {
					lock (owner.lockObj)
						return currentThread.Break;
				}
			}
			internal CurrentObject<DbgThreadImpl> currentThread;
			readonly DbgManagerImpl owner;
			public DbgCurrentThread(DbgManagerImpl owner) => this.owner = owner;
		}

		void RaiseCurrentObjectEvents_DbgThread(DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs, DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs, DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs) {
			Dispatcher.VerifyAccess();
			if (processEventArgs.CurrentChanged || processEventArgs.BreakChanged)
				CurrentProcessChanged?.Invoke(this, processEventArgs);
			if (runtimeEventArgs.CurrentChanged || runtimeEventArgs.BreakChanged)
				CurrentRuntimeChanged?.Invoke(this, runtimeEventArgs);
			if (threadEventArgs.CurrentChanged || threadEventArgs.BreakChanged)
				CurrentThreadChanged?.Invoke(this, threadEventArgs);
		}

		void SetCurrentProcess_DbgThread(DbgProcessImpl newProcess) {
			Dispatcher.VerifyAccess();
			if (newProcess is null || newProcess.State != DbgProcessState.Paused)
				return;

			DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs;
			lock (lockObj) {
				var newCurrentProcess = new CurrentObject<DbgProcessImpl>(newProcess, dbgCurrentProcess.currentProcess.Break);
				var newCurrentRuntime = newProcess.CurrentRuntime;
				var newCurrentThread = newCurrentRuntime.Current?.CurrentThread ?? default;
				processEventArgs = new DbgCurrentObjectChangedEventArgs<DbgProcess>(currentChanged: dbgCurrentProcess.currentProcess.Current != newCurrentProcess.Current, breakChanged: dbgCurrentProcess.currentProcess.Break != newCurrentProcess.Break);
				runtimeEventArgs = new DbgCurrentObjectChangedEventArgs<DbgRuntime>(currentChanged: dbgCurrentRuntime.currentRuntime.Current != newCurrentRuntime.Current, breakChanged: dbgCurrentRuntime.currentRuntime.Break != newCurrentRuntime.Break);
				threadEventArgs = new DbgCurrentObjectChangedEventArgs<DbgThread>(currentChanged: dbgCurrentThread.currentThread.Current != newCurrentThread.Current, breakChanged: dbgCurrentThread.currentThread.Break != newCurrentThread.Break);
				dbgCurrentProcess.currentProcess = newCurrentProcess;
				dbgCurrentRuntime.currentRuntime = newCurrentRuntime;
				dbgCurrentThread.currentThread = newCurrentThread;
			}
			RaiseCurrentObjectEvents_DbgThread(processEventArgs, runtimeEventArgs, threadEventArgs);
		}

		void SetCurrentRuntime_DbgThread(DbgRuntimeImpl newRuntime) {
			Dispatcher.VerifyAccess();
			if (!(newRuntime?.Process is DbgProcessImpl newProcess) || newProcess.State != DbgProcessState.Paused || newRuntime.IsClosed)
				return;

			DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs;
			lock (lockObj) {
				var newCurrentProcess = new CurrentObject<DbgProcessImpl>(newProcess, dbgCurrentProcess.currentProcess.Break);
				newProcess.SetCurrentRuntime_DbgThread(newRuntime);
				var newCurrentRuntime = newProcess.CurrentRuntime;
				Debug.Assert(newCurrentRuntime.Current == newRuntime);
				var newCurrentThread = newCurrentRuntime.Current?.CurrentThread ?? default;
				processEventArgs = new DbgCurrentObjectChangedEventArgs<DbgProcess>(currentChanged: dbgCurrentProcess.currentProcess.Current != newCurrentProcess.Current, breakChanged: dbgCurrentProcess.currentProcess.Break != newCurrentProcess.Break);
				runtimeEventArgs = new DbgCurrentObjectChangedEventArgs<DbgRuntime>(currentChanged: dbgCurrentRuntime.currentRuntime.Current != newCurrentRuntime.Current, breakChanged: dbgCurrentRuntime.currentRuntime.Break != newCurrentRuntime.Break);
				threadEventArgs = new DbgCurrentObjectChangedEventArgs<DbgThread>(currentChanged: dbgCurrentThread.currentThread.Current != newCurrentThread.Current, breakChanged: dbgCurrentThread.currentThread.Break != newCurrentThread.Break);
				dbgCurrentProcess.currentProcess = newCurrentProcess;
				dbgCurrentRuntime.currentRuntime = newCurrentRuntime;
				dbgCurrentThread.currentThread = newCurrentThread;
			}
			RaiseCurrentObjectEvents_DbgThread(processEventArgs, runtimeEventArgs, threadEventArgs);
		}

		void SetCurrentThread_DbgThread(DbgThreadImpl newThread) {
			Dispatcher.VerifyAccess();
			if (!(newThread?.Process is DbgProcessImpl newProcess) || newProcess.State != DbgProcessState.Paused || newThread.IsClosed)
				return;

			DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs;
			lock (lockObj) {
				var newRuntime = (DbgRuntimeImpl)newThread.Runtime;
				var newCurrentProcess = new CurrentObject<DbgProcessImpl>(newProcess, dbgCurrentProcess.currentProcess.Break);
				var newCurrentRuntime = new CurrentObject<DbgRuntimeImpl>(newRuntime, newProcess.CurrentRuntime.Break);
				newRuntime.SetCurrentThread_DbgThread(newThread);
				Debug.Assert(newRuntime.CurrentThread.Current == newThread);
				var newCurrentThread = newRuntime.CurrentThread;
				processEventArgs = new DbgCurrentObjectChangedEventArgs<DbgProcess>(currentChanged: dbgCurrentProcess.currentProcess.Current != newCurrentProcess.Current, breakChanged: dbgCurrentProcess.currentProcess.Break != newCurrentProcess.Break);
				runtimeEventArgs = new DbgCurrentObjectChangedEventArgs<DbgRuntime>(currentChanged: dbgCurrentRuntime.currentRuntime.Current != newCurrentRuntime.Current, breakChanged: dbgCurrentRuntime.currentRuntime.Break != newCurrentRuntime.Break);
				threadEventArgs = new DbgCurrentObjectChangedEventArgs<DbgThread>(currentChanged: dbgCurrentThread.currentThread.Current != newCurrentThread.Current, breakChanged: dbgCurrentThread.currentThread.Break != newCurrentThread.Break);
				dbgCurrentProcess.currentProcess = newCurrentProcess;
				dbgCurrentRuntime.currentRuntime = newCurrentRuntime;
				dbgCurrentThread.currentThread = newCurrentThread;
			}
			RaiseCurrentObjectEvents_DbgThread(processEventArgs, runtimeEventArgs, threadEventArgs);
		}

		bool SetCurrentEngineIfCurrentIsNull_DbgThread(DbgEngine engine, bool forceSet) {
			Dispatcher.VerifyAccess();
			DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs;
			lock (lockObj) {
				if (!forceSet && dbgCurrentProcess.currentProcess.Current is not null)
					return false;
				var info = GetEngineInfo_NoLock(engine);
				var process = info.Process;
				var runtime = info.Runtime;
				var newCurrentProcess = new CurrentObject<DbgProcessImpl>(process, process);
				var newCurrentRuntime = new CurrentObject<DbgRuntimeImpl>(runtime, runtime);
				var newCurrentThread = runtime?.CurrentThread ?? default;
				processEventArgs = new DbgCurrentObjectChangedEventArgs<DbgProcess>(currentChanged: dbgCurrentProcess.currentProcess.Current != newCurrentProcess.Current, breakChanged: dbgCurrentProcess.currentProcess.Break != newCurrentProcess.Break);
				runtimeEventArgs = new DbgCurrentObjectChangedEventArgs<DbgRuntime>(currentChanged: dbgCurrentRuntime.currentRuntime.Current != newCurrentRuntime.Current, breakChanged: dbgCurrentRuntime.currentRuntime.Break != newCurrentRuntime.Break);
				threadEventArgs = new DbgCurrentObjectChangedEventArgs<DbgThread>(currentChanged: dbgCurrentThread.currentThread.Current != newCurrentThread.Current, breakChanged: dbgCurrentThread.currentThread.Break != newCurrentThread.Break);
				dbgCurrentProcess.currentProcess = newCurrentProcess;
				dbgCurrentRuntime.currentRuntime = newCurrentRuntime;
				dbgCurrentThread.currentThread = newCurrentThread;
			}
			RaiseCurrentObjectEvents_DbgThread(processEventArgs, runtimeEventArgs, threadEventArgs);
			return true;
		}

		void RecheckAndUpdateCurrentProcess_DbgThread() {
			Dispatcher.VerifyAccess();
			DbgCurrentObjectChangedEventArgs<DbgProcess> processEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgRuntime> runtimeEventArgs;
			DbgCurrentObjectChangedEventArgs<DbgThread> threadEventArgs;
			lock (lockObj) {
				var pausedProcess = processes.FirstOrDefault(a => a.State == DbgProcessState.Paused);
				var currentProcess = GetProcess_NoLock(dbgCurrentProcess.currentProcess.Current, pausedProcess) ?? pausedProcess;
				var breakProcess = GetProcess_NoLock(dbgCurrentProcess.currentProcess.Break, pausedProcess) ?? pausedProcess;
				var newCurrentProcess = new CurrentObject<DbgProcessImpl>(currentProcess, breakProcess);
				var newCurrentRuntime = currentProcess?.CurrentRuntime ?? default;
				var newCurrentThread = newCurrentRuntime.Current?.CurrentThread ?? default;
				processEventArgs = new DbgCurrentObjectChangedEventArgs<DbgProcess>(currentChanged: dbgCurrentProcess.currentProcess.Current != newCurrentProcess.Current, breakChanged: dbgCurrentProcess.currentProcess.Break != newCurrentProcess.Break);
				runtimeEventArgs = new DbgCurrentObjectChangedEventArgs<DbgRuntime>(currentChanged: dbgCurrentRuntime.currentRuntime.Current != newCurrentRuntime.Current, breakChanged: dbgCurrentRuntime.currentRuntime.Break != newCurrentRuntime.Break);
				threadEventArgs = new DbgCurrentObjectChangedEventArgs<DbgThread>(currentChanged: dbgCurrentThread.currentThread.Current != newCurrentThread.Current, breakChanged: dbgCurrentThread.currentThread.Break != newCurrentThread.Break);
				dbgCurrentProcess.currentProcess = newCurrentProcess;
				dbgCurrentRuntime.currentRuntime = newCurrentRuntime;
				dbgCurrentThread.currentThread = newCurrentThread;
			}
			RaiseCurrentObjectEvents_DbgThread(processEventArgs, runtimeEventArgs, threadEventArgs);
		}

		DbgProcessImpl? GetProcess_NoLock(DbgProcessImpl? current, DbgProcessImpl? @default) {
			if (current is null)
				return @default;
			switch (current.State) {
			case DbgProcessState.Running:
				var runtime = current.CurrentRuntime.Current;
				var engine = current.TryGetEngine(runtime);
				lock (lockObj) {
					var info = TryGetEngineInfo_NoLock(engine);
					if (info?.DelayedIsRunning != false)
						return @default;
				}
				return current;

			case DbgProcessState.Paused: return current;
			case DbgProcessState.Terminated: return @default;
			default: throw new InvalidOperationException();
			}
		}
	}
}
