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
using System.Collections.ObjectModel;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET property
	/// </summary>
	public abstract class DmdPropertyInfo : DmdMemberInfo, IEquatable<DmdPropertyInfo?> {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public sealed override DmdAppDomain AppDomain => DeclaringType!.AppDomain;

		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => DmdMemberTypes.Property;

		/// <summary>
		/// Gets the property type
		/// </summary>
		public abstract DmdType PropertyType { get; }

		/// <summary>
		/// Gets the property attributes
		/// </summary>
		public abstract DmdPropertyAttributes Attributes { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public bool IsSpecialName => (Attributes & DmdPropertyAttributes.SpecialName) != 0;
		public bool IsRTSpecialName => (Attributes & DmdPropertyAttributes.RTSpecialName) != 0;
		public bool HasDefault => (Attributes & DmdPropertyAttributes.HasDefault) != 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// true if the property can be read
		/// </summary>
		public bool CanRead => GetMethod is not null;

		/// <summary>
		/// true if the property can be written to
		/// </summary>
		public bool CanWrite => SetMethod is not null;

		/// <summary>
		/// Resolves a property reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo? ResolveMember(bool throwOnError) => this;

		/// <summary>
		/// Returns false since there are no property references
		/// </summary>
		public sealed override bool IsMetadataReference => false;

		/// <summary>
		/// Gets the constant stored in metadata
		/// </summary>
		/// <returns></returns>
		public abstract object? GetRawConstantValue();

		/// <summary>
		/// Gets all accessors
		/// </summary>
		/// <param name="nonPublic">true to include all accessors, false to only include public accessors</param>
		/// <returns></returns>
		public DmdMethodInfo[] GetAccessors(bool nonPublic) => GetAccessors(nonPublic ? DmdGetAccessorOptions.NonPublic : DmdGetAccessorOptions.None);

		/// <summary>
		/// Gets all accessors
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdMethodInfo[] GetAccessors(DmdGetAccessorOptions options);

		/// <summary>
		/// Gets the get method
		/// </summary>
		/// <param name="nonPublic">true to return any get method, false to only return a public get method</param>
		/// <returns></returns>
		public DmdMethodInfo? GetGetMethod(bool nonPublic) => GetGetMethod(nonPublic ? DmdGetAccessorOptions.NonPublic : DmdGetAccessorOptions.None);

		/// <summary>
		/// Gets the get method
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdMethodInfo? GetGetMethod(DmdGetAccessorOptions options);

		/// <summary>
		/// Gets the set method
		/// </summary>
		/// <param name="nonPublic">true to return any set method, false to only return a public set method</param>
		/// <returns></returns>
		public DmdMethodInfo? GetSetMethod(bool nonPublic) => GetSetMethod(nonPublic ? DmdGetAccessorOptions.NonPublic : DmdGetAccessorOptions.None);

		/// <summary>
		/// Gets the set method
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdMethodInfo? GetSetMethod(DmdGetAccessorOptions options);

		/// <summary>
		/// Gets the index parameters
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdParameterInfo> GetIndexParameters();

		/// <summary>
		/// Gets all required custom modifiers
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetRequiredCustomModifiers() => DmdCustomModifierUtilities.GetModifiers(GetCustomModifiers(), requiredModifiers: true);

		/// <summary>
		/// Gets all optional custom modifiers
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetOptionalCustomModifiers() => DmdCustomModifierUtilities.GetModifiers(GetCustomModifiers(), requiredModifiers: false);

		/// <summary>
		/// Gets all custom modifiers
		/// </summary>
		/// <returns></returns>
		public ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => PropertyType.GetCustomModifiers();

		/// <summary>
		/// Gets all public accessors
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo[] GetAccessors() => GetAccessors(nonPublic: false);

		/// <summary>
		/// Gets the get method
		/// </summary>
		public DmdMethodInfo? GetMethod => GetGetMethod(nonPublic: true);

		/// <summary>
		/// Gets the set method
		/// </summary>
		public DmdMethodInfo? SetMethod => GetSetMethod(nonPublic: true);

		/// <summary>
		/// Gets the public get method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo? GetGetMethod() => GetGetMethod(nonPublic: false);

		/// <summary>
		/// Gets the public set method
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo? GetSetMethod() => GetSetMethod(nonPublic: false);

		/// <summary>
		/// Gets the method signature
		/// </summary>
		/// <returns></returns>
		public abstract DmdMethodSignature GetMethodSignature();

		/// <summary>
		/// Gets the property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <returns></returns>
		public object? GetValue(object? context, object? obj) => GetValue(context, obj, null);

		/// <summary>
		/// Gets the property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <param name="index">Property indexes</param>
		/// <returns></returns>
		public object? GetValue(object? context, object? obj, object?[]? index) => GetValue(context, obj, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public | DmdBindingFlags.NonPublic, index);

		/// <summary>
		/// Gets the property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <param name="invokeAttr">Binding flags</param>
		/// <param name="index">Property indexes</param>
		/// <returns></returns>
		public object? GetValue(object? context, object? obj, DmdBindingFlags invokeAttr, object?[]? index) {
			var method = GetGetMethod(nonPublic: true);
			if (method is null)
				throw new ArgumentException();
			return method.Invoke(context, obj, invokeAttr, index);
		}

		/// <summary>
		/// Writes a new property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <param name="value">New value</param>
		public void SetValue(object? context, object? obj, object? value) => SetValue(context, obj, value, null);

		/// <summary>
		/// Writes a new property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <param name="value">New value</param>
		/// <param name="index">Property indexes</param>
		public void SetValue(object? context, object? obj, object? value, object?[]? index) => SetValue(context, obj, value, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public | DmdBindingFlags.NonPublic, index);

		/// <summary>
		/// Writes a new property value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static property</param>
		/// <param name="value">New value</param>
		/// <param name="invokeAttr">Binding flags</param>
		/// <param name="index">Property indexes</param>
		public void SetValue(object? context, object? obj, object? value, DmdBindingFlags invokeAttr, object?[]? index) {
			var method = GetSetMethod(nonPublic: true);
			if (method is null)
				throw new ArgumentException();
			object?[] parameters;
			if (index is null || index.Length == 0)
				parameters = new[] { value };
			else {
				parameters = new object[index.Length + 1];
				int i = 0;
				for (; i < index.Length; i++)
					parameters[i] = index[i];
				parameters[i] = value;
			}
			method.Invoke(context, obj, invokeAttr, parameters);
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdPropertyInfo? left, DmdPropertyInfo? right) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
		public static bool operator !=(DmdPropertyInfo? left, DmdPropertyInfo? right) => !DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdPropertyInfo? other) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdPropertyInfo);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(this);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public sealed override string? ToString() => DmdMemberFormatter.Format(this);
	}
}
