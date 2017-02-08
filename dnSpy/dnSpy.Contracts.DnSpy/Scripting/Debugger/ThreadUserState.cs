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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Thread user state
	/// </summary>
	[Flags]
	public enum ThreadUserState {
		// IMPORTANT: Must be identical to dndbg.COM.CorDebug.CorDebugUserState (enum field names may be different)

		/// <summary>
		/// A termination of the thread has been requested.
		/// </summary>
		StopRequested = 1,
		/// <summary>
		/// A suspension of the thread has been requested.
		/// </summary>
		SuspendRequested,
		/// <summary>
		/// The thread is running in the background.
		/// </summary>
		Background = 4,
		/// <summary>
		/// The thread has not started executing.
		/// </summary>
		Unstarted = 8,
		/// <summary>
		/// The thread has been terminated.
		/// </summary>
		Stopped = 16,
		/// <summary>
		/// The thread is waiting for another thread to complete a task.
		/// </summary>
		WaitSleepJoin = 32,
		/// <summary>
		/// The thread has been suspended.
		/// </summary>
		Suspended = 64,
		/// <summary>
		/// The thread is at an unsafe point. That is, the thread is at a point in execution where it may block garbage collection.
		/// 
		/// Debug events may be dispatched from unsafe points, but suspending a thread at an unsafe point will very likely cause a deadlock until the thread is resumed. The safe and unsafe points are determined by the just-in-time (JIT) and garbage collection implementation.
		/// </summary>
		UnsafePoint = 128,
		/// <summary>
		/// The thread is from the thread pool.
		/// </summary>
		ThreadPool = 256
	}
}
