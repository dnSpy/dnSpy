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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.ToolWindows.Threads {
	enum ThreadPriority {
		Lowest				= -2,
		BelowNormal			= -1,
		Normal				= 0,
		AboveNormal			= 1,
		Highest				= 2,
	}

	sealed class ThreadVM : ViewModelBase {
		internal bool IsCurrentThread {
			get => isCurrentThread;
			set {
				Context.UIDispatcher.VerifyAccess();
				if (isCurrentThread != value) {
					isCurrentThread = value;
					OnPropertyChanged(nameof(CurrentImageReference));
				}
			}
		}
		bool isCurrentThread;

		internal bool IsBreakThread {
			get => isBreakThread;
			set {
				Context.UIDispatcher.VerifyAccess();
				if (isBreakThread != value) {
					isBreakThread = value;
					OnPropertyChanged(nameof(CurrentImageReference));
				}
			}
		}
		bool isBreakThread;

		public ImageReference CurrentImageReference {
			get {
				if (IsCurrentThread)
					return DsImages.CurrentInstructionPointer;
				if (IsBreakThread)
					return DsImages.DraggedCurrentInstructionPointer;
				return ImageReference.None;
			}
		}

		public IThreadContext Context { get; }
		public DbgThread Thread { get; }
		public object IdObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowId);
		public object ManagedIdObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowManagedId);
		public object CategoryTextObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowCategoryText);
		public object NameObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowName);
		public object LocationObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowLocation);
		public object PriorityObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowPriority);
		public object AffinityMaskObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowAffinityMask);
		public object SuspendedCountObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowSuspended);
		public object ProcessNameObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowProcess);
		public object AppDomainObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowAppDomain);
		public object StateObject => new FormatterObject<ThreadVM>(this, PredefinedTextClassifierTags.ThreadsWindowUserState);
		internal int Order { get; }

		public ImageReference CategoryImageReference {
			get {
				if (initializeThreadCategory)
					InitializeThreadCategory_UI();
				return categoryImageReference;
			}
		}
		ImageReference categoryImageReference;

		public string CategoryText {
			get {
				if (initializeThreadCategory)
					InitializeThreadCategory_UI();
				return categoryText;
			}
		}
		string categoryText;

		public ThreadPriority Priority {
			get {
				if (hThread is null)
					OpenThread_UI();
				if (priority is null)
					priority = CalculateThreadPriority_UI();
				return priority.Value;
			}
		}
		ThreadPriority? priority;

		public ulong AffinityMask {
			get {
				if (hThread is null)
					OpenThread_UI();
				if (affinityMask is null)
					affinityMask = CalculateAffinityMask_UI();
				return affinityMask.Value;
			}
		}
		ulong? affinityMask;

		public ClassifiedTextCollection LocationCachedOutput {
			get {
				if (locationCachedOutput.IsDefault)
					locationCachedOutput = CreateLocationCachedOutput();
				return locationCachedOutput;
			}
		}
		ClassifiedTextCollection locationCachedOutput;

		public IEditableValue NameEditableValue { get; }
		public IEditValueProvider NameEditValueProvider { get; }

		readonly DbgLanguageService dbgLanguageService;
		readonly ThreadCategoryService threadCategoryService;
		bool initializeThreadCategory;
		SafeAccessTokenHandle? hThread;

		public ThreadVM(DbgLanguageService dbgLanguageService, DbgThread thread, IThreadContext context, int order, ThreadCategoryService threadCategoryService, IEditValueProvider nameEditValueProvider) {
			categoryText = null!;
			this.dbgLanguageService = dbgLanguageService ?? throw new ArgumentNullException(nameof(dbgLanguageService));
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			NameEditValueProvider = nameEditValueProvider ?? throw new ArgumentNullException(nameof(nameEditValueProvider));
			NameEditableValue = new EditableValueImpl(() => Thread.HasName() ? Thread.UIName : string.Empty, s => Thread.UIName = s ?? string.Empty);
			this.threadCategoryService = threadCategoryService ?? throw new ArgumentNullException(nameof(threadCategoryService));
			initializeThreadCategory = true;
			thread.PropertyChanged += DbgThread_PropertyChanged;
		}

		// UI thread
		ThreadPriority CalculateThreadPriority_UI() {
			Context.UIDispatcher.VerifyAccess();
			Debug2.Assert(hThread is not null);
			if (hThread is null || hThread.IsInvalid)
				return ThreadPriority.Normal;
			return (ThreadPriority)NativeMethods.GetThreadPriority(hThread.DangerousGetHandle());
		}

		// UI thread
		ulong CalculateAffinityMask_UI() {
			Context.UIDispatcher.VerifyAccess();
			Debug2.Assert(hThread is not null);
			if (hThread is null || hThread.IsInvalid)
				return 0;
			var affinityMask = NativeMethods.SetThreadAffinityMask(hThread.DangerousGetHandle(), new IntPtr(-1));
			if (affinityMask != IntPtr.Zero)
				NativeMethods.SetThreadAffinityMask(hThread.DangerousGetHandle(), affinityMask);
			if (IntPtr.Size == 4)
				return (uint)affinityMask.ToInt32();
			return (ulong)affinityMask.ToInt64();
		}

		// UI thread
		void InitializeThreadCategory_UI() {
			Context.UIDispatcher.VerifyAccess();
			initializeThreadCategory = false;
			var info = threadCategoryService.GetInfo(Thread.Kind);
			categoryImageReference = info.Image;
			categoryText = info.Category;
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(IdObject));
			OnPropertyChanged(nameof(ManagedIdObject));
			OnPropertyChanged(nameof(CategoryTextObject));
			OnPropertyChanged(nameof(NameObject));
			locationCachedOutput = default;
			OnPropertyChanged(nameof(LocationObject));
			OnPropertyChanged(nameof(PriorityObject));
			OnPropertyChanged(nameof(AffinityMaskObject));
			OnPropertyChanged(nameof(SuspendedCountObject));
			OnPropertyChanged(nameof(ProcessNameObject));
			OnPropertyChanged(nameof(AppDomainObject));
			OnPropertyChanged(nameof(StateObject));
		}

		// UI thread
		internal void RefreshHexFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(IdObject));
			OnPropertyChanged(nameof(ManagedIdObject));
			locationCachedOutput = default;
			OnPropertyChanged(nameof(LocationObject));
			OnPropertyChanged(nameof(SuspendedCountObject));
		}

		// UI thread
		internal void RefreshLanguageFields_UI() {
			locationCachedOutput = default;
			OnPropertyChanged(nameof(LocationObject));
		}

		// UI thread
		internal void RefreshAppDomainNames_UI(DbgAppDomain appDomain) {
			Context.UIDispatcher.VerifyAccess();
			if (Thread.AppDomain == appDomain)
				OnPropertyChanged(nameof(AppDomainObject));
		}

		// UI thread
		internal void UpdateFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			if (hThread is null)
				OpenThread_UI();

			locationCachedOutput = default;
			OnPropertyChanged(nameof(LocationObject));

			var newPriority = CalculateThreadPriority_UI();
			if (newPriority != priority) {
				priority = newPriority;
				OnPropertyChanged(nameof(Priority));
			}

			var newAffinityMask = CalculateAffinityMask_UI();
			if (newAffinityMask != affinityMask) {
				affinityMask = newAffinityMask;
				OnPropertyChanged(nameof(AffinityMask));
			}
		}

		// DbgManager thread
		void DbgThread_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			Context.UIDispatcher.UI(() => DbgThread_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DbgThread_PropertyChanged_UI(string? propertyName) {
			Context.UIDispatcher.VerifyAccess();
			if (disposed)
				return;
			switch (propertyName) {
			case nameof(DbgThread.AppDomain):
				OnPropertyChanged(nameof(AppDomainObject));
				break;

			case nameof(DbgThread.Kind):
				initializeThreadCategory = true;
				OnPropertyChanged(nameof(CategoryImageReference));
				OnPropertyChanged(nameof(CategoryTextObject));
				break;

			case nameof(DbgThread.Id):
				CloseThreadHandle_UI();
				OnPropertyChanged(nameof(IdObject));
				OnPropertyChanged(nameof(PriorityObject));
				OnPropertyChanged(nameof(AffinityMaskObject));
				break;

			case nameof(DbgThread.ManagedId):
				OnPropertyChanged(nameof(ManagedIdObject));
				break;

			case nameof(DbgThread.Name):
				break;

			case nameof(DbgThread.UIName):
				OnPropertyChanged(nameof(NameObject));
				break;

			case nameof(DbgThread.SuspendedCount):
				OnPropertyChanged(nameof(SuspendedCountObject));
				break;

			case nameof(DbgThread.State):
				OnPropertyChanged(nameof(StateObject));
				break;

			default:
				Debug.Fail($"Unknown thread property: {propertyName}");
				break;
			}
		}

		// UI thread
		void CloseThreadHandle_UI() {
			Context.UIDispatcher.VerifyAccess();
			hThread?.Close();
			hThread = null;
			priority = null;
			affinityMask = null;
		}

		// UI thread
		void OpenThread_UI() {
			Context.UIDispatcher.VerifyAccess();
			if (hThread is not null)
				return;
			const int dwDesiredAccess = NativeMethods.THREAD_QUERY_INFORMATION | NativeMethods.THREAD_SET_INFORMATION;
			hThread = NativeMethods.OpenThread(dwDesiredAccess, false, (uint)Thread.Id);
		}

		// UI thread
		internal void ClearEditingValueProperties() {
			Context.UIDispatcher.VerifyAccess();
			NameEditableValue.IsEditingValue = false;
		}

		// UI thread
		ClassifiedTextCollection CreateLocationCachedOutput() {
			Context.UIDispatcher.VerifyAccess();
			DbgStackWalker? stackWalker = null;
			DbgStackFrame[]? frames = null;
			DbgEvaluationContext? context = null;
			try {
				stackWalker = Thread.CreateStackWalker();
				frames = stackWalker.GetNextStackFrames(1);
				if (frames.Length == 0)
					return new ClassifiedTextCollection(new[] { new ClassifiedText(DbgTextColor.Text, dnSpy_Debugger_Resources.Thread_LocationNotAvailable) });
				else {
					Debug.Assert(frames.Length == 1);
					var frame = frames[0];
					var language = dbgLanguageService.GetCurrentLanguage(Thread.Runtime.RuntimeKindGuid);
					const DbgEvaluationContextOptions ctxOptions = DbgEvaluationContextOptions.NoMethodBody;
					CancellationToken cancellationToken = default;
					const CultureInfo? cultureInfo = null;
					context = language.CreateContext(frame, ctxOptions);
					var evalInfo = new DbgEvaluationInfo(context, frame, cancellationToken);
					language.Formatter.FormatFrame(evalInfo, Context.ClassifiedTextWriter, GetStackFrameFormatterOptions(), DbgValueFormatterOptions.None, cultureInfo);
					return Context.ClassifiedTextWriter.GetClassifiedText();
				}
			}
			finally {
				if (frames is not null && frames.Length != 0) {
					Debug.Assert(frames.Length == 1);
					Thread.Process.DbgManager.Close(new DbgObject[] { stackWalker!, frames[0] });
				}
				else
					stackWalker?.Close();
				context?.Close();
			}
		}

		// random thread
		DbgStackFrameFormatterOptions GetStackFrameFormatterOptions() {
			var options =
				DbgStackFrameFormatterOptions.ModuleNames |
				DbgStackFrameFormatterOptions.ParameterTypes |
				DbgStackFrameFormatterOptions.ParameterNames |
				DbgStackFrameFormatterOptions.DeclaringTypes |
				DbgStackFrameFormatterOptions.Namespaces |
				DbgStackFrameFormatterOptions.IntrinsicTypeKeywords |
				DbgStackFrameFormatterOptions.IP;
			if (!Context.UseHexadecimal)
				options |= DbgStackFrameFormatterOptions.Decimal;
			if (Context.DigitSeparators)
				options |= DbgStackFrameFormatterOptions.DigitSeparators;
			if (Context.FullString)
				options |= DbgStackFrameFormatterOptions.FullString;
			return options;
		}

		// UI thread
		internal void Dispose() {
			Context.UIDispatcher.VerifyAccess();
			if (disposed)
				return;
			disposed = true;
			Thread.PropertyChanged -= DbgThread_PropertyChanged;
			CloseThreadHandle_UI();
			ClearEditingValueProperties();
		}
		bool disposed;

		// UI thread
		internal bool IsEditingValues {
			get {
				Context.UIDispatcher.VerifyAccess();
				return NameEditableValue.IsEditingValue;
			}
		}
	}
}
