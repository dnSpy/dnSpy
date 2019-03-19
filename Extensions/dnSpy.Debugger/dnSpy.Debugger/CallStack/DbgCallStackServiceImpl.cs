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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;

namespace dnSpy.Debugger.CallStack {
	[Export(typeof(IDbgManagerStartListener))]
	[Export(typeof(DbgCallStackService))]
	sealed class DbgCallStackServiceImpl : DbgCallStackService, IDbgManagerStartListener {
		const int MaxShownFrames = 5000;

		public override DbgThread Thread => dbgManager?.CurrentThread.Current;

		public override int ActiveFrameIndex {
			get {
				lock (lockObj)
					return activeFrameIndex;
			}
			set {
				lock (lockObj) {
					if (activeFrameIndex == value)
						return;
					if (dbgManager == null)
						return;
				}
				Dbg(() => SetActiveFrameIndex_DbgThread(value));
			}
		}

		public override event EventHandler<FramesChangedEventArgs> FramesChanged;
		public override DbgCallStackFramesInfo Frames {
			get {
				lock (lockObj)
					return new DbgCallStackFramesInfo(readOnlyFrames, framesTruncated, activeFrameIndex);
			}
		}

		readonly object lockObj;
		DbgManager dbgManager;
		DbgStackFrame[] frames;
		ReadOnlyCollection<DbgStackFrame> readOnlyFrames;
		bool framesTruncated;
		int activeFrameIndex;
		DbgProcess currentThreadProcess;
		static ReadOnlyCollection<DbgStackFrame> emptyFrames = new ReadOnlyCollection<DbgStackFrame>(Array.Empty<DbgStackFrame>());

		[ImportingConstructor]
		DbgCallStackServiceImpl() {
			lockObj = new object();
			frames = Array.Empty<DbgStackFrame>();
			readOnlyFrames = emptyFrames;
			framesTruncated = false;
			activeFrameIndex = 0;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			this.dbgManager = dbgManager;
			dbgManager.CurrentThreadChanged += DbgManager_CurrentThreadChanged;
			dbgManager.MessageSetIPComplete += DbgManager_MessageSetIPComplete;
		}

		// Note that dbgManager can be null if called before debugging has started
		void Dbg(Action callback) => dbgManager?.Dispatcher.BeginInvoke(callback);

		void SetActiveFrameIndex_DbgThread(int newIndex) {
			dbgManager.Dispatcher.VerifyAccess();
			lock (lockObj) {
				if (activeFrameIndex == newIndex)
					return;
				if ((uint)newIndex >= (uint)frames.Length)
					return;
				activeFrameIndex = newIndex;
			}
			FramesChanged?.Invoke(this, new FramesChangedEventArgs(framesChanged: false, activeFrameIndexChanged: true));
		}

		void DbgManager_MessageSetIPComplete(object sender, DbgMessageSetIPCompleteEventArgs e) {
			if (e.FramesInvalidated && dbgManager.CurrentThread.Current == e.Thread)
				RefreshAllFrames_DbgThread();
		}

		void DbgManager_CurrentThreadChanged(object sender, DbgCurrentObjectChangedEventArgs<DbgThread> e) {
			if (e.CurrentChanged)
				RefreshAllFrames_DbgThread();
		}

		void RefreshAllFrames_DbgThread() {
			UpdateCurrentThreadProcess_DbgThread(dbgManager.CurrentThread.Current?.Process);
			UpdateFrames_DbgThread();
		}

		void UpdateCurrentThreadProcess_DbgThread(DbgProcess process) {
			dbgManager.Dispatcher.VerifyAccess();
			if (currentThreadProcess == process)
				return;
			if (currentThreadProcess != null)
				currentThreadProcess.IsRunningChanged -= DbgProcess_IsRunningChanged;
			currentThreadProcess = process;
			if (process != null)
				process.IsRunningChanged += DbgProcess_IsRunningChanged;
		}

		void DbgProcess_IsRunningChanged(object sender, EventArgs e) {
			if (currentThreadProcess != sender)
				return;
			if (!currentThreadProcess.IsRunning)
				UpdateFrames_DbgThread();
		}

		void UpdateFrames_DbgThread() {
			dbgManager.Dispatcher.VerifyAccess();
			bool raiseActiveFrameIndexChanged, raiseFramesChanged;
			DbgStackWalker stackWalker = null;
			DbgStackFrame[] newFrames = null;
			try {
				var thread = dbgManager.CurrentThread.Current;
				if (thread == null || thread.Process.State != DbgProcessState.Paused)
					newFrames = Array.Empty<DbgStackFrame>();
				else {
					stackWalker = thread.CreateStackWalker();
					newFrames = stackWalker.GetNextStackFrames(MaxShownFrames + 1);
				}
				lock (lockObj) {
					if (frames.Length > 0)
						dbgManager.Close(frames);
					int newActiveFrameIndex = GetFrameIndex(newFrames);
					// Always raise frames-changed event even if old and new arrays have 0 length.
					raiseFramesChanged = true;
					raiseActiveFrameIndexChanged = newActiveFrameIndex != activeFrameIndex;

					// Note that we keep the extra frame so we don't have to create a new array with one less
					// frame. If we got MaxShownFrames+1 frames, it's not very likely that the last one in the
					// array is actually the last frame.
					// Another solution is to create a custom IList<DbgStackFrame> that hides the last one
					// but that's not worth it.
					frames = newFrames;
					readOnlyFrames = newFrames.Length == 0 ? emptyFrames : new ReadOnlyCollection<DbgStackFrame>(newFrames);
					activeFrameIndex = newActiveFrameIndex;
					framesTruncated = newFrames.Length > MaxShownFrames;
				}
			}
			finally {
				stackWalker?.Close();
				if (newFrames != null && frames != newFrames && newFrames.Length > 0)
					dbgManager.Close(newFrames);
			}
			if (raiseFramesChanged || raiseActiveFrameIndexChanged)
				FramesChanged?.Invoke(this, new FramesChangedEventArgs(raiseFramesChanged, raiseActiveFrameIndexChanged));
		}

		int GetFrameIndex(DbgStackFrame[] newFrames) {
			for (int i = 0; i < newFrames.Length; i++) {
				if (newFrames[i].Location != null)
					return i;
			}
			return 0;
		}
	}
}
