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
using dnSpy.Debugger.DotNet.Metadata.Impl;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET method parameter
	/// </summary>
	public abstract class DmdParameterInfo : DmdObject, IDmdCustomAttributeProvider, IEquatable<DmdParameterInfo?> {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdParameterInfo"/>
		/// </summary>
		private protected abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Gets the parameter type
		/// </summary>
		public abstract DmdType ParameterType { get; }

		/// <summary>
		/// Gets the parameter name
		/// </summary>
		public abstract string? Name { get; }

		/// <summary>
		/// true if <see cref="RawDefaultValue"/> is valid
		/// </summary>
		public abstract bool HasDefaultValue { get; }

		/// <summary>
		/// Gets the default value, see also <see cref="HasDefaultValue"/>
		/// </summary>
		public abstract object? RawDefaultValue { get; }

		/// <summary>
		/// Gets the parameter index
		/// </summary>
		public abstract int Position { get; }

		/// <summary>
		/// Gets the parameter attributes
		/// </summary>
		public abstract DmdParameterAttributes Attributes { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public bool IsIn => (Attributes & DmdParameterAttributes.In) != 0;
		public bool IsOut => (Attributes & DmdParameterAttributes.Out) != 0;
		public bool IsLcid => (Attributes & DmdParameterAttributes.Lcid) != 0;
		public bool IsRetval => (Attributes & DmdParameterAttributes.Retval) != 0;
		public bool IsOptional => (Attributes & DmdParameterAttributes.Optional) != 0;
		public bool HasDefault => (Attributes & DmdParameterAttributes.HasDefault) != 0;
		public bool HasFieldMarshal => (Attributes & DmdParameterAttributes.HasFieldMarshal) != 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Gets the owner method or property
		/// </summary>
		public abstract DmdMemberInfo Member { get; }

		/// <summary>
		/// true if this is not the real method parameter since the declaring method is just a reference.
		/// Resolve the method to get the real method parameters.
		/// </summary>
		public bool IsMetadataReference => Member.IsMetadataReference;

		/// <summary>
		/// Gets the metadata token
		/// </summary>
		public abstract int MetadataToken { get; }

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
		public ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => ParameterType.GetCustomModifiers();

		/// <summary>
		/// Gets the custom attributes
		/// </summary>
		public ReadOnlyCollection<DmdCustomAttributeData> CustomAttributes => GetCustomAttributesData();

		/// <summary>
		/// Gets the custom attributes
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData();

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public bool IsDefined(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), attributeTypeFullName);

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public bool IsDefined(DmdType? attributeType, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), attributeType);

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public bool IsDefined(Type attributeType, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, Member.AppDomain));

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData? FindCustomAttribute(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), attributeTypeFullName);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData? FindCustomAttribute(DmdType? attributeType, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), attributeType);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData? FindCustomAttribute(Type attributeType, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, Member.AppDomain));

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdParameterInfo? left, DmdParameterInfo? right) => DmdMemberInfoEqualityComparer.DefaultParameter.Equals(left, right);
		public static bool operator !=(DmdParameterInfo? left, DmdParameterInfo? right) => !DmdMemberInfoEqualityComparer.DefaultParameter.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdParameterInfo? other) => DmdMemberInfoEqualityComparer.DefaultParameter.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdParameterInfo);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultParameter.GetHashCode(this);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => DmdMemberFormatter.Format(this);
	}
}
