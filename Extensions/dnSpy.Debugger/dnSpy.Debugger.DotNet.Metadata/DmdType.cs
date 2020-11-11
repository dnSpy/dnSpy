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
using System.Runtime.InteropServices;
using dnSpy.Debugger.DotNet.Metadata.Impl;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET type
	/// </summary>
	public abstract partial class DmdType : DmdMemberInfo, IEquatable<DmdType?> {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public override DmdAppDomain AppDomain => Assembly.AppDomain;

		/// <summary>
		/// Gets the member type
		/// </summary>
		public sealed override DmdMemberTypes MemberType => (TypeSignatureKind == DmdTypeSignatureKind.Type || TypeSignatureKind == DmdTypeSignatureKind.GenericInstance) && DeclaringType is not null ? DmdMemberTypes.NestedType : DmdMemberTypes.TypeInfo;

		/// <summary>
		/// Gets the type signature kind
		/// </summary>
		public abstract DmdTypeSignatureKind TypeSignatureKind { get; }

		/// <summary>
		/// Gets the type scope. This property is only valid if it's a TypeDef or TypeRef (i.e., not an array, generic instance, etc)
		/// </summary>
		public abstract DmdTypeScope TypeScope { get; }

		/// <summary>
		/// Gets the reflected type. This is the type that owns this member, see also <see cref="DmdMemberInfo.DeclaringType"/>
		/// </summary>
		public override DmdType? ReflectedType => DeclaringType;

		/// <summary>
		/// Gets the declaring method or null
		/// </summary>
		public abstract DmdMethodBase? DeclaringMethod { get; }

		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract override DmdModule Module { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		public abstract DmdAssembly Assembly { get; }

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string? FullName => DmdMemberFormatter.FormatFullName(this);

		/// <summary>
		/// Gets the namespace or null
		/// </summary>
		public abstract string? Namespace { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		public sealed override string Name => DmdMemberFormatter.FormatName(this);

		/// <summary>
		/// Gets the namespace or null. This is the namespace stored in the metadata. <see cref="Namespace"/>
		/// is the namespace of the non-declaring type.
		/// </summary>
		public abstract string? MetadataNamespace { get; }

		/// <summary>
		/// Gets the name stored in the metadata. It's not escaped like <see cref="DmdMemberInfo.Name"/>
		/// </summary>
		public abstract string? MetadataName { get; }

		/// <summary>
		/// Gets the assembly qualified name
		/// </summary>
		public string? AssemblyQualifiedName => DmdMemberFormatter.FormatAssemblyQualifiedName(this);

		/// <summary>
		/// Gets the base type or null if none
		/// </summary>
		public abstract DmdType? BaseType { get; }

		/// <summary>
		/// Gets the struct layout attribute
		/// </summary>
		public abstract StructLayoutAttribute? StructLayoutAttribute { get; }

		/// <summary>
		/// true if it's a nested type
		/// </summary>
		public bool IsNested => DeclaringType is not null;

		/// <summary>
		/// Gets the generic parameter attributes
		/// </summary>
		public abstract DmdGenericParameterAttributes GenericParameterAttributes { get; }

		/// <summary>
		/// true if this is a public type and all its declaring types are public
		/// </summary>
		public bool IsVisible => CalculateIsVisible(this);
		static bool CalculateIsVisible(DmdType type) {
			while (type.GetElementType() is DmdType elementType)
				type = elementType;
			if (type.IsGenericParameter)
				return true;
			for (;;) {
				var declType = type.DeclaringType;
				if (declType is null)
					break;
				if (!type.IsNestedPublic)
					return false;
				type = declType;
			}
			if (!type.IsPublic)
				return false;
			if (type.IsConstructedGenericType) {
				foreach (var genArg in type.GetGenericArguments()) {
					if (!genArg.IsVisible)
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the type attributes
		/// </summary>
		public abstract DmdTypeAttributes Attributes { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public bool IsNotPublic => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NotPublic;
		public bool IsPublic => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.Public;
		public bool IsNestedPublic => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedPublic;
		public bool IsNestedPrivate => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedPrivate;
		public bool IsNestedFamily => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedFamily;
		public bool IsNestedAssembly => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedAssembly;
		public bool IsNestedFamANDAssem => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedFamANDAssem;
		public bool IsNestedFamORAssem => (Attributes & DmdTypeAttributes.VisibilityMask) == DmdTypeAttributes.NestedFamORAssem;
		public bool IsAutoLayout => (Attributes & DmdTypeAttributes.LayoutMask) == DmdTypeAttributes.AutoLayout;
		public bool IsLayoutSequential => (Attributes & DmdTypeAttributes.LayoutMask) == DmdTypeAttributes.SequentialLayout;
		public bool IsExplicitLayout => (Attributes & DmdTypeAttributes.LayoutMask) == DmdTypeAttributes.ExplicitLayout;
		public bool IsClass => (Attributes & DmdTypeAttributes.ClassSemanticsMask) == DmdTypeAttributes.Class && !IsValueType;
		public bool IsInterface => (Attributes & DmdTypeAttributes.ClassSemanticsMask) == DmdTypeAttributes.Interface;
		public bool IsValueType => BaseType is DmdType baseType && (baseType == AppDomain.System_ValueType ? this != AppDomain.System_Enum : baseType == AppDomain.System_Enum);
		public bool IsAbstract => (Attributes & DmdTypeAttributes.Abstract) != 0;
		public bool IsSealed => (Attributes & DmdTypeAttributes.Sealed) != 0;
		public bool IsEnum => BaseType == AppDomain.System_Enum;
		public bool IsSpecialName => (Attributes & DmdTypeAttributes.SpecialName) != 0;
		public bool IsImport => (Attributes & DmdTypeAttributes.Import) != 0;
		public bool IsWindowsRuntime => (Attributes & DmdTypeAttributes.WindowsRuntime) != 0;
		public bool IsBeforeFieldInit => (Attributes & DmdTypeAttributes.BeforeFieldInit) != 0;
		public bool IsForwarder => (Attributes & DmdTypeAttributes.Forwarder) != 0;
		public bool IsRTSpecialName => (Attributes & DmdTypeAttributes.RTSpecialName) != 0;
		public bool HasSecurity => (Attributes & DmdTypeAttributes.HasSecurity) != 0;
		public bool IsAnsiClass => (Attributes & DmdTypeAttributes.StringFormatMask) == DmdTypeAttributes.AnsiClass;
		public bool IsUnicodeClass => (Attributes & DmdTypeAttributes.StringFormatMask) == DmdTypeAttributes.UnicodeClass;
		public bool IsAutoClass => (Attributes & DmdTypeAttributes.StringFormatMask) == DmdTypeAttributes.AutoClass;
		public bool IsCustomFormatClass => (Attributes & DmdTypeAttributes.StringFormatMask) == DmdTypeAttributes.CustomFormatClass;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// true if this type is <see cref="Nullable{T}"/>
		/// </summary>
		public bool IsNullable {
			get {
				switch (TypeSignatureKind) {
				case DmdTypeSignatureKind.GenericInstance:
					return GetGenericTypeDefinition() == AppDomain.System_Nullable_T;

				case DmdTypeSignatureKind.Type:
					return this == AppDomain.System_Nullable_T;

				default:
					return false;
				}
			}
		}

		/// <summary>
		/// Gets the nullable value type, eg. <see cref="int"/> if it's a nullable <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		public DmdType GetNullableElementType() {
			if (!IsNullable)
				throw new ArgumentException();
			var genArgs = GetGenericArguments();
			Debug.Assert(genArgs.Count == 1);
			if (genArgs.Count != 1)
				throw new ArgumentException();
			return genArgs[0];
		}

		/// <summary>
		/// true if it's a serializable type
		/// </summary>
		public bool IsSerializable {
			get {
				if ((Attributes & DmdTypeAttributes.Serializable) != 0)
					return true;
				var systemDelegate = AppDomain.System_Delegate;
				var systemEnum = AppDomain.System_Enum;
				for (DmdType? type = this; type is not null; type = type.BaseType) {
					if (type == systemDelegate || type == systemEnum)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Only used by CanCastTo(). true if it or any of the types it depends on (base type, generic args,
		/// element type, interfaces) has the TypeIdentifierAttribute or similar attributes that enable type
		/// equivalency checks.
		/// </summary>
		internal abstract bool HasTypeEquivalence { get; }

		/// <summary>
		/// Resolves a member reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public sealed override DmdMemberInfo? ResolveMember(bool throwOnError) => Resolve(throwOnError);

		/// <summary>
		/// Resolves a type reference and throws if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdType Resolve() => Resolve(throwOnError: true)!;

		/// <summary>
		/// Resolves a type reference and returns null if it doesn't exist
		/// </summary>
		/// <returns></returns>
		public DmdType? ResolveNoThrow() => Resolve(throwOnError: false);

		/// <summary>
		/// Resolves a type reference
		/// </summary>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdType? Resolve(bool throwOnError);

		/// <summary>
		/// Makes a pointer type
		/// </summary>
		/// <returns></returns>
		public abstract DmdType MakePointerType();

		/// <summary>
		/// Makes a by-ref type
		/// </summary>
		/// <returns></returns>
		public abstract DmdType MakeByRefType();

		/// <summary>
		/// Makes a SZ array type
		/// </summary>
		/// <returns></returns>
		public abstract DmdType MakeArrayType();

		/// <summary>
		/// Makes a multi-dimensional array type
		/// </summary>
		/// <param name="rank">Number of dimensions</param>
		/// <returns></returns>
		public DmdType MakeArrayType(int rank) => MakeArrayType(rank, Array.Empty<int>(), Array.Empty<int>());

		/// <summary>
		/// Makes a multi-dimensional array type
		/// </summary>
		/// <param name="rank">Number of dimensions</param>
		/// <param name="sizes">Sizes</param>
		/// <param name="lowerBounds">Lower bounds</param>
		/// <returns></returns>
		public abstract DmdType MakeArrayType(int rank, IList<int> sizes, IList<int> lowerBounds);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdType MakeGenericType(params DmdType[] typeArguments) => MakeGenericType((IList<DmdType>)typeArguments);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdType MakeGenericType(params Type[] typeArguments) => MakeGenericType((IList<Type>)typeArguments);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericType(IList<DmdType> typeArguments);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="typeArguments">Generic arguments</param>
		/// <returns></returns>
		public DmdType MakeGenericType(IList<Type> typeArguments) => MakeGenericType(typeArguments.ToDmdTypeNoNull(AppDomain));

		/// <summary>
		/// Gets the type code
		/// </summary>
		/// <param name="type">Type or null</param>
		/// <returns></returns>
		public static TypeCode GetTypeCode(DmdType? type) {
			if (type is null) return TypeCode.Empty;
			const TypeCode defaultValue = TypeCode.Object;
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();
			if (type.MetadataNamespace == "System" && !type.IsNested) {
				switch (type.MetadataName) {
				case "Boolean":	return type == type.AppDomain.System_Boolean	? TypeCode.Boolean	: defaultValue;
				case "Char":	return type == type.AppDomain.System_Char		? TypeCode.Char		: defaultValue;
				case "SByte":	return type == type.AppDomain.System_SByte		? TypeCode.SByte	: defaultValue;
				case "Byte":	return type == type.AppDomain.System_Byte		? TypeCode.Byte		: defaultValue;
				case "Int16":	return type == type.AppDomain.System_Int16		? TypeCode.Int16	: defaultValue;
				case "UInt16":	return type == type.AppDomain.System_UInt16		? TypeCode.UInt16	: defaultValue;
				case "Int32":	return type == type.AppDomain.System_Int32		? TypeCode.Int32	: defaultValue;
				case "UInt32":	return type == type.AppDomain.System_UInt32		? TypeCode.UInt32	: defaultValue;
				case "Int64":	return type == type.AppDomain.System_Int64		? TypeCode.Int64	: defaultValue;
				case "UInt64":	return type == type.AppDomain.System_UInt64		? TypeCode.UInt64	: defaultValue;
				case "Single":	return type == type.AppDomain.System_Single		? TypeCode.Single	: defaultValue;
				case "Double":	return type == type.AppDomain.System_Double		? TypeCode.Double	: defaultValue;
				case "Decimal":	return type == type.AppDomain.System_Decimal	? TypeCode.Decimal	: defaultValue;
				case "DateTime":return type == type.AppDomain.System_DateTime	? TypeCode.DateTime	: defaultValue;
				case "String":	return type == type.AppDomain.System_String		? TypeCode.String	: defaultValue;
				case "DBNull":
					if (type == type.AppDomain.GetWellKnownType(DmdWellKnownType.System_DBNull, isOptional: true))
						return TypeCode.DBNull;
					break;
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Gets all fields
		/// </summary>
		public abstract IEnumerable<DmdFieldInfo> Fields { get; }

		/// <summary>
		/// Gets all methods and constructors
		/// </summary>
		public abstract IEnumerable<DmdMethodBase> Methods { get; }

		/// <summary>
		/// Gets all properties
		/// </summary>
		public abstract IEnumerable<DmdPropertyInfo> Properties { get; }

		/// <summary>
		/// Gets all events
		/// </summary>
		public abstract IEnumerable<DmdEventInfo> Events { get; }

		/// <summary>
		/// Gets all declared fields
		/// </summary>
		public abstract ReadOnlyCollection<DmdFieldInfo> DeclaredFields { get; }

		/// <summary>
		/// Gets all declared methods and constructors
		/// </summary>
		public abstract ReadOnlyCollection<DmdMethodBase> DeclaredMethods { get; }

		/// <summary>
		/// Gets all declared properties
		/// </summary>
		public abstract ReadOnlyCollection<DmdPropertyInfo> DeclaredProperties { get; }

		/// <summary>
		/// Gets all declared events
		/// </summary>
		public abstract ReadOnlyCollection<DmdEventInfo> DeclaredEvents { get; }

		/// <summary>
		/// Gets all nested types
		/// </summary>
		public abstract ReadOnlyCollection<DmdType> NestedTypes { get; }

		/// <summary>
		/// Gets a method or returns null if it doesn't exist
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <returns></returns>
		public DmdMethodBase? GetMethod(DmdModule module, int metadataToken) => GetMethod(module, metadataToken, throwOnError: false);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdMethodBase? GetMethod(DmdModule module, int metadataToken, bool throwOnError);

		/// <summary>
		/// Gets a field or returns null if it doesn't exist
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <returns></returns>
		public DmdFieldInfo? GetField(DmdModule module, int metadataToken) => GetField(module, metadataToken, throwOnError: false);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdFieldInfo? GetField(DmdModule module, int metadataToken, bool throwOnError);

		/// <summary>
		/// Gets a property or returns null if it doesn't exist
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(DmdModule module, int metadataToken) => GetProperty(module, metadataToken, throwOnError: false);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdPropertyInfo? GetProperty(DmdModule module, int metadataToken, bool throwOnError);

		/// <summary>
		/// Gets an event or returns null if it doesn't exist
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <returns></returns>
		public DmdEventInfo? GetEvent(DmdModule module, int metadataToken) => GetEvent(module, metadataToken, throwOnError: false);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="metadataToken">Metadata token</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdEventInfo? GetEvent(DmdModule module, int metadataToken, bool throwOnError);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSignature">Method signature</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdMethodBase? GetMethod(string name, DmdMethodSignature methodSignature, bool throwOnError) {
			if (methodSignature is null)
				throw new ArgumentNullException(nameof(methodSignature));
			return GetMethod(name, methodSignature.Flags, methodSignature.GenericParameterCount, methodSignature.ReturnType, methodSignature.GetParameterTypes(), throwOnError);
		}

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="flags">Method signature flags</param>
		/// <param name="genericParameterCount">Generic parameter count</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdMethodBase? GetMethod(string name, DmdSignatureCallingConvention flags, int genericParameterCount, DmdType? returnType, IList<DmdType> parameterTypes, bool throwOnError);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="flags">Method signature flags</param>
		/// <param name="genericParameterCount">Generic parameter count</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdMethodBase? GetMethod(string name, DmdSignatureCallingConvention flags, int genericParameterCount, Type? returnType, IList<Type> parameterTypes, bool throwOnError) =>
			GetMethod(name, flags, genericParameterCount, DmdTypeUtilities.ToDmdType(returnType, AppDomain), parameterTypes.ToDmdTypeNoNull(AppDomain), throwOnError);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdMethodBase? GetMethod(string name, DmdType? returnType, IList<DmdType> parameterTypes, bool throwOnError) =>
			GetMethod(name, DmdSignatureCallingConvention.Default, 0, returnType, parameterTypes, throwOnError);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdMethodBase? GetMethod(string name, Type? returnType, IList<Type> parameterTypes, bool throwOnError) =>
			GetMethod(name, DmdSignatureCallingConvention.Default, 0, DmdTypeUtilities.ToDmdType(returnType, AppDomain), parameterTypes.ToDmdTypeNoNull(AppDomain), throwOnError);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="fieldType">Field type</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdFieldInfo? GetField(string name, DmdType fieldType, bool throwOnError);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="fieldType">Field type</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdFieldInfo? GetField(string name, Type fieldType, bool throwOnError) =>
			GetField(name, DmdTypeUtilities.ToDmdTypeNoNull(fieldType, AppDomain), throwOnError);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="methodSignature">Method signature</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdMethodSignature methodSignature, bool throwOnError) {
			if (methodSignature is null)
				throw new ArgumentNullException(nameof(methodSignature));
			return GetProperty(name, methodSignature.Flags, methodSignature.GenericParameterCount, methodSignature.ReturnType, methodSignature.GetParameterTypes(), throwOnError);
		}

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="flags">Property signature flags</param>
		/// <param name="genericParameterCount">Generic parameter count</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdPropertyInfo? GetProperty(string name, DmdSignatureCallingConvention flags, int genericParameterCount, DmdType? returnType, IList<DmdType> parameterTypes, bool throwOnError);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="flags">Property signature flags</param>
		/// <param name="genericParameterCount">Generic parameter count</param>
		/// <param name="returnType">Return type or null to ignore it</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdSignatureCallingConvention flags, int genericParameterCount, Type? returnType, IList<Type> parameterTypes, bool throwOnError) =>
			GetProperty(name, flags, genericParameterCount, DmdTypeUtilities.ToDmdType(returnType, AppDomain), parameterTypes.ToDmdTypeNoNull(AppDomain), throwOnError);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="eventHandlerType">Event handler type</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public abstract DmdEventInfo? GetEvent(string name, DmdType eventHandlerType, bool throwOnError);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="eventHandlerType">Event handler type</param>
		/// <param name="throwOnError">true to throw if it doesn't exist, false to return null if it doesn't exist</param>
		/// <returns></returns>
		public DmdEventInfo? GetEvent(string name, Type eventHandlerType, bool throwOnError) =>
			GetEvent(name, DmdTypeUtilities.ToDmdTypeNoNull(eventHandlerType, AppDomain), throwOnError);

		/// <summary>
		/// Gets a constructor
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public abstract DmdConstructorInfo? GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types);

		/// <summary>
		/// Gets a constructor
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public DmdConstructorInfo? GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<Type> types) =>
			GetConstructor(bindingAttr, callConvention, types.ToDmdTypeNoNull(AppDomain));

		/// <summary>
		/// Gets a constructor
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public DmdConstructorInfo? GetConstructor(DmdBindingFlags bindingAttr, IList<DmdType> types) => GetConstructor(bindingAttr, DmdCallingConventions.Any, types);

		/// <summary>
		/// Gets a constructor
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public DmdConstructorInfo? GetConstructor(DmdBindingFlags bindingAttr, IList<Type> types) => GetConstructor(bindingAttr, DmdCallingConventions.Any, types.ToDmdTypeNoNull(AppDomain));

		/// <summary>
		/// Gets a public constructor
		/// </summary>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public DmdConstructorInfo? GetConstructor(IList<DmdType> types) => GetConstructor(DmdBindingFlags.Instance | DmdBindingFlags.Public, types);

		/// <summary>
		/// Gets a public constructor
		/// </summary>
		/// <param name="types">Parameter types</param>
		/// <returns></returns>
		public DmdConstructorInfo? GetConstructor(IList<Type> types) => GetConstructor(DmdBindingFlags.Instance | DmdBindingFlags.Public, types.ToDmdTypeNoNull(AppDomain));

		/// <summary>
		/// Gets all public constructors
		/// </summary>
		/// <returns></returns>
		public DmdConstructorInfo[] GetConstructors() => GetConstructors(DmdBindingFlags.Instance | DmdBindingFlags.Public);

		/// <summary>
		/// Gets constructors
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets the type initializer
		/// </summary>
		public DmdConstructorInfo? TypeInitializer => GetConstructor(DmdBindingFlags.Static | DmdBindingFlags.Public | DmdBindingFlags.NonPublic, DmdCallingConventions.Any, Array.Empty<DmdType>());

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public abstract DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType>? types);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<Type>? types) =>
			GetMethod(name, bindingAttr, callConvention, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr, IList<DmdType>? types) => GetMethod(name, bindingAttr, DmdCallingConventions.Any, types);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr, IList<Type>? types) => GetMethod(name, bindingAttr, DmdCallingConventions.Any, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a public static or instance method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, IList<DmdType>? types) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, types);

		/// <summary>
		/// Gets a public static or instance method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, IList<Type>? types) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr) => GetMethod(name, bindingAttr, DmdCallingConventions.Any, (IList<DmdType>?)null);

		/// <summary>
		/// Gets a public static or instance method
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdMethodInfo? GetMethod(string name) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, (IList<DmdType>?)null);

		/// <summary>
		/// Gets all public static or instance methods
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo[] GetMethods() => GetMethods(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all methods
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdFieldInfo? GetField(string name, DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets a public static or instance field
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdFieldInfo? GetField(string name) => GetField(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all public static or instance fields
		/// </summary>
		/// <returns></returns>
		public DmdFieldInfo[] GetFields() => GetFields(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all fields
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets an interface
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdType? GetInterface(string name) => GetInterface(name, false);

		/// <summary>
		/// Gets an interface
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="ignoreCase">true if ignore case</param>
		/// <returns></returns>
		public abstract DmdType? GetInterface(string name, bool ignoreCase);

		/// <summary>
		/// Gets all interfaces
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdType> GetInterfaces();

		/// <summary>
		/// Gets a public static or instance event
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdEventInfo? GetEvent(string name) => GetEvent(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdEventInfo? GetEvent(string name, DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets all public static or instance events
		/// </summary>
		/// <returns></returns>
		public DmdEventInfo[] GetEvents() => GetEvents(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all events
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="returnType">Return type or null</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public abstract DmdPropertyInfo? GetProperty(string name, DmdBindingFlags bindingAttr, DmdType? returnType, IList<DmdType>? types);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <param name="returnType">Return type</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdBindingFlags bindingAttr, Type returnType, IList<Type>? types) =>
			GetProperty(name, bindingAttr, DmdTypeUtilities.ToDmdType(returnType, AppDomain), types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdBindingFlags bindingAttr) => GetProperty(name, bindingAttr, null, (IList<DmdType>?)null);

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="returnType">Return type or null</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdType? returnType, IList<DmdType>? types) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, returnType, types);

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="returnType">Return type or null</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, Type? returnType, IList<Type>? types) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdTypeUtilities.ToDmdType(returnType, AppDomain), types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, IList<DmdType>? types) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, null, types);

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="types">Parameter types or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, IList<Type>? types) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, null, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="returnType">Return type or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, DmdType? returnType) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, returnType, null);

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="returnType">Return type or null</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name, Type? returnType) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdTypeUtilities.ToDmdType(returnType, AppDomain), null);

		/// <summary>
		/// Gets a public static or instance property
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdPropertyInfo? GetProperty(string name) => GetProperty(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, null, (IList<DmdType>?)null);

		/// <summary>
		/// Gets all properties
		/// </summary>
		/// <param name="bindingAttr">Bindig flags</param>
		/// <returns></returns>
		public abstract DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets all public static or instance properties
		/// </summary>
		/// <returns></returns>
		public DmdPropertyInfo[] GetProperties() => GetProperties(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all public nested types
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetNestedTypes() => GetNestedTypes(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all nested types
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdType[] GetNestedTypes(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets a public nested type
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdType? GetNestedType(string name) => GetNestedType(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets a nested type
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdType? GetNestedType(string name, DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets a public static or instance member
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public DmdMemberInfo[] GetMember(string name) => GetMember(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets a public static or instance member
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public DmdMemberInfo[] GetMember(string name, DmdBindingFlags bindingAttr) => GetMember(name, DmdMemberTypes.All, bindingAttr);

		/// <summary>
		/// Gets members
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="type">Member type</param>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets all public static or instance members
		/// </summary>
		/// <returns></returns>
		public DmdMemberInfo[] GetMembers() => GetMembers(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all members
		/// </summary>
		/// <param name="bindingAttr">Binding flags</param>
		/// <returns></returns>
		public abstract DmdMemberInfo[] GetMembers(DmdBindingFlags bindingAttr);

		/// <summary>
		/// Gets all default members
		/// </summary>
		/// <returns></returns>
		public abstract DmdMemberInfo[] GetDefaultMembers();

		/// <summary>
		/// Gets the number of dimensions if this is an array
		/// </summary>
		/// <returns></returns>
		public abstract int GetArrayRank();

		/// <summary>
		/// Gets the array sizes of each dimension of an array. The returned list could
		/// have less elements than the rank of the array.
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<int> GetArraySizes();

		/// <summary>
		/// Gets the lower bounds of each dimension of an array. The returned list could
		/// have less elements than the rank of the array.
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<int> GetArrayLowerBounds();

		/// <summary>
		/// true if it's an array (SZ array or MD array)
		/// </summary>
		public bool IsArray => TypeSignatureKind == DmdTypeSignatureKind.SZArray || TypeSignatureKind == DmdTypeSignatureKind.MDArray;

		/// <summary>
		/// true if it's an SZ array
		/// </summary>
		public bool IsSZArray => TypeSignatureKind == DmdTypeSignatureKind.SZArray;

		/// <summary>
		/// true if it's a multi-dimensional array
		/// </summary>
		public bool IsVariableBoundArray => TypeSignatureKind == DmdTypeSignatureKind.MDArray;

		/// <summary>
		/// true if it's a generic type
		/// </summary>
		public abstract bool IsGenericType { get; }

		/// <summary>
		/// true if it's a generic type definition
		/// </summary>
		public abstract bool IsGenericTypeDefinition { get; }

		/// <summary>
		/// true if it's a constructed generic type. These types can be instantiated.
		/// </summary>
		public bool IsConstructedGenericType => IsGenericType && !IsGenericTypeDefinition;

		/// <summary>
		/// true if it's a generic type or method parameter
		/// </summary>
		public bool IsGenericParameter => TypeSignatureKind == DmdTypeSignatureKind.TypeGenericParameter || TypeSignatureKind == DmdTypeSignatureKind.MethodGenericParameter;

		/// <summary>
		/// Gets the generic parameter position if this is a generic parameter
		/// </summary>
		public abstract int GenericParameterPosition { get; }

		/// <summary>
		/// true if it's a non constructed type with a TypeDef token
		/// </summary>
		public bool IsTypeDefinition => TypeSignatureKind == DmdTypeSignatureKind.Type;// Also if it's a TypeRef since it can be resolved to a TypeDef

		/// <summary>
		/// true if it's a generic type parameter
		/// </summary>
		public bool IsGenericTypeParameter => TypeSignatureKind == DmdTypeSignatureKind.TypeGenericParameter;

		/// <summary>
		/// true if it's a generic method parameter
		/// </summary>
		public bool IsGenericMethodParameter => TypeSignatureKind == DmdTypeSignatureKind.MethodGenericParameter;

		/// <summary>
		/// true if this type contains generic parameters
		/// </summary>
		public bool ContainsGenericParameters => CalculateContainsGenericParameters(this);
		static bool CalculateContainsGenericParameters(DmdType type) {
			while (type.GetElementType() is DmdType elementType)
				type = elementType;
			if (type.IsGenericParameter)
				return true;
			if (!type.IsGenericType)
				return false;
			foreach (var gaType in type.GetGenericArguments()) {
				if (gaType.ContainsGenericParameters)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Gets all generic parameter constraints
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdType> GetGenericParameterConstraints();

		/// <summary>
		/// true if this is a by-ref type
		/// </summary>
		public bool IsByRef => TypeSignatureKind == DmdTypeSignatureKind.ByRef;

		/// <summary>
		/// true if this is a pointer type
		/// </summary>
		public bool IsPointer => TypeSignatureKind == DmdTypeSignatureKind.Pointer;

		/// <summary>
		/// true if this is a function pointer type
		/// </summary>
		public bool IsFunctionPointer => TypeSignatureKind == DmdTypeSignatureKind.FunctionPointer;

		/// <summary>
		/// Gets the method signature if this is a function pointer type
		/// </summary>
		/// <returns></returns>
		public abstract DmdMethodSignature GetFunctionPointerMethodSignature();

		/// <summary>
		/// true if this is a primitive type (<see cref="bool"/>, <see cref="char"/>, <see cref="sbyte"/>,
		/// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
		/// <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="IntPtr"/>,
		/// <see cref="UIntPtr"/>)
		/// </summary>
		public bool IsPrimitive {
			get {
				if (MetadataNamespace != "System" || IsNested)
					return false;
				switch (MetadataName) {
				case "Boolean":	return this == AppDomain.System_Boolean;
				case "Char":	return this == AppDomain.System_Char;
				case "SByte":	return this == AppDomain.System_SByte;
				case "Byte":	return this == AppDomain.System_Byte;
				case "Int16":	return this == AppDomain.System_Int16;
				case "UInt16":	return this == AppDomain.System_UInt16;
				case "Int32":	return this == AppDomain.System_Int32;
				case "UInt32":	return this == AppDomain.System_UInt32;
				case "Int64":	return this == AppDomain.System_Int64;
				case "UInt64":	return this == AppDomain.System_UInt64;
				case "Single":	return this == AppDomain.System_Single;
				case "Double":	return this == AppDomain.System_Double;
				case "IntPtr":	return this == AppDomain.System_IntPtr;
				case "UIntPtr":	return this == AppDomain.System_UIntPtr;
				}
				return false;
			}
		}

		/// <summary>
		/// true if it's a COM object
		/// </summary>
		public bool IsCOMObject => CanCastTo(AppDomain.GetWellKnownType(DmdWellKnownType.System___ComObject, isOptional: true));

		/// <summary>
		/// true if it has an element type, i.e., it's an array, a by-ref or a pointer type
		/// </summary>
		public abstract bool HasElementType { get; }

		/// <summary>
		/// true if it's a <see cref="ContextBoundObject"/>
		/// </summary>
		public bool IsContextful => CanCastTo(AppDomain.GetWellKnownType(DmdWellKnownType.System_ContextBoundObject, isOptional: true));

		/// <summary>
		/// true if it's a <see cref="MarshalByRefObject"/>
		/// </summary>
		public bool IsMarshalByRef => CanCastTo(AppDomain.GetWellKnownType(DmdWellKnownType.System_MarshalByRefObject, isOptional: true));

		/// <summary>
		/// Gets the <see cref="DmdWellKnownType"/> value or <see cref="DmdWellKnownType.None"/> if it's not a well known type
		/// </summary>
		/// <returns></returns>
		public DmdWellKnownType GetWellKnownType() {
			if (!DmdWellKnownTypeUtils.TryGetWellKnownType(DmdTypeName.Create(this), out var wkt))
				return DmdWellKnownType.None;
			if (this != AppDomain.GetWellKnownType(wkt, isOptional: true))
				return DmdWellKnownType.None;
			return wkt;
		}

		/// <summary>
		/// Gets the element type if it's an array, a by-ref or a pointer type
		/// </summary>
		/// <returns></returns>
		public abstract DmdType? GetElementType();

		/// <summary>
		/// Gets the generic arguments
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdType> GetGenericArguments();

		/// <summary>
		/// Gets all generic arguments if it's a constructed generic type (<see cref="IsConstructedGenericType"/>)
		/// </summary>
		public ReadOnlyCollection<DmdType> GenericTypeArguments => IsConstructedGenericType ? GetGenericArguments() : ReadOnlyCollectionHelpers.Empty<DmdType>();

		/// <summary>
		/// Gets the generic type definition if it's a generic type
		/// </summary>
		/// <returns></returns>
		public abstract DmdType GetGenericTypeDefinition();

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
		public abstract ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers();

		/// <summary>
		/// Returns a type with the specified custom modifiers
		/// </summary>
		/// <param name="customModifiers">New custom modifiers</param>
		/// <returns></returns>
		public abstract DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers);

		/// <summary>
		/// Returns a type without custom modifiers
		/// </summary>
		/// <returns></returns>
		public abstract DmdType WithoutCustomModifiers();

		/// <summary>
		/// Returns the names of the members of the enum type
		/// </summary>
		/// <returns></returns>
		public abstract string[] GetEnumNames();

		/// <summary>
		/// Gets the underlying type of an enum (a primitive type)
		/// </summary>
		/// <returns></returns>
		public DmdType GetEnumUnderlyingType() {
			if (!IsEnum)
				throw new ArgumentException();
			var fields = GetFields(DmdBindingFlags.Instance | DmdBindingFlags.Public | DmdBindingFlags.NonPublic);
			if (fields.Length != 1)
				throw new ArgumentException();
			return fields[0].FieldType;
		}

		/// <summary>
		/// Returns true if this instance derives from <paramref name="type"/>. Also returns
		/// true if this type is an interface and <paramref name="type"/> is <see cref="object"/>.
		/// </summary>
		/// <param name="type">Other type</param>
		/// <returns></returns>
		public bool IsSubclassOf(DmdType type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			if (this == type)
				return false;
			for (var t = BaseType; t is not null; t = t.BaseType) {
				if (t == type)
					return true;
			}
			return type == type.AppDomain.System_Object;
		}

		/// <summary>
		/// Returns true if this instance derives from <paramref name="type"/>. Also returns
		/// true if this type is an interface and <paramref name="type"/> is <see cref="object"/>.
		/// </summary>
		/// <param name="type">Other type</param>
		/// <returns></returns>
		public bool IsSubclassOf(Type type) => IsSubclassOf(DmdTypeUtilities.ToDmdTypeNoNull(type, AppDomain));

		/// <summary>
		/// Returns true if an instance of <paramref name="c"/> can be assigned to an instance of this type
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool IsAssignableFrom(DmdType? c) {
			if (c is null)
				return false;
			return __CanCastTo(c, this);
		}

		/// <summary>
		/// Returns true if an instance of <paramref name="c"/> can be assigned to an instance of this type
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool IsAssignableFrom(Type c) => IsAssignableFrom(DmdTypeUtilities.ToDmdType(c, AppDomain));

		/// <summary>
		/// Returns true if it's possible to cast this type to <paramref name="target"/>
		/// </summary>
		/// <param name="target">Target type</param>
		/// <returns></returns>
		public bool CanCastTo(DmdType? target) {
			if (target is null)
				return false;
			return __CanCastTo(this, target);
		}

		/// <summary>
		/// Returns true if it's possible to cast this type to <paramref name="target"/>
		/// </summary>
		/// <param name="target">Target type</param>
		/// <returns></returns>
		public bool CanCastTo(Type target) => CanCastTo(DmdTypeUtilities.ToDmdType(target, AppDomain));

		internal static HashSet<DmdType> GetAllInterfaces(DmdType type) {
			DmdType? currentType = type;
			HashSet<DmdType>? hash = ObjectPools.AllocHashSetOfType();
			Stack<DmdType>? stack = ObjectPools.AllocStackOfType();
			stack.Push(currentType);
			while (stack.Count > 0) {
				currentType = stack.Pop();
				for (;;) {
					var ifaces = currentType.GetInterfaces();
					foreach (var iface in ifaces) {
						if (hash.Add(iface))
							stack.Push(iface);
					}
					currentType = currentType.BaseType;
					if (currentType is null)
						break;
				}
			}
			ObjectPools.Free(ref stack);
			// Callers free the hash
			return hash;
		}

		/// <summary>
		/// Returns true if this type is equivalent to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other types</param>
		/// <returns></returns>
		public bool IsEquivalentTo(DmdType? other) => new DmdSigComparer(DmdMemberInfoEqualityComparer.DefaultTypeOptions | DmdSigComparerOptions.CheckTypeEquivalence).Equals(this, other);

		/// <summary>
		/// Returns true if this type is equivalent to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other types</param>
		/// <returns></returns>
		public bool IsEquivalentTo(Type other) => IsEquivalentTo(DmdTypeUtilities.ToDmdType(other, AppDomain));

		/// <summary>
		/// true if this is a by-ref like value type
		/// </summary>
		public bool IsByRefLike =>
			//TODO: .NET adds this attribute to by ref like types, but .NET Framework does not, eg.
			//		ArgIterator, RuntimeArgumentHandle, TypedReference
			CustomAttributesHelper.IsDefined(this, "System.Runtime.CompilerServices.IsByRefLikeAttribute", inherit: false);

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
		public sealed override bool IsDefined(DmdType? attributeType, bool inherit) => CustomAttributesHelper.IsDefined(this, attributeType, inherit);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override DmdCustomAttributeData? FindCustomAttribute(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.Find(this, attributeTypeFullName, inherit);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public sealed override DmdCustomAttributeData? FindCustomAttribute(DmdType? attributeType, bool inherit) => CustomAttributesHelper.Find(this, attributeType, inherit);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdType? left, DmdType? right) => DmdMemberInfoEqualityComparer.DefaultType.Equals(left, right);
		public static bool operator !=(DmdType? left, DmdType? right) => !DmdMemberInfoEqualityComparer.DefaultType.Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdType? other) => DmdMemberInfoEqualityComparer.DefaultType.Equals(this, other);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as DmdType);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultType.GetHashCode(this);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public sealed override string? ToString() => DmdMemberFormatter.Format(this);
	}
}
