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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Hex;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.UI;
using Microsoft.Win32.SafeHandles;

namespace dnSpy.Debugger.ToolWindows.Memory {
	interface IHexBufferInfo {
		/// <summary>
		/// Gets the buffer
		/// </summary>
		HexBuffer Buffer { get; }

		/// <summary>
		/// Raised when the underlying stream has changed
		/// </summary>
		event EventHandler UnderlyingStreamChanged;
	}

	/// <summary>
	/// Creates <see cref="HexBuffer"/>s used by memory windows' <see cref="Contracts.Hex.Editor.HexView"/>s.
	/// It allows viewing any debugged process' memory. If a process is closed, the <see cref="HexBuffer"/>
	/// automatically gets a new process stream or a null stream if there are no debugged processes.
	/// </summary>
	abstract class ProcessHexBufferProvider {
		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>. This <see cref="HexBuffer"/> should only be used
		/// by one <see cref="Contracts.Hex.Editor.HexView"/> and should not be shared or both
		/// <see cref="Contracts.Hex.Editor.HexView"/>s will always show the identical process' memory.
		/// </summary>
		/// <returns></returns>
		public abstract IHexBufferInfo CreateBuffer();

		/// <summary>
		/// Updates <paramref name="buffer"/> so it uses another process stream
		/// </summary>
		/// <param name="buffer">Buffer, created by <see cref="CreateBuffer"/></param>
		/// <param name="pid">Process id of process to use</param>
		public abstract void SetProcessStream(HexBuffer buffer, int pid);

		/// <summary>
		/// Gets the process id that is used by the underlying buffer stream. This can be null
		/// if there are no debugged processes.
		/// </summary>
		/// <param name="buffer">Buffer, created by <see cref="CreateBuffer"/></param>
		/// <returns></returns>
		public abstract int? GetProcessId(HexBuffer buffer);

		/// <summary>
		/// Gets all process ids that can be passed to <see cref="SetProcessStream(HexBuffer, int)"/>
		/// </summary>
		public abstract int[] ProcessIds { get; }

		/// <summary>
		/// Invalidates all memory which causes all <see cref="Contracts.Hex.Editor.HexView"/>s to
		/// use the current memory content instead of cached values.
		/// </summary>
		public abstract void InvalidateMemory();

		/// <summary>
		/// Invalidates all memory which causes all <see cref="Contracts.Hex.Editor.HexView"/>s to
		/// use the current memory content instead of cached values.
		/// </summary>
		/// <param name="pid">Process id</param>
		public abstract void InvalidateMemory(int pid);
	}

	[Export(typeof(ProcessHexBufferProvider))]
	sealed class ProcessHexBufferProviderImpl : ProcessHexBufferProvider {
		readonly DbgManager dbgManager;
		readonly DebuggerDispatcher debuggerDispatcher;
		readonly HexBufferFactoryService hexBufferFactoryService;
		readonly HexBufferStreamFactoryService hexBufferStreamFactoryService;
		readonly List<ProcessInfo> processInfos;
		readonly List<BufferState> bufferStates;

		sealed class ProcessInfo {
			public DbgProcess Process { get; }
			public HexCachedBufferStream Stream { get; }
			public SafeFileHandle ProcessHandle { get; }
			public ProcessInfo(DbgProcess process, HexCachedBufferStream stream, SafeFileHandle processHandle) {
				Process = process ?? throw new ArgumentNullException(nameof(process));
				Stream = stream ?? throw new ArgumentNullException(nameof(stream));
				ProcessHandle = processHandle;
			}

			public void Dispose() {
				Stream.Dispose();
				ProcessHandle.Close();
			}
		}

		sealed class BufferState : IHexBufferInfo {
			public HexBuffer Buffer { get; }
			DebuggerHexBufferStream DebuggerHexBufferStream { get; }
			public DbgProcess Process { get; private set; }
			public event EventHandler UnderlyingStreamChanged;

			public BufferState(HexBuffer buffer, DebuggerHexBufferStream debuggerHexBufferStream) {
				Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
				DebuggerHexBufferStream = debuggerHexBufferStream ?? throw new ArgumentNullException(nameof(debuggerHexBufferStream));
			}

			public void SetUnderlyingStream(HexBufferStream stream, DbgProcess process) {
				Process = process;
				DebuggerHexBufferStream.UnderlyingStream = stream;
				UnderlyingStreamChanged?.Invoke(this, EventArgs.Empty);
			}

			public void InvalidateSpan(NormalizedHexChangeCollection changes) => DebuggerHexBufferStream.Invalidate(changes);
		}

		[ImportingConstructor]
		ProcessHexBufferProviderImpl(DbgManager dbgManager, DebuggerDispatcher debuggerDispatcher, HexBufferFactoryService hexBufferFactoryService, HexBufferStreamFactoryService hexBufferStreamFactoryService) {
			this.dbgManager = dbgManager;
			this.debuggerDispatcher = debuggerDispatcher;
			this.hexBufferFactoryService = hexBufferFactoryService;
			this.hexBufferStreamFactoryService = hexBufferStreamFactoryService;
			processInfos = new List<ProcessInfo>();
			bufferStates = new List<BufferState>();
			dbgManager.DispatcherThread.Invoke(() => {
				dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;
				InitializeProcesses_DbgManager(dbgManager.Processes, added: true);
			});
		}

