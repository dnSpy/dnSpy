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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Contains state and localized state that can be shown in the UI
	/// </summary>
	public readonly struct DbgStateInfo : IEquatable<DbgStateInfo> {
		/// <summary>
		/// Non-localized string
		/// </summary>
		public string State { get; }

		/// <summary>
		/// Localized string (shown in the UI)
		/// </summary>
		public string LocalizedState { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="state">State</param>
		public DbgStateInfo(string state)
			: this(state, state) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="state">State (non-localized)</param>
		/// <param name="localizedState">Localized state</param>
		public DbgStateInfo(string state, string localizedState) {
			State = state ?? throw new ArgumentNullException(nameof(state));
			LocalizedState = localizedState ?? localizedState ?? throw new ArgumentNullException(nameof(localizedState));
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgStateInfo left, DbgStateInfo right) => left.Equals(right);
		public static bool operator !=(DbgStateInfo left, DbgStateInfo right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DbgStateInfo other) => StringComparer.Ordinal.Equals(State, other.State) && StringComparer.Ordinal.Equals(LocalizedState, other.LocalizedState);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => obj is DbgStateInfo info && Equals(info);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(State ?? string.Empty) ^ StringComparer.Ordinal.GetHashCode(LocalizedState ?? string.Empty);

		/// <summary>
		/// Returns <see cref="LocalizedState"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() => LocalizedState;
	}
}
