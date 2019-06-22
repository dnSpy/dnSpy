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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// .NET method signature
	/// </summary>
	public sealed class DmdMethodSignature : IEquatable<DmdMethodSignature?> {
		/// <summary>
		/// Gets the flags
		/// </summary>
		public DmdSignatureCallingConvention Flags { get; }

		/// <summary>
		/// true if it's a generic method signature
		/// </summary>
		public bool IsGeneric => (Flags & DmdSignatureCallingConvention.Generic) != 0;

		/// <summary>
		/// true if 'this' is a hidden parameter
		/// </summary>
		public bool HasThis => (Flags & DmdSignatureCallingConvention.HasThis) != 0;

		/// <summary>
		/// true if 'this' is an explicit parameter
		/// </summary>
		public bool ExplicitThis => (Flags & DmdSignatureCallingConvention.ExplicitThis) != 0;

		/// <summary>
		/// Generic parameter count
		/// </summary>
		public int GenericParameterCount { get; }

		/// <summary>
		/// Gets the return type
		/// </summary>
		public DmdType ReturnType { get; }

		internal bool HasTypeEquivalence {
			get {
				if (ReturnType.HasTypeEquivalence)
					return true;
				var list = parameterTypes;
				for (int i = 0; i < list.Count; i++) {
					if (list[i].HasTypeEquivalence)
						return true;
				}
				list = varArgsParameterTypes;
				for (int i = 0; i < list.Count; i++) {
					if (list[i].HasTypeEquivalence)
						return true;
				}
				return false;
			}
		}

		readonly ReadOnlyCollection<DmdType> parameterTypes;
		readonly ReadOnlyCollection<DmdType> varArgsParameterTypes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="genericParameterCount">Number of generic parameters</param>
		/// <param name="returnType">Return type</param>
		/// <param name="parameterTypes">Parameter types or null</param>
		/// <param name="varArgsParameterTypes">Var args parameter types or null</param>
		public DmdMethodSignature(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType>? parameterTypes, IList<DmdType>? varArgsParameterTypes) {
			if (genericParameterCount < 0)
				throw new ArgumentOutOfRangeException(nameof(genericParameterCount));
			Flags = flags;
			GenericParameterCount = genericParameterCount;
			ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
			this.parameterTypes = ReadOnlyCollectionHelpers.Create(parameterTypes);
			this.varArgsParameterTypes = ReadOnlyCollectionHelpers.Create(varArgsParameterTypes);
		}

		/// <summary>
		/// Gets the parameter types, see also <see cref="GetVarArgsParameterTypes"/>
		/// </summary>
		/// <returns></returns>
		public ReadOnlyCollection<DmdType> GetParameterTypes() => parameterTypes;

		/// <summary>
		/// Gets the var args parameter types
		/// </summary>
		/// <returns></returns>
		public ReadOnlyCollection<DmdType> GetVarArgsParameterTypes() => varArgsParameterTypes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdMethodSignature? left, DmdMethodSignature? right) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
		public static bool operator !=(DmdMethodSignature? left, DmdMethodSignature? right) => !DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdMethodSignature? other) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdMethodSignature);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(this);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => DmdMemberFormatter.Format(this);
	}
}
