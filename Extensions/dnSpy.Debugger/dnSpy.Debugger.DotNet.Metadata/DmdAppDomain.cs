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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET AppDomain
	/// </summary>
	public abstract class DmdAppDomain : DmdObject {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdAppDomain"/>
		/// </summary>
		private protected abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DmdRuntime Runtime { get; }

		/// <summary>
		/// Gets the unique AppDomain id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Creates an assembly and adds it to the AppDomain. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) =>
			CreateAssembly(getMetadata, isInMemory, isDynamic, fullyQualifiedName, assemblyLocation, assemblySimpleName: null, isSynthetic: false, addAssembly: true);

		/// <summary>
		/// Creates a synthetic assembly but does not add it to the AppDomain
		/// </summary>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <param name="assemblySimpleName">The assembly's simple name or null if it's unknown</param>
		/// <returns></returns>
		public DmdAssembly CreateSyntheticAssembly(Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation, string? assemblySimpleName) =>
			CreateAssembly(getMetadata, isInMemory, isDynamic, fullyQualifiedName, assemblyLocation, assemblySimpleName: assemblySimpleName, isSynthetic: true, addAssembly: false);

		/// <summary>
		/// Creates an assembly and optionally adds it to the AppDomain. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <param name="assemblySimpleName">The assembly's simple name or null if it's unknown</param>
		/// <param name="isSynthetic">true if it's a synthetic assembly; it's not loaded in the debugged process</param>
		/// <param name="addAssembly">true if the assembly should be added to the AppDomain</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation, string? assemblySimpleName, bool isSynthetic, bool addAssembly) =>
			CreateAssembly(getMetadata, new DmdCreateAssemblyInfo(isInMemory, isDynamic, isSynthetic, addAssembly, fullyQualifiedName, assemblyLocation, assemblySimpleName));

		/// <summary>
		/// Creates an assembly and optionally adds it to the AppDomain. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="assemblyInfo">Assembly info</param>
		/// <returns></returns>
		public abstract DmdAssembly CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, DmdCreateAssemblyInfo assemblyInfo);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(string filename, bool isFileLayout = true, bool isInMemory = false, bool isDynamic = false, string? fullyQualifiedName = null, string? assemblyLocation = null) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			return CreateAssembly(() => new DmdLazyMetadataBytesFile(filename, isFileLayout), isInMemory, isDynamic, fullyQualifiedName ?? filename, assemblyLocation ?? filename);
		}

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="address">Address of PE file</param>
		/// <param name="size">Size of PE file</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(IntPtr address, uint size, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) =>
			CreateAssembly(() => new DmdLazyMetadataBytesPtr(address, size, isFileLayout), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="assemblyBytes">Raw PE file bytes</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(byte[] assemblyBytes, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) {
			if (assemblyBytes is null)
				throw new ArgumentNullException(nameof(assemblyBytes));
			return CreateAssembly(() => new DmdLazyMetadataBytesArray(assemblyBytes, isFileLayout), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation);
		}

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="comMetadata">COM <c>IMetaDataImport</c> instance</param>
		/// <param name="dynamicModuleHelper">Helper class</param>
		/// <param name="dispatcher">Dispatcher to use when accessing <paramref name="comMetadata"/></param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssembly CreateAssembly(object comMetadata, DmdDynamicModuleHelper dynamicModuleHelper, DmdDispatcher dispatcher, bool isInMemory, bool isDynamic, string fullyQualifiedName, string? assemblyLocation = null) {
			if (comMetadata is null)
				throw new ArgumentNullException(nameof(comMetadata));
			if (dynamicModuleHelper is null)
				throw new ArgumentNullException(nameof(dynamicModuleHelper));
			if (dispatcher is null)
				throw new ArgumentNullException(nameof(dispatcher));
			var mdi = comMetadata as Impl.COMD.IMetaDataImport2 ?? throw new ArgumentException("Only IMetaDataImport is supported");
			return CreateAssembly(() => new DmdLazyMetadataBytesCom(mdi, dynamicModuleHelper, dispatcher), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation ?? string.Empty);
		}

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <returns></returns>
		public abstract DmdModule CreateModule(DmdAssembly assembly, Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="filename">Filename</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <returns></returns>
		public DmdModule CreateModule(DmdAssembly assembly, string filename, bool isFileLayout = true, bool isInMemory = false, bool isDynamic = false, string? fullyQualifiedName = null) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			return CreateModule(assembly, () => new DmdLazyMetadataBytesFile(filename, isFileLayout), isInMemory, isDynamic, fullyQualifiedName ?? filename);
		}

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="address">Address of the PE file</param>
		/// <param name="size">Size of the PE file</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <returns></returns>
		public DmdModule CreateModule(DmdAssembly assembly, IntPtr address, uint size, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName) =>
			CreateModule(assembly, () => new DmdLazyMetadataBytesPtr(address, size, isFileLayout), isInMemory, isDynamic, fullyQualifiedName);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="moduleBytes">Raw PE file bytes</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <returns></returns>
		public DmdModule CreateModule(DmdAssembly assembly, byte[] moduleBytes, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (moduleBytes is null)
				throw new ArgumentNullException(nameof(moduleBytes));
			return CreateModule(assembly, () => new DmdLazyMetadataBytesArray(moduleBytes, isFileLayout), isInMemory, isDynamic, fullyQualifiedName);
		}

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="comMetadata">COM <c>IMetaDataImport</c> instance</param>
		/// <param name="dynamicModuleHelper">Helper class</param>
		/// <param name="dispatcher">Dispatcher to use when accessing <paramref name="comMetadata"/></param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <returns></returns>
		public DmdModule CreateModule(DmdAssembly assembly, object comMetadata, DmdDynamicModuleHelper dynamicModuleHelper, DmdDispatcher dispatcher, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (comMetadata is null)
				throw new ArgumentNullException(nameof(comMetadata));
			if (dynamicModuleHelper is null)
				throw new ArgumentNullException(nameof(dynamicModuleHelper));
			if (dispatcher is null)
				throw new ArgumentNullException(nameof(dispatcher));
			var mdi = comMetadata as Impl.COMD.IMetaDataImport2 ?? throw new ArgumentException("Only IMetaDataImport is supported");
			return CreateModule(assembly, () => new DmdLazyMetadataBytesCom(mdi, dynamicModuleHelper, dispatcher), isInMemory, isDynamic, fullyQualifiedName);
		}

		/// <summary>
		/// Adds an assembly
		/// </summary>
		/// <param name="assembly">Assembly to add</param>
		public abstract void Add(DmdAssembly assembly);

		/// <summary>
		/// Removes an assembly
		/// </summary>
		/// <param name="assembly">Assembly to remove</param>
		public abstract void Remove(DmdAssembly assembly);

		/// <summary>
		/// Adds an assembly. It gets removed when the return value's <see cref="IDisposable.Dispose"/> method gets called
		/// </summary>
		/// <param name="assembly">Assembly to add</param>
		/// <returns></returns>
		public TemporaryAssemblyDisposable AddTemporaryAssembly(DmdAssembly assembly) => new TemporaryAssemblyDisposable(assembly);

		/// <summary>
		/// Adds and removes an assembly
		/// </summary>
		public readonly struct TemporaryAssemblyDisposable : IDisposable {
			readonly DmdAssembly assembly;
			internal TemporaryAssemblyDisposable(DmdAssembly assembly) {
				this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
				assembly.AppDomain.Add(assembly);
			}

			/// <summary>
			/// Dispose()
			/// </summary>
			public void Dispose() => assembly.AppDomain.Remove(assembly);
		}

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		/// <returns></returns>
		public DmdAssembly[] GetAssemblies() => GetAssemblies(includeSyntheticAssemblies: true);

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		/// <param name="includeSyntheticAssemblies">true to include synthetic assemblies</param>
		/// <returns></returns>
		public abstract DmdAssembly[] GetAssemblies(bool includeSyntheticAssemblies);

		/// <summary>
		/// Gets an assembly or returns null if there's no such assembly
		/// </summary>
		/// <param name="simpleName">Simple name of the assembly, eg. "System"</param>
		/// <returns></returns>
		public abstract DmdAssembly? GetAssembly(string simpleName);

		/// <summary>
		/// Gets an assembly or returns null if there's no such assembly
		/// </summary>
		/// <param name="name">Assembly name</param>
		/// <returns></returns>
		public abstract DmdAssembly? GetAssembly(IDmdAssemblyName name);

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="assemblyName">Full assembly name</param>
		/// <returns></returns>
		public DmdAssembly? Load(object? context, string assemblyName) => Load(context, new DmdReadOnlyAssemblyName(assemblyName));

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="name">Assembly name</param>
		/// <returns></returns>
		public abstract DmdAssembly? Load(object? context, IDmdAssemblyName name);

		/// <summary>
		/// Loads an assembly. Will fail on .NET Core 1.x (but not on .NET Core 2.x or later)
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="assemblyFile">Assembly name or path to assembly</param>
		/// <returns></returns>
		public abstract DmdAssembly? LoadFrom(object? context, string assemblyFile);

		/// <summary>
		/// Loads an assembly. Will fail on .NET Core 1.x (but not on .NET Core 2.x or later)
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="path">Path to assembly</param>
		/// <returns></returns>
		public abstract DmdAssembly? LoadFile(object? context, string path);

		/// <summary>
		/// Gets the core library (eg. mscorlib if it's .NET Framework)
		/// </summary>
		public abstract DmdAssembly? CorLib { get; }

		/// <summary>
		/// Checks if a well known type exists in one of the loaded assemblies
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <returns></returns>
		public bool HasWellKnownType(DmdWellKnownType wellKnownType) => !(GetWellKnownType(wellKnownType, isOptional: true) is null);

		/// <summary>
		/// Gets a well known type
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <returns></returns>
		public DmdType GetWellKnownType(DmdWellKnownType wellKnownType) => GetWellKnownType(wellKnownType, isOptional: false)!;

		/// <summary>
		/// Gets a well known type
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <param name="isOptional">Used if the type couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdType? GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional) =>
			GetWellKnownType(wellKnownType, isOptional, onlyCorlib: false);

		internal abstract DmdType? GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional, bool onlyCorlib);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public DmdType System_Object => GetWellKnownType(DmdWellKnownType.System_Object);
		public DmdType System_Enum => GetWellKnownType(DmdWellKnownType.System_Enum);
		public DmdType System_MulticastDelegate => GetWellKnownType(DmdWellKnownType.System_MulticastDelegate);
		public DmdType System_Delegate => GetWellKnownType(DmdWellKnownType.System_Delegate);
		public DmdType System_ValueType => GetWellKnownType(DmdWellKnownType.System_ValueType);
		public DmdType System_Void => GetWellKnownType(DmdWellKnownType.System_Void);
		public DmdType System_Boolean => GetWellKnownType(DmdWellKnownType.System_Boolean);
		public DmdType System_Char => GetWellKnownType(DmdWellKnownType.System_Char);
		public DmdType System_SByte => GetWellKnownType(DmdWellKnownType.System_SByte);
		public DmdType System_Byte => GetWellKnownType(DmdWellKnownType.System_Byte);
		public DmdType System_Int16 => GetWellKnownType(DmdWellKnownType.System_Int16);
		public DmdType System_UInt16 => GetWellKnownType(DmdWellKnownType.System_UInt16);
		public DmdType System_Int32 => GetWellKnownType(DmdWellKnownType.System_Int32);
		public DmdType System_UInt32 => GetWellKnownType(DmdWellKnownType.System_UInt32);
		public DmdType System_Int64 => GetWellKnownType(DmdWellKnownType.System_Int64);
		public DmdType System_UInt64 => GetWellKnownType(DmdWellKnownType.System_UInt64);
		public DmdType System_Decimal => GetWellKnownType(DmdWellKnownType.System_Decimal);
		public DmdType System_Single => GetWellKnownType(DmdWellKnownType.System_Single);
		public DmdType System_Double => GetWellKnownType(DmdWellKnownType.System_Double);
		public DmdType System_String => GetWellKnownType(DmdWellKnownType.System_String);
		public DmdType System_IntPtr => GetWellKnownType(DmdWellKnownType.System_IntPtr);
		public DmdType System_UIntPtr => GetWellKnownType(DmdWellKnownType.System_UIntPtr);
		public DmdType System_Array => GetWellKnownType(DmdWellKnownType.System_Array);
		public DmdType System_Collections_IEnumerable => GetWellKnownType(DmdWellKnownType.System_Collections_IEnumerable);
		public DmdType System_Collections_Generic_IEnumerable_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IEnumerable_T);
		public DmdType System_Collections_Generic_IList_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IList_T);
		public DmdType System_Collections_Generic_ICollection_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_ICollection_T);
		public DmdType System_Collections_IEnumerator => GetWellKnownType(DmdWellKnownType.System_Collections_IEnumerator);
		public DmdType System_Collections_Generic_IEnumerator_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IEnumerator_T);
		public DmdType System_Collections_Generic_IReadOnlyList_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T);
		public DmdType System_Collections_Generic_IReadOnlyCollection_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T);
		public DmdType System_Nullable_T => GetWellKnownType(DmdWellKnownType.System_Nullable_T);
		public DmdType System_DateTime => GetWellKnownType(DmdWellKnownType.System_DateTime);
		public DmdType System_Runtime_CompilerServices_IsVolatile => GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_IsVolatile);
		public DmdType System_IDisposable => GetWellKnownType(DmdWellKnownType.System_IDisposable);
		public DmdType System_TypedReference => GetWellKnownType(DmdWellKnownType.System_TypedReference);
		public DmdType System_ArgIterator => GetWellKnownType(DmdWellKnownType.System_ArgIterator);
		public DmdType System_RuntimeArgumentHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeArgumentHandle);
		public DmdType System_RuntimeFieldHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle);
		public DmdType System_RuntimeMethodHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle);
		public DmdType System_RuntimeTypeHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
		public DmdType System_IAsyncResult => GetWellKnownType(DmdWellKnownType.System_IAsyncResult);
		public DmdType System_AsyncCallback => GetWellKnownType(DmdWellKnownType.System_AsyncCallback);
		public DmdType System_Type => GetWellKnownType(DmdWellKnownType.System_Type);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Returns a cached type if present else the input type
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType Intern(DmdType type, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a pointer type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakePointerType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a by-ref type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeByRefType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a SZ array type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeArrayType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a multi-dimensional array type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="rank">Number of dimensions</param>
		/// <param name="sizes">Sizes</param>
		/// <param name="lowerBounds">Lower bounds</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="genericTypeDefinition">Generic type definition</param>
		/// <param name="typeArguments">Generic arguments</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a generic method
		/// </summary>
		/// <param name="genericMethodDefinition">Generic method definition</param>
		/// <param name="typeArguments">Generic arguments</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdMethodInfo MakeGenericMethod(DmdMethodInfo genericMethodDefinition, IList<DmdType> typeArguments, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a function pointer type
		/// </summary>
		/// <param name="methodSignature">Method signature</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeFunctionPointerType(DmdMethodSignature methodSignature, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a function pointer type
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="genericParameterCount">Number of generic parameters</param>
		/// <param name="returnType">Return type</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="varArgsParameterTypes">VarArgs parameter types</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeFunctionPointerType(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a generic type parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringType">Declaring type</param>
		/// <returns></returns>
		public DmdType MakeGenericTypeParameter(int position, DmdType declaringType) => MakeGenericTypeParameter(position, declaringType, string.Empty, 0, null);

		/// <summary>
		/// Makes a generic type parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringType">Declaring type</param>
		/// <param name="name">Name</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericTypeParameter(int position, DmdType declaringType, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

		/// <summary>
		/// Makes a generic method parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringMethod">Declaring method</param>
		/// <returns></returns>
		public DmdType MakeGenericMethodParameter(int position, DmdMethodBase declaringMethod) => MakeGenericMethodParameter(position, declaringMethod, string.Empty, 0, null);

		/// <summary>
		/// Makes a generic method parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringMethod">Declaring method</param>
		/// <param name="name">Name</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericMethodParameter(int position, DmdMethodBase declaringMethod, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options = DmdMakeTypeOptions.None);

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
			var assemblyQualifiedName = type.AssemblyQualifiedName ?? throw new ArgumentException();
			return GetType(assemblyQualifiedName, options);
		}

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType? GetType(string typeName, DmdGetTypeOptions options);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <returns></returns>
		public DmdType? GetType(string typeName) => GetType(typeName, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <returns></returns>
		public DmdType GetTypeThrow(string typeName) => GetType(typeName, DmdGetTypeOptions.ThrowOnError)!;

		/// <summary>
		/// Creates a new instance of a type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="ctor">Constructor</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <returns></returns>
		public abstract object? CreateInstance(object? context, DmdConstructorInfo ctor, object?[] parameters);

		/// <summary>
		/// Executes a method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">Instance object or null if it's a static method</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <returns></returns>
		public abstract object? Invoke(object? context, DmdMethodBase method, object? obj, object?[]? parameters);

		/// <summary>
		/// Loads a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <returns></returns>
		public abstract object? LoadField(object? context, DmdFieldInfo field, object? obj);

		/// <summary>
		/// Stores a value in a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		public abstract void StoreField(object? context, DmdFieldInfo field, object? obj, object? value);
	}

	/// <summary>
	/// Options used when creating types
	/// </summary>
	[Flags]
	public enum DmdMakeTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Don't try to resolve a reference
		/// </summary>
		NoResolve			= 0x00000001,
	}

	/// <summary>
	/// Options used when finding a type
	/// </summary>
	[Flags]
	public enum DmdGetTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Throw if the type couldn't be found
		/// </summary>
		ThrowOnError		= 0x00000001,

		/// <summary>
		/// Ignore case
		/// </summary>
		IgnoreCase			= 0x00000002,
	}

	/// <summary>
	/// Create-assembly-options
	/// </summary>
	public enum DmdCreateAssemblyOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Set if the module is in memory (<see cref="DmdModule.IsInMemory"/>)
		/// </summary>
		InMemory			= 0x00000001,

		/// <summary>
		/// Set if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)
		/// </summary>
		Dynamic				= 0x00000002,

		/// <summary>
		/// Synthetic assembly, eg. created by the expression compiler
		/// </summary>
		Synthetic			= 0x00000004,

		/// <summary>
		/// Don't add the assembly to the AppDomain
		/// </summary>
		DontAddAssembly		= 0x00000008,

		/// <summary>
		/// It's an exe file. If it's not set, it's either a DLL or it's unknown
		/// </summary>
		IsEXE				= 0x00000010,

		/// <summary>
		/// It's a dll file. If it's not set, it's either an EXE or it's unknown
		/// </summary>
		IsDLL				= 0x00000020,
	}

	/// <summary>
	/// Info needed when creating an assembly
	/// </summary>
	public readonly struct DmdCreateAssemblyInfo {
		/// <summary>
		/// Gets the options
		/// </summary>
		public DmdCreateAssemblyOptions Options { get; }

		/// <summary>
		/// The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/>
		/// </summary>
		public string FullyQualifiedName { get; }

		/// <summary>
		/// Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)
		/// </summary>
		public string AssemblyLocation { get; }

		/// <summary>
		/// Gets the assembly's simple name or null if it's unknown
		/// </summary>
		public string? AssemblySimpleName { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <param name="assemblySimpleName">The assembly's simple name or null if it's unknown</param>
		public DmdCreateAssemblyInfo(DmdCreateAssemblyOptions options, string fullyQualifiedName, string assemblyLocation, string? assemblySimpleName) {
			if ((options & (DmdCreateAssemblyOptions.IsEXE | DmdCreateAssemblyOptions.IsDLL)) == (DmdCreateAssemblyOptions.IsEXE | DmdCreateAssemblyOptions.IsDLL))
				throw new ArgumentException();
			Options = options;
			FullyQualifiedName = fullyQualifiedName ?? throw new ArgumentNullException(nameof(fullyQualifiedName));
			AssemblyLocation = assemblyLocation ?? throw new ArgumentNullException(nameof(assemblyLocation));
			AssemblySimpleName = assemblySimpleName;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="isSynthetic">true if it's a synthetic assembly, eg. created by the expression compiler</param>
		/// <param name="addAssembly">true if the assembly should be added to the AppDomain</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>). See <see cref="DmdModule.GetFullyQualifiedName(bool, bool, string)"/></param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <param name="assemblySimpleName">The assembly's simple name or null if it's unknown</param>
		public DmdCreateAssemblyInfo(bool isInMemory, bool isDynamic, bool isSynthetic, bool addAssembly, string fullyQualifiedName, string assemblyLocation, string? assemblySimpleName)
			: this(GetOptions(isInMemory, isDynamic, isSynthetic, addAssembly), fullyQualifiedName, assemblyLocation, assemblySimpleName) {
		}

		static DmdCreateAssemblyOptions GetOptions(bool isInMemory, bool isDynamic, bool isSynthetic, bool addAssembly) {
			var options = DmdCreateAssemblyOptions.None;
			if (isInMemory)
				options |= DmdCreateAssemblyOptions.InMemory;
			if (isDynamic)
				options |= DmdCreateAssemblyOptions.Dynamic;
			if (isSynthetic)
				options |= DmdCreateAssemblyOptions.Synthetic;
			if (!addAssembly)
				options |= DmdCreateAssemblyOptions.DontAddAssembly;
			return options;
		}
	}
}
