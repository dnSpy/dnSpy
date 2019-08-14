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
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Steppers;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A thread in a debugged process
	/// </summary>
	public abstract class DbgThread : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the app domain or null if it's a process thread
		/// </summary>
		/// <returns></returns>
		public abstract DbgAppDomain? AppDomain { get; }

		/// <summary>
		/// true if this is the main thread
		/// </summary>
		public bool IsMain => Kind == PredefinedThreadKinds.Main;

		/// <summary>
		/// Gets the thread kind, see <see cref="PredefinedThreadKinds"/>
		/// </summary>
		public abstract string Kind { get; }

		/// <summary>
		/// Gets the id of this thread
		/// </summary>
		public abstract ulong Id { get; }

		/// <summary>
		/// Gets the managed id of this thread or null if it's unknown or if it's not a managed thread
		/// </summary>
		public abstract ulong? ManagedId { get; }

		/// <summary>
		/// Gets the thread name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the thread name shown in the UI
		/// </summary>
		public abstract string UIName { get; set; }

		/// <summary>
		/// Gets the suspended count. It's 0 if the thread isn't suspended, and greater than zero if it's suspended.
		/// </summary>
		public abstract int SuspendedCount { get; }

		/// <summary>
		/// Thread state
		/// </summary>
		public abstract ReadOnlyCollection<DbgStateInfo> State { get; }

		/// <summary>
		/// Returns true if the thread has a name
		/// </summary>
		/// <returns></returns>
		public abstract bool HasName();

		/// <summary>
		/// Freezes the thread
		/// </summary>
		public abstract void Freeze();

		/// <summary>
		/// Thaws the thread
		/// </summary>
		public abstract void Thaw();

		/// <summary>
		/// Creates a new <see cref="DbgStackWalker"/> instance that can be used to get the call stack.
		/// </summary>
		/// <returns></returns>
		public abstract DbgStackWalker CreateStackWalker();

		/// <summary>
		/// Gets the top stack frame or null if there's none
		/// </summary>
		/// <returns></returns>
		public DbgStackFrame? GetTopStackFrame() {
			DbgStackWalker? stackWalker = null;
			try {
				stackWalker = CreateStackWalker();
				var frames = stackWalker.GetNextStackFrames(1);
				Debug.Assert(frames.Length <= 1);
				return frames.Length == 0 ? null : frames[0];
			}
			finally {
				stackWalker?.Close();
			}
		}

		/// <summary>
		/// Gets the first <paramref name="count"/> frames.
		/// The returned frame count can be less than <paramref name="count"/> if there's not enough frames available.
		/// </summary>
		/// <param name="count">Max number of frames to return</param>
		/// <returns></returns>
		public DbgStackFrame[] GetFrames(int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (count == 0)
				return Array.Empty<DbgStackFrame>();
			DbgStackWalker? stackWalker = null;
			try {
				stackWalker = CreateStackWalker();
				var frames = stackWalker.GetNextStackFrames(count);
				Debug.Assert(frames.Length <= count);
				return frames;
			}
			finally {
				stackWalker?.Close();
			}
		}

		/// <summary>
		/// Creates a stepper.
		/// The caller must close the returned instance by calling <see cref="DbgStepper.Close"/>.
		/// </summary>
		/// <returns></returns>
		public abstract DbgStepper CreateStepper();

		/// <summary>
		/// Sets a new instruction pointer
		/// </summary>
		/// <param name="location">New location</param>
		public abstract void SetIP(DbgCodeLocation location);

		/// <summary>
		/// Checks if <see cref="SetIP(DbgCodeLocation)"/> can be called
		/// </summary>
		/// <param name="location">New location</param>
		/// <returns></returns>
		public abstract bool CanSetIP(DbgCodeLocation location);
	}
}
