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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Steppers;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.Properties;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.Impl {
	sealed class DbgThreadImpl : DbgThread {
		public override DbgRuntime Runtime => runtime;
		public override DbgAppDomain? AppDomain => appDomain;
		public override string Kind => kind;
		public override string Name => name;
		public override int SuspendedCount => suspendedCount;

		public override string UIName {
			get {
				lock (lockObj)
					return GetUINameNoDefaultName_NoLock() ?? dnSpy_Debugger_Resources.Thread_NoName;
			}
			set => Dispatcher.BeginInvoke(() => WriteUIName_DbgThread(value));
		}
		string? userName;

		public override ReadOnlyCollection<DbgStateInfo> State {
			get {
				lock (lockObj)
					return state;
			}
		}

		public override ulong Id {
			get {
				lock (lockObj)
					return id;
			}
		}

		public override ulong? ManagedId {
			get {
				lock (lockObj)
					return managedId;
			}
		}

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;
		internal DbgRuntimeImpl RuntimeImpl => runtime;

		readonly object lockObj;
		readonly DbgRuntimeImpl runtime;
		readonly SafeAccessTokenHandle hThread;
		readonly HashSet<DbgObject> autoCloseObjects;
		DbgAppDomainImpl? appDomain;
		string kind;
		ulong id;
		ulong? managedId;
		string name;
		int suspendedCount;
		ReadOnlyCollection<DbgStateInfo> state;
		static readonly ReadOnlyCollection<DbgStateInfo> emptyState = new ReadOnlyCollection<DbgStateInfo>(Array.Empty<DbgStateInfo>());

		public DbgThreadImpl(DbgRuntimeImpl runtime, DbgAppDomainImpl? appDomain, string kind, ulong id, ulong? managedId, string? name, int suspendedCount, ReadOnlyCollection<DbgStateInfo> state) {
			lockObj = new object();
			autoCloseObjects = new HashSet<DbgObject>();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.appDomain = appDomain;
			this.kind = kind;
			this.id = id;
			this.managedId = managedId;
			this.name = name ?? string.Empty;
			this.suspendedCount = suspendedCount;
			this.state = state ?? emptyState;
			const int dwDesiredAccess = NativeMethods.THREAD_QUERY_INFORMATION;
			hThread = NativeMethods.OpenThread(dwDesiredAccess, false, (uint)id);
		}

		public override event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged(string propName) {
			Dispatcher.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateAppDomain_DbgThread(DbgAppDomainImpl? appDomain) {
			Dispatcher.VerifyAccess();
			if (this.appDomain != appDomain) {
				// Caller has verified that it's a valid app domain
				this.appDomain = appDomain;
				OnPropertyChanged(nameof(AppDomain));
			}
		}

		internal void UpdateKind_DbgThread(string? kind) {
			Dispatcher.VerifyAccess();
			bool raiseEvent, raiseUINameEvent;
			lock (lockObj) {
				var oldUIName = GetUINameNoDefaultName_NoLock();
				raiseEvent = this.kind != kind && kind is not null;
				if (raiseEvent)
					this.kind = kind!;
				raiseUINameEvent = oldUIName != GetUINameNoDefaultName_NoLock();
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Kind));
			if (raiseUINameEvent)
				OnPropertyChanged(nameof(UIName));
		}

		internal void UpdateId_DbgThread(ulong id) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.id != id;
				this.id = id;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Id));
		}

		internal void UpdateManagedId_DbgThread(ulong? managedId) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.managedId != managedId;
				this.managedId = managedId;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(ManagedId));
		}

		internal void UpdateName_DbgThread(string? name) {
			Dispatcher.VerifyAccess();
			bool raiseEvent, raiseUINameEvent;
			lock (lockObj) {
				var oldUIName = GetUINameNoDefaultName_NoLock();
				raiseEvent = this.name != name && name is not null;
				if (raiseEvent)
					this.name = name!;
				raiseUINameEvent = oldUIName != GetUINameNoDefaultName_NoLock();
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Name));
			if (raiseUINameEvent)
				OnPropertyChanged(nameof(UIName));
		}

		internal void UpdateSuspendedCount_DbgThread(int suspendedCount) {
			Dispatcher.VerifyAccess();
			if (this.suspendedCount != suspendedCount) {
				this.suspendedCount = suspendedCount;
				OnPropertyChanged(nameof(SuspendedCount));
			}
		}

		internal void UpdateState_DbgThread(ReadOnlyCollection<DbgStateInfo>? state) {
			Dispatcher.VerifyAccess();
			if (state is null)
				state = emptyState;
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = !EqualsState(this.state, state);
				this.state = state;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(State));
		}

		static bool EqualsState(ReadOnlyCollection<DbgStateInfo> a, ReadOnlyCollection<DbgStateInfo> b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		internal int GetExitCode() {
			if (NativeMethods.GetExitCodeThread(hThread.DangerousGetHandle(), out int threadExitCode))
				return threadExitCode;
			return -1;
		}

		internal void Remove(DbgEngineMessageFlags messageFlags) => Dispatcher.BeginInvoke(() => runtime.Remove_DbgThread(this, messageFlags));

		public override void Freeze() => runtime.Freeze(this);
		public override void Thaw() => runtime.Thaw(this);

		public override bool HasName() {
			lock (lockObj)
				return GetUINameNoDefaultName_NoLock() is not null;
		}

		void WriteUIName_DbgThread(string newUserName) {
			Dispatcher.VerifyAccess();
			lock (lockObj) {
				if (newUserName == userName)
					return;
				userName = newUserName;
			}
			OnPropertyChanged(nameof(UIName));
		}

		string? GetUINameNoDefaultName_NoLock() {
			if (userName is not null)
				return userName;

			var threadName = name;
			if (threadName is not null)
				return threadName;

			if (kind == PredefinedThreadKinds.Main)
				return dnSpy_Debugger_Resources.ThreadType_Main;

			return null;
		}

		public override DbgStackWalker CreateStackWalker() => runtime.CreateStackWalker(this);
		public override DbgStepper CreateStepper() => runtime.CreateStepper(this);
		public override void SetIP(DbgCodeLocation location) => runtime.SetIP(this, location ?? throw new ArgumentNullException(nameof(location)));
		public override bool CanSetIP(DbgCodeLocation location) => runtime.CanSetIP(this, location ?? throw new ArgumentNullException(nameof(location)));

		internal void AddAutoClose(DbgObject obj) {
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			lock (lockObj) {
				if (IsClosed)
					return;
				autoCloseObjects.Add(obj);
			}
		}

		internal void RemoveAutoClose(DbgObject obj) {
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			lock (lockObj)
				autoCloseObjects.Remove(obj);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			hThread.Dispose();
			DbgObject[] objsToClose;
			lock (lockObj) {
				objsToClose = autoCloseObjects.Count == 0 ? Array.Empty<DbgObject>() : autoCloseObjects.ToArray();
				autoCloseObjects.Clear();
			}
			foreach (var obj in objsToClose)
				obj.Close(dispatcher);
		}
	}
}
