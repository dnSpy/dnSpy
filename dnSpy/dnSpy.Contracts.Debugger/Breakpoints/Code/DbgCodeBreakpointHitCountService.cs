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

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Breakpoint hit count service
	/// </summary>
	public abstract class DbgCodeBreakpointHitCountService {
		/// <summary>
		/// Raised when the hit count is updated
		/// </summary>
		public abstract event EventHandler<DbgHitCountChangedEventArgs> HitCountChanged;

		/// <summary>
		/// Gets the hit count or null if we're not debugging
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <returns></returns>
		public abstract int? GetHitCount(DbgCodeBreakpoint breakpoint);

		/// <summary>
		/// Resets the hit count
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		public void Reset(DbgCodeBreakpoint breakpoint) => Reset(new[] { breakpoint ?? throw new ArgumentNullException(nameof(breakpoint)) });

		/// <summary>
		/// Resets the hit count
		/// </summary>
		/// <param name="breakpoints">Breakpoints</param>
		public abstract void Reset(DbgCodeBreakpoint[] breakpoints);
	}

	/// <summary>
	/// Breakpoint and hit count
	/// </summary>
	public readonly struct DbgCodeBreakpointAndHitCount {
		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgCodeBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the current hit count. It's null if we're not debugging
		/// </summary>
		public int? HitCount { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <param name="hitCount">Current hit count or null if we're not debugging</param>
		public DbgCodeBreakpointAndHitCount(DbgCodeBreakpoint breakpoint, int? hitCount) {
			Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
			HitCount = hitCount;
		}
	}

	/// <summary>
	/// <see cref="DbgCodeBreakpointHitCountService.HitCountChanged"/> event args
	/// </summary>
	public readonly struct DbgHitCountChangedEventArgs {
		/// <summary>
		/// Gets breakpoints and hit counts
		/// </summary>
		public ReadOnlyCollection<DbgCodeBreakpointAndHitCount> Breakpoints { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="breakpoints">Breakpoints and hit counts</param>
		public DbgHitCountChangedEventArgs(ReadOnlyCollection<DbgCodeBreakpointAndHitCount> breakpoints) =>
			Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
	}
}
