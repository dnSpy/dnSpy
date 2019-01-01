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

using System.Collections.Generic;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Compares types, members, parameters
	/// </summary>
	public sealed class DmdMemberInfoEqualityComparer :
			IEqualityComparer<DmdMemberInfo>, IEqualityComparer<DmdType>, IEqualityComparer<DmdFieldInfo>,
			IEqualityComparer<DmdMethodBase>, IEqualityComparer<DmdConstructorInfo>, IEqualityComparer<DmdMethodInfo>,
			IEqualityComparer<DmdPropertyInfo>, IEqualityComparer<DmdEventInfo>, IEqualityComparer<DmdParameterInfo>,
			IEqualityComparer<DmdMethodSignature>, IEqualityComparer<IDmdAssemblyName>, IEqualityComparer<DmdCustomModifier> {
		/// <summary>
		/// Should be used when comparing types that aren't part of a member signature. Custom modifiers and
		/// MD arrays' lower bounds and sizes are ignored.
		/// </summary>
		public static readonly DmdMemberInfoEqualityComparer DefaultType = new DmdMemberInfoEqualityComparer(DefaultTypeOptions);

		/// <summary>
		/// Should be used when comparing member signatures or when comparing types in member signatures.
		/// Custom modifiers are compared and types are checked for equivalence.
		/// </summary>
		public static readonly DmdMemberInfoEqualityComparer DefaultMember = new DmdMemberInfoEqualityComparer(DmdSigComparerOptions.CompareDeclaringType | DmdSigComparerOptions.CompareCustomModifiers | DmdSigComparerOptions.CheckTypeEquivalence);

		/// <summary>
		/// Should be used when comparing parameters
		/// </summary>
		public static readonly DmdMemberInfoEqualityComparer DefaultParameter = new DmdMemberInfoEqualityComparer(DefaultTypeOptions);

		/// <summary>
		/// Should be used when comparing custom modifiers
		/// </summary>
		internal static readonly DmdMemberInfoEqualityComparer DefaultCustomModifier = new DmdMemberInfoEqualityComparer(DefaultTypeOptions);

		/// <summary>
		/// Should be used when comparing any other supported class, eg. <see cref="IDmdAssemblyName"/>s
		/// </summary>
		internal static readonly DmdMemberInfoEqualityComparer DefaultOther = new DmdMemberInfoEqualityComparer(DefaultTypeOptions);

		/// <summary>
		/// Gets the default options used by <see cref="DefaultType"/>
		/// </summary>
		public const DmdSigComparerOptions DefaultTypeOptions = DmdSigComparerOptions.CompareDeclaringType | DmdSigComparerOptions.IgnoreMultiDimensionalArrayLowerBoundsAndSizes;

		/// <summary>
		/// Gets the options
		/// </summary>
		public DmdSigComparerOptions Options => options;

		readonly DmdSigComparerOptions options;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public DmdMemberInfoEqualityComparer(DmdSigComparerOptions options) => this.options = options;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
		public bool Equals(IDmdAssemblyName x, IDmdAssemblyName y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(IDmdAssemblyName obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals(DmdCustomModifier x, DmdCustomModifier y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode(DmdCustomModifier obj) => new DmdSigComparer(options).GetHashCode(obj);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
