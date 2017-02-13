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
	/// </summary>
	public abstract class DbgManager {
		/// <summary>
		/// Starts debugging. Returns a string if it failed to create a debug engine, or null on success
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
		/// Gets all debugged processes. Can be empty even if <see cref="IsDebugging"/> is true
		/// if the process hasn't been created yet.
		/// </summary>
		public abstract DbgProcess[] Processes { get; }

		/// <summary>
		/// Raised when a process gets added or removed
		/// </summary>
		public abstract event EventHandler<ProcessesChangedEventArgs> ProcessesChanged;
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
			if (process == null)
				throw new ArgumentNullException(nameof(process));
			Process = process;
			Added = added;
		}
	}
}
