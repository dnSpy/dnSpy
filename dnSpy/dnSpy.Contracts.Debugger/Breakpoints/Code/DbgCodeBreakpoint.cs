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
		public abstract string[] Labels { get; set; }

		/// <summary>
		/// Gets the breakpoint location
		/// </summary>
		public abstract DbgBreakpointLocation Location { get; }

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
}
