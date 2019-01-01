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
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata.Impl;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET method
	/// </summary>
	public abstract class DmdMethodInfo : DmdMethodBase, IEquatable<DmdMethodInfo> {
		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => DmdMemberTypes.Method;

		/// <summary>
		/// Resolves a member reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo ResolveMember(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a method reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMethodBase ResolveMethodBase(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a method reference and throws if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo Resolve() => Resolve(throwOnError: true);

		/// <summary>
		/// Resolves a method reference and returns null if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo ResolveNoThrow() => Resolve(throwOnError: false);

		/// <summary>
		/// Resolves a method reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdMethodInfo Resolve(bool throwOnError);

		/// <summary>
		/// Gets the return type
		/// </summary>
		public abstract DmdType ReturnType { get; }

		/// <summary>
		/// Gets the return parameter
		/// </summary>
		public abstract DmdParameterInfo ReturnParameter { get; }

		/// <summary>
		/// Gets the return type's custom attributes
		/// </summary>
		public IDmdCustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;

		/// <summary>
		/// true if it contains generic parameters
		/// </summary>
		public override bool ContainsGenericParameters {
			get {
				if (DeclaringType.ContainsGenericParameters)
					return true;
				if (!IsGenericMethod)
					return false;
				foreach (var genArg in GetGenericArguments()) {
					if (genArg.ContainsGenericParameters)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the base method definition or itself if it doesn't override a method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo GetBaseDefinition() {
			if (!IsVirtual && !IsAbstract)
				return this;
			if (DeclaringType.IsInterface)
				return this;
			var method = this;
			for (;;) {
				var parentMethod = method.GetParentDefinition();
				if ((object)parentMethod == null) {
					Debug.Assert((object)ReflectedType == method.ReflectedType);
					if ((object)method.DeclaringType == method.ReflectedType)
						return method;
					return method.DeclaringType.GetMethod(method.Module, method.MetadataToken) as DmdMethodInfo ?? throw new InvalidOperationException();
				}
				method = parentMethod;
			}
		}

		/// <summary>
		/// Gets the parent method
		/// </summary>
		/// <returns></returns>
		internal abstract DmdMethodInfo GetParentDefinition();

		/// <summary>
		/// Gets all generic arguments if it's a generic method
		/// </summary>
		/// <returns></returns>
		public abstract override ReadOnlyCollection<DmdType> GetGenericArguments();

		/// <summary>
		/// Gets the generic method definition
		/// </summary>
		/// <returns></returns>
		public abstract DmdMethodInfo GetGenericMethodDefinition();

		/// <summary>
		/// Creates a generic method
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdMethodInfo MakeGenericMethod(params DmdType[] typeArguments) => MakeGenericMethod((IList<DmdType>)typeArguments);

		/// <summary>
		/// Creates a generic method
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdMethodInfo MakeGenericMethod(params Type[] typeArguments) => MakeGenericMethod((IList<Type>)typeArguments);

		/// <summary>
		/// Creates a generic method
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public abstract DmdMethodInfo MakeGenericMethod(IList<DmdType> typeArguments);

		/// <summary>
		/// Creates a generic method
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdMethodInfo MakeGenericMethod(IList<Type> typeArguments) => MakeGenericMethod(typeArguments.ToDmdType(AppDomain));

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override bool IsDefined(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.IsDefined(this, attributeTypeFullName, inherit);

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override bool IsDefined(DmdType attributeType, bool inherit) => CustomAttributesHelper.IsDefined(this, attributeType, inherit);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override DmdCustomAttributeData FindCustomAttribute(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.Find(this, attributeTypeFullName, inherit);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override DmdCustomAttributeData FindCustomAttribute(DmdType attributeType, bool inherit) => CustomAttributesHelper.Find(this, attributeType, inherit);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdMethodInfo left, DmdMethodInfo right) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
		public static bool operator !=(DmdMethodInfo left, DmdMethodInfo right) => !DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdMethodInfo other) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as DmdMethodInfo);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(this);
	}
}
