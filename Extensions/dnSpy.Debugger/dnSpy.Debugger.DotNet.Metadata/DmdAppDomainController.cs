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
	/// Contains an AppDomain and allows adding and removing assemblies
	/// </summary>
	public abstract class DmdAppDomainController {
		/// <summary>
		/// Gets the AppDomain
		/// </summary>
		public abstract DmdAppDomain AppDomain { get; }

		/// <summary>
		/// Removes the AppDomain from the runtime
		/// </summary>
		public abstract void Remove();

		/// <summary>
		/// Creates an assembly and adds it to the AppDomain. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public abstract DmdAssemblyController CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssemblyController CreateAssembly(string filename, bool isFileLayout = true, bool isInMemory = false, bool isDynamic = false, string fullyQualifiedName = null, string assemblyLocation = null) =>
			CreateAssembly(() => new DmdLazyMetadataBytesFile(filename, isFileLayout), isInMemory, isDynamic, fullyQualifiedName ?? filename, assemblyLocation ?? filename);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="address">Address of PE file</param>
		/// <param name="size">Size of PE file</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssemblyController CreateAssembly(IntPtr address, uint size, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) =>
			CreateAssembly(() => new DmdLazyMetadataBytesPtr(address, size, isFileLayout), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="assemblyBytes">Raw PE file bytes</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssemblyController CreateAssembly(byte[] assemblyBytes, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) =>
			CreateAssembly(() => new DmdLazyMetadataBytesArray(assemblyBytes, isFileLayout), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation);

		/// <summary>
		/// Creates an assembly. The first created assembly must be the corlib (<see cref="DmdAppDomain.CorLib"/>)
		/// </summary>
		/// <param name="comMetadata">COM <c>IMetaDataImport</c> instance</param>
		/// <param name="dispatcher">Dispatcher to use when accessing <paramref name="comMetadata"/></param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <param name="assemblyLocation">Location of the assembly or an empty string (<see cref="DmdAssembly.Location"/>)</param>
		/// <returns></returns>
		public DmdAssemblyController CreateAssembly(object comMetadata, DmdDispatcher dispatcher, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation = null) =>
			CreateAssembly(() => new DmdLazyMetadataBytesCom(comMetadata, dispatcher), isInMemory, isDynamic, fullyQualifiedName, assemblyLocation ?? string.Empty);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="getMetadata">Called to provide the metadata</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <returns></returns>
		public abstract DmdModuleController CreateModule(DmdAssembly assembly, Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="filename">Filename</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <returns></returns>
		public DmdModuleController CreateModule(DmdAssembly assembly, string filename, bool isFileLayout = true, bool isInMemory = false, bool isDynamic = false, string fullyQualifiedName = null) =>
			CreateModule(assembly, () => new DmdLazyMetadataBytesFile(filename, isFileLayout), isInMemory, isDynamic, fullyQualifiedName ?? filename);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="address">Address of the PE file</param>
		/// <param name="size">Size of the PE file</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <returns></returns>
		public DmdModuleController CreateModule(DmdAssembly assembly, IntPtr address, uint size, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName) =>
			CreateModule(assembly, () => new DmdLazyMetadataBytesPtr(address, size, isFileLayout), isInMemory, isDynamic, fullyQualifiedName);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="assemblyBytes">Raw PE file bytes</param>
		/// <param name="isFileLayout">true if file layout, false if memory layout</param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <returns></returns>
		public DmdModuleController CreateModule(DmdAssembly assembly, byte[] assemblyBytes, bool isFileLayout, bool isInMemory, bool isDynamic, string fullyQualifiedName) =>
			CreateModule(assembly, () => new DmdLazyMetadataBytesArray(assemblyBytes, isFileLayout), isInMemory, isDynamic, fullyQualifiedName);

		/// <summary>
		/// Adds a module to an existing assembly
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="comMetadata">COM <c>IMetaDataImport</c> instance</param>
		/// <param name="dispatcher">Dispatcher to use when accessing <paramref name="comMetadata"/></param>
		/// <param name="isInMemory">true if the module is in memory (<see cref="DmdModule.IsInMemory"/>)</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added at runtime) (<see cref="DmdModule.IsDynamic"/>)</param>
		/// <param name="fullyQualifiedName">The fully qualified name of the module (<see cref="DmdModule.FullyQualifiedName"/>)</param>
		/// <returns></returns>
		public DmdModuleController CreateModule(DmdAssembly assembly, object comMetadata, DmdDispatcher dispatcher, bool isInMemory, bool isDynamic, string fullyQualifiedName) =>
			CreateModule(assembly, () => new DmdLazyMetadataBytesCom(comMetadata, dispatcher), isInMemory, isDynamic, fullyQualifiedName);
	}
}
