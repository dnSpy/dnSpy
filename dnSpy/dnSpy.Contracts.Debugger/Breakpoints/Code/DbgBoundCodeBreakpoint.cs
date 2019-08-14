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
using System.ComponentModel;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// A bound breakpoint. These only exist while debugging a program. A bound breakpoint is only created
	/// if the breakpoint is enabled.
	/// </summary>
	public abstract class DbgBoundCodeBreakpoint : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public abstract DbgCodeBreakpoint Breakpoint { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the module or null if none
		/// </summary>
		public abstract DbgModule? Module { get; }

		/// <summary>
		/// Gets the address of the breakpoint. This property is valid if <see cref="HasAddress"/> is true
		/// </summary>
		public abstract ulong Address { get; }

		/// <summary>
		/// true if <see cref="Address"/> is a valid address
		/// </summary>
		public bool HasAddress => Address != DbgObjectFactory.BoundBreakpointNoAddress;

		/// <summary>
		/// Gets the warning/error message
		/// </summary>
		public abstract DbgBoundCodeBreakpointMessage Message { get; }
	}

	/// <summary>
	/// Bound breakpoint message severity
	/// </summary>
	public enum DbgBoundCodeBreakpointSeverity {
		/// <summary>
		/// No error/warning message
		/// </summary>
		None,

		/// <summary>
		/// Warning
		/// </summary>
		Warning,

		/// <summary>
		/// Error
		/// </summary>
		Error,
	}

	/// <summary>
	/// Bound breakpoint message
	/// </summary>
	public readonly struct DbgBoundCodeBreakpointMessage : IEquatable<DbgBoundCodeBreakpointMessage> {
		/// <summary>
		/// No error/warning message
		/// </summary>
		public static readonly DbgBoundCodeBreakpointMessage None = new DbgBoundCodeBreakpointMessage(DbgBoundCodeBreakpointSeverity.None, string.Empty);

		/// <summary>
		/// Gets the severity
		/// </summary>
		public DbgBoundCodeBreakpointSeverity Severity { get; }

		/// <summary>
		/// Gets the message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="severity">Severity</param>
		/// <param name="message">Message</param>
		public DbgBoundCodeBreakpointMessage(DbgBoundCodeBreakpointSeverity severity, string message) {
			Severity = severity;
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgBoundCodeBreakpointMessage left, DbgBoundCodeBreakpointMessage right) => left.Equals(right);
		public static bool operator !=(DbgBoundCodeBreakpointMessage left, DbgBoundCodeBreakpointMessage right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgBoundCodeBreakpointMessage other) => Severity == other.Severity && StringComparer.Ordinal.Equals(Message, other.Message);

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgBoundCodeBreakpointMessage other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (int)Severity ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Severity}: {Message}";
	}
}
