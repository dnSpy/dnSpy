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
using System.Diagnostics.CodeAnalysis;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Compares types, members, parameters
	/// </summary>
	public sealed class DmdMemberInfoEqualityComparer :
			IEqualityComparer<DmdMemberInfo?>, IEqualityComparer<DmdType?>, IEqualityComparer<DmdFieldInfo?>,
			IEqualityComparer<DmdMethodBase?>, IEqualityComparer<DmdConstructorInfo?>, IEqualityComparer<DmdMethodInfo?>,
			IEqualityComparer<DmdPropertyInfo?>, IEqualityComparer<DmdEventInfo?>, IEqualityComparer<DmdParameterInfo?>,
			IEqualityComparer<DmdMethodSignature?>, IEqualityComparer<IDmdAssemblyName?>, IEqualityComparer<DmdCustomModifier> {
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
		public bool Equals([AllowNull] DmdMemberInfo? x, [AllowNull] DmdMemberInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdMemberInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdType? x, [AllowNull] DmdType? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdType? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdFieldInfo? x, [AllowNull] DmdFieldInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdFieldInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdMethodBase? x, [AllowNull] DmdMethodBase? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdMethodBase? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdConstructorInfo? x, [AllowNull] DmdConstructorInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdConstructorInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdMethodInfo? x, [AllowNull] DmdMethodInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdMethodInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdPropertyInfo? x, [AllowNull] DmdPropertyInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdPropertyInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdEventInfo? x, [AllowNull] DmdEventInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdEventInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdParameterInfo? x, [AllowNull] DmdParameterInfo? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdParameterInfo? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdMethodSignature? x, [AllowNull] DmdMethodSignature? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdMethodSignature? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] IDmdAssemblyName? x, [AllowNull] IDmdAssemblyName? y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] IDmdAssemblyName? obj) => new DmdSigComparer(options).GetHashCode(obj);
		public bool Equals([AllowNull] DmdCustomModifier x, [AllowNull] DmdCustomModifier y) => new DmdSigComparer(options).Equals(x, y);
		public int GetHashCode([DisallowNull] DmdCustomModifier obj) => new DmdSigComparer(options).GetHashCode(obj);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
