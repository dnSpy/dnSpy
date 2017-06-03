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
	/// A .NET event
	/// </summary>
	public abstract class DmdEventInfo : DmdMemberInfo, IEquatable<DmdEventInfo> {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public sealed override DmdAppDomain AppDomain => DeclaringType.AppDomain;

		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => DmdMemberTypes.Event;

		/// <summary>
		/// Gets the event attributes
		/// </summary>
		public abstract DmdEventAttributes Attributes { get; }

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public bool IsSpecialName => (Attributes & DmdEventAttributes.SpecialName) != 0;
		public bool IsRTSpecialName => (Attributes & DmdEventAttributes.RTSpecialName) != 0;
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Gets the event handler type
		/// </summary>
		public DmdType EventHandlerType {
			get {
				var delegateType = DeclaringType.Assembly.AppDomain.System_Delegate;
				foreach (var param in GetAddMethod(nonPublic: true)?.GetParameters() ?? Array.Empty<DmdParameterInfo>()) {
					var paramType = param.ParameterType;
					if (paramType.IsSubclassOf(delegateType))
						return paramType;
				}
				return null;
			}
		}

		/// <summary>
		/// true if it's a multi-cast delegate
		/// </summary>
		public bool IsMulticast {
			get {
				var multicastDelegate = DeclaringType.Assembly.AppDomain.System_MulticastDelegate;
				return multicastDelegate.IsAssignableFrom(EventHandlerType);
			}
		}

		/// <summary>
		/// Resolves a member reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo ResolveMember(bool throwOnError) => this;

		/// <summary>
		/// Gets the add method
		/// </summary>
		public DmdMethodInfo AddMethod => GetAddMethod(nonPublic: true);

		/// <summary>
		/// Gets the remove method
		/// </summary>
		public DmdMethodInfo RemoveMethod => GetRemoveMethod(nonPublic: true);

		/// <summary>
		/// Gets the raise method
		/// </summary>
		public DmdMethodInfo RaiseMethod => GetRaiseMethod(nonPublic: true);

		/// <summary>
		/// Gets all public 'other' methods
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo[] GetOtherMethods() => GetOtherMethods(nonPublic: false);

		/// <summary>
		/// Gets the public add method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo GetAddMethod() => GetAddMethod(nonPublic: false);

		/// <summary>
		/// Gets the public remove method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo GetRemoveMethod() => GetRemoveMethod(nonPublic: false);

		/// <summary>
		/// Gets the public raise method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo GetRaiseMethod() => GetRaiseMethod(nonPublic: false);

		/// <summary>
		/// Gets 'other' methods
		/// </summary>
		/// <param name="nonPublic">true to include all methods, false to only include public methods</param>
		/// <returns></returns>
		public abstract DmdMethodInfo[] GetOtherMethods(bool nonPublic);

		/// <summary>
		/// Gets the add method
		/// </summary>
		/// <param name="nonPublic">true to return any method, false to only return a public method</param>
		/// <returns></returns>
		public abstract DmdMethodInfo GetAddMethod(bool nonPublic);

		/// <summary>
		/// Gets the remove method
		/// </summary>
		/// <param name="nonPublic">true to return any method, false to only return a public method</param>
		/// <returns></returns>
		public abstract DmdMethodInfo GetRemoveMethod(bool nonPublic);

		/// <summary>
		/// Gets the raise method
		/// </summary>
		/// <param name="nonPublic">true to return any method, false to only return a public method</param>
		/// <returns></returns>
		public abstract DmdMethodInfo GetRaiseMethod(bool nonPublic);

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdEventInfo left, DmdEventInfo right) => DmdMemberInfoEqualityComparer.Default.Equals(left, right);
		public static bool operator !=(DmdEventInfo left, DmdEventInfo right) => !DmdMemberInfoEqualityComparer.Default.Equals(left, right);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdEventInfo other) => DmdMemberInfoEqualityComparer.Default.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as DmdEventInfo);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.Default.GetHashCode(this);
	}
}
