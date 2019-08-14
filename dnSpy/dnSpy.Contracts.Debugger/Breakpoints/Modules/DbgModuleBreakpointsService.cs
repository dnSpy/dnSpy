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

namespace dnSpy.Contracts.Debugger.Breakpoints.Modules {
	/// <summary>
	/// Module breakpoints service
	/// </summary>
	public abstract class DbgModuleBreakpointsService {
		/// <summary>
		/// Modifies a breakpoint
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="settings">New settings</param>
		public void Modify(DbgModuleBreakpoint breakpoint, DbgModuleBreakpointSettings settings) =>
			Modify(new[] { new DbgModuleBreakpointAndSettings(breakpoint, settings) });

		/// <summary>
		/// Modifies breakpoints
		/// </summary>
		/// <param name="settings">New settings</param>
		public abstract void Modify(DbgModuleBreakpointAndSettings[] settings);

		/// <summary>
		/// Raised when breakpoints are modified
		/// </summary>
		public abstract event EventHandler<DbgBreakpointsModifiedEventArgs>? BreakpointsModified;

		/// <summary>
		/// Gets all breakpoints
		/// </summary>
		public abstract DbgModuleBreakpoint[] Breakpoints { get; }

		/// <summary>
		/// Raised when <see cref="Breakpoints"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgModuleBreakpoint>>? BreakpointsChanged;

		/// <summary>
		/// Adds a breakpoint
		/// </summary>
		/// <param name="settings">Breakpoint settings</param>
		/// <returns></returns>
		public DbgModuleBreakpoint Add(DbgModuleBreakpointSettings settings) => Add(new[] { settings })[0];

		/// <summary>
		/// Adds breakpoints
		/// </summary>
		/// <param name="settings">Breakpoint settings</param>
		/// <returns></returns>
		public abstract DbgModuleBreakpoint[] Add(DbgModuleBreakpointSettings[] settings);

		/// <summary>
		/// Removes a breakpoint
		/// </summary>
		/// <param name="breakpoint">Breakpoint to remove</param>
		public void Remove(DbgModuleBreakpoint breakpoint) => Remove(new[] { breakpoint ?? throw new ArgumentNullException(nameof(breakpoint)) });

		/// <summary>
		/// Removes breakpoints
		/// </summary>
		/// <param name="breakpoints">Breakpoints to remove</param>
		public abstract void Remove(DbgModuleBreakpoint[] breakpoints);

		/// <summary>
		/// Removes all module breakpoints
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Finds breakpoints
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public abstract DbgModuleBreakpoint[] Find(in DbgModuleBreakpointInfo module);

		/// <summary>
		/// Checks if <paramref name="module"/> matches at least one breakpoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public abstract bool IsMatch(in DbgModuleBreakpointInfo module);
	}

	/// <summary>
	/// Breakpoint and old settings
	/// </summary>
	public readonly struct DbgModuleBreakpointAndOldSettings {
		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgModuleBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the old settings
		/// </summary>
		public DbgModuleBreakpointSettings OldSettings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="oldSettings">Old settings</param>
		public DbgModuleBreakpointAndOldSettings(DbgModuleBreakpoint breakpoint, DbgModuleBreakpointSettings oldSettings) {
			Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			OldSettings = oldSettings;
		}
	}

	/// <summary>
	/// Breakpoints modified event args
	/// </summary>
	public readonly struct DbgBreakpointsModifiedEventArgs {
		/// <summary>
		/// Gets the breakpoints
		/// </summary>
		public ReadOnlyCollection<DbgModuleBreakpointAndOldSettings> Breakpoints { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoints">Breakpoints and old settings</param>
		public DbgBreakpointsModifiedEventArgs(ReadOnlyCollection<DbgModuleBreakpointAndOldSettings> breakpoints) =>
			Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
	}

	/// <summary>
	/// Breakpoint and settings
	/// </summary>
	public readonly struct DbgModuleBreakpointAndSettings {
		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgModuleBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the new settings
		/// </summary>
		public DbgModuleBreakpointSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="settings">New settings</param>
		public DbgModuleBreakpointAndSettings(DbgModuleBreakpoint breakpoint, DbgModuleBreakpointSettings settings) {
			Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			Settings = settings;
		}
	}
}
