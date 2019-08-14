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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Contracts.Debugger.Code;

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
		public abstract event EventHandler<DbgBreakpointsModifiedEventArgs>? BreakpointsModified;

		/// <summary>
		/// Gets all breakpoints
		/// </summary>
		public abstract DbgCodeBreakpoint[] Breakpoints { get; }

		/// <summary>
		/// Gets all visible breakpoints
		/// </summary>
		public IEnumerable<DbgCodeBreakpoint> VisibleBreakpoints => Breakpoints.Where(a => !a.IsHidden);

		/// <summary>
		/// Raised when <see cref="Breakpoints"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgCodeBreakpoint>>? BreakpointsChanged;

		/// <summary>
		/// Adds a breakpoint. If the breakpoint already exists, null is returned.
		/// </summary>
		/// <param name="breakpoint">Breakpoint info</param>
		/// <returns></returns>
		public DbgCodeBreakpoint? Add(DbgCodeBreakpointInfo breakpoint) => Add(new[] { breakpoint }).FirstOrDefault();

		/// <summary>
		/// Adds breakpoints. Duplicate breakpoints are ignored.
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
		/// Returns an existing breakpoint at <paramref name="location"/> or null if none exists
		/// </summary>
		/// <param name="location">Location</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpoint? TryGetBreakpoint(DbgCodeLocation location);

		/// <summary>
		/// Removes all visible breakpoints
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Raised when <see cref="DbgCodeBreakpoint.BoundBreakpointsMessage"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgBoundBreakpointsMessageChangedEventArgs>? BoundBreakpointsMessageChanged;
	}

	/// <summary>
	/// <see cref="DbgCodeBreakpoint.BoundBreakpointsMessage"/> changed event args
	/// </summary>
	public readonly struct DbgBoundBreakpointsMessageChangedEventArgs {
		/// <summary>
		/// Gets all breakpoints
		/// </summary>
		public ReadOnlyCollection<DbgCodeBreakpoint> Breakpoints { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoints">Breakpoints</param>
		public DbgBoundBreakpointsMessageChangedEventArgs(ReadOnlyCollection<DbgCodeBreakpoint> breakpoints) =>
			Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
	}

	/// <summary>
	/// Breakpoint and old settings
	/// </summary>
	public readonly struct DbgCodeBreakpointAndOldSettings {
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
	public readonly struct DbgBreakpointsModifiedEventArgs {
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
	public readonly struct DbgCodeBreakpointAndSettings {
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
	public readonly struct DbgCodeBreakpointInfo {
		/// <summary>
		/// Breakpoint location
		/// </summary>
		public DbgCodeLocation Location { get; }

		/// <summary>
		/// Breakpoint settings
		/// </summary>
		public DbgCodeBreakpointSettings Settings { get; }

		/// <summary>
		/// Breakpoint options
		/// </summary>
		public DbgCodeBreakpointOptions Options { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Breakpoint location. If you don't own this location instance, you must call <see cref="DbgCodeLocation.Clone"/></param>
		/// <param name="settings">Breakpoint settings</param>
		public DbgCodeBreakpointInfo(DbgCodeLocation location, DbgCodeBreakpointSettings settings) {
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Settings = settings;
			Options = DbgCodeBreakpointOptions.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Breakpoint location. If you don't own this location instance, you must call <see cref="DbgCodeLocation.Clone"/></param>
		/// <param name="settings">Breakpoint settings</param>
		/// <param name="options">Breakpoint options</param>
		public DbgCodeBreakpointInfo(DbgCodeLocation location, DbgCodeBreakpointSettings settings, DbgCodeBreakpointOptions options) {
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Settings = settings;
			Options = options;
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
