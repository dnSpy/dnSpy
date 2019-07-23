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
using dnSpy.Debugger.DotNet.Metadata.Impl;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET assembly
	/// </summary>
	public abstract class DmdAssembly : DmdObject, IDmdCustomAttributeProvider, IDmdSecurityAttributeProvider {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdAssembly"/>
		/// </summary>
		private protected abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

		/// <summary>
		/// true if this is the corlib assembly
		/// </summary>
		public bool IsCorLib => this == AppDomain.CorLib;

		/// <summary>
		/// Creates a qualified type name
		/// </summary>
		/// <param name="assemblyName">Full assembly name of the type</param>
		/// <param name="typeName">Full type name</param>
		/// <returns></returns>
		public static string CreateQualifiedName(string assemblyName, string typeName) => typeName + ", " + assemblyName;

		/// <summary>
		/// Gets the assembly name
		/// </summary>
		/// <returns></returns>
		public abstract DmdReadOnlyAssemblyName GetName();

		/// <summary>
		/// Gets the full name of the assembly
		/// </summary>
		public string FullName => GetName().ToString();

		/// <summary>
		/// Gets the assembly location or an empty string
		/// </summary>
		public abstract string Location { get; }

		/// <summary>
		/// Gets the runtime version found in the metadata
		/// </summary>
		public abstract string ImageRuntimeVersion { get; }

		/// <summary>
		/// true if it's a dynamic assembly (types can be added at runtime)
		/// </summary>
		public bool IsDynamic => ManifestModule.IsDynamic;

		/// <summary>
		/// true if it's an in-memory assembly (eg. loaded from a <see cref="byte"/> array)
		/// </summary>
		public bool IsInMemory => ManifestModule.IsInMemory;

		/// <summary>
		/// true if it's a synthetic assembly; it's not loaded in the debugged process
		/// </summary>
		public bool IsSynthetic => ManifestModule.IsSynthetic;

		/// <summary>
		/// true if the assembly has been added to its AppDomain
		/// </summary>
		public abstract bool IsLoaded { get; }

		/// <summary>
		/// Gets the entry point or null
		/// </summary>
		public abstract DmdMethodInfo? EntryPoint { get; }

		/// <summary>
		/// Gets the first module of this assembly
		/// </summary>
		public abstract DmdModule ManifestModule { get; }

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public DmdType? GetType(Type type) => GetType(type, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public DmdType GetTypeThrow(Type type) => GetType(type, DmdGetTypeOptions.ThrowOnError)!;

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public DmdType? GetType(Type type, DmdGetTypeOptions options) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			var fullname = type.FullName ?? throw new ArgumentException();
			return GetType(fullname, options);
		}

		/// <summary>
		/// Gets a type in this assembly or null if it doesn't exist
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <returns></returns>
		public DmdType? GetType(string name) => GetType(name, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <returns></returns>
		public DmdType GetTypeThrow(string name) => GetType(name, DmdGetTypeOptions.ThrowOnError)!;

		/// <summary>
		/// Gets a type in this assembly
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <param name="throwOnError">true to throw if the type doesn't exist</param>
		/// <returns></returns>
		public DmdType? GetType(string name, bool throwOnError) => GetType(name, throwOnError ? DmdGetTypeOptions.ThrowOnError : DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type in this assembly
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <param name="throwOnError">true to throw if the type doesn't exist</param>
		/// <param name="ignoreCase">true if case insensitive comparisons</param>
		/// <returns></returns>
		public DmdType? GetType(string name, bool throwOnError, bool ignoreCase) =>
			GetType(name, (throwOnError ? DmdGetTypeOptions.ThrowOnError : 0) | (ignoreCase ? DmdGetTypeOptions.IgnoreCase : 0));

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType? GetType(string typeName, DmdGetTypeOptions options);

		/// <summary>
		/// Gets all public types in this assembly
		/// </summary>
		public IEnumerable<DmdType> ExportedTypes => GetExportedTypes();

		/// <summary>
		/// Gets all public types in this assembly
		/// </summary>
		public abstract DmdType[] GetExportedTypes();

		/// <summary>
		/// Gets all forwarded types (types that now exist in another assembly)
		/// </summary>
		/// <returns></returns>
		public abstract DmdType[] GetForwardedTypes();

		/// <summary>
		/// Gets all types in this assembly
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetTypes() {
			var modules = GetModules();
			if (modules.Length == 1)
				return modules[0].GetTypes();
			var list = new List<DmdType>();
			foreach (var module in modules)
				list.AddRange(module.GetTypes());
			return list.ToArray();
		}

		/// <summary>
		/// Gets the security attributes
		/// </summary>
		public ReadOnlyCollection<DmdCustomAttributeData> SecurityAttributes => GetSecurityAttributesData();

		/// <summary>
		/// Gets the security attributes
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData();

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
		public bool IsDefined(Type attributeType, bool inherit) => CustomAttributesHelper.IsDefined(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, AppDomain));

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
		public DmdCustomAttributeData? FindCustomAttribute(Type attributeType, bool inherit) => CustomAttributesHelper.Find(GetCustomAttributesData(), DmdTypeUtilities.ToDmdType(attributeType, AppDomain));

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		public object? CreateInstance(object? context, string typeName) => CreateInstance(context, typeName, false, DmdBindingFlags.Instance | DmdBindingFlags.Public, null, (IList<DmdType>?)null);

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		public object? CreateInstance(object? context, string typeName, bool ignoreCase) => CreateInstance(context, typeName, ignoreCase, DmdBindingFlags.Instance | DmdBindingFlags.Public, null, (IList<DmdType>?)null);

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="args">Constructor arguments or null</param>
		public object? CreateInstance(object? context, string typeName, bool ignoreCase, DmdBindingFlags bindingAttr, object?[]? args) => CreateInstance(context, typeName, ignoreCase, bindingAttr, args, (IList<DmdType>?)null);

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="args">Constructor arguments or null</param>
		/// <param name="argTypes">Constructor parameter types or null</param>
		/// <returns></returns>
		public object? CreateInstance(object? context, string typeName, bool ignoreCase, DmdBindingFlags bindingAttr, object?[]? args, IList<DmdType>? argTypes) {
			args ??= Array.Empty<object?>();
			if (!(argTypes is null) && args.Length != argTypes.Count)
				throw new ArgumentException();
			var type = GetType(typeName, false, ignoreCase);
			if (type is null)
				return null;
			DmdConstructorInfo? ctor;
			if (!(argTypes is null))
				ctor = type.GetConstructor(bindingAttr, argTypes);
			else {
				ctor = null;
				foreach (var c in type.GetConstructors(bindingAttr)) {
					if (c.GetMethodSignature().GetParameterTypes().Count != args.Length)
						continue;
					if (!(ctor is null))
						return null;
					ctor = c;
				}
			}
			return ctor?.Invoke(context, args);
		}

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="args">Constructor arguments or null</param>
		/// <param name="argTypes">Constructor parameter types or null</param>
		/// <returns></returns>
		public object? CreateInstance(object? context, string typeName, bool ignoreCase, DmdBindingFlags bindingAttr, object?[]? args, IList<Type>? argTypes) =>
			CreateInstance(context, typeName, ignoreCase, bindingAttr, args, argTypes.ToDmdType(AppDomain));

		/// <summary>
		/// Gets all loaded modules
		/// </summary>
		public IEnumerable<DmdModule> Modules => GetLoadedModules();

		/// <summary>
		/// Gets all loaded modules
		/// </summary>
		/// <returns></returns>
		public abstract DmdModule[] GetLoadedModules();

		/// <summary>
		/// Gets all modules
		/// </summary>
		/// <returns></returns>
		public abstract DmdModule[] GetModules();

		/// <summary>
		/// Gets a module
		/// </summary>
		/// <param name="name">Name of module</param>
		/// <returns></returns>
		public abstract DmdModule? GetModule(string name);

		/// <summary>
		/// Gets all referenced assemblies
		/// </summary>
		/// <returns></returns>
		public abstract DmdReadOnlyAssemblyName[] GetReferencedAssemblies();

		/// <summary>
		/// Removes a module from the assembly
		/// </summary>
		/// <param name="module">Module to remove</param>
		public abstract void Remove(DmdModule module);

		/// <summary>
		/// Gets the full name
		/// </summary>
		/// <returns></returns>
		public sealed override string ToString() => FullName;
	}
}
