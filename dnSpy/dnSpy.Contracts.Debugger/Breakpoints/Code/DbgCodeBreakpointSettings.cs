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
	/// Code breakpoint settings
	/// </summary>
	public struct DbgCodeBreakpointSettings : IEquatable<DbgCodeBreakpointSettings> {
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

		/// <summary>
		/// Labels
		/// </summary>
		public ReadOnlyCollection<string> Labels { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgCodeBreakpointSettings left, DbgCodeBreakpointSettings right) => left.Equals(right);
		public static bool operator !=(DbgCodeBreakpointSettings left, DbgCodeBreakpointSettings right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgCodeBreakpointSettings other) =>
			IsEnabled == other.IsEnabled &&
			Condition == other.Condition &&
			HitCount == other.HitCount &&
			Filter == other.Filter &&
			Trace == other.Trace &&
			LabelsEquals(Labels, other.Labels);

		static bool LabelsEquals(ReadOnlyCollection<string>? a, ReadOnlyCollection<string>? b) {
			if (a is null)
				a = emptyLabels;
			if (b is null)
				b = emptyLabels;
			if (a == b)
				return true;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!StringComparer.Ordinal.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		static int LabelsGetHashCode(ReadOnlyCollection<string> a) {
			int hc = 0;
			foreach (var s in a ?? emptyLabels)
				hc ^= StringComparer.Ordinal.GetHashCode(s ?? string.Empty);
			return hc;
		}

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgCodeBreakpointSettings other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			(IsEnabled ? 1 : 0) ^
			Condition.GetValueOrDefault().GetHashCode() ^
			HitCount.GetValueOrDefault().GetHashCode() ^
			Filter.GetValueOrDefault().GetHashCode() ^
			Trace.GetValueOrDefault().GetHashCode() ^
			LabelsGetHashCode(Labels);
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
	public readonly struct DbgCodeBreakpointCondition : IEquatable<DbgCodeBreakpointCondition> {
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgCodeBreakpointCondition left, DbgCodeBreakpointCondition right) => left.Equals(right);
		public static bool operator !=(DbgCodeBreakpointCondition left, DbgCodeBreakpointCondition right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgCodeBreakpointCondition other) => Kind == other.Kind && StringComparer.Ordinal.Equals(Condition, other.Condition);

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgCodeBreakpointCondition other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (int)Kind ^ StringComparer.Ordinal.GetHashCode(Condition ?? string.Empty);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Kind} {Condition}";
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
	public readonly struct DbgCodeBreakpointHitCount : IEquatable<DbgCodeBreakpointHitCount> {
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgCodeBreakpointHitCount left, DbgCodeBreakpointHitCount right) => left.Equals(right);
		public static bool operator !=(DbgCodeBreakpointHitCount left, DbgCodeBreakpointHitCount right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgCodeBreakpointHitCount other) => Kind == other.Kind && Count == other.Count;

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgCodeBreakpointHitCount other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (int)Kind ^ Count;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Kind} {Count}";
	}

	/// <summary>
	/// Code breakpoint filter
	/// </summary>
	public readonly struct DbgCodeBreakpointFilter : IEquatable<DbgCodeBreakpointFilter> {
		/// <summary>
		/// Filter
		/// </summary>
		public string Filter { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filter">Filter</param>
		public DbgCodeBreakpointFilter(string filter) => Filter = filter ?? throw new ArgumentNullException(nameof(filter));

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgCodeBreakpointFilter left, DbgCodeBreakpointFilter right) => left.Equals(right);
		public static bool operator !=(DbgCodeBreakpointFilter left, DbgCodeBreakpointFilter right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgCodeBreakpointFilter other) => StringComparer.Ordinal.Equals(Filter, other.Filter);

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgCodeBreakpointFilter other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Filter ?? string.Empty);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Filter;
	}

	/// <summary>
	/// Code breakpoint trace message
	/// </summary>
	public readonly struct DbgCodeBreakpointTrace : IEquatable<DbgCodeBreakpointTrace> {
		/// <summary>
		/// Message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// true to continue execution (trace) or false to break (breakpoint)
		/// </summary>
		public bool Continue { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="continue">true to continue execution (tracepoint) or false to break (breakpoint)</param>
		public DbgCodeBreakpointTrace(string message, bool @continue) {
			Message = message ?? throw new ArgumentNullException(nameof(message));
			Continue = @continue;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgCodeBreakpointTrace left, DbgCodeBreakpointTrace right) => left.Equals(right);
		public static bool operator !=(DbgCodeBreakpointTrace left, DbgCodeBreakpointTrace right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgCodeBreakpointTrace other) => Continue == other.Continue && StringComparer.Ordinal.Equals(Message, other.Message);

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgCodeBreakpointTrace other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Continue ? -1 : 0) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{(Continue ? "Continue" : "Break")}: {Message}";
	}
}
