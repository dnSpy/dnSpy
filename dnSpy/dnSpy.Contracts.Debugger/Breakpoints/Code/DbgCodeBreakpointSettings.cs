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
	/// Code breakpoint settings
	/// </summary>
	public struct DbgCodeBreakpointSettings {
		/// <summary>
		/// true if the breakpoint is enabled
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Condition
		/// </summary>
		public DbgCodeBreakpointCondition? Condition { get; set; }

		/// <summary>
		/// Hit count
		/// </summary>
		public DbgCodeBreakpointHitCount? HitCount { get; set; }

		/// <summary>
		/// Filter
		/// </summary>
		public DbgCodeBreakpointFilter? Filter { get; set; }

		/// <summary>
		/// Trace message
		/// </summary>
		public DbgCodeBreakpointTrace? Trace { get; set; }
	}

	/// <summary>
	/// Code breakpoint condition kind
	/// </summary>
	public enum DbgCodeBreakpointConditionKind {
		/// <summary>
		/// Condition is true
		/// </summary>
		IsTrue,

		/// <summary>
		/// Condition is changed
		/// </summary>
		WhenChanged,
	}

	/// <summary>
	/// Code breakpoint condition
	/// </summary>
	public struct DbgCodeBreakpointCondition {
		/// <summary>
		/// Condition kind
		/// </summary>
		public DbgCodeBreakpointConditionKind Kind { get; }

		/// <summary>
		/// Condition expression
		/// </summary>
		public string Condition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Condition kind</param>
		/// <param name="condition">Condition expression</param>
		public DbgCodeBreakpointCondition(DbgCodeBreakpointConditionKind kind, string condition) {
			Kind = kind;
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
		}
	}

	/// <summary>
	/// Hit count kind
	/// </summary>
	public enum DbgCodeBreakpointHitCountKind {
		/// <summary>
		/// Hit count == value
		/// </summary>
		Equals,

		/// <summary>
		/// (Hit count % value) == 0
		/// </summary>
		MultipleOf,

		/// <summary>
		/// Hit count >= value
		/// </summary>
		GreaterThanOrEquals,
	}

	/// <summary>
	/// Hit count
	/// </summary>
	public struct DbgCodeBreakpointHitCount {
		/// <summary>
		/// Hit count kind
		/// </summary>
		public DbgCodeBreakpointHitCountKind Kind { get; }

		/// <summary>
		/// Hit count
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Hit count kind</param>
		/// <param name="count">Hit count</param>
		public DbgCodeBreakpointHitCount(DbgCodeBreakpointHitCountKind kind, int count) {
			Kind = kind;
			Count = count;
		}
	}

	/// <summary>
	/// Code breakpoint filter
	/// </summary>
	public struct DbgCodeBreakpointFilter {
		/// <summary>
		/// Filter
		/// </summary>
		public string Filter { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filter">Filter</param>
		public DbgCodeBreakpointFilter(string filter) => Filter = filter ?? throw new ArgumentNullException(nameof(filter));
	}

	/// <summary>
	/// Code breakpoint trace message
	/// </summary>
	public struct DbgCodeBreakpointTrace {
		/// <summary>
		/// Message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		public DbgCodeBreakpointTrace(string message) => Message = message ?? throw new ArgumentNullException(nameof(message));
	}
}
