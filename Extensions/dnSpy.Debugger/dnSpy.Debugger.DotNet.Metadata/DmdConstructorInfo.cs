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
	/// A .NET constructor
	/// </summary>
	public abstract class DmdConstructorInfo : DmdMethodBase, IEquatable<DmdConstructorInfo?> {
		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => DmdMemberTypes.Constructor;

		/// <summary>
		/// Resolves a constructor reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo? ResolveMember(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a constructor reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMethodBase? ResolveMethodBase(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a constructor reference and throws if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdConstructorInfo Resolve() => Resolve(throwOnError: true)!;

		/// <summary>
		/// Resolves a constructor reference and returns null if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdConstructorInfo? ResolveNoThrow() => Resolve(throwOnError: false);

		/// <summary>
		/// Resolves a constructor reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdConstructorInfo? Resolve(bool throwOnError);

		/// <summary>
		/// true if it contains generic parameters
		/// </summary>
		public override bool ContainsGenericParameters => DeclaringType!.ContainsGenericParameters;

		/// <summary>
		/// Calls the method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="parameters">Parameters</param>
		/// <returns></returns>
		public object? Invoke(object? context, object?[] parameters) => Invoke(context, DmdBindingFlags.Default, parameters);

		/// <summary>
		/// Calls the method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="invokeAttr">Binding flags</param>
		/// <param name="parameters">Parameters</param>
		/// <returns></returns>
		public abstract object? Invoke(object? context, DmdBindingFlags invokeAttr, object?[] parameters);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdConstructorInfo? left, DmdConstructorInfo? right) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
		public static bool operator !=(DmdConstructorInfo? left, DmdConstructorInfo? right) => !DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdConstructorInfo? other) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdConstructorInfo);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(this);

		/// <summary>
		/// Gets the name of instance constructors
		/// </summary>
		public static readonly string ConstructorName = ".ctor";

		/// <summary>
		/// Gets the name of type initializers
		/// </summary>
		public static readonly string TypeConstructorName = ".cctor";
	}
}
