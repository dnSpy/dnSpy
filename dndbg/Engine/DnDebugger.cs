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

// CLR error codes: https://github.com/dotnet/coreclr/blob/master/src/inc/corerror.xml

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dndbg.Engine.COM.CorDebug;
using dndbg.Engine.COM.MetaHost;

namespace dndbg.Engine {
	public delegate void DebugCallbackEventHandler(DnDebugger dbg, DebugCallbackEventArgs e);

	public sealed class DnDebugger : IDisposable {
		readonly IDebugMessageDispatcher debugMessageDispatcher;
		readonly ICorDebug corDebug;
		readonly DebuggerCollection<ICorDebugProcess, DnProcess> processes;

		DnDebugger(ICorDebug corDebug, IDebugMessageDispatcher debugMessageDispatcher) {
			if (debugMessageDispatcher == null)
				throw new ArgumentNullException("debugMessageDispatcher");
			this.processes = new DebuggerCollection<ICorDebugProcess, DnProcess>(CreateDnProcess);
			this.debugMessageDispatcher = debugMessageDispatcher;
			this.corDebug = corDebug;

			corDebug.Initialize();
			corDebug.SetManagedHandler(new CorDebugManagedCallback(this));
		}

		DnProcess CreateDnProcess(ICorDebugProcess comProcess, int id) {
			return new DnProcess(this, comProcess, id);
		}

		static ICorDebug CreateCorDebug(string debuggeeVersion) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(ICLRMetaHost).GUID;
			var mh = (ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);

			riid = typeof(ICLRRuntimeInfo).GUID;
			var ri = (ICLRRuntimeInfo)mh.GetRuntime(debuggeeVersion, ref riid);

			clsid = new Guid("DF8395B5-A4BA-450B-A77C-A9A47762C520");
			riid = typeof(ICorDebug).GUID;
			return (ICorDebug)ri.GetInterface(ref clsid, ref riid);
		}

		/// <summary>
		/// Called from the debugger thread
		/// </summary>
		public event DebugCallbackEventHandler DebugCallbackEvent;

		// Could be called from any thread
		internal void OnManagedCallbackFromAnyThread(DebugCallbackEventArgs e) {
			debugMessageDispatcher.ExecuteAsync(() => OnManagedCallbackInDebuggerThread(e));
		}

		void OnManagedCallbackInDebuggerThread(DebugCallbackEventArgs e) {
			HandleManagedCallback(e);
			var ev = DebugCallbackEvent;
			if (ev != null)
				ev(this, e);

			if (!e.Stop) {
				if (e.Type != DebugCallbackType.ExitProcess) {
					try {
						e.CorDebugController.Continue(0);
					}
					catch (COMException) {
						// 0x80131301: CORDBG_E_PROCESS_TERMINATED
					}
				}
			}
			// Continue() has been called, so another message could have been dequeued, and we
			// could be executing code in another thread. Don't access any debugger state now.
		}

