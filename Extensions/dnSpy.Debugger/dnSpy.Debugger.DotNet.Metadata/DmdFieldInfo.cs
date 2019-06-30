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
	/// A .NET field
	/// </summary>
	public abstract class DmdFieldInfo : DmdMemberInfo, IEquatable<DmdFieldInfo?> {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public override DmdAppDomain AppDomain => DeclaringType!.AppDomain;

		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => DmdMemberTypes.Field;

		/// <summary>
		/// Gets the field type
		/// </summary>
		public abstract DmdType FieldType { get; }

		/// <summary>
		/// Gets the field attributes
		/// </summary>
		public abstract DmdFieldAttributes Attributes { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public bool IsPublic => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.Public;
		public bool IsPrivate => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.Private;
		public bool IsFamily => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.Family;
		public bool IsAssembly => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.Assembly;
		public bool IsFamilyAndAssembly => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.FamANDAssem;
		public bool IsFamilyOrAssembly => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.FamORAssem;
		public bool IsPrivateScope => (Attributes & DmdFieldAttributes.FieldAccessMask) == DmdFieldAttributes.PrivateScope;
		public bool IsStatic => (Attributes & DmdFieldAttributes.Static) != 0;
		public bool IsInitOnly => (Attributes & DmdFieldAttributes.InitOnly) != 0;
		public bool IsLiteral => (Attributes & DmdFieldAttributes.Literal) != 0;
		public bool IsNotSerialized => (Attributes & DmdFieldAttributes.NotSerialized) != 0;
		public bool IsSpecialName => (Attributes & DmdFieldAttributes.SpecialName) != 0;
		public bool IsPinvokeImpl => (Attributes & DmdFieldAttributes.PinvokeImpl) != 0;
		public bool IsRTSpecialName => (Attributes & DmdFieldAttributes.RTSpecialName) != 0;
		public bool HasFieldMarshal => (Attributes & DmdFieldAttributes.HasFieldMarshal) != 0;
		public bool HasDefault => (Attributes & DmdFieldAttributes.HasDefault) != 0;
		public bool HasFieldRVA => (Attributes & DmdFieldAttributes.HasFieldRVA) != 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Gets the RVA of the data if <see cref="HasFieldRVA"/> is true
		/// </summary>
		public abstract uint FieldRVA { get; }

		/// <summary>
		/// Resolves a field reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo? ResolveMember(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a field reference and throws if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdFieldInfo Resolve() => Resolve(throwOnError: true)!;

		/// <summary>
		/// Resolves a field reference and returns null if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdFieldInfo? ResolveNoThrow() => Resolve(throwOnError: false);

		/// <summary>
		/// Resolves a field reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdFieldInfo? Resolve(bool throwOnError);

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
		public ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => FieldType.GetCustomModifiers();

		/// <summary>
		/// Gets the constant value stored in metadata if any exists
		/// </summary>
		/// <returns></returns>
		public abstract object? GetRawConstantValue();

		/// <summary>
		/// Gets the current value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static field</param>
		/// <returns></returns>
		public abstract object? GetValue(object? context, object? obj);

		/// <summary>
		/// Sets a new value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static field</param>
		/// <param name="value">New value</param>
		/// <param name="invokeAttr">Binding attributes</param>
		public abstract void SetValue(object? context, object? obj, object? value, DmdBindingFlags invokeAttr);

		/// <summary>
		/// Sets a new value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="obj">Instance or null if it's a static field</param>
		/// <param name="value">New value</param>
		public void SetValue(object? context, object? obj, object? value) => SetValue(context, obj, value, DmdBindingFlags.Default);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdFieldInfo? left, DmdFieldInfo? right) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
		public static bool operator !=(DmdFieldInfo? left, DmdFieldInfo? right) => !DmdMemberInfoEqualityComparer.DefaultMember.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdFieldInfo? other) => DmdMemberInfoEqualityComparer.DefaultMember.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdFieldInfo);

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