		// random thread
		void UI(Action action) =>
			debuggerDispatcher.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) =>
			InitializeProcesses_DbgManager(e.Objects, e.Added);

		// DbgManager thread
		void InitializeProcesses_DbgManager(IList<DbgProcess> processes, bool added) => UI(() => InitializeProcesses_DbgManager_UI(processes, added));

		// UI thread
		void InitializeProcesses_DbgManager_UI(IList<DbgProcess> processes, bool added) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			if (added) {
				foreach (var p in processes) {
					const int dwDesiredAccess = NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE;
					var processHandle = NativeMethods.OpenProcess(dwDesiredAccess, false, p.Id);
					Debug.Assert(!processHandle.IsInvalid);
					var stream = CreateHexBufferStream_UI(processHandle.DangerousGetHandle());
					processInfos.Add(new ProcessInfo(p, stream, processHandle));
				}
			}
			else {
				foreach (var p in processes) {
					var info = TryGetProcessInfo_UI(p.Id);
					Debug.Assert(info != null);
					if (info == null)
						continue;
					ClearProcessStream_UI(info);
					processInfos.Remove(info);
					info.Dispose();
				}
			}
			InitializeNonInitializedBuffers_UI(processInfos.FirstOrDefault());
		}

		// UI thread
		void ClearProcessStream_UI(ProcessInfo closedInfo) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			foreach (var bufferState in bufferStates) {
				if (bufferState.Process == closedInfo.Process)
					bufferState.SetUnderlyingStream(null, null);
			}
		}

		// UI thread
		void InitializeNonInitializedBuffers_UI(ProcessInfo info) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			if (info == null)
				return;
			foreach (var bufferState in bufferStates) {
				if (bufferState.Process == null)
					bufferState.SetUnderlyingStream(info.Stream, info.Process);
			}
		}

		// UI thread
		HexCachedBufferStream CreateHexBufferStream_UI(IntPtr processHandle) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var processStream = hexBufferStreamFactoryService.CreateSimpleProcessStream(processHandle);
			return hexBufferStreamFactoryService.CreateCached(processStream, disposeStream: true);
		}

		// UI thread
		ProcessInfo TryGetProcessInfo_UI(int pid) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			foreach (var info in processInfos) {
				if (info.Process.Id == pid)
					return info;
			}
			return null;
		}

		// UI thread
		BufferState TryGetBufferState_UI(HexBuffer buffer) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			foreach (var bufferState in bufferStates) {
				if (bufferState.Buffer == buffer)
					return bufferState;
			}
			return null;
		}

		// UI thread
		public override IHexBufferInfo CreateBuffer() {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var debuggerHexBufferStream = new DebuggerHexBufferStream();
			var buffer = hexBufferFactoryService.Create(debuggerHexBufferStream, hexBufferFactoryService.DefaultMemoryTags, disposeStream: true);
			var bufferState = new BufferState(buffer, debuggerHexBufferStream);
			bufferStates.Add(bufferState);
			buffer.Disposed += Buffer_Disposed;
			buffer.ChangedLowPriority += Buffer_ChangedLowPriority;
			var info = processInfos.FirstOrDefault();
			bufferState.SetUnderlyingStream(info?.Stream, info?.Process);
			return bufferState;
		}

		// UI thread
		void Buffer_ChangedLowPriority(object sender, HexContentChangedEventArgs e) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var callerState = TryGetBufferState_UI((HexBuffer)sender);
			foreach (var bufferState in bufferStates) {
				if (bufferState == callerState)
					continue;
				if (bufferState.Process == null)
					continue;
				if (bufferState.Process != callerState.Process)
					continue;
				bufferState.InvalidateSpan(e.Changes);
			}
		}

		// UI thread
		void Buffer_Disposed(object sender, EventArgs e) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var buffer = (HexBuffer)sender;
			buffer.Disposed -= Buffer_Disposed;
			buffer.ChangedLowPriority -= Buffer_ChangedLowPriority;
			var bufferState = TryGetBufferState_UI(buffer);
			bool b = bufferStates.Remove(bufferState);
			Debug.Assert(b);
		}

		// UI thread
		public override void SetProcessStream(HexBuffer buffer, int pid) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var bufferState = TryGetBufferState_UI(buffer);
			if (bufferState == null)
				throw new ArgumentOutOfRangeException(nameof(buffer));
			var info = TryGetProcessInfo_UI(pid);
			if (info == null)
				info = processInfos.FirstOrDefault();
			bufferState.SetUnderlyingStream(info?.Stream, info?.Process);
		}

		// UI thread
		public override int? GetProcessId(HexBuffer buffer) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			debuggerDispatcher.Dispatcher.VerifyAccess();
			var bufferState = TryGetBufferState_UI(buffer);
			if (bufferState == null)
				throw new ArgumentOutOfRangeException(nameof(buffer));
			return bufferState.Process?.Id;
		}

		// UI thread
		public override int[] ProcessIds {
			get {
				debuggerDispatcher.Dispatcher.VerifyAccess();
				return processInfos.Select(a => a.Process.Id).ToArray();
			}
		}

		// UI thread
		public override void InvalidateMemory() {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			foreach (var info in processInfos)
				info.Stream.InvalidateAll();
		}

		// UI thread
		public override void InvalidateMemory(int pid) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			foreach (var info in processInfos) {
				if (info.Process.Id == pid)
					info.Stream.InvalidateAll();
			}
		}
	}
}
