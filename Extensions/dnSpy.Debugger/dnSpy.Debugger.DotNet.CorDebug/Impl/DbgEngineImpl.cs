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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dndbg.DotNet;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Code;
using dnSpy.Debugger.DotNet.CorDebug.DAC;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Attach;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.Properties;
using dnSpy.Debugger.DotNet.CorDebug.Steppers;
using dnSpy.Debugger.DotNet.CorDebug.Utilities;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl : DbgEngine, IClrDacDebugger {
		public override DbgStartKind StartKind { get; }
		public override string[] DebugTags => new[] { PredefinedDebugTags.DotNetDebugger };
		public override event EventHandler<DbgEngineMessage> Message;
		public event EventHandler ClrDacRunning;
		public event EventHandler ClrDacPaused;
		public event EventHandler ClrDacTerminated;

		internal DebuggerThread DebuggerThread => debuggerThread;
		internal DbgObjectFactory ObjectFactory => objectFactory;

		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgDotNetNativeCodeLocationFactory> dbgDotNetNativeCodeLocationFactory;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgEngineStepperFactory dbgEngineStepperFactory;
		readonly DebuggerThread debuggerThread;
		readonly object lockObj;
		readonly ClrDacProvider clrDacProvider;
		internal ClrDac clrDac;
		bool clrDacInitd;
		readonly DbgManager dbgManager;
		readonly DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier;
		DnDebugger dnDebugger;
		SafeHandle hProcess_debuggee;
		DbgObjectFactory objectFactory;
		readonly Dictionary<DnAppDomain, DbgEngineAppDomain> toEngineAppDomain;
		readonly Dictionary<CorModule, DbgEngineModule> toEngineModule;
		readonly Dictionary<DnThread, DbgEngineThread> toEngineThread;
		readonly Dictionary<DnAssembly, List<DnModule>> toAssemblyModules;
		internal readonly StackFrameData stackFrameData;
		readonly HashSet<DnDebuggerObjectHolder> objectHolders;
		readonly DmdRuntime dmdRuntime;
		readonly Dictionary<CorModule, DmdDynamicModuleHelperImpl> toDynamicModuleHelper;
		internal DmdDispatcherImpl DmdDispatcher { get; }
		internal DbgRawMetadataService RawMetadataService { get; }
		readonly List<DbgDotNetValueImpl> dotNetValuesToCloseOnContinue;
		readonly List<DbgCorValueHolder> valuesToCloseNow;
		bool isUnhandledException;

		protected DbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, DbgStartKind startKind) {
			if (deps == null)
				throw new ArgumentNullException(nameof(deps));
			StartKind = startKind;
			lockObj = new object();
			toEngineAppDomain = new Dictionary<DnAppDomain, DbgEngineAppDomain>();
			toEngineModule = new Dictionary<CorModule, DbgEngineModule>();
			toEngineThread = new Dictionary<DnThread, DbgEngineThread>();
			toAssemblyModules = new Dictionary<DnAssembly, List<DnModule>>();
			stackFrameData = new StackFrameData();
			objectHolders = new HashSet<DnDebuggerObjectHolder>();
			debuggerSettings = deps.DebuggerSettings;
			dbgDotNetNativeCodeLocationFactory = deps.DbgDotNetNativeCodeLocationFactory;
			dbgDotNetCodeLocationFactory = deps.DbgDotNetCodeLocationFactory;
			dbgEngineStepperFactory = deps.EngineStepperFactory;
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			dbgModuleMemoryRefreshedNotifier = deps.DbgModuleMemoryRefreshedNotifier;
			clrDacProvider = deps.ClrDacProvider;
			clrDac = NullClrDac.Instance;
			debuggerThread = new DebuggerThread("CorDebug");
			debuggerThread.CallDispatcherRun();
			dotNetValuesToCloseOnContinue = new List<DbgDotNetValueImpl>();
			valuesToCloseNow = new List<DbgCorValueHolder>();
			currentReturnValues = Array.Empty<DbgDotNetReturnValueInfo>();
			dmdRuntime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl(this), IntPtr.Size == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			toDynamicModuleHelper = new Dictionary<CorModule, DmdDynamicModuleHelperImpl>();
			DmdDispatcher = new DmdDispatcherImpl(this);
			RawMetadataService = deps.RawMetadataService;
		}

		internal event EventHandler<ClassLoadedEventArgs> ClassLoaded;

		internal bool CheckCorDebugThread() => debuggerThread.CheckAccess();
		internal void VerifyCorDebugThread() => debuggerThread.VerifyAccess();
		internal T InvokeCorDebugThread<T>(Func<T> callback) => debuggerThread.Invoke(callback);
		internal void CorDebugThread(Action callback) => debuggerThread.BeginInvoke(callback);
		internal string DebuggeeVersion => dnDebugger.DebuggeeVersion;
		internal bool IsPaused => dnDebugger.ProcessState == DebuggerProcessState.Paused;

		internal DbgEngineMessageFlags GetMessageFlags(bool pause = false) {
			VerifyCorDebugThread();
			var flags = DbgEngineMessageFlags.None;
			if (pause)
				flags |= DbgEngineMessageFlags.Pause;
			if (dnDebugger?.IsEvaluating == true)
				flags |= DbgEngineMessageFlags.Continue;
			return flags;
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			string msg;
			DbgModule module;
			switch (e.Kind) {
			case DebugCallbackKind.CreateProcess:
				var cp = (CreateProcessDebugCallbackEventArgs)e;
				hProcess_debuggee = Native.NativeMethods.OpenProcess(Native.NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)(cp.CorProcess?.ProcessId ?? -1));
				SendMessage(new DbgMessageConnected(cp.CorProcess.ProcessId, GetMessageFlags()));
				e.AddPauseReason(DebuggerPauseReason.Other);
				break;

			case DebugCallbackKind.CreateAppDomain:
				// CreateProcess is too early, we must do this when the AppDomain gets created
				if (!clrDacInitd) {
					clrDacInitd = true;
					var p = dnDebugger.Processes.FirstOrDefault();
					if (p != null)
						clrDac = clrDacProvider.Create(p.ProcessId, dnDebugger.CLRPath, this);
				}
				break;

			case DebugCallbackKind.Exception2:
				var e2 = (Exception2DebugCallbackEventArgs)e;
				DbgExceptionEventFlags exFlags;
				if (e2.EventType == CorDebugExceptionCallbackType.DEBUG_EXCEPTION_FIRST_CHANCE)
					exFlags = DbgExceptionEventFlags.FirstChance;
				else if (e2.EventType == CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED) {
					exFlags = DbgExceptionEventFlags.SecondChance | DbgExceptionEventFlags.Unhandled;
					isUnhandledException = true;
				}
				else
					break;

				// Ignore exceptions when evaluating except if it's an unhandled exception, those must always be reported
				if (dbg.IsEvaluating && e2.EventType != CorDebugExceptionCallbackType.DEBUG_EXCEPTION_UNHANDLED)
					break;

				module = TryGetModule(e2.CorFrame, e2.CorThread);
				var exObj = e2.CorThread?.CurrentException;
				var reflectionAppDomain = module?.GetReflectionModule().AppDomain;
				DbgDotNetValueImpl dnExObj = null;
				try {
					if (exObj != null && reflectionAppDomain != null)
						dnExObj = CreateDotNetValue_CorDebug(exObj, reflectionAppDomain, tryCreateStrongHandle: false) as DbgDotNetValueImpl;
					objectFactory.CreateException(new DbgExceptionId(PredefinedExceptionCategories.DotNet, TryGetExceptionName(dnExObj) ?? "???"), exFlags, TryGetExceptionMessage(dnExObj), TryGetThread(e2.CorThread), module, GetMessageFlags());
					e.AddPauseReason(DebuggerPauseReason.Other);
				}
				finally {
					dnExObj?.Dispose();
				}
				break;

			case DebugCallbackKind.MDANotification:
				if (dbg.IsEvaluating)
					break;
				var mdan = (MDANotificationDebugCallbackEventArgs)e;
				objectFactory.CreateException(new DbgExceptionId(PredefinedExceptionCategories.MDA, mdan.CorMDA?.Name ?? "???"), DbgExceptionEventFlags.FirstChance, mdan.CorMDA?.Description, TryGetThread(mdan.CorThread), TryGetModule(null, mdan.CorThread), GetMessageFlags());
				e.AddPauseReason(DebuggerPauseReason.Other);
				break;

			case DebugCallbackKind.LogMessage:
				if (dbg.IsEvaluating)
					break;
				var lmsgArgs = (LogMessageDebugCallbackEventArgs)e;
				msg = lmsgArgs.Message;
				if (msg != null) {
					e.AddPauseReason(DebuggerPauseReason.Other);
					var thread = TryGetThread(lmsgArgs.CorThread);
					SendMessage(new DbgMessageProgramMessage(msg, thread, GetMessageFlags()));
				}
				break;

			case DebugCallbackKind.LoadClass:
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				var cls = lcArgs.CorClass;
				Debug.Assert(cls != null);
				if (cls != null) {
					var dnModule = dbg.TryGetModule(lcArgs.CorAppDomain, cls);
					if (dnModule.IsDynamic) {
						UpdateDynamicModuleIds(dnModule);
						module = TryGetModule(dnModule.CorModule);
						Debug.Assert(module != null);
						if (module != null)
							dbgModuleMemoryRefreshedNotifier.RaiseModulesRefreshed(new[] { module });
						if (dnModule?.CorModuleDef != null && module != null) {
							if (TryGetModuleData(module, out var data))
								data.OnLoadClass();
							ClassLoaded?.Invoke(this, new ClassLoadedEventArgs(module, cls.Token));
						}
						GetDynamicModuleHelper(dnModule).RaiseTypeLoaded(new DmdTypeLoadedEventArgs((int)cls.Token));
					}
				}
				break;

			case DebugCallbackKind.DebuggerError:
				var deArgs = (DebuggerErrorDebugCallbackEventArgs)e;
				if (deArgs.HError == CordbgErrors.CORDBG_E_UNCOMPATIBLE_PLATFORMS)
					msg = GetIncompatiblePlatformErrorMessage();
				else
					msg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CLRDebuggerErrorOccurred, deArgs.HError, deArgs.ErrorCode);
				SendMessage(new DbgMessageBreak(msg, GetMessageFlags(pause: true)));
				break;
			}
		}

		internal void RaiseModulesRefreshed(DbgModule module) => dbgModuleMemoryRefreshedNotifier.RaiseModulesRefreshed(new[] { module });

		internal DmdDynamicModuleHelperImpl GetDynamicModuleHelper(DnModule dnModule) {
			Debug.Assert(dnModule.IsDynamic);
			lock (lockObj) {
				if (!toDynamicModuleHelper.TryGetValue(dnModule.CorModule, out var helper))
					toDynamicModuleHelper.Add(dnModule.CorModule, helper = new DmdDynamicModuleHelperImpl(this));
				return helper;
			}
		}

		string TryGetExceptionName(DbgDotNetValue exObj) {
			if (exObj == null)
				return null;
			var type = exObj.Type;
			if (type.IsConstructedGenericType)
				type = type.GetGenericTypeDefinition();
			return type.FullName;
		}

		string TryGetExceptionMessage(DbgDotNetValueImpl exObj) {
			if (exObj == null)
				return null;
			var res = ReadField_CorDebug(exObj, "_message");
			if (res == null || !res.Value.HasRawValue)
				return null;
			return res.Value.RawValue as string ?? dnSpy_Debugger_DotNet_CorDebug_Resources.ExceptionMessageIsNull;
		}

		internal DbgThread TryGetThread(CorThread thread) {
			if (thread == null)
				return null;
			var dnThread = dnDebugger.Processes.FirstOrDefault()?.Threads.FirstOrDefault(a => a.CorThread == thread);
			return TryGetThread(dnThread);
		}

		DbgThread TryGetThread(DnThread dnThread) {
			if (dnThread == null)
				return null;
			DbgEngineThread engineThread;
			lock (lockObj)
				toEngineThread.TryGetValue(dnThread, out engineThread);
			return engineThread?.Thread;
		}

		DbgModule TryGetModule(CorFrame frame, CorThread thread) {
			if (frame?.Function == null && thread != null) {
				frame = thread.ActiveFrame;
				if (frame?.Function == null) {
					// Ignore the first frame(s) that have a null function. This rarely happens (eg. it
					// happens when debugging dnSpy built for .NET Core x86)
					frame = thread.AllFrames.FirstOrDefault(a => a.Function != null);
				}
			}
			return TryGetModule(frame?.Function?.Module);
		}

		internal DbgModule TryGetModule(CorModule corModule) {
			if (corModule == null)
				return null;
			lock (lockObj) {
				if (toEngineModule.TryGetValue(corModule, out var engineModule))
					return engineModule.Module;
			}
			return null;
		}

		void HookDnDebuggerEvents() {
			dnDebugger.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
			dnDebugger.OnProcessStateChanged += DnDebugger_OnProcessStateChanged;
			dnDebugger.OnNameChanged += DnDebugger_OnNameChanged;
			dnDebugger.OnThreadAdded += DnDebugger_OnThreadAdded;
			dnDebugger.OnAppDomainAdded += DnDebugger_OnAppDomainAdded;
			dnDebugger.OnModuleAdded += DnDebugger_OnModuleAdded;
			dnDebugger.OnCorModuleDefCreated += DnDebugger_OnCorModuleDefCreated;
			dnDebugger.OnAttachComplete += DnDebugger_OnAttachComplete;
		}

		void UnhookDnDebuggerEventsAndCloseProcessHandle() {
			if (dnDebugger != null) {
				dnDebugger.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				dnDebugger.OnProcessStateChanged -= DnDebugger_OnProcessStateChanged;
				dnDebugger.OnNameChanged -= DnDebugger_OnNameChanged;
				dnDebugger.OnThreadAdded -= DnDebugger_OnThreadAdded;
				dnDebugger.OnAppDomainAdded -= DnDebugger_OnAppDomainAdded;
				dnDebugger.OnModuleAdded -= DnDebugger_OnModuleAdded;
				dnDebugger.OnCorModuleDefCreated -= DnDebugger_OnCorModuleDefCreated;
				dnDebugger.OnAttachComplete -= DnDebugger_OnAttachComplete;
				dnDebugger.OnRedirectedOutput -= DnDebugger_OnRedirectedOutput;
			}
			hProcess_debuggee?.Close();
		}

		void DnDebugger_OnAttachComplete(object sender, EventArgs e) => DetectMainThread();

		void DnDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			Debug.Assert(sender != null && sender == dnDebugger);

			if (dnDebugger.ProcessState == DebuggerProcessState.Terminated) {
				if (hProcess_debuggee == null || hProcess_debuggee.IsClosed || hProcess_debuggee.IsInvalid || !Native.NativeMethods.GetExitCodeProcess(hProcess_debuggee.DangerousGetHandle(), out int exitCode))
					exitCode = -1;
				clrDac = NullClrDac.Instance;
				ClrDacTerminated?.Invoke(this, EventArgs.Empty);
				UnhookDnDebuggerEventsAndCloseProcessHandle();

				SendMessage(new DbgMessageDisconnected(exitCode, GetMessageFlags()));
				return;
			}
			else if (dnDebugger.ProcessState == DebuggerProcessState.Paused) {
				ClrDacPaused?.Invoke(this, EventArgs.Empty);
				UpdateThreadProperties_CorDebug();

				foreach (var debuggerState in dnDebugger.DebuggerStates) {
					foreach (var pauseState in debuggerState.PauseStates) {
						if (pauseState.Handled)
							continue;
						pauseState.Handled = true;
						switch (pauseState.Reason) {
						case DebuggerPauseReason.Other:
							// We use this reason when we pause the process, DbgManager already knows that we're paused
							continue;

						case DebuggerPauseReason.AsyncStepperBreakpoint:
							// Used by async stepper code. We shouldn't notify DbgManager. The async stepper code will eventually
							// create a stepper event.
							continue;

						case DebuggerPauseReason.UserBreak:
							// BreakCore() sends the Break message
							continue;

						case DebuggerPauseReason.ILCodeBreakpoint:
							var ilbp = (ILCodeBreakpointPauseState)pauseState;
							SendCodeBreakpointHitMessage_CorDebug(ilbp.Breakpoint, TryGetThread(ilbp.CorThread));
							break;

						case DebuggerPauseReason.Break:
							var bs = (BreakPauseState)pauseState;
							SendMessage(new DbgMessageProgramBreak(TryGetThread(bs.CorThread), GetMessageFlags()));
							break;

						case DebuggerPauseReason.NativeCodeBreakpoint:
							var nbp = (NativeCodeBreakpointPauseState)pauseState;
							SendCodeBreakpointHitMessage_CorDebug(nbp.Breakpoint, TryGetThread(nbp.CorThread));
							break;

						case DebuggerPauseReason.EntryPointBreakpoint:
							var epbp = (EntryPointBreakpointPauseState)pauseState;
							SendMessage(new DbgMessageEntryPointBreak(TryGetThread(epbp.CorThread), GetMessageFlags()));
							break;

						case DebuggerPauseReason.Eval:
							// Don't send a message, that will confuse DbgManager. It thinks we're running or paused.
							break;

						case DebuggerPauseReason.DebugEventBreakpoint:
						case DebuggerPauseReason.AnyDebugEventBreakpoint:
							SendMessage(new DbgMessageBreak(TryGetThread(debuggerState.Thread), GetMessageFlags()));
							break;

						default:
							Debug.Fail($"Unknown reason: {pauseState.Reason}");
							SendMessage(new DbgMessageBreak(TryGetThread(debuggerState.Thread), GetMessageFlags()));
							break;
						}
					}
				}
			}
		}

		void DnDebugger_OnRedirectedOutput(object sender, RedirectedOutputEventArgs e) {
			var source = e.IsStandardOutput ? AsyncProgramMessageSource.StandardOutput : AsyncProgramMessageSource.StandardError;
			SendMessage(new DbgMessageAsyncProgramMessage(source, e.Text));
		}

		void DnDebugger_OnNameChanged(object sender, NameChangedDebuggerEventArgs e) {
			TryGetEngineAppDomain(e.AppDomain)?.UpdateName(e.AppDomain.Name);
			OnNewThreadName_CorDebug(e.Thread);
		}

		DbgEngineAppDomain TryGetEngineAppDomain(DnAppDomain dnAppDomain) {
			if (dnAppDomain == null)
				return null;
			DbgEngineAppDomain engineAppDomain;
			bool b;
			lock (lockObj)
				b = toEngineAppDomain.TryGetValue(dnAppDomain, out engineAppDomain);
			Debug.Assert(b);
			return engineAppDomain;
		}

		void DnDebugger_OnAppDomainAdded(object sender, AppDomainDebuggerEventArgs e) {
			Debug.Assert(objectFactory != null);
			if (e.Added) {
				e.ShouldPause = true;
				var appDomain = dmdRuntime.CreateAppDomain(e.AppDomain.Id);
				var internalAppDomain = new DbgCorDebugInternalAppDomainImpl(appDomain, e.AppDomain);
				var engineAppDomain = objectFactory.CreateAppDomain<object>(internalAppDomain, e.AppDomain.Name, e.AppDomain.Id, GetMessageFlags(), data: null, onCreated: engineAppDomain2 => internalAppDomain.SetAppDomain(engineAppDomain2.AppDomain));
				lock (lockObj)
					toEngineAppDomain.Add(e.AppDomain, engineAppDomain);
			}
			else {
				DbgEngineAppDomain engineAppDomain;
				lock (lockObj) {
					if (toEngineAppDomain.TryGetValue(e.AppDomain, out engineAppDomain)) {
						toEngineAppDomain.Remove(e.AppDomain);
						var appDomain = engineAppDomain.AppDomain;
						dmdRuntime.Remove(((DbgCorDebugInternalAppDomainImpl)appDomain.InternalAppDomain).ReflectionAppDomain);
						foreach (var kv in toEngineThread.ToArray()) {
							if (kv.Value.Thread.AppDomain == appDomain)
								toEngineThread.Remove(kv.Key);
						}
						foreach (var kv in toEngineModule.ToArray()) {
							if (kv.Value.Module.AppDomain == appDomain) {
								toEngineModule.Remove(kv.Key);
								kv.Value.Remove(GetMessageFlags());
								toDynamicModuleHelper.Remove(kv.Key);
							}
						}
					}
				}
				if (engineAppDomain != null) {
					e.ShouldPause = true;
					engineAppDomain.Remove(GetMessageFlags());
				}
			}
		}

		sealed class DbgModuleData {
			public DbgEngineImpl Engine { get; }
			public DnModule DnModule { get; }
			public ModuleId ModuleId { get; private set; }
			public bool HasUpdatedModuleId { get; private set; }
			public int LoadClassVersion => loadClassVersion;
			volatile int loadClassVersion;
			public DbgModuleData(DbgEngineImpl engine, DnModule dnModule, ModuleId moduleId) {
				Engine = engine;
				DnModule = dnModule;
				ModuleId = moduleId;
			}
			public void OnLoadClass() => Interlocked.Increment(ref loadClassVersion);
			public void UpdateModuleId(ModuleId moduleId) {
				if (!moduleId.IsDynamic)
					throw new InvalidOperationException();
				ModuleId = moduleId;
				HasUpdatedModuleId = true;
			}
		}

		internal ModuleId GetModuleId(DbgModule module) {
			if (TryGetModuleData(module, out var data))
				return data.ModuleId;
			throw new InvalidOperationException();
		}

		bool TryGetModuleData(DbgModule module, out DbgModuleData data) {
			if (module.TryGetData(out data) && data.Engine == this)
				return true;
			data = null;
			return false;
		}

		internal bool TryGetDnModuleAndVersion(DbgModule module, out DnModule dnModule, out int loadClassVersion) {
			if (module.TryGetData(out DbgModuleData data) && data.Engine == this) {
				dnModule = data.DnModule;
				loadClassVersion = data.LoadClassVersion;
				return true;
			}
			dnModule = null;
			loadClassVersion = -1;
			return false;
		}

		internal bool TryGetDnModule(DbgModule module, out DnModule dnModule) {
			if (module.TryGetData(out DbgModuleData data) && data.Engine == this) {
				dnModule = data.DnModule;
				return true;
			}
			dnModule = null;
			return false;
		}

		// When a dynamic assembly is created with option Run, a module gets created and its
		// metadata name is "RefEmit_InMemoryManifestModule". Shortly thereafter, its name
		// gets changed to the name the user chose.
		// This name is also saved in ModuleIds, and used when setting breakpoints...
		// There's code that caches ModuleIds, but they don't cache it if IsDynamic is true.
		// This method updates the ModuleId and resets breakpoints in the module.
		void UpdateDynamicModuleIds(DnModule dnModule) {
			debuggerThread.VerifyAccess();
			if (!dnModule.IsDynamic)
				return;
			var module = TryGetModule(dnModule.CorModule);
			if (module == null || !TryGetModuleData(module, out var data) || data.HasUpdatedModuleId)
				return;
			List<(DbgModule dbgModule, DnModule dnModule)> updatedModules = null;
			lock (lockObj) {
				if (toAssemblyModules.TryGetValue(dnModule.Assembly, out var modules)) {
					for (int i = 0; i < modules.Count; i++) {
						dnModule = modules[i];
						if (!dnModule.IsDynamic)
							continue;
						if (!toEngineModule.TryGetValue(dnModule.CorModule, out var em))
							continue;
						if (!TryGetModuleData(em.Module, out data))
							continue;
						dnModule.CorModule.ClearCachedDnlibName();
						var moduleId = dnModule.DnModuleId.ToModuleId();
						if (data.ModuleId == moduleId)
							continue;
						data.UpdateModuleId(moduleId);
						if (dnModule.CorModuleDef != null) {
							//TODO: This doesn't update the treeview node
							dnModule.CorModuleDef.Name = moduleId.ModuleName;
						}
						if (updatedModules == null)
							updatedModules = new List<(DbgModule, DnModule)>();
						updatedModules.Add((em.Module, dnModule));
					}
				}
			}
			if (updatedModules != null) {
				foreach (var info in updatedModules) {
					var mdi = info.dnModule.CorModule.GetMetaDataInterface<IMetaDataImport2>();
					var scopeName = MDAPI.GetModuleName(mdi) ?? string.Empty;
					((DbgCorDebugInternalModuleImpl)info.dbgModule.InternalModule).ReflectionModule.ScopeName = scopeName;
				}
				dbgModuleMemoryRefreshedNotifier.RaiseModulesRefreshed(updatedModules.Select(a => a.dbgModule).ToArray());
			}
		}

		void DnDebugger_OnModuleAdded(object sender, ModuleDebuggerEventArgs e) {
			Debug.Assert(objectFactory != null);
			if (e.Added) {
				e.ShouldPause = true;
				var appDomain = TryGetEngineAppDomain(e.Module.AppDomain)?.AppDomain;
				var moduleId = e.Module.DnModuleId.ToModuleId();
				var moduleData = new DbgModuleData(this, e.Module, moduleId);
				var engineModule = ModuleCreator.CreateModule(this, objectFactory, appDomain, e.Module, moduleData);
				lock (lockObj) {
					if (!toAssemblyModules.TryGetValue(e.Module.Assembly, out var modules))
						toAssemblyModules.Add(e.Module.Assembly, modules = new List<DnModule>());
					modules.Add(e.Module);
					toEngineModule.Add(e.Module.CorModule, engineModule);
				}

				var reflectionModule = ((DbgCorDebugInternalModuleImpl)engineModule.Module.InternalModule).ReflectionModule;
				if (reflectionModule.IsCorLib) {
					var type = reflectionModule.AppDomain.GetWellKnownType(DmdWellKnownType.System_Diagnostics_Debugger_CrossThreadDependencyNotification, isOptional: true);
					Debug.Assert((object)type != null || dnDebugger.DebuggeeVersion.StartsWith("v2."));
					if ((object)type != null)
						dnDebugger.AddCustomNotificationClassToken(e.Module, (uint)type.MetadataToken);
				}
			}
			else {
				DbgEngineModule engineModule;
				lock (lockObj) {
					if (toAssemblyModules.TryGetValue(e.Module.Assembly, out var modules)) {
						modules.Remove(e.Module);
						if (modules.Count == 0)
							toAssemblyModules.Remove(e.Module.Assembly);
					}
					toDynamicModuleHelper.Remove(e.Module.CorModule);
					if (toEngineModule.TryGetValue(e.Module.CorModule, out engineModule)) {
						toEngineModule.Remove(e.Module.CorModule);
						((DbgCorDebugInternalModuleImpl)engineModule.Module.InternalModule).Remove();
					}
				}
				if (engineModule != null) {
					e.ShouldPause = true;
					engineModule.Remove(GetMessageFlags());
				}
			}
		}

		internal (CorModuleDef metadata, ModuleId moduleId) GetDynamicMetadata_EngineThread(DbgModule module) {
			debuggerThread.VerifyAccess();
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (!TryGetModuleData(module, out var data))
				return (null, default);
			return (data.DnModule.GetOrCreateCorModuleDef(), data.DnModule.DnModuleId.ToModuleId());
		}

		internal static ModuleId? TryGetModuleId(DbgModule module) {
			if (module.TryGetData(out DbgModuleData data))
				return data.ModuleId;
			return null;
		}

		void SendMessage(DbgEngineMessage message) => Message?.Invoke(this, message);

		public override void Start(DebugProgramOptions options) => CorDebugThread(() => {
			if (StartKind == DbgStartKind.Start)
				StartCore((CorDebugStartDebuggingOptions)options);
			else if (StartKind == DbgStartKind.Attach)
				AttachCore((CorDebugAttachToProgramOptions)options);
			else
				throw new InvalidOperationException();
		});

		protected abstract CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options);
		protected abstract CLRTypeAttachInfo CreateAttachInfo(CorDebugAttachToProgramOptions options);

		void StartCore(CorDebugStartDebuggingOptions options) {
			debuggerThread.VerifyAccess();
			try {
				if (debuggerThread.HasShutdownStarted)
					throw new InvalidOperationException("Dispatcher has shut down");

				var env = new DbgEnvironment(options.Environment);
				bool disableMDA = !debuggerSettings.EnableManagedDebuggingAssistants;
				// If 32-bit .NET Framework and MDAs are enabled, pinvoke methods are called from clr.dll
				// which breaks anti-IsDebuggerPresent() code. Our workaround is to disable MDAs.
				// We can only debug processes with the same bitness, so check IntPtr.Size.
				if (IntPtr.Size == 4 && debuggerSettings.AntiIsDebuggerPresent && options is DotNetFrameworkStartDebuggingOptions)
					disableMDA = true;
				// .NET Core doesn't support MDAs
				if (options is DotNetCoreStartDebuggingOptions)
					disableMDA = false;
				if (disableMDA) {
					// https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/diagnosing-errors-with-managed-debugging-assistants
					env.Add("COMPLUS_MDA", "0");
				}
				if (debuggerSettings.SuppressJITOptimization_SystemModules) {
					env.Add("COMPlus_ZapDisable", "1");
					env.Add("COMPlus_ReadyToRun", "0");
				}

				var dbgOptions = new DebugProcessOptions(CreateDebugInfo(options)) {
					DebugMessageDispatcher = debuggerThread.GetDebugMessageDispatcher(),
					CurrentDirectory = options.WorkingDirectory,
					Filename = PathUtils.NormalizeFilename(options.Filename),
					CommandLine = options.CommandLine,
					BreakProcessKind = GetBreakProcessKind(options.BreakKind),
					Environment = env.Environment,
				};
				dbgOptions.DebugOptions.IgnoreBreakInstructions = false;
				dbgOptions.DebugOptions.DebugOptionsProvider = new DebugOptionsProviderImpl(debuggerSettings);
				if (debuggerSettings.RedirectGuiConsoleOutput && PortableExecutableFileHelpers.IsGuiApp(options.Filename))
					dbgOptions.RedirectConsoleOutput = true;

				dnDebugger = DnDebugger.DebugProcess(dbgOptions);
				if (dbgOptions.RedirectConsoleOutput)
					dnDebugger.OnRedirectedOutput += DnDebugger_OnRedirectedOutput;
				OnDebugProcess(dnDebugger);
				HookDnDebuggerEvents();
				return;
			}
			catch (Exception ex) {
				var cex = ex as COMException;
				const int ERROR_NOT_SUPPORTED = unchecked((int)0x80070032);
				string errMsg;
				if (ex is StartDebuggerException sde) {
					switch (sde.Error) {
					case StartDebuggerError.UnsupportedBitness:
						errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
						break;
					default:
						throw new InvalidOperationException();
					}
				}
				else if (cex != null && cex.ErrorCode == ERROR_NOT_SUPPORTED)
					errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == CordbgErrors.CORDBG_E_UNCOMPATIBLE_PLATFORMS)
					errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
				else if (cex != null && cex.ErrorCode == unchecked((int)0x800702E4)) {
					// The x64 CLR debugger doesn't return the correct error code when we try to debug a 32-bit req-admin program.
					// It doesn't support debugging 32-bit programs, so it should return CORDBG_E_UNCOMPATIBLE_PLATFORMS or ERROR_NOT_SUPPORTED.
					if (IntPtr.Size == 8 && DotNetAssemblyUtilities.TryGetProgramBitness(options.Filename) == 32)
						errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger, GetIncompatiblePlatformErrorMessage());
					else
						errMsg = dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebuggerRequireAdminPrivLvl;
				}
				else
					errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebuggerCheckAccessToFile, options.Filename ?? "<???>", ex.Message);

				SendMessage(new DbgMessageConnected(errMsg, GetMessageFlags()));
				return;
			}
		}

		static BreakProcessKind GetBreakProcessKind(string breakKind) {
			if (breakKind == PredefinedBreakKinds.EntryPoint)
				return BreakProcessKind.EntryPoint;
			return BreakProcessKind.None;
		}

		static string GetIncompatiblePlatformErrorMessage() {
			if (IntPtr.Size == 4)
				return dnSpy_Debugger_DotNet_CorDebug_Resources.UseDnSpyExeToDebug64;
			return dnSpy_Debugger_DotNet_CorDebug_Resources.UseDnSpy64ExeToDebug32;
		}

		void AttachCore(CorDebugAttachToProgramOptions options) {
			debuggerThread.VerifyAccess();
			try {
				if (debuggerThread.HasShutdownStarted)
					throw new InvalidOperationException("Dispatcher has shut down");
				var dbgOptions = new AttachProcessOptions(CreateAttachInfo(options)) {
					DebugMessageDispatcher = debuggerThread.GetDebugMessageDispatcher(),
					ProcessId = (int)options.ProcessId,
				};
				dbgOptions.DebugOptions.DebugOptionsProvider = new DebugOptionsProviderImpl(debuggerSettings);

				dnDebugger = DnDebugger.Attach(dbgOptions);
				if (dnDebugger.Processes.Length == 0)
					throw new ErrorException(string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotAttachToProcess, $"PID={options.ProcessId.ToString()}"));
				OnDebugProcess(dnDebugger);
				HookDnDebuggerEvents();
				return;
			}
			catch (Exception ex) {
				string errMsg;
				if (ex is ErrorException errEx)
					errMsg = errEx.Message;
				else if (CorDebugRuntimeKind == CorDebugRuntimeKind.DotNetCore && ex is ArgumentException) {
					// .NET Core throws ArgumentException if it can't attach to it (.NET Framework throws a COM exception with the correct error message)
					errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger2,
						string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_ProcessIsAlreadyBeingDebugged, options.ProcessId.ToString()));
				}
				else
					errMsg = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotStartDebugger2, ex.Message);

				SendMessage(new DbgMessageConnected(errMsg, GetMessageFlags()));
				return;
			}
		}

		sealed class ErrorException : Exception {
			public ErrorException(string msg) : base(msg) { }
		}

		protected abstract void OnDebugProcess(DnDebugger dnDebugger);

		protected abstract CorDebugRuntimeKind CorDebugRuntimeKind { get; }

		sealed class RuntimeData {
			public DbgEngineImpl Engine { get; }
			public RuntimeData(DbgEngineImpl engine) => Engine = engine;
		}

		internal static DbgEngineImpl TryGetEngine(DbgRuntime runtime) {
			if (runtime.TryGetData(out RuntimeData data))
				return data.Engine;
			return null;
		}

		internal DbgModule[] GetAssemblyModules(DbgModule module) {
			if (!TryGetModuleData(module, out var data))
				return Array.Empty<DbgModule>();
			lock (lockObj) {
				toAssemblyModules.TryGetValue(data.DnModule.Assembly, out var modules);
				if (modules == null || modules.Count == 0)
					return Array.Empty<DbgModule>();
				var res = new List<DbgModule>(modules.Count);
				foreach (var dnModule in modules) {
					if (toEngineModule.TryGetValue(dnModule.CorModule, out var engineModule))
						res.Add(engineModule.Module);
				}
				return res.ToArray();
			}
		}

		internal IDbgDotNetRuntime DotNetRuntime => internalRuntime;
		DbgCorDebugInternalRuntimeImpl internalRuntime;
		public override DbgInternalRuntime CreateInternalRuntime(DbgRuntime runtime) {
			if (internalRuntime != null)
				throw new InvalidOperationException();
			return internalRuntime = new DbgCorDebugInternalRuntimeImpl(this, runtime, dmdRuntime, CorDebugRuntimeKind, dnDebugger.DebuggeeVersion ?? string.Empty, dnDebugger.CLRPath, dnDebugger.RuntimeDirectory);
		}

		public override void OnConnected(DbgObjectFactory objectFactory, DbgRuntime runtime) {
			Debug.Assert(objectFactory.Runtime == runtime);
			Debug.Assert(Array.IndexOf(objectFactory.Process.Runtimes, runtime) < 0);
			this.objectFactory = objectFactory;
			runtime.GetOrCreateData(() => new RuntimeData(this));
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			UnhookDnDebuggerEventsAndCloseProcessHandle();
			debuggerThread.Terminate();
			DnDebuggerObjectHolder[] objHoldersToClose;
			lock (lockObj) {
				framesBuffer = null;
				toEngineAppDomain.Clear();
				toEngineModule.Clear();
				toDynamicModuleHelper.Clear();
				toEngineThread.Clear();
				objHoldersToClose = objectHolders.ToArray();
				objectHolders.Clear();
			}
			foreach (var obj in objHoldersToClose)
				obj.Dispose();
		}

		bool HasConnected_DebugThread {
			get {
				debuggerThread.VerifyAccess();
				// If it's null, we haven't connected yet (most likely due to timeout, eg. trying to debug
				// a .NET Framework program with the .NET Core engine)
				return dnDebugger != null;
			}
		}

		public override void Break() => CorDebugThread(BreakCore);
		void BreakCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;

			// If we haven't gotten the CreateProcess event yet, wait for it.
			if (dnDebugger.ProcessState == DebuggerProcessState.Starting)
				return;

			if (dnDebugger.ProcessState == DebuggerProcessState.Running) {
				int hr = dnDebugger.TryBreakProcesses();
				if (hr < 0) {
					// We also sometimes get 0x80070005 before the process is terminated
					if (hr == CordbgErrors.CORDBG_E_PROCESS_TERMINATED)
						return;
					SendMessage(new DbgMessageBreak(string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.Error_CouldNotBreakProcess, hr), GetMessageFlags()));
				}
				else {
					Debug.Assert(dnDebugger.ProcessState == DebuggerProcessState.Paused);
					// The debugger just picks the first thread in the first AppDomain, and this isn't
					// always the main thread, eg. when we've attached to a proces. It also doesn't
					// have enough info to find the main thread so we have to do it.
					SendMessage(new DbgMessageBreak(GetThreadPreferMain_CorDebug(), GetMessageFlags()));
				}
			}
			else
				SendMessage(new DbgMessageBreak(GetThreadPreferMain_CorDebug(), GetMessageFlags()));
		}

		DbgThread GetThreadPreferMain_CorDebug() {
			debuggerThread.VerifyAccess();
			DbgThread firstThread = null;
			foreach (var p in dnDebugger.Processes) {
				foreach (var t in p.Threads) {
					var thread = TryGetThread(t);
					if (firstThread == null)
						firstThread = thread;
					if (thread?.IsMain == true)
						return thread;
				}
			}
			return firstThread;
		}

		public override void Run() => CorDebugThread(RunCore);
		void RunCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState == DebuggerProcessState.Paused)
				Continue_CorDebug();
		}

		internal void Continue_CorDebug() {
			debuggerThread.VerifyAccess();
			ClrDacRunning?.Invoke(this, EventArgs.Empty);
			// We could be func evaluating and get a CreateThread event. The DbgManager will call Run()
			// but we mustn't dispose of the handles that we're still using.
			if (!dnDebugger.IsEvaluating) {
				SetReturnValues(Array.Empty<DbgDotNetReturnValueInfo>());
				CloseDotNetValues_CorDebug();
			}
			dnDebugger.Continue();
		}

		public override void Terminate() => CorDebugThread(TerminateCore);
		void TerminateCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState != DebuggerProcessState.Terminated)
				dnDebugger.TerminateProcesses();
		}

		public override bool CanDetach => true;

		public override void Detach() => CorDebugThread(DetachCore);
		void DetachCore() {
			debuggerThread.VerifyAccess();
			if (!HasConnected_DebugThread)
				return;
			if (dnDebugger.ProcessState != DebuggerProcessState.Terminated) {
				int hr = dnDebugger.TryDetach();
				if (hr < 0) {
					Debug.Assert(hr == CordbgErrors.CORDBG_E_UNRECOVERABLE_ERROR || hr == CordbgErrors.CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
					dnDebugger.TerminateProcesses();
				}
			}
		}

		internal DnDebuggerObjectHolder<T> CreateDnDebuggerObjectHolder<T>(T obj) where T : class {
			var res = DnDebuggerObjectHolderImpl<T>.Create_DONT_CALL(this, obj);
			lock (lockObj)
				objectHolders.Add(res);
			return res;
		}

		internal void Remove<T>(DnDebuggerObjectHolder<T> obj) where T : class {
			lock (lockObj) {
				bool b = objectHolders.Remove(obj);
				Debug.Assert(b);
			}
			obj.Dispose();
		}

		public override DbgEngineStepper CreateStepper(DbgThread thread) =>
			dbgEngineStepperFactory.Create(DotNetRuntime, new DbgDotNetEngineStepperImpl(this, dnDebugger), thread);
	}
}
