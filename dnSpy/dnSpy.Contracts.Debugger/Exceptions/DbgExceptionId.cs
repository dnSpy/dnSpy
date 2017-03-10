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
		/// <summary>
		/// Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/>
		/// </summary>
		public string Group { get; }

		/// <summary>
		/// Name of exception or null if this is the 'default' id
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// true if this is the default exception ID
		/// </summary>
		public bool IsDefaultId => Name == null;

		/// <summary>
		/// Constructor for default ids
		/// </summary>
		/// <param name="group">Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/></param>
		public DbgExceptionId(string group) {
			Group = group ?? throw new ArgumentNullException(nameof(group));
			Name = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Exception group, same as <see cref="DbgExceptionGroupDefinition.Name"/></param>
		/// <param name="name">Name of exception, must not be null</param>
		public DbgExceptionId(string group, string name) {
			Group = group ?? throw new ArgumentNullException(nameof(group));
			Name = name ?? throw new ArgumentNullException(nameof(name));
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
		public bool Equals(DbgExceptionId other) => StringComparer.Ordinal.Equals(Name, other.Name) && StringComparer.Ordinal.Equals(Group, other.Group);

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
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Group) ^ (Name == null ? 0 : StringComparer.Ordinal.GetHashCode(Name));

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Group == null ? "<not-initialized>" : $"{Group} - {Name ?? "<<default>>"}";
	}
}
