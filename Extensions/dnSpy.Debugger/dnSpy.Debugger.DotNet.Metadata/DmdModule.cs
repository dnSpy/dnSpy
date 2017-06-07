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
using System.IO;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET module
	/// </summary>
	public abstract class DmdModule : DmdObject, IDmdCustomAttributeProvider {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the fully qualified name
		/// </summary>
		public abstract string FullyQualifiedName { get; }

		/// <summary>
		/// Gets all types in this module
		/// </summary>
		/// <returns></returns>
		public abstract DmdType[] GetTypes();

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
		public abstract string ScopeName { get; }

		/// <summary>
		/// Gets the module name
		/// </summary>
		public string Name {
			get {
				var fqn = FullyQualifiedName;
				// Don't use Path.GetFileName() since fqn could contain invalid characters
				int index = fqn.LastIndexOfAny(dirSepChars);
				if (index >= 0)
					return fqn.Substring(index + 1);
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

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken) => ResolveMethod(metadataToken, null, null);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, true);

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="throwOnError">true to throw if the method couldn't be found</param>
		/// <returns></returns>
		public abstract DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken) => ResolveField(metadataToken, null, null);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, true);

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="throwOnError">true to throw if the field couldn't be found</param>
		/// <returns></returns>
		public abstract DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken) => ResolveType(metadataToken, null, null);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, true);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="throwOnError">true to throw if the type couldn't be found</param>
		/// <returns></returns>
		public abstract DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken) => ResolveMember(metadataToken, null, null);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <returns></returns>
		public DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) =>
			ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments, true);

		/// <summary>
		/// Resolves a member
		/// </summary>
		/// <param name="metadataToken">Token</param>
		/// <param name="genericTypeArguments">Generic type arguments or null</param>
		/// <param name="genericMethodArguments">Generic method arguments or null</param>
		/// <param name="throwOnError">true to throw if the member couldn't be found</param>
		/// <returns></returns>
		public abstract DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError);

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
		/// <param name="className">Name of type</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <returns></returns>
		public DmdType GetType(string className, bool ignoreCase) => GetType(className, false, ignoreCase);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <returns></returns>
		public DmdType GetType(string className) => GetType(className, false, false);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="className">Name of type</param>
		/// <param name="throwOnError">true to throw if the type couldn't be found</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <returns></returns>
		public abstract DmdType GetType(string className, bool throwOnError, bool ignoreCase);

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
		/// <returns></returns>
		public DmdMethodInfo GetMethod(string name) => GetMethod(name, DmdBindingFlags.Instance | DmdBindingFlags.Static | DmdBindingFlags.Public, DmdCallingConventions.Any, null);

		/// <summary>
		/// Returns the metadata name (<see cref="ScopeName"/>)
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => ScopeName;
	}
}
