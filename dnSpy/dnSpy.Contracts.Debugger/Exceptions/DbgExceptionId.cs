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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception ID
	/// </summary>
	public struct DbgExceptionId : IEquatable<DbgExceptionId> {
		readonly string group;
		readonly string name;
		readonly int code;
		readonly Flags flags;

		[Flags]
		enum Flags : byte {
			None		= 0,
			HasCode		= 1,
			Default		= 2,
		}

		/// <summary>
		/// Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/>
		/// </summary>
		public string Group => group;

		/// <summary>
		/// Name of exception. This property is only valid if <see cref="HasName"/> is true
		/// </summary>
		public string Name => name;

		/// <summary>
		/// Exception code. This property is only valid if <see cref="HasCode"/> is true
		/// </summary>
		public int Code => code;

		/// <summary>
		/// true if the exception has a code, and not a name
		/// </summary>
		public bool HasCode => (flags & (Flags.HasCode | Flags.Default)) == Flags.HasCode;

		/// <summary>
		/// true if the exception has a name, and not a code
		/// </summary>
		public bool HasName => (flags & (Flags.HasCode | Flags.Default)) == 0;

		/// <summary>
		/// true if this is the default exception ID
		/// </summary>
		public bool IsDefaultId => (flags & Flags.Default) != 0;

		/// <summary>
		/// Constructor for default ids
		/// </summary>
		/// <param name="group">Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/></param>
		public DbgExceptionId(string group) {
			this.group = group ?? throw new ArgumentNullException(nameof(group));
			name = null;
			code = 0;
			flags = Flags.Default;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/></param>
		/// <param name="name">Name of exception, must not be null</param>
		public DbgExceptionId(string group, string name) {
			this.group = group ?? throw new ArgumentNullException(nameof(group));
			this.name = name ?? throw new ArgumentNullException(nameof(name));
			code = 0;
			flags = Flags.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/></param>
		/// <param name="code">Exception code</param>
		public DbgExceptionId(string group, int code) {
			this.group = group ?? throw new ArgumentNullException(nameof(group));
			name = null;
			this.code = code;
			flags = Flags.HasCode;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgExceptionId left, DbgExceptionId right) => left.Equals(right);
		public static bool operator !=(DbgExceptionId left, DbgExceptionId right) => !left.Equals(right);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgExceptionId other) {
			if (flags != other.flags)
				return false;
			if ((flags & Flags.HasCode) != 0) {
				if (code != other.code)
					return false;
			}
			else {
				if (!StringComparer.Ordinal.Equals(name, other.name))
					return false;
			}
			if (!StringComparer.Ordinal.Equals(group, other.group))
				return false;
			return true;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DbgExceptionId other && Equals(other);

		/// <summary>
		/// Gets the hashcode
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			int hc = (int)flags ^ StringComparer.Ordinal.GetHashCode(group ?? string.Empty);
			if ((flags & Flags.HasCode) != 0)
				hc ^= code;
			else
				hc ^= StringComparer.Ordinal.GetHashCode(name ?? string.Empty);
			return hc;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (group == null)
				return "<not-initialized>";
			if (IsDefaultId)
				return group + " - <<default>>";
			if (HasCode)
				return "0x" + code.ToString("X8");
			return group + " - " + name;
		}
	}
}
