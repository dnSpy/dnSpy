/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Images;
using dnSpy.Shared.MVVM;

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

	interface IThreadContext {
		IImageManager ImageManager { get; }
		ITheDebugger TheDebugger { get; }
		IDebuggerSettings DebuggerSettings { get; }
		bool SyntaxHighlight { get; }
		bool UseHexadecimal { get; }
	}

	sealed class ThreadContext : IThreadContext {
		public IImageManager ImageManager { get; }
		public ITheDebugger TheDebugger { get; }
		public IDebuggerSettings DebuggerSettings { get; }
		public bool SyntaxHighlight { get; set; }
		public bool UseHexadecimal { get; set; }

		public ThreadContext(IImageManager imageManager, ITheDebugger theDebugger, IDebuggerSettings debuggerSettings) {
			this.ImageManager = imageManager;
			this.TheDebugger = theDebugger;
			this.DebuggerSettings = debuggerSettings;
		}
	}

	sealed class ThreadVM : ViewModelBase {
		public bool IsCurrent {
			get { return isCurrent; }
			set {
				if (isCurrent != value) {
					isCurrent = value;
					OnPropertyChanged(nameof(IsCurrent));
					OnPropertyChanged(nameof(CurrentImageObject));
				}
			}
		}
		bool isCurrent;

		public ThreadType Type {
			get { return threadType; }
			set {
				if (threadType != value) {
					threadType = value;
					OnPropertyChanged(nameof(Type));
					OnPropertyChanged(nameof(CurrentImageObject));
					OnPropertyChanged(nameof(CategoryImageObject));
					OnPropertyChanged(nameof(CategoryTextObject));
				}
			}
		}
		ThreadType threadType;

		public bool IsSuspended {
			get { return isSuspended; }
			set {
				if (isSuspended != value) {
					Thread.CorThread.State = value ? CorDebugThreadState.THREAD_SUSPEND : CorDebugThreadState.THREAD_RUN;
					// Read the value again because there could've been an error
					isSuspended = Thread.CorThread.State == CorDebugThreadState.THREAD_SUSPEND;
					OnPropertyChanged(nameof(IsSuspended));
					OnPropertyChanged(nameof(SuspendedObject));
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
					OnPropertyChanged(nameof(AppDomain));
					OnPropertyChanged(nameof(AppDomainObject));
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
					OnPropertyChanged(nameof(UserState));
					OnPropertyChanged(nameof(UserStateObject));
				}
			}
		}
		CorDebugUserState userState;

		public object CurrentImageObject => this;
		public object IdObject => this;
		public object ManagedIdObject => this;
		public object CategoryImageObject => this;
		public object CategoryTextObject => this;
		public object NameObject => this;
		public object LocationObject => this;
		public object PriorityObject => this;
		public object AffinityMaskObject => this;
		public object SuspendedObject => this;
		public object ProcessObject => this;
		public object AppDomainObject => this;
		public object UserStateObject => this;

		public int Id {
			get { return id; }
			set {
				if (id != value) {
					id = value;
					OnPropertyChanged(nameof(Id));
					OnPropertyChanged(nameof(IdObject));
				}
			}
		}
		int id;

		bool HasValidThreadObject {
			get {
				var obj = Thread.CorThread.Object;
				return obj != null && !obj.IsNull && obj.NeuterCheckDereferencedValue != null;
			}
		}

		public int? ManagedId {
			get {
				if (reinitManagedId)
					reinitManagedId = !EvalUtils.ReflectionReadValue(Thread.CorThread.Object, "m_ManagedThreadId", ref managedId);

				if (!Context.TheDebugger.EvalCompleted && reinitManagedId) {
					if (Context.TheDebugger.CanEvaluate && Context.DebuggerSettings.PropertyEvalAndFunctionCalls) {
						if (HasValidThreadObject) {
							reinitManagedId = false;
							managedId = EvalUtils.EvaluateCallMethod<int?>(Context.TheDebugger, Context.DebuggerSettings, Thread, Thread.CorThread.Object, "get_ManagedThreadId");
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

		public bool UnknownName {
			get {
				var n = Name;	// Will init unknownName
				return unknownName;
			}
		}

		public string Name {
			get {
				if (reinitName) {
					reinitName = !EvalUtils.ReflectionReadValue(Thread.CorThread.Object, "m_Name", ref name);
					if (!reinitName)
						unknownName = false;
				}

				if (!Context.TheDebugger.EvalCompleted && reinitName) {
					if (Context.TheDebugger.CanEvaluate && Context.DebuggerSettings.PropertyEvalAndFunctionCalls) {
						if (HasValidThreadObject) {
							reinitName = false;
							unknownName = false;
							name = EvalUtils.EvaluateCallMethod<string>(Context.TheDebugger, Context.DebuggerSettings, Thread, Thread.CorThread.Object, "get_Name");
						}
					}
					else {
						unknownName = true;
						name = null;
						reinitName = false;
					}
				}
				return name;
			}
		}
		bool unknownName = true;
		string name;
		bool reinitName = true;

		public IntPtr AffinityMask {
			get {
				if (affinityMask == null) {
					affinityMask = NativeMethods.SetThreadAffinityMask(Thread.CorThread.Handle, new IntPtr(-1));
					if (affinityMask.Value != IntPtr.Zero)
						NativeMethods.SetThreadAffinityMask(Thread.CorThread.Handle, affinityMask.Value);
				}
				return affinityMask.Value;
			}
		}
		IntPtr? affinityMask;

		public ThreadPriority Priority {
			get {
				if (prio == null)
					prio = (ThreadPriority)NativeMethods.GetThreadPriority(Thread.CorThread.Handle);
				return prio.Value;
			}
		}
		ThreadPriority? prio;

		public DnThread Thread { get; }
		public IThreadContext Context { get; }

		public ThreadVM(DnThread thread, IThreadContext context) {
			this.Thread = thread;
			this.Context = context;
		}

		internal void NameChanged(DnThread thread) {
			if (thread == this.Thread)
				reinitName = true;
		}

		internal void UpdateFields() {
			Type = CalculateType();
			Id = Thread.VolatileThreadId;

			if (reinitManagedId) {
				OnPropertyChanged(nameof(ManagedId));
				OnPropertyChanged(nameof(ManagedIdObject));
			}

			if (reinitName) {
				OnPropertyChanged(nameof(Name));
				OnPropertyChanged(nameof(NameObject));
			}

			if (affinityMask != null) {
				affinityMask = null;
				OnPropertyChanged(nameof(AffinityMask));
				OnPropertyChanged(nameof(AffinityMaskObject));
			}

			if (prio != null) {
				prio = null;
				OnPropertyChanged(nameof(Priority));
				OnPropertyChanged(nameof(PriorityObject));
			}

			OnPropertyChanged(nameof(LocationObject));

			if (isSuspended != Thread.CorThread.IsSuspended) {
				isSuspended = Thread.CorThread.IsSuspended;
				OnPropertyChanged(nameof(IsSuspended));
				OnPropertyChanged(nameof(SuspendedObject));
			}

			AppDomain = Thread.CorThread.AppDomain;
			UserState = Thread.CorThread.UserState;
		}

		ThreadType CalculateType() {
			//TODO: ICLRTaskManager::GetCurrentTaskType()

			if (Thread.CorThread.IsStopped)
				return ThreadType.Terminated;
			if (Thread.CorThread.IsThreadPool)
				return ThreadType.ThreadPool;
			if (CheckIfMainThread())
				return ThreadType.Main;
			if (CheckIfBGCOrFinalizerThread())
				return ThreadType.BGCOrFinalizer;
			if (CheckIfWorkerThread())
				return ThreadType.Worker;

			return ThreadType.Unknown;
		}

		//TODO: This is not correct:
		bool CheckIfWorkerThread() => Thread.CorThread.IsBackground;

		bool CheckIfMainThread() {
			if (!Thread.Process.WasAttached)
				return Thread.UniqueIdProcess == 0;

			var ad = Thread.AppDomainOrNull;
			if (ad == null || ad.Id != 1)
				return false;
			if (Thread.CorThread.IsBackground)
				return false;
			return Thread.UniqueIdProcess == 0;
		}

		bool CheckIfBGCOrFinalizerThread() {
			if (Thread.CorThread.IsThreadPool)
				return false;
			if (!Thread.CorThread.IsBackground)
				return false;
			if (Priority != ThreadPriority.Highest)
				return false;
			return true;
		}

		internal void RefreshThemeFields() {
			OnPropertyChanged(nameof(CurrentImageObject));
			OnPropertyChanged(nameof(IdObject));
			OnPropertyChanged(nameof(ManagedIdObject));
			OnPropertyChanged(nameof(CategoryImageObject));
			OnPropertyChanged(nameof(CategoryTextObject));
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(LocationObject));
			OnPropertyChanged(nameof(PriorityObject));
			OnPropertyChanged(nameof(AffinityMaskObject));
			OnPropertyChanged(nameof(SuspendedObject));
			OnPropertyChanged(nameof(ProcessObject));
			OnPropertyChanged(nameof(AppDomainObject));
		}

		internal void RefreshHexFields() {
			OnPropertyChanged(nameof(IdObject));
			OnPropertyChanged(nameof(ManagedIdObject));
			OnPropertyChanged(nameof(LocationObject));
			OnPropertyChanged(nameof(ProcessObject));
		}

		internal void RefreshEvalFields() {
			if (unknownName) {
				name = null;
				reinitName = true;
				OnPropertyChanged(nameof(NameObject));
			}
			if (managedId == null) {
				reinitManagedId = true;
				OnPropertyChanged(nameof(ManagedIdObject));
			}
		}
	}
}
