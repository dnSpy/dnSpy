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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Steppers;
using dnSpy.Debugger.CallStack;
using dnSpy.Debugger.Steppers;

namespace dnSpy.Debugger.Impl {
	sealed class DbgRuntimeImpl : DbgRuntime {
		public override DbgProcess Process { get; }
		public override RuntimeId Id { get; }
		public override Guid Guid { get; }
		public override Guid RuntimeKindGuid { get; }
		public override string Name { get; }
		public override ReadOnlyCollection<string> Tags { get; }
		public override DbgInternalRuntime InternalRuntime { get; }

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgAppDomain>>? AppDomainsChanged;
		public override DbgAppDomain[] AppDomains {
			get {
				lock (lockObj)
					return appDomains.ToArray();
			}
		}
		readonly List<DbgAppDomain> appDomains;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgModule>>? ModulesChanged;
		public override DbgModule[] Modules {
			get {
				lock (lockObj)
					return modules.ToArray();
			}
		}
		readonly List<DbgModule> modules;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgThread>>? ThreadsChanged;
		public override DbgThread[] Threads {
			get {
				lock (lockObj)
					return threads.ToArray();
			}
		}
		readonly List<DbgThreadImpl> threads;

		public override ReadOnlyCollection<DbgBreakInfo> BreakInfos => breakInfos;
		volatile ReadOnlyCollection<DbgBreakInfo> breakInfos;
		static readonly ReadOnlyCollection<DbgBreakInfo> emptyBreakInfos = new ReadOnlyCollection<DbgBreakInfo>(Array.Empty<DbgBreakInfo>());

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;
		internal DbgEngine Engine { get; }

		internal CurrentObject<DbgThreadImpl> CurrentThread => currentThread;

		readonly object lockObj;
		readonly DbgManagerImpl owner;
		readonly List<DbgObject> closeOnContinueList;
		readonly List<DbgObject> closeOnExitList;
		readonly List<IDisposable> disposeOnExitList;
		CurrentObject<DbgThreadImpl> currentThread;

