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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception settings
	/// </summary>
	public readonly struct DbgExceptionSettings : IEquatable<DbgExceptionSettings> {
		/// <summary>
		/// Flags
		/// </summary>
		public DbgExceptionDefinitionFlags Flags { get; }

		/// <summary>
		/// Conditions
		/// </summary>
		public ReadOnlyCollection<DbgExceptionConditionSettings> Conditions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="conditions">Conditions or null</param>
		public DbgExceptionSettings(DbgExceptionDefinitionFlags flags, ReadOnlyCollection<DbgExceptionConditionSettings>? conditions = null) {
			Flags = flags;
			Conditions = conditions is null || conditions.Count == 0 ? emptyConditionsCollection : conditions;
		}
		static readonly ReadOnlyCollection<DbgExceptionConditionSettings> emptyConditionsCollection = new ReadOnlyCollection<DbgExceptionConditionSettings>(Array.Empty<DbgExceptionConditionSettings>());

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgExceptionSettings left, DbgExceptionSettings right) => left.Equals(right);
		public static bool operator !=(DbgExceptionSettings left, DbgExceptionSettings right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DbgExceptionSettings other) {
			if (Flags != other.Flags)
				return false;
			return Equals(Conditions, other.Conditions);
		}

		static bool Equals(ReadOnlyCollection<DbgExceptionConditionSettings>? a, ReadOnlyCollection<DbgExceptionConditionSettings>? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i].ConditionType != b[i].ConditionType)
					return false;
				if (!StringComparer.Ordinal.Equals(a[i].Condition, b[i].Condition))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgExceptionSettings other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			int hc = (int)Flags;
			if (!(Conditions is null)) {
				foreach (var c in Conditions)
					hc ^= (int)c.ConditionType ^ StringComparer.Ordinal.GetHashCode(c.Condition ?? string.Empty);
			}
			return hc;
		}
	}
}
