/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Current <see cref="Intellisense.Completion"/>
	/// </summary>
	public sealed class CurrentCompletion : IEquatable<CurrentCompletion> {
		/// <summary>
		/// An instance with no <see cref="Completion"/>
		/// </summary>
		public static readonly CurrentCompletion Empty = new CurrentCompletion(null, isSelected: false, isUnique: false);

		/// <summary>
		/// Gets the completion or null if none
		/// </summary>
		public Completion Completion { get; }

		/// <summary>
		/// true if it's selected
		/// </summary>
		public bool IsSelected { get; }

		/// <summary>
		/// true if it's a unique match
		/// </summary>
		public bool IsUnique { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="completion">Completion or null</param>
		/// <param name="isSelected">true if it's selected</param>
		/// <param name="isUnique">true if it's a unique match</param>
		public CurrentCompletion(Completion completion, bool isSelected, bool isUnique) {
			Completion = completion;
			IsSelected = isSelected;
			IsUnique = isUnique;
		}

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(CurrentCompletion left, CurrentCompletion right) => !(left == right);

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(CurrentCompletion left, CurrentCompletion right) {
			if ((object)left == right)
				return true;
			if ((object)left == null)
				return false;
			return left.Equals(right);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(CurrentCompletion other) => (object)other != null && other.IsSelected == IsSelected && other.IsUnique == IsUnique && Equals(other.Completion, Completion);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as CurrentCompletion);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Completion?.GetHashCode() ?? 0) ^ (IsSelected ? int.MinValue : 0) ^ (IsUnique ? 0x40000000 : 0);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"Selected={IsSelected} Unique={IsUnique} {Completion?.DisplayText ?? "<null>"}";
	}
}
