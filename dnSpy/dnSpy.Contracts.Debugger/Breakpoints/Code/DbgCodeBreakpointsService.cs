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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Code breakpoints service
	/// </summary>
	public abstract class DbgCodeBreakpointsService {
		/// <summary>
		/// Modifies a breakpoint
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="settings">New settings</param>
		public void Modify(DbgCodeBreakpoint breakpoint, DbgCodeBreakpointSettings settings) =>
			Modify(new[] { new DbgCodeBreakpointAndSettings(breakpoint, settings) });

		/// <summary>
		/// Modifies breakpoints
		/// </summary>
		/// <param name="settings">New settings</param>
		public abstract void Modify(DbgCodeBreakpointAndSettings[] settings);

		/// <summary>
		/// Raised when breakpoints are modified
		/// </summary>
		public abstract event EventHandler<DbgBreakpointsModifiedEventArgs> BreakpointsModified;

		/// <summary>
		/// Gets all breakpoints
		/// </summary>
		public abstract DbgCodeBreakpoint[] Breakpoints { get; }

		/// <summary>
		/// Raised when <see cref="Breakpoints"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgCodeBreakpoint>> BreakpointsChanged;

		/// <summary>
		/// Adds a breakpoint
		/// </summary>
		/// <param name="breakpoint">Breakpoint info</param>
		/// <returns></returns>
		public DbgCodeBreakpoint Add(DbgCodeBreakpointInfo breakpoint) => Add(new[] { breakpoint })[0];

		/// <summary>
		/// Adds breakpoints
		/// </summary>
		/// <param name="breakpoints">Breakpoints</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpoint[] Add(DbgCodeBreakpointInfo[] breakpoints);

		/// <summary>
		/// Removes a breakpoint
		/// </summary>
		/// <param name="breakpoint">Breakpoint to remove</param>
		public void Remove(DbgCodeBreakpoint breakpoint) => Remove(new[] { breakpoint ?? throw new ArgumentNullException(nameof(breakpoint)) });

		/// <summary>
		/// Removes breakpoints
		/// </summary>
		/// <param name="breakpoints">Breakpoints to remove</param>
		public abstract void Remove(DbgCodeBreakpoint[] breakpoints);

		/// <summary>
		/// Removes all code breakpoints
		/// </summary>
		public abstract void Clear();
	}

	/// <summary>
	/// Breakpoint and old settings
	/// </summary>
	public struct DbgCodeBreakpointAndOldSettings {
		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgCodeBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the old settings
		/// </summary>
		public DbgCodeBreakpointSettings OldSettings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="oldSettings">Old settings</param>
		public DbgCodeBreakpointAndOldSettings(DbgCodeBreakpoint breakpoint, DbgCodeBreakpointSettings oldSettings) {
			Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			OldSettings = oldSettings;
		}
	}

	/// <summary>
	/// Breakpoints modified event args
	/// </summary>
	public struct DbgBreakpointsModifiedEventArgs {
		/// <summary>
		/// Gets the breakpoints
		/// </summary>
		public ReadOnlyCollection<DbgCodeBreakpointAndOldSettings> Breakpoints { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoints">Breakpoints and old settings</param>
		public DbgBreakpointsModifiedEventArgs(ReadOnlyCollection<DbgCodeBreakpointAndOldSettings> breakpoints) =>
			Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
	}

	/// <summary>
	/// Breakpoint and settings
	/// </summary>
	public struct DbgCodeBreakpointAndSettings {
		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgCodeBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the new settings
		/// </summary>
		public DbgCodeBreakpointSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="settings">New settings</param>
		public DbgCodeBreakpointAndSettings(DbgCodeBreakpoint breakpoint, DbgCodeBreakpointSettings settings) {
			Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			Settings = settings;
		}
	}

	/// <summary>
	/// Info needed to add a breakpoint
	/// </summary>
	public struct DbgCodeBreakpointInfo {
		/// <summary>
		/// Breakpoint location
		/// </summary>
		public DbgBreakpointLocation BreakpointLocation { get; }

		/// <summary>
		/// Breakpoint settings
		/// </summary>
		public DbgCodeBreakpointSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpointLocation">Breakpoint location</param>
		/// <param name="settings">Breakpoint settings</param>
		public DbgCodeBreakpointInfo(DbgBreakpointLocation breakpointLocation, DbgCodeBreakpointSettings settings) {
			BreakpointLocation = breakpointLocation ?? throw new ArgumentNullException(nameof(breakpointLocation));
			Settings = settings;
		}
	}

	/// <summary>
	/// Export an instance to get created when <see cref="DbgCodeBreakpointsService"/> gets created
	/// </summary>
	public interface IDbgCodeBreakpointsServiceListener {
		/// <summary>
		/// Called once by <see cref="DbgCodeBreakpointsService"/>
		/// </summary>
		/// <param name="dbgCodeBreakpointsService">Breakpoints service</param>
		void Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService);
	}
}
