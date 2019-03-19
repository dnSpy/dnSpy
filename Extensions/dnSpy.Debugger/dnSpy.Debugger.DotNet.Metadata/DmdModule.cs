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
using System.IO;
using dnSpy.Debugger.DotNet.Metadata.Impl;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET module
	/// </summary>
	public abstract class DmdModule : DmdObject, IDmdCustomAttributeProvider {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdModule"/>
		/// </summary>
		private protected abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Returns the fully qualified name
		/// </summary>
		/// <param name="isInMemory">true if the module is in memory</param>
		/// <param name="isDynamic">true if it's a dynamic module</param>
		/// <param name="fullyQualifiedName">Module filename or null</param>
		/// <returns></returns>
		public static string GetFullyQualifiedName(bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (isDynamic)
				return "<In Memory Module>";
			if (isInMemory)
				return "<Unknown>";
			return fullyQualifiedName ?? string.Empty;
		}

		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the fully qualified name
		/// </summary>
		public abstract string FullyQualifiedName { get; }

		/// <summary>
		/// true if this is the corlib module
		/// </summary>
		public bool IsCorLib => this == AppDomain.CorLib.ManifestModule;

		/// <summary>
		/// Gets all types in this module
		/// </summary>
		/// <returns></returns>
		public abstract DmdType[] GetTypes();

		/// <summary>
		/// Gets all types that exist in the ExportedType table. This includes types that have been
		/// forwarded to other assemblies.
		/// </summary>
		/// <returns></returns>
		public abstract DmdType[] GetExportedTypes();

		/// <summary>
		/// Gets the module version ID
		/// </summary>
		public abstract Guid ModuleVersionId { get; }

		/// <summary>
		/// Gets the metadata token
		/// </summary>
		public abstract int MetadataToken { get; }

		/// <summary>
		/// Gets the global type
		/// </summary>
		public abstract DmdType GlobalType { get; }

		/// <summary>
		/// Gets the metadata stream version
		/// </summary>
		public abstract int MDStreamVersion { get; }

		/// <summary>
		/// Gets the metadata name of the module
		/// </summary>
		public abstract string ScopeName { get; set; }

		/// <summary>
		/// Gets a dynamic module's version number. It gets incremented each time a new type gets added to the dynamic module.
		/// </summary>
		public abstract int DynamicModuleVersion { get; }

		/// <summary>
		/// Gets the module name
		/// </summary>
		public string Name {
			get {
				var fqn = FullyQualifiedName;
				// Don't use Path.GetFileName() since fqn could contain invalid characters
				int index = fqn.LastIndexOfAny(dirSepChars);
				if (index >= 0)
					fqn = fqn.Substring(index + 1);
				if (fqn.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase))
					fqn = fqn.Substring(0, fqn.Length - ".ni.dll".Length) + fqn.Substring(fqn.Length - ".dll".Length);
				else if (fqn.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase))
					fqn = fqn.Substring(0, fqn.Length - ".ni.exe".Length) + fqn.Substring(fqn.Length - ".exe".Length);
				return fqn;
			}
		}
		static readonly char[] dirSepChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

		/// <summary>
		/// Gets the assembly
		/// </summary>
		public abstract DmdAssembly Assembly { get; }

		/// <summary>
		/// true if it's a dynamic module (types can be added at runtime)
		/// </summary>
		public abstract bool IsDynamic { get; }

		/// <summary>
		/// true if it's an in-memory module (eg. loaded from a <see cref="byte"/> array)
		/// </summary>
		public abstract bool IsInMemory { get; }

		/// <summary>
		/// true if it's a synthetic module; it's not loaded in the debugged process
		/// </summary>
		public abstract bool IsSynthetic { get; }

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
		public bool IsDefined(DmdType attributeType, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), attributeType);

		/// <summary>
		/// Checks if a custom attribute is present
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public bool IsDefined(Type attributeType, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, AppDomain));

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeTypeFullName">Full name of the custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData FindCustomAttribute(string attributeTypeFullName, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), attributeTypeFullName);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData FindCustomAttribute(DmdType attributeType, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), attributeType);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attributeType">Custom attribute type</param>
		/// <param name="inherit">true to check custom attributes in all base classes</param>
		/// <returns></returns>
		public DmdCustomAttributeData FindCustomAttribute(Type attributeType, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, AppDomain));

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken) => ResolveMethod(metadataToken, (IList<DmdType>)null, null);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken, DmdResolveOptions options) => ResolveMethod(metadataToken, (IList<DmdType>)null, null, options);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments) =>
			ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public abstract DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments, DmdResolveOptions options) =>
			ResolveMethod(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), options);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken) => ResolveField(metadataToken, (IList<DmdType>)null, null);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken, DmdResolveOptions options) => ResolveField(metadataToken, (IList<DmdType>)null, null, options);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments) =>
			ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public abstract DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments, DmdResolveOptions options) =>
			ResolveField(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), options);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken) => ResolveType(metadataToken, (IList<DmdType>)null, null);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken, DmdResolveOptions options) => ResolveType(metadataToken, (IList<DmdType>)null, null, options);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments) =>
			ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public abstract DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments, DmdResolveOptions options) =>
			ResolveType(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), options);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken) => ResolveMember(metadataToken, (IList<DmdType>)null, null);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken, DmdResolveOptions options) => ResolveMember(metadataToken, (IList<DmdType>)null, null, options);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments) =>
			ResolveMember(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public abstract DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments, DmdResolveOptions options) =>
			ResolveMember(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), options);

		/// <summary>
		/// Resolves a method signature
		/// </summary>
		/// <param name="metadataToken">StandaloneSig token from a method body</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveMethodSignature(metadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a method signature
		/// </summary>
		/// <param name="metadataToken">StandaloneSig token from a method body</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments) =>
			ResolveMethodSignature(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), DmdResolveOptions.ThrowOnError);

		/// <summary>
		/// Resolves a method signature
		/// </summary>
		/// <param name="metadataToken">StandaloneSig token from a method body</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public abstract DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options);

		/// <summary>
		/// Resolves a method signature
		/// </summary>
		/// <param name="metadataToken">StandaloneSig token from a method body</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="options">Resolve options</param>
		/// <returns></returns>
		public DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<Type> genericTypeArguments, IList<Type> genericMethodArguments, DmdResolveOptions options) =>
			ResolveMethodSignature(metadataToken, genericTypeArguments.ToDmdType(AppDomain), genericMethodArguments.ToDmdType(AppDomain), options);

		/// <summary>
		/// Resolves a signature
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public abstract byte[] ResolveSignature(int metadataToken);

		/// <summary>
		/// Resolves a string
		/// </summary>
		/// <param name="metadataToken">String token (<c>0x70xxxxxx</c>)</param>
		/// <returns></returns>
		public abstract string ResolveString(int metadataToken);

		/// <summary>
		/// Gets PE information
		/// </summary>
		/// <param name="peKind">PE Kind</param>
		/// <param name="machine">Machine</param>
		public abstract void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public DmdType GetType(Type type) => GetType(type, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public DmdType GetTypeThrow(Type type) => GetType(type, DmdGetTypeOptions.ThrowOnError);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public DmdType GetType(Type type, DmdGetTypeOptions options) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));
			return GetType(type.FullName, options);
		}

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <returns></returns>
		public DmdType GetType(string className, bool ignoreCase) => GetType(className, ignoreCase ? DmdGetTypeOptions.IgnoreCase : 0);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <returns></returns>
		public DmdType GetType(string className) => GetType(className, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <returns></returns>
		public DmdType GetTypeThrow(string className) => GetType(className, DmdGetTypeOptions.ThrowOnError);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <param name="throwOnError">true to throw if the type couldn't be found</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <returns></returns>
		public DmdType GetType(string className, bool throwOnError, bool ignoreCase) =>
			GetType(className, (throwOnError ? DmdGetTypeOptions.ThrowOnError : 0) | (ignoreCase ? DmdGetTypeOptions.IgnoreCase : 0));

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType GetType(string typeName, DmdGetTypeOptions options);

		/// <summary>
		/// Gets all global public static and instance fields
		/// </summary>
		/// <returns></returns>
		public DmdFieldInfo[] GetFields() => GetFields(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets all global fields
		/// </summary>
		/// <param name="bindingFlags">Binding attributes</param>
		/// <returns></returns>
		public DmdFieldInfo[] GetFields(DmdBindingFlags bindingFlags) => GlobalType.GetFields(bindingFlags);

		/// <summary>
		/// Gets a global public static or instance field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns></returns>
		public DmdFieldInfo GetField(string name) => GetField(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets a global field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <returns></returns>
		public DmdFieldInfo GetField(string name, DmdBindingFlags bindingAttr) => GlobalType.GetField(name, bindingAttr);

		/// <summary>
		/// Gets all global public static or instance methods
		/// </summary>
		/// <returns></returns>
		public DmdMethodInfo[] GetMethods() => GetMethods(DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public);

		/// <summary>
		/// Gets global methods
		/// </summary>
		/// <param name="bindingFlags">Binding attributes</param>
		/// <returns></returns>
		public DmdMethodInfo[] GetMethods(DmdBindingFlags bindingFlags) => GlobalType.GetMethods(bindingFlags);

		/// <summary>
		/// Gets a global method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Method parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types) {
			if (types == null)
				return GlobalType.GetMethod(name, bindingAttr);
			return GlobalType.GetMethod(name, bindingAttr, callConvention, types);
		}

		/// <summary>
		/// Gets a global method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="callConvention">Calling convention</param>
		/// <param name="types">Method parameter types or null</param>
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<Type> types) =>
			GetMethod(name, bindingAttr, callConvention, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a global public static or instance method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="types">Method parameter types</param>
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name, IList<DmdType> types) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, types);

		/// <summary>
		/// Gets a global public static or instance method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="types">Method parameter types</param>
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name, IList<Type> types) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, types.ToDmdType(AppDomain));

		/// <summary>
		/// Gets a global public static or instance method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, (IList<DmdType>)null);

		/// <summary>
		/// Gets all referenced assemblies
		/// </summary>
		/// <returns></returns>
		public abstract DmdReadOnlyAssemblyName[] GetReferencedAssemblies();

		/// <summary>
		/// Reads memory. Returns false if data couldn't be read.
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="destination">Destination address</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public abstract unsafe bool ReadMemory(uint rva, void* destination, int size);

		/// <summary>
		/// Reads memory. Returns false if data couldn't be read.
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="destination">Destination buffer</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public unsafe bool ReadMemory(uint rva, byte[] destination, int destinationIndex, int size) {
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if ((uint)destinationIndex > (uint)destination.Length)
				throw new ArgumentOutOfRangeException(nameof(destinationIndex));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if ((uint)(destinationIndex + size) > (uint)destination.Length)
				throw new ArgumentOutOfRangeException(nameof(destinationIndex));
			if (size == 0)
				return true;
			fixed (void* p = &destination[destinationIndex])
				return ReadMemory(rva, p, size);
		}

		/// <summary>
		/// Reads memory. Returns null if data couldn't be read.
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public byte[] ReadMemory(uint rva, int size) {
			if (size == 0)
				return Array.Empty<byte>();
			var res = new byte[size];
			if (!ReadMemory(rva, res, 0, size))
				return null;
			return res;
		}

		/// <summary>
		/// Returns the metadata name (<see cref="ScopeName"/>)
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => ScopeName;
	}

	/// <summary>
	/// Type/member resolve options
	/// </summary>
	[Flags]
	public enum DmdResolveOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Throw if the type or member couldn't be resolved
		/// </summary>
		ThrowOnError			= 0x00000001,

		/// <summary>
		/// Don't try to resolve type refs, field refs, method refs
		/// </summary>
		NoTryResolveRefs		= 0x00000002,
	}
}
