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
using System.Collections.Generic;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Base class of all types, fields, methods, constructors, properties, events
	/// </summary>
	public abstract class DmdMemberInfo : DmdObject, IDmdCustomAttributeProvider, IEquatable<DmdMemberInfo> {
		/// <summary>
		/// Resolves a member reference and throws if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember() => ResolveMember(throwOnError: true);

		/// <summary>
		/// Resolves a member reference and returns null if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdMemberInfo ResolveMemberNoThrow() => ResolveMember(throwOnError: false);

		/// <summary>
		/// Resolves a member reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdMemberInfo ResolveMember(bool throwOnError);

		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the member type
		/// </summary>
		public abstract DmdMemberTypes MemberType { get; }

		/// <summary>
		/// Gets the member name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the declaring type. This is the type that declares the member, see also <see cref="ReflectedType"/>
		/// </summary>
		public abstract DmdType DeclaringType { get; }

		/// <summary>
		/// Gets the reflected type. This is the type that owns this member, see also <see cref="DeclaringType"/>
		/// </summary>
		public abstract DmdType ReflectedType { get; }

		/// <summary>
		/// Gets the metadata token
		/// </summary>
		public abstract int MetadataToken { get; }

		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract DmdModule Module { get; }

		/// <summary>
		/// true if it's a reference to another type or member, eg. a TypeRef, MemberRef
		/// </summary>
		public abstract bool IsMetadataReference { get; }

		/// <summary>
		/// Gets the custom attributes
		/// </summary>
		public IEnumerable<DmdCustomAttributeData> CustomAttributes => GetCustomAttributesData();

		/// <summary>
		/// Gets the custom attributes
		/// </summary>
		/// <returns></returns>
		public abstract IList<DmdCustomAttributeData> GetCustomAttributesData();

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public abstract bool IsDefined(string attributeTypeFullName, bool inherit);

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public abstract bool IsDefined(DmdType attributeType, bool inherit);

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdMemberInfo left, DmdMemberInfo right) => DmdMemberInfoEqualityComparer.Default.Equals(left, right);
		public static bool operator !=(DmdMemberInfo left, DmdMemberInfo right) => !DmdMemberInfoEqualityComparer.Default.Equals(left, right);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdMemberInfo other) => DmdMemberInfoEqualityComparer.Default.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public abstract override bool Equals(object obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public abstract override int GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public abstract override string ToString();
	}
}
