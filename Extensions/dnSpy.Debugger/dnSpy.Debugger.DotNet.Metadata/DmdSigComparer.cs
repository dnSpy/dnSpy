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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Type and member comparer options
	/// </summary>
	[Flags]
	public enum DmdSigComparerOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None = 0,

		/// <summary>
		/// Don't compare type scope (assembly / module)
		/// </summary>
		DontCompareTypeScope = 1,

		/// <summary>
		/// Compare declaring type. It's ignored if it's a nested type and only used if it's a field, constructor, method, property, event, parameter
		/// </summary>
		CompareDeclaringType = 2,

		/// <summary>
		/// Don't compare return types
		/// </summary>
		DontCompareReturnType = 4,

		/// <summary>
		/// Case insensitive member names
		/// </summary>
		CaseInsensitiveMemberNames = 8,

		/// <summary>
		/// Project WinMD references
		/// </summary>
		ProjectWinMDReferences = 0x10,

		/// <summary>
		/// Check type equivalence
		/// </summary>
		CheckTypeEquivalence = 0x20,
	}

	/// <summary>
	/// Compares types and members
	/// </summary>
	public struct DmdSigComparer {
		readonly DmdSigComparerOptions options;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public DmdSigComparer(DmdSigComparerOptions options) {
			this.options = options;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public bool Equals(DmdMemberInfo a, DmdMemberInfo b) => throw new NotImplementedException();
		public bool Equals(DmdType a, DmdType b) => throw new NotImplementedException();
		public bool Equals(DmdFieldInfo a, DmdFieldInfo b) => throw new NotImplementedException();
		public bool Equals(DmdMethodBase a, DmdMethodBase b) => throw new NotImplementedException();
		public bool Equals(DmdConstructorInfo a, DmdConstructorInfo b) => throw new NotImplementedException();
		public bool Equals(DmdMethodInfo a, DmdMethodInfo b) => throw new NotImplementedException();
		public bool Equals(DmdPropertyInfo a, DmdPropertyInfo b) => throw new NotImplementedException();
		public bool Equals(DmdEventInfo a, DmdEventInfo b) => throw new NotImplementedException();
		public bool Equals(DmdParameterInfo a, DmdParameterInfo b) => throw new NotImplementedException();
		public bool Equals(DmdAssemblyName a, DmdAssemblyName b) => throw new NotImplementedException();
		public bool Equals(DmdMethodSignature a, DmdMethodSignature b) => throw new NotImplementedException();
		public int GetHashCode(DmdMemberInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdType a) => throw new NotImplementedException();
		public int GetHashCode(DmdFieldInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdMethodBase a) => throw new NotImplementedException();
		public int GetHashCode(DmdConstructorInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdMethodInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdPropertyInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdEventInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdParameterInfo a) => throw new NotImplementedException();
		public int GetHashCode(DmdAssemblyName a) => throw new NotImplementedException();
		public int GetHashCode(DmdMethodSignature a) => throw new NotImplementedException();
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