		void HandleManagedCallback(DebugCallbackEventArgs e) {
			bool b;
			DnProcess process;
			DnAppDomain appDomain;
			DnAssembly assembly;
			switch (e.Type) {
			case DebugCallbackType.Breakpoint:
				break;

			case DebugCallbackType.StepComplete:
				break;

			case DebugCallbackType.Break:
				break;

			case DebugCallbackType.Exception:
				break;

			case DebugCallbackType.EvalComplete:
				break;

			case DebugCallbackType.EvalException:
				break;

			case DebugCallbackType.CreateProcess:
				var cpArgs = (CreateProcessDebugCallbackEventArgs)e;
				process = TryAdd(cpArgs.Process);
				if (process != null) {
					process.EnableLogMessages(true);
					process.SetDesiredNGENCompilerFlags(CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION);
					process.SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode.AlwaysShowUpdates);
					//TODO: ICorDebugProcess8::EnableExceptionCallbacksOutsideOfMyCode
				}
				break;

			case DebugCallbackType.ExitProcess:
				var epArgs = (ExitProcessDebugCallbackEventArgs)e;
				process = processes.TryGet(epArgs.Process);
				if (process != null)
					process.SetHasExited();
				b = processes.Remove(epArgs.Process);
				Debug.WriteLineIf(!b, string.Format("ExitProcess: could not remove process: {0:X8}", epArgs.Process.GetHashCode()));
				break;

			case DebugCallbackType.CreateThread:
				var ctArgs = (CreateThreadDebugCallbackEventArgs)e;
				process = TryGetValidProcess(ctArgs.Thread);
				if (process != null) {
					process.TryAdd(ctArgs.Thread);
					//TODO: ICorDebugThread::SetDebugState
				}
				break;

			case DebugCallbackType.ExitThread:
				var etArgs = (ExitThreadDebugCallbackEventArgs)e;
				process = TryGetValidProcess(etArgs.Thread);
				if (process != null)
					process.ThreadExited(etArgs.Thread);
				break;

			case DebugCallbackType.LoadModule:
				var lmArgs = (LoadModuleDebugCallbackEventArgs)e;
				assembly = TryGetValidAssembly(lmArgs.AppDomain, lmArgs.Module);
				if (assembly != null) {
					assembly.TryAdd(lmArgs.Module);
					//TODO: ICorDebugModule::EnableJITDebugging 
					//TODO: ICorDebugModule::EnableClassLoadCallbacks
					//TODO: ICorDebugModule2::SetJITCompilerFlags
					//TODO: ICorDebugModule2::SetJMCStatus
				}
				break;

			case DebugCallbackType.UnloadModule:
				var umArgs = (UnloadModuleDebugCallbackEventArgs)e;
				assembly = TryGetValidAssembly(umArgs.AppDomain, umArgs.Module);
				if (assembly != null)
					assembly.ModuleUnloaded(umArgs.Module);
				break;

			case DebugCallbackType.LoadClass:
				break;

			case DebugCallbackType.UnloadClass:
				break;

			case DebugCallbackType.DebuggerError:
				break;

			case DebugCallbackType.LogMessage:
				break;

			case DebugCallbackType.LogSwitch:
				break;

			case DebugCallbackType.CreateAppDomain:
				var cadArgs = (CreateAppDomainDebugCallbackEventArgs)e;
				process = TryGetValidProcess(cadArgs.Process);
				if (process != null) {
					b = cadArgs.AppDomain.Attach() >= 0;
					Debug.WriteLineIf(!b, string.Format("CreateAppDomain: could not attach to AppDomain: {0:X8}", cadArgs.AppDomain.GetHashCode()));
					if (b) {
						process.TryAdd(cadArgs.AppDomain);
						//TODO: ICorDebugProcess3::SetEnableCustomNotification
					}
				}
				break;

			case DebugCallbackType.ExitAppDomain:
				var eadArgs = (ExitAppDomainDebugCallbackEventArgs)e;
				process = processes.TryGet(eadArgs.Process);
				if (process != null)
					process.AppDomainExited(eadArgs.AppDomain);
				break;

			case DebugCallbackType.LoadAssembly:
				var laArgs = (LoadAssemblyDebugCallbackEventArgs)e;
				appDomain = TryGetValidAppDomain(laArgs.AppDomain);
				if (appDomain != null)
					appDomain.TryAdd(laArgs.Assembly);
				break;

			case DebugCallbackType.UnloadAssembly:
				var uaArgs = (UnloadAssemblyDebugCallbackEventArgs)e;
				appDomain = TryGetValidAppDomain(uaArgs.AppDomain);
				if (appDomain != null)
					appDomain.AssemblyUnloaded(uaArgs.Assembly);
				break;

			case DebugCallbackType.ControlCTrap:
				break;

			case DebugCallbackType.NameChange:
				var ncArgs = (NameChangeDebugCallbackEventArgs)e;
				if (ncArgs.AppDomain != null) {
					appDomain = TryGetValidAppDomain(ncArgs.AppDomain);
					if (appDomain != null)
						appDomain.NameChanged();
				}
				if (ncArgs.Thread != null) {
					var thread = TryGetValidThread(ncArgs.Thread);
					if (thread != null)
						thread.NameChanged();
				}
				break;

			case DebugCallbackType.UpdateModuleSymbols:
				break;

			case DebugCallbackType.EditAndContinueRemap:
				break;

			case DebugCallbackType.BreakpointSetError:
				break;

			case DebugCallbackType.FunctionRemapOpportunity:
				break;

			case DebugCallbackType.CreateConnection:
				break;

			case DebugCallbackType.ChangeConnection:
				break;

			case DebugCallbackType.DestroyConnection:
				break;

			case DebugCallbackType.Exception2:
				break;

			case DebugCallbackType.ExceptionUnwind:
				break;

			case DebugCallbackType.FunctionRemapComplete:
				break;

			case DebugCallbackType.MDANotification:
				break;

			case DebugCallbackType.CustomNotification:
				break;

			default:
				Debug.Fail(string.Format("Unknown debug callback type: {0}", e.Type));
				break;
			}
		}

		public static DnDebugger DebugProcess(DebugOptions options) {
			if (options.DebugMessageDispatcher == null)
				throw new ArgumentException("DebugMessageDispatcher is null");
			var debuggeeVersion = options.DebuggeeVersion ?? DebuggeeVersionDetector.GetVersion(options.Filename);
			var dbg = new DnDebugger(CreateCorDebug(debuggeeVersion), options.DebugMessageDispatcher);

			var dwCreationFlags = options.ProcessCreationFlags ?? DebugOptions.DefaultProcessCreationFlags;
			var si = new STARTUPINFO();
			si.cb = (uint)(4 * 1 + IntPtr.Size * 3 + 4 * 8 + 2 * 2 + IntPtr.Size * 4);
			var pi = new PROCESS_INFORMATION();
			ICorDebugProcess comProcess;
			dbg.corDebug.CreateProcess(options.Filename, options.CommandLine, IntPtr.Zero, IntPtr.Zero,
						options.InheritHandles ? 1 : 0, dwCreationFlags, IntPtr.Zero, options.CurrentDirectory,
						ref si, ref pi, CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS, out comProcess);
			// We don't need these
			NativeMethods.CloseHandle(pi.hProcess);
			NativeMethods.CloseHandle(pi.hThread);

			var process = dbg.TryAdd(comProcess);
			if (process != null)
				process.Initialize(options.Filename, options.CurrentDirectory, options.CommandLine);

			return dbg;
		}

		public static DnDebugger Attach(uint pid) {
			//TODO:
			throw new NotImplementedException();
		}

		DnProcess TryAdd(ICorDebugProcess comProcess) {
			// This method is called twice, once from DebugProcess() and once from the CreateProcess
			// handler. It's possible that it's been terminated before DebugProcess() calls this method.

			// Check if it's terminated. Error should be 0x8013134F: CORDBG_E_OBJECT_NEUTERED
			int running;
			if (comProcess.IsRunning(out running) < 0)
				return null;

			return processes.Add(comProcess);
		}

		/// <summary>
		/// Gets all processes, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnProcess[] GetProcesses() {
			var list = processes.GetAll();
			Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
			return list;
		}

		/// <summary>
		/// Gets a process or null if it has exited
		/// </summary>
		/// <param name="comProcess">Process</param>
		/// <returns></returns>
		public DnProcess TryGetValidProcess(ICorDebugProcess comProcess) {
			var process = processes.TryGet(comProcess);
			if (process == null)
				return null;
			if (!process.CheckValid())
				return null;
			return process;
		}

		public DnProcess TryGetValidProcess(ICorDebugAppDomain comAppDomain) {
			ICorDebugProcess comProcess;
			int hr = comAppDomain.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		public DnProcess TryGetValidProcess(ICorDebugThread comThread) {
			ICorDebugProcess comProcess;
			int hr = comThread.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidProcess(comProcess);
		}

		public DnAppDomain TryGetValidAppDomain(ICorDebugAppDomain comAppDomain) {
			ICorDebugProcess comProcess;
			int hr = comAppDomain.GetProcess(out comProcess);
			if (hr < 0)
				return null;
			return TryGetValidAppDomain(comProcess, comAppDomain);
		}

		public DnAppDomain TryGetValidAppDomain(ICorDebugProcess comProcess, ICorDebugAppDomain comAppDomain) {
			var process = TryGetValidProcess(comProcess);
			if (process == null)
				return null;
			return process.TryGetValidAppDomain(comAppDomain);
		}

		public DnAssembly TryGetValidAssembly(ICorDebugAppDomain comAppDomain, ICorDebugModule comModule) {
			if (comModule == null)
				return null;

			var appDomain = TryGetValidAppDomain(comAppDomain);
			if (appDomain == null)
				return null;

			ICorDebugAssembly comAssembly;
			int hr = comModule.GetAssembly(out comAssembly);
			if (hr < 0)
				return null;

			return appDomain.TryGetAssembly(comAssembly);
		}

		public DnThread TryGetValidThread(ICorDebugThread comThread) {
			var process = TryGetValidProcess(comThread);
			return process == null ? null : process.TryGetValidThread(comThread);
		}

		~DnDebugger() {
			Dispose(false);
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		void Dispose(bool disposing) {
			foreach (var process in GetProcesses())
				process.Terminate(-1);
		}
	}
}
