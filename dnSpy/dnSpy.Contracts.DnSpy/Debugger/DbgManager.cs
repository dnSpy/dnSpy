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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Manages all debug engines. All events can be raised in any thread.
	/// If you need to hook events before debugging starts, you should export an <see cref="IDbgManagerStartListener"/>.
	/// It gets called when <see cref="Start(StartDebuggingOptions)"/> gets called for the first time.
	/// </summary>
	public abstract class DbgManager {
		/// <summary>
		/// Starts debugging. Returns an error string if it failed to create a debug engine, or null on success.
		/// See <see cref="IDbgManagerStartListener"/> on how to get called the first time this method gets called.
		/// </summary>
		/// <param name="options">Options needed to start the program or attach to it</param>
		public abstract string Start(StartDebuggingOptions options);

		/// <summary>
		/// true if a program is being debugged
		/// </summary>
		public abstract bool IsDebugging { get; }

		/// <summary>
		/// Raised when <see cref="IsDebugging"/> is changed
		/// </summary>
		public abstract event EventHandler IsDebuggingChanged;

		/// <summary>
		/// true if the processes are running, false if they're paused.
		/// This property is valid only if <see cref="IsDebugging"/> is true.
		/// </summary>
		public abstract bool IsRunning { get; }

		/// <summary>
		/// Raised when <see cref="IsRunning"/> is changed
		/// </summary>
		public abstract event EventHandler IsRunningChanged;

		/// <summary>
		/// Gets all debug tags, see <see cref="PredefinedDebugTags"/>
		/// </summary>
		public abstract string[] DebugTags { get; }

		/// <summary>
		/// Raised when <see cref="DebugTags"/> is changed
		/// </summary>
		public abstract event EventHandler<DebugTagsChangedEventArgs> DebugTagsChanged;

		/// <summary>
		/// Gets all debugged processes. Can be empty even if <see cref="IsDebugging"/> is true
		/// if the process hasn't been created yet.
		/// </summary>
		public abstract DbgProcess[] Processes { get; }

		/// <summary>
		/// Raised when a process gets added or removed
		/// </summary>
		public abstract event EventHandler<ProcessesChangedEventArgs> ProcessesChanged;

		/// <summary>
		/// Pauses all debugged processes
		/// </summary>
		public abstract void BreakAll();

		/// <summary>
		/// Lets all programs run again. This is the inverse of <see cref="BreakAll"/>
		/// </summary>
		public abstract void RunAll();

		/// <summary>
		/// Stops debugging. All programs started by the debugger will be terminated. All
		/// other programs will be detached, if possible, else terminated.
		/// </summary>
		public abstract void StopDebuggingAll();

		/// <summary>
		/// Terminates all debugged programs
		/// </summary>
		public abstract void TerminateAll();

		/// <summary>
		/// Detaches all debugged programs, if possible. If it's not possible to detach a
		/// program, it will be terminated.
		/// </summary>
		public abstract void DetachAll();

		/// <summary>
		/// true if <see cref="DetachAll"/> can be called without terminating any programs
		/// </summary>
		public abstract bool CanDetachWithoutTerminating { get; }
	}

	/// <summary>
	/// <see cref="DbgManager.ProcessesChanged"/> event args
	/// </summary>
	public struct ProcessesChangedEventArgs {
		/// <summary>
		/// The process that got added or removed
		/// </summary>
		public DbgProcess Process { get; }

		/// <summary>
		/// true if the process was added, false if it was removed
		/// </summary>
		public bool Added { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="process">The process that got added or removed</param>
		/// <param name="added">true if the process was added, false if it was removed</param>
		public ProcessesChangedEventArgs(DbgProcess process, bool added) {
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Added = added;
		}
	}

	/// <summary>
	/// <see cref="DbgManager.DebugTagsChanged"/> event args
	/// </summary>
	public struct DebugTagsChangedEventArgs {
		/// <summary>
		/// Debug tags, see <see cref="PredefinedDebugTags"/>
		/// </summary>
		public string[] DebugTags { get; }

		/// <summary>
		/// true if the tags were added, false if they were removed
		/// </summary>
		public bool Added { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="debugTags">Debug tags, see <see cref="PredefinedDebugTags"/></param>
		/// <param name="added">true if the tags were added, false if they were removed</param>
		public DebugTagsChangedEventArgs(string[] debugTags, bool added) {
			DebugTags = debugTags ?? throw new ArgumentNullException(nameof(debugTags));
			Added = added;
		}
	}
}
