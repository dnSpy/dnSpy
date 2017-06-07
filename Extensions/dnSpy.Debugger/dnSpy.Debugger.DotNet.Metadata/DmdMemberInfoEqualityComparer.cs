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

using System.Collections.Generic;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Compares types, members, parameters
	/// </summary>
	public sealed class DmdMemberInfoEqualityComparer :
			IEqualityComparer<DmdMemberInfo>, IEqualityComparer<DmdType>, IEqualityComparer<DmdFieldInfo>,
			IEqualityComparer<DmdMethodBase>, IEqualityComparer<DmdConstructorInfo>, IEqualityComparer<DmdMethodInfo>,
			IEqualityComparer<DmdPropertyInfo>, IEqualityComparer<DmdEventInfo>, IEqualityComparer<DmdParameterInfo>,
			IEqualityComparer<DmdMethodSignature>, IEqualityComparer<DmdAssemblyName>, IEqualityComparer<DmdCustomModifier> {
		/// <summary>
		/// Gets an <see cref="IEqualityComparer{T}"/> that can be used to compare types, members and parameters using default
		/// <see cref="DmdSigComparer"/> options (<see cref="DefaultOptions"/> == <see cref="DmdSigComparerOptions.CompareDeclaringType"/>).
		/// </summary>
		public static readonly DmdMemberInfoEqualityComparer Default = new DmdMemberInfoEqualityComparer(DefaultOptions);

		/// <summary>
		/// Gets the default options used by <see cref="Default"/>
		/// </summary>
		public const DmdSigComparerOptions DefaultOptions = DmdSigComparerOptions.CompareDeclaringType;

		/// <summary>
		/// Doesn't compare declaring types
		/// </summary>
		public static readonly DmdMemberInfoEqualityComparer NoDeclaringTypes = new DmdMemberInfoEqualityComparer(DefaultOptions & ~DmdSigComparerOptions.CompareDeclaringType);

		readonly DmdSigComparerOptions options;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public DmdMemberInfoEqualityComparer(DmdSigComparerOptions options) => this.options = options;

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public bool Equals(DmdMemberInfo x, DmdMemberInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdMemberInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdType x, DmdType y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdType obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdFieldInfo x, DmdFieldInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdFieldInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdMethodBase x, DmdMethodBase y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdMethodBase obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdConstructorInfo x, DmdConstructorInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdConstructorInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdMethodInfo x, DmdMethodInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdMethodInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdPropertyInfo x, DmdPropertyInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdPropertyInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdEventInfo x, DmdEventInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdEventInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdParameterInfo x, DmdParameterInfo y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdParameterInfo obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdMethodSignature x, DmdMethodSignature y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdMethodSignature obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdAssemblyName x, DmdAssemblyName y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdAssemblyName obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdCustomModifier x, DmdCustomModifier y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdCustomModifier obj) => new DmdSigComparer(options).GetHashCode(obj);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