		public DbgRuntimeImpl(DbgManagerImpl owner, DbgProcess process, DbgEngine engine) {
			lockObj = new object();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));
			var info = engine.RuntimeInfo;
			Id = info.Id;
			Guid = info.Guid;
			RuntimeKindGuid = info.RuntimeKindGuid;
			Name = info.Name;
			Tags = info.Tags;
			appDomains = new List<DbgAppDomain>();
			modules = new List<DbgModule>();
			threads = new List<DbgThreadImpl>();
			closeOnContinueList = new List<DbgObject>();
			closeOnExitList = new List<DbgObject>();
			disposeOnExitList = new List<IDisposable>();
			breakInfos = emptyBreakInfos;
			InternalRuntime = engine.CreateInternalRuntime(this) ?? throw new InvalidOperationException();
		}

		internal void SetBreakInfos_DbgThread(DbgBreakInfo[] infos) {
			owner.Dispatcher.VerifyAccess();
			breakInfos = infos.Length == 0 ? emptyBreakInfos : new ReadOnlyCollection<DbgBreakInfo>(infos);
		}

		internal void SetCurrentThread_DbgThread(DbgThreadImpl thread) {
			owner.Dispatcher.VerifyAccess();
			currentThread = new CurrentObject<DbgThreadImpl>(thread, currentThread.Break);
		}

		internal DbgThread? SetBreakThread(DbgThreadImpl? thread, bool tryOldCurrentThread = false) {
			Dispatcher.VerifyAccess();
			DbgThreadImpl? newCurrent, newBreak;
			lock (lockObj) {
				newBreak = GetThread_NoLock(thread);
				if (tryOldCurrentThread && currentThread.Current?.IsClosed == false)
					newCurrent = currentThread.Current;
				else
					newCurrent = newBreak;
			}
			Debug2.Assert((!(newBreak is null)) == (!(newCurrent is null)));
			currentThread = new CurrentObject<DbgThreadImpl>(newCurrent, newBreak);
			return newCurrent;
		}

		DbgThreadImpl? GetThread_NoLock(DbgThreadImpl? thread) {
			if (thread?.IsClosed == false)
				return thread;
			return threads.FirstOrDefault(a => a.IsMain) ?? (threads.Count == 0 ? null : threads[0]);
		}

		internal void ClearBreakThread() {
			Dispatcher.VerifyAccess();
			currentThread = default;
		}

		internal void Add_DbgThread(DbgAppDomainImpl appDomain) {
			Dispatcher.VerifyAccess();
			lock (lockObj)
				appDomains.Add(appDomain);
			AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: true));
		}

		internal void Remove_DbgThread(DbgAppDomainImpl appDomain, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			List<DbgThread>? threadsToRemove = null;
			List<DbgModule>? modulesToRemove = null;
			lock (lockObj) {
				bool b = appDomains.Remove(appDomain);
				if (!b)
					return;
				for (int i = threads.Count - 1; i >= 0; i--) {
					var thread = threads[i];
					if (thread.AppDomain == appDomain) {
						if (threadsToRemove is null)
							threadsToRemove = new List<DbgThread>();
						threadsToRemove.Add(thread);
						threads.RemoveAt(i);
					}
				}
				for (int i = modules.Count - 1; i >= 0; i--) {
					var module = modules[i];
					if (module.AppDomain == appDomain) {
						if (modulesToRemove is null)
							modulesToRemove = new List<DbgModule>();
						modulesToRemove.Add(module);
						modules.RemoveAt(i);
					}
				}
			}
			if (!(threadsToRemove is null) && threadsToRemove.Count != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(threadsToRemove, added: false));
			if (!(modulesToRemove is null) && modulesToRemove.Count != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(modulesToRemove, added: false));
			owner.RemoveAppDomain_DbgThread(this, appDomain, messageFlags);
			AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: false));
			if (!(threadsToRemove is null)) {
				foreach (var thread in threadsToRemove)
					thread.Close(Dispatcher);
			}
			if (!(modulesToRemove is null)) {
				foreach (var module in modulesToRemove)
					module.Close(Dispatcher);
			}
			appDomain.Close(Dispatcher);
		}

		internal void Add_DbgThread(DbgModuleImpl module) {
			Dispatcher.VerifyAccess();
			lock (lockObj)
				modules.Add(module);
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: true));
		}

		internal void Remove_DbgThread(DbgModuleImpl module, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				bool b = modules.Remove(module);
				if (!b)
					return;
			}
			owner.RemoveModule_DbgThread(this, module, messageFlags);
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: false));
			module.Close(Dispatcher);
		}

		internal void Add_DbgThread(DbgThreadImpl thread) {
			Dispatcher.VerifyAccess();
			lock (lockObj)
				threads.Add(thread);
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(thread, added: true));
		}

		internal void Remove_DbgThread(DbgThreadImpl thread, DbgEngineMessageFlags messageFlags) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				bool b = threads.Remove(thread);
				if (!b)
					return;
			}
			owner.RemoveThread_DbgThread(this, thread, messageFlags);
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(thread, added: false));
			thread.Close(Dispatcher);
		}

		internal void Remove_DbgThread(DbgEngineBoundCodeBreakpointImpl[] breakpoints) {
			Dispatcher.VerifyAccess();
			owner.RemoveBoundCodeBreakpoints_DbgThread(this, breakpoints);
		}

		internal void Freeze(DbgThreadImpl thread) => Engine.Freeze(thread);
		internal void Thaw(DbgThreadImpl thread) => Engine.Thaw(thread);

		internal DbgStackWalker CreateStackWalker(DbgThreadImpl thread) {
			DbgEngineStackWalker engineStackWalker;
			if (Engine.IsClosed)
				engineStackWalker = new NullDbgEngineStackWalker();
			else
				engineStackWalker = Engine.CreateStackWalker(thread);
			var stackWalker = new DbgStackWalkerImpl(thread, engineStackWalker);
			CloseOnContinue(stackWalker);
			return stackWalker;
		}

		sealed class NullDbgEngineStackWalker : DbgEngineStackWalker {
			public override DbgEngineStackFrame[] GetNextStackFrames(int maxFrames) => Array.Empty<DbgEngineStackFrame>();
			protected override void CloseCore(DbgDispatcher dispatcher) { }
		}

		internal DbgStepper CreateStepper(DbgThreadImpl thread) => new DbgStepperImpl(owner, thread, Engine.CreateStepper(thread));
		internal void SetIP(DbgThreadImpl thread, DbgCodeLocation location) => Dispatcher.BeginInvoke(() => SetIP_DbgThread(thread, location));
		internal bool CanSetIP(DbgThreadImpl thread, DbgCodeLocation location) => Engine.CanSetIP(thread, location);

		void SetIP_DbgThread(DbgThreadImpl thread, DbgCodeLocation location) {
			Dispatcher.VerifyAccess();
			OnBeforeContinuing_DbgThread();
			Engine.SetIP(thread, location);
		}

		public override void CloseOnContinue(DbgObject obj) {
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			lock (lockObj) {
				if (IsClosed)
					Process.DbgManager.Close(obj);
				else
					closeOnContinueList.Add(obj);
			}
		}

		public override void CloseOnContinue(IEnumerable<DbgObject> objs) {
			if (objs is null)
				throw new ArgumentNullException(nameof(objs));
			lock (lockObj) {
				if (IsClosed)
					Process.DbgManager.Close(objs);
				else
					closeOnContinueList.AddRange(objs);
			}
		}

		public override void CloseOnExit(IEnumerable<DbgObject> objs) {
			if (objs is null)
				throw new ArgumentNullException(nameof(objs));
			lock (lockObj) {
				if (IsClosed)
					Process.DbgManager.Close(objs);
				else
					closeOnExitList.AddRange(objs);
			}
		}

		public override void CloseOnExit(IEnumerable<IDisposable> objs) {
			if (objs is null)
				throw new ArgumentNullException(nameof(objs));
			lock (lockObj) {
				if (!IsClosed) {
					disposeOnExitList.AddRange(objs);
					return;
				}
			}
			foreach (var obj in objs)
				obj.Dispose();
		}

		internal void OnBeforeContinuing_DbgThread() {
			Dispatcher.VerifyAccess();
			DbgObject[] objsToClose;
			lock (lockObj) {
				objsToClose = closeOnContinueList.Count == 0 ? Array.Empty<DbgObject>() : closeOnContinueList.ToArray();
				closeOnContinueList.Clear();
			}
			foreach (var obj in objsToClose)
				obj.Close(Dispatcher);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			DbgThread[] removedThreads;
			DbgModule[] removedModules;
			DbgAppDomain[] removedAppDomains;
			DbgObject[] objsToClose1;
			DbgObject[] objsToClose2;
			IDisposable[] objsToDispose;
			lock (lockObj) {
				removedThreads = threads.ToArray();
				removedModules = modules.ToArray();
				removedAppDomains = appDomains.ToArray();
				objsToClose1 = closeOnContinueList.ToArray();
				objsToClose2 = closeOnExitList.ToArray();
				objsToDispose = disposeOnExitList.ToArray();
				threads.Clear();
				modules.Clear();
				appDomains.Clear();
				closeOnContinueList.Clear();
				closeOnExitList.Clear();
				disposeOnExitList.Clear();
			}
			currentThread = default;
			if (removedThreads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(removedThreads, added: false));
			if (removedModules.Length != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(removedModules, added: false));
			if (removedAppDomains.Length != 0)
				AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(removedAppDomains, added: false));
			foreach (var obj in objsToDispose)
				obj.Dispose();
			foreach (var obj in objsToClose2)
				obj.Close(dispatcher);
			foreach (var obj in objsToClose1)
				obj.Close(dispatcher);
			foreach (var thread in removedThreads)
				thread.Close(dispatcher);
			foreach (var module in removedModules)
				module.Close(dispatcher);
			foreach (var appDomain in removedAppDomains)
				appDomain.Close(dispatcher);
			InternalRuntime.Close(dispatcher);
		}
	}
}
