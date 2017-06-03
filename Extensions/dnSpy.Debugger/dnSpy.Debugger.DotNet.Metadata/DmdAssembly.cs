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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET assembly
	/// </summary>
	public abstract class DmdAssembly : DmdObject, IDmdCustomAttributeProvider {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

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
		public abstract DmdAssemblyName GetName();

		/// <summary>
		/// Gets the full name of the assembly
		/// </summary>
		public abstract string FullName { get; }

		/// <summary>
		/// Gets the assembly location or an empty string
		/// </summary>
		public abstract string Location { get; }

		/// <summary>
		/// Gets the runtime version found in the metadata
		/// </summary>
		public abstract string ImageRuntimeVersion { get; }

		/// <summary>
		/// true if the assembly is in the GAC
		/// </summary>
		public abstract bool GlobalAssemblyCache { get; }

		/// <summary>
		/// true if it's a dynamic assembly (types can be added at runtime)
		/// </summary>
		public bool IsDynamic => ManifestModule.IsDynamic;

		/// <summary>
		/// true if it's an in-memory assembly (eg. loaded from a <see cref="byte"/> array)
		/// </summary>
		public bool IsInMemory => ManifestModule.IsInMemory;

		/// <summary>
		/// Gets the entry point or null
		/// </summary>
		public abstract DmdMethodInfo EntryPoint { get; }

		/// <summary>
		/// Gets the first module of this assembly
		/// </summary>
		public abstract DmdModule ManifestModule { get; }

		/// <summary>
		/// Gets a type in this assembly or null if it doesn't exist
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <returns></returns>
		public DmdType GetType(string name) => GetType(name, false, false);

		/// <summary>
		/// Gets a type in this assembly
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <param name="throwOnError">true to throw if the type doesn't exist</param>
		/// <returns></returns>
		public DmdType GetType(string name, bool throwOnError) => GetType(name, throwOnError, false);

		/// <summary>
		/// Gets a type in this assembly
		/// </summary>
		/// <param name="name">Name of type</param>
		/// <param name="throwOnError">true to throw if the type doesn't exist</param>
		/// <param name="ignoreCase">true if case insensitive comparisons</param>
		/// <returns></returns>
		public abstract DmdType GetType(string name, bool throwOnError, bool ignoreCase);

		/// <summary>
		/// Gets all public types in this assembly
		/// </summary>
		public IEnumerable<DmdType> ExportedTypes => GetExportedTypes();

		/// <summary>
		/// Gets all public types in this assembly
		/// </summary>
		public abstract DmdType[] GetExportedTypes();

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
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		public object CreateInstance(IDmdEvaluationContext context, string typeName) => CreateInstance(context, typeName, false, DmdBindingFlags.Instance | DmdBindingFlags.Public, null, null);

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		public object CreateInstance(IDmdEvaluationContext context, string typeName, bool ignoreCase) => CreateInstance(context, typeName, ignoreCase, DmdBindingFlags.Instance | DmdBindingFlags.Public, null, null);

		/// <summary>
		/// Creates an instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="typeName">Fully qualified name of type to create</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <param name="bindingAttr">Binding attributes</param>
		/// <param name="args">Constructor arguments or null</param>
		public object CreateInstance(IDmdEvaluationContext context, string typeName, bool ignoreCase, DmdBindingFlags bindingAttr, object[] args) => CreateInstance(context, typeName, ignoreCase, bindingAttr, args, null);

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
		public object CreateInstance(IDmdEvaluationContext context, string typeName, bool ignoreCase, DmdBindingFlags bindingAttr, object[] args, DmdType[] argTypes) {
			args = args ?? Array.Empty<object>();
			if (argTypes != null && args.Length != argTypes.Length)
				throw new ArgumentException();
			var type = GetType(typeName, false, ignoreCase);
			if ((object)type == null)
				return null;
			DmdConstructorInfo ctor;
			if (argTypes != null)
				ctor = type.GetConstructor(bindingAttr, argTypes, null);
			else {
				ctor = null;
				foreach (var c in type.GetConstructors(bindingAttr)) {
					if (c.GetParameters().Length != args.Length)
						continue;
					if (ctor != null)
						return null;
					ctor = c;
				}
			}
			return ctor?.Invoke(context, args);
		}

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
		public abstract DmdModule GetModule(string name);

		/// <summary>
		/// Gets all referenced assemblies
		/// </summary>
		/// <returns></returns>
		public abstract DmdAssemblyName[] GetReferencedAssemblies();

		/// <summary>
		/// Gets the full name
		/// </summary>
		/// <returns></returns>
		public override string ToString() => FullName;
	}
}
