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
using dnSpy.Contracts.Debugger.Code;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Code breakpoint
	/// </summary>
	public abstract class DbgCodeBreakpoint : DbgObject {
		/// <summary>
		/// Gets the unique code breakpoint id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Can be used by code to add custom hit test conditions
		/// </summary>
		public abstract event EventHandler<DbgBreakpointHitCheckEventArgs> HitCheck;

		/// <summary>
		/// Raised when the breakpoint is hit and the process will be paused
		/// </summary>
		public abstract event EventHandler<DbgBreakpointHitEventArgs> Hit;

		/// <summary>
		/// Gets the breakpoint options
		/// </summary>
		public abstract DbgCodeBreakpointOptions Options { get; }

		/// <summary>
		/// true if it's a temporary breakpoint that gets removed when all debugged processes have exited
		/// </summary>
		public bool IsTemporary => (Options & DbgCodeBreakpointOptions.Temporary) != 0;

		/// <summary>
		/// true if it's a hidden breakpoint. It's not shown in the UI (eg. breakpoints window, call stack window, glyph margin, text view)
		/// </summary>
		public bool IsHidden => (Options & DbgCodeBreakpointOptions.Hidden) != 0;

		/// <summary>
		/// true if it's a one-shot breakpoint. When the breakpoint is hit, the process is paused and the breakpoint is removed
		/// </summary>
		public bool IsOneShot => (Options & DbgCodeBreakpointOptions.OneShot) != 0;

		/// <summary>
		/// Gets/sets the current settings
		/// </summary>
		public abstract DbgCodeBreakpointSettings Settings { get; set; }

		/// <summary>
		/// true if the breakpoint is enabled
		/// </summary>
		public abstract bool IsEnabled { get; set; }

		/// <summary>
		/// Condition
		/// </summary>
		public abstract DbgCodeBreakpointCondition? Condition { get; set; }

		/// <summary>
		/// Hit count
		/// </summary>
		public abstract DbgCodeBreakpointHitCount? HitCount { get; set; }

		/// <summary>
		/// Filter
		/// </summary>
		public abstract DbgCodeBreakpointFilter? Filter { get; set; }

		/// <summary>
		/// Trace message
		/// </summary>
		public abstract DbgCodeBreakpointTrace? Trace { get; set; }

		/// <summary>
		/// Labels
		/// </summary>
		public abstract ReadOnlyCollection<string> Labels { get; set; }

		/// <summary>
		/// Gets the breakpoint location
		/// </summary>
		public abstract DbgCodeLocation Location { get; }

		/// <summary>
		/// Gets all bound breakpoints
		/// </summary>
		public abstract DbgBoundCodeBreakpoint[] BoundBreakpoints { get; }

		/// <summary>
		/// Raised when <see cref="BoundBreakpoints"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgBoundCodeBreakpoint>> BoundBreakpointsChanged;

		/// <summary>
		/// Gets the bound breakpoints warning/error message
		/// </summary>
		public abstract DbgBoundCodeBreakpointMessage BoundBreakpointsMessage { get; }

		/// <summary>
		/// Raised when <see cref="BoundBreakpointsMessage"/> is changed
		/// </summary>
		public abstract event EventHandler BoundBreakpointsMessageChanged;

		/// <summary>
		/// Removes the breakpoint
		/// </summary>
		public abstract void Remove();
	}

	/// <summary>
	/// Breakpoint hit check event args
	/// </summary>
	public sealed class DbgBreakpointHitCheckEventArgs : EventArgs {
		/// <summary>
		/// If false, it doesn't count as a hit and the process is not paused.
		/// If true, the normal BP settings decide if the process gets paused.
		/// The default value is true.
		/// </summary>
		public bool Pause {
			get => pause;
			set => pause = value;
		}
		bool pause;

		/// <summary>
		/// Gets the bound breakpoint
		/// </summary>
		public DbgBoundCodeBreakpoint BoundBreakpoint { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="boundBreakpoint">Bound breakpoint</param>
		/// <param name="thread">Thread</param>
		public DbgBreakpointHitCheckEventArgs(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread) {
			pause = true;
			BoundBreakpoint = boundBreakpoint ?? throw new ArgumentNullException(nameof(boundBreakpoint));
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
		}
	}

	/// <summary>
	/// Breakpoint hit event args
	/// </summary>
	public sealed class DbgBreakpointHitEventArgs : EventArgs {
		/// <summary>
		/// Gets the bound breakpoint
		/// </summary>
		public DbgBoundCodeBreakpoint BoundBreakpoint { get; }

		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="boundBreakpoint">Bound breakpoint</param>
		/// <param name="thread">Thread</param>
		public DbgBreakpointHitEventArgs(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread) {
			BoundBreakpoint = boundBreakpoint ?? throw new ArgumentNullException(nameof(boundBreakpoint));
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
		}
	}
}
