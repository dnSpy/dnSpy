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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A required or optional custom modifier
	/// </summary>
	public readonly struct DmdCustomModifier : IEquatable<DmdCustomModifier> {
		/// <summary>
		/// true if it's a required C modifier
		/// </summary>
		public bool IsRequired { get; }

		/// <summary>
		/// true if it's an optional C modifier
		/// </summary>
		public bool IsOptional => !IsRequired;

		/// <summary>
		/// Gets the type
		/// </summary>
		public DmdType Type { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="isRequired">true if it's a required C modifier, false if it's an optional C modifier</param>
		public DmdCustomModifier(DmdType type, bool isRequired) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			IsRequired = isRequired;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdCustomModifier left, DmdCustomModifier right) => DmdMemberInfoEqualityComparer.DefaultCustomModifier.Equals(left, right);
		public static bool operator !=(DmdCustomModifier left, DmdCustomModifier right) => !DmdMemberInfoEqualityComparer.DefaultCustomModifier.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdCustomModifier other) => DmdMemberInfoEqualityComparer.DefaultCustomModifier.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DmdCustomModifier other && Equals(other);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultCustomModifier.GetHashCode(this);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => (IsRequired ? "[Req] " : "[Opt] ") + Type?.ToString();
	}
}
