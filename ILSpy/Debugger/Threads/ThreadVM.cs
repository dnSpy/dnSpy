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

using System;
using System.Runtime.InteropServices;
using dndbg.Engine;
using dndbg.Engine.COM.CorDebug;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Threads {
	enum ThreadType {
		Unknown,
		Main,
		ThreadPool,
		Worker,
		BGCOrFinalizer,
		Terminated,
	}

	enum ThreadPriority {
		Lowest				= -2,
		BelowNormal			= -1,
		Normal				= 0,
		AboveNormal			= 1,
		Highest				= 2,
	}

	sealed class ThreadVM : ViewModelBase {
		[DllImport("kernel32", SetLastError = true)]
		static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		[DllImport("kernel32", SetLastError = true)]
		static extern int GetThreadPriority(IntPtr hThread);

		public bool IsCurrent {
			get { return isCurrent; }
			set {
				if (isCurrent != value) {
					isCurrent = value;
					OnPropertyChanged("IsCurrent");
					OnPropertyChanged("CurrentImageObject");
				}
			}
		}
		bool isCurrent;

		public ThreadType Type {
			get { return threadType; }
			set {
				if (threadType != value) {
					threadType = value;
					OnPropertyChanged("Type");
					OnPropertyChanged("CurrentImageObject");
					OnPropertyChanged("CategoryImageObject");
					OnPropertyChanged("CategoryTextObject");
				}
			}
		}
		ThreadType threadType;

		public bool IsSuspended {
			get { return isSuspended; }
			set {
				if (isSuspended != value) {
					thread.CorThread.State = value ? CorDebugThreadState.THREAD_SUSPEND : CorDebugThreadState.THREAD_RUN;
					isSuspended = value;
					OnPropertyChanged("IsSuspended");
					OnPropertyChanged("SuspendedObject");
				}
			}
		}
		bool isSuspended;

		public CorAppDomain AppDomain {
			get { return appDomain; }
			set {
				if (appDomain != value || (value != null && value.Name != appDomainName)) {
					appDomain = value;
					appDomainName = value == null ? null : value.Name;
					OnPropertyChanged("AppDomain");
					OnPropertyChanged("AppDomainObject");
				}
			}
		}
		CorAppDomain appDomain;
		string appDomainName;

		public CorDebugUserState UserState {
			get { return userState; }
			set {
				if (userState != value) {
					userState = value;
					OnPropertyChanged("UserState");
					OnPropertyChanged("UserStateObject");
				}
			}
		}
		CorDebugUserState userState;

		public object CurrentImageObject { get { return this; } }
		public object IdObject { get { return this; } }
		public object ManagedIdObject { get { return this; } }
		public object CategoryImageObject { get { return this; } }
		public object CategoryTextObject { get { return this; } }
		public object NameObject { get { return this; } }
		public object LocationObject { get { return this; } }
		public object PriorityObject { get { return this; } }
		public object AffinityMaskObject { get { return this; } }
		public object SuspendedObject { get { return this; } }
		public object ProcessObject { get { return this; } }
		public object AppDomainObject { get { return this; } }
		public object UserStateObject { get { return this; } }

		public int Id {
			get { return id; }
			set {
				if (id != value) {
					id = value;
					OnPropertyChanged("Id");
					OnPropertyChanged("IdObject");
				}
			}
		}
		int id;

		bool HasValidThreadObject {
			get {
				var obj = thread.CorThread.Object;
				return obj != null && !obj.IsNull && obj.DereferencedValue != null;
			}
		}

		public int? ManagedId {
			get {
				if (reinitManagedId)
					reinitManagedId = !EvalUtils.ReflectionReadValue(thread.CorThread.Object, "m_ManagedThreadId", ref managedId);

				if (!DebugManager.Instance.EvalCompleted && reinitManagedId) {
					if (DebugManager.Instance.CanEvaluate) {
						if (HasValidThreadObject) {
							reinitManagedId = false;
							managedId = EvalUtils.EvaluateCallMethod<int?>(thread, thread.CorThread.Object, "get_ManagedThreadId");
						}
					}
					else {
						managedId = null;
						reinitManagedId = false;
					}
				}
				return managedId;
			}
		}
		int? managedId = null;
		bool reinitManagedId = true;

		public string Name {
			get {
				if (reinitName)
					reinitName = !EvalUtils.ReflectionReadValue(thread.CorThread.Object, "m_Name", ref name);

				if (!DebugManager.Instance.EvalCompleted && reinitName) {
					if (DebugManager.Instance.CanEvaluate) {
						if (HasValidThreadObject) {
							reinitName = false;
							name = EvalUtils.EvaluateCallMethod<string>(thread, thread.CorThread.Object, "get_Name");
						}
					}
					else {
						name = "???";
						reinitName = false;
					}
				}
				return name;
			}
		}
		string name;
		bool reinitName = true;

		public IntPtr AffinityMask {
			get {
				if (affinityMask == null) {
					affinityMask = SetThreadAffinityMask(thread.CorThread.Handle, new IntPtr(-1));
					if (affinityMask.Value != IntPtr.Zero)
						SetThreadAffinityMask(thread.CorThread.Handle, affinityMask.Value);
				}
				return affinityMask.Value;
			}
		}
		IntPtr? affinityMask;

		public ThreadPriority Priority {
			get {
				if (prio == null)
					prio = (ThreadPriority)GetThreadPriority(thread.CorThread.Handle);
				return prio.Value;
			}
		}
		ThreadPriority? prio;

		public DnThread Thread {
			get { return thread; }
		}
		readonly DnThread thread;

		public ThreadVM(DnThread thread) {
			this.thread = thread;
		}

		internal void NameChanged(DnThread thread) {
			if (thread == this.thread)
				reinitName = true;
		}

		internal void UpdateFields() {
			Type = CalculateType();
			Id = thread.VolatileThreadId;

			if (reinitManagedId) {
				OnPropertyChanged("ManagedId");
				OnPropertyChanged("ManagedIdObject");
			}

			if (reinitName) {
				OnPropertyChanged("Name");
				OnPropertyChanged("NameObject");
			}

			if (affinityMask != null) {
				affinityMask = null;
				OnPropertyChanged("AffinityMask");
				OnPropertyChanged("AffinityMaskObject");
			}

			if (prio != null) {
				prio = null;
				OnPropertyChanged("Priority");
				OnPropertyChanged("PriorityObject");
			}

			OnPropertyChanged("LocationObject");

			if (isSuspended != thread.CorThread.IsSuspended) {
				isSuspended = thread.CorThread.IsSuspended;
				OnPropertyChanged("IsSuspended");
				OnPropertyChanged("SuspendedObject");
			}

			AppDomain = thread.CorThread.AppDomain;
			UserState = thread.CorThread.UserState;
		}

		ThreadType CalculateType() {
			//TODO: ICLRTaskManager::GetCurrentTaskType()

			if (thread.CorThread.IsStopped)
				return ThreadType.Terminated;
			if (thread.CorThread.IsThreadPool)
				return ThreadType.ThreadPool;
			if (CheckIfMainThread())
				return ThreadType.Main;
			if (CheckIfBGCOrFinalizerThread())
				return ThreadType.BGCOrFinalizer;
			if (CheckIfWorkerThread())
				return ThreadType.Worker;

			return ThreadType.Unknown;
		}

		bool CheckIfWorkerThread() {
			//TODO: This is not correct:
			return thread.CorThread.IsBackground;
		}

		bool CheckIfMainThread() {
			if (!thread.Process.WasAttached)
				return thread.IncrementedId == 0;

			var ad = thread.AppDomainOrNull;
			if (ad == null || ad.Id != 1)
				return false;
			if (thread.CorThread.IsBackground)
				return false;
			return thread.IncrementedId == 0;
		}

		bool CheckIfBGCOrFinalizerThread() {
			if (thread.CorThread.IsThreadPool)
				return false;
			if (!thread.CorThread.IsBackground)
				return false;
			if (Priority != ThreadPriority.Highest)
				return false;
			return true;
		}

		internal void RefreshThemeFields() {
			OnPropertyChanged("CurrentImageObject");
			OnPropertyChanged("IdObject");
			OnPropertyChanged("ManagedIdObject");
			OnPropertyChanged("CategoryImageObject");
			OnPropertyChanged("CategoryTextObject");
			OnPropertyChanged("NameObject");
			OnPropertyChanged("LocationObject");
			OnPropertyChanged("PriorityObject");
			OnPropertyChanged("AffinityMaskObject");
			OnPropertyChanged("SuspendedObject");
			OnPropertyChanged("ProcessObject");
			OnPropertyChanged("AppDomainObject");
		}

		internal void RefreshHexFields() {
			OnPropertyChanged("IdObject");
			OnPropertyChanged("ManagedIdObject");
			OnPropertyChanged("LocationObject");
			OnPropertyChanged("ProcessObject");
		}
	}
}
