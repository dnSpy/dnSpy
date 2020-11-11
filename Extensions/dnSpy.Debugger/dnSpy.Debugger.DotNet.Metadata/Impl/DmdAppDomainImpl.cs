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
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAppDomainImpl : DmdAppDomain {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdRuntime Runtime => runtime;
		public override int Id { get; }

		public override DmdAssembly? CorLib {
			get {
				lock (assembliesLockObj) {
					// Assume that the first assembly is always the corlib. This is documented in DmdAppDomain.CreateAssembly()
					var assemblies = this.assemblies;
					for (int i = 0; i < assemblies.Count; i++) {
						var asm = assemblies[i];
						if (!asm.IsSynthetic)
							return asm;
					}
					return null;
				}
			}
		}

		internal AssemblyNameEqualityComparer AssemblyNameEqualityComparer => assemblyNameEqualityComparer;
		readonly AssemblyNameEqualityComparer assemblyNameEqualityComparer;

		// Assemblies lock fields
		readonly object assembliesLockObj;
		readonly List<DmdAssemblyImpl> assemblies;
		readonly Dictionary<string, DmdAssemblyImpl> simpleNameToAssembly;
		readonly Dictionary<IDmdAssemblyName, DmdAssemblyImpl> assemblyNameToAssembly;
		readonly List<AssemblyLoadedListener> assemblyLoadedListeners;

		// Fully resolved type lock fields
		readonly object fullyResolvedTypesLockObj;
		readonly Dictionary<DmdType, DmdType> fullyResolvedTypes;

		// Module type lock fields
		readonly object moduleTypeLockObj;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>> toModuleTypeDict;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>> toModuleTypeDictIgnoreCase;

		// Exported type lock fields
		readonly object exportedTypeLockObj;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>> toModuleExportedTypeDict;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>> toModuleExportedTypeDictIgnoreCase;

		// No locks required
		readonly DmdRuntimeImpl runtime;
		readonly WellKnownMemberResolver wellKnownMemberResolver;
		const DmdSigComparerOptions moduleTypeOptions = DmdSigComparerOptions.DontCompareTypeScope;
		static readonly DmdMemberInfoEqualityComparer moduleTypeDictComparer = new DmdMemberInfoEqualityComparer(moduleTypeOptions);
		static readonly DmdMemberInfoEqualityComparer moduleTypeDictComparerIgnoreCase = new DmdMemberInfoEqualityComparer(moduleTypeOptions | DmdSigComparerOptions.CaseInsensitiveMemberNames);
		readonly Func<DmdModuleImpl, DmdLazyMetadataBytes, DmdMetadataReader> metadataReaderFactory;

		public DmdAppDomainImpl(DmdRuntimeImpl runtime, int id) {
			assembliesLockObj = new object();
			fullyResolvedTypesLockObj = new object();
			moduleTypeLockObj = new object();
			exportedTypeLockObj = new object();
			assemblies = new List<DmdAssemblyImpl>();
			simpleNameToAssembly = new Dictionary<string, DmdAssemblyImpl>(StringComparer.OrdinalIgnoreCase);
			// Ignore PKT, see comment in DmdSigComparer: bool Equals(IDmdAssemblyName a, IDmdAssemblyName b)
			assemblyNameEqualityComparer = new AssemblyNameEqualityComparer(ignorePublicKeyToken: true);
			assemblyNameToAssembly = new Dictionary<IDmdAssemblyName, DmdAssemblyImpl>(assemblyNameEqualityComparer);
			fullyResolvedTypes = new Dictionary<DmdType, DmdType>(new DmdMemberInfoEqualityComparer(DmdSigComparerOptions.CompareDeclaringType | DmdSigComparerOptions.CompareCustomModifiers | DmdSigComparerOptions.CompareGenericParameterDeclaringMember));
			toModuleTypeDict = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>>();
			toModuleTypeDictIgnoreCase = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>>();
			toModuleExportedTypeDict = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>>();
			toModuleExportedTypeDictIgnoreCase = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>>();
			wellKnownMemberResolver = new WellKnownMemberResolver(this);
			assemblyLoadedListeners = new List<AssemblyLoadedListener>();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			metadataReaderFactory = CreateMetadataReader;
			Id = id;
		}

		DmdMetadataReader CreateMetadataReader(DmdModuleImpl module, DmdLazyMetadataBytes lzmd) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			if (lzmd is null)
				throw new ArgumentNullException(nameof(lzmd));
			try {
				switch (lzmd) {
				case DmdLazyMetadataBytesPtr lzmdPtr:		return MD.DmdEcma335MetadataReader.Create(module, lzmdPtr.Address, lzmdPtr.Size, lzmdPtr.IsFileLayout);
				case DmdLazyMetadataBytesArray lzmdArray:	return MD.DmdEcma335MetadataReader.Create(module, lzmdArray.Bytes, lzmdArray.IsFileLayout);
				case DmdLazyMetadataBytesFile lzmdFile:		return MD.DmdEcma335MetadataReader.Create(module, lzmdFile.Filename, lzmdFile.IsFileLayout);
				case DmdLazyMetadataBytesCom lzmdCom:		return new COMD.DmdComMetadataReader(module, lzmdCom.MetaDataImport, lzmdCom.DynamicModuleHelper, lzmdCom.Dispatcher);
				}
			}
			catch {
				Debug.Fail("Failed to create metadata");
				return new DmdNullMetadataReader(module);
			}
			throw new NotSupportedException($"Unknown lazy metadata: {lzmd.GetType()}");
		}

		public override DmdAssembly CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, DmdCreateAssemblyInfo assemblyInfo) {
			if (getMetadata is null)
				throw new ArgumentNullException(nameof(getMetadata));
			if (assemblyInfo.FullyQualifiedName is null || assemblyInfo.AssemblyLocation is null)
				throw new ArgumentException();
			var metadataReader = new DmdLazyMetadataReader(getMetadata, metadataReaderFactory);

			var options = assemblyInfo.Options;
			var assembly = new DmdAssemblyImpl(this, metadataReader, assemblyInfo.AssemblyLocation, assemblyInfo.AssemblySimpleName, (options & DmdCreateAssemblyOptions.IsEXE) != 0);
			var module = new DmdModuleImpl(assembly, metadataReader,
				(options & DmdCreateAssemblyOptions.InMemory) != 0,
				(options & DmdCreateAssemblyOptions.Dynamic) != 0,
				(options & DmdCreateAssemblyOptions.Synthetic) != 0,
				assemblyInfo.FullyQualifiedName);
			metadataReader.SetModule(module);
			assembly.Add(module);
			if ((options & DmdCreateAssemblyOptions.DontAddAssembly) == 0)
				Add(assembly);
			return assembly;
		}

		public override DmdModule CreateModule(DmdAssembly assembly, Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));
			if (getMetadata is null)
				throw new ArgumentNullException(nameof(getMetadata));
			if (fullyQualifiedName is null)
				throw new ArgumentNullException(nameof(fullyQualifiedName));
			var assemblyImpl = assembly as DmdAssemblyImpl;
			if (assemblyImpl is null)
				throw new ArgumentException();
			var metadataReader = new DmdLazyMetadataReader(getMetadata, metadataReaderFactory);
			var module = new DmdModuleImpl(assemblyImpl, metadataReader, isInMemory, isDynamic, assemblyImpl.IsSynthetic, fullyQualifiedName);
			metadataReader.SetModule(module);
			assemblyImpl.Add(module);
			return module;
		}

		public override void Add(DmdAssembly assembly) {
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));
			if (assembly.AppDomain != this)
				throw new InvalidOperationException();
			var assemblyImpl = assembly as DmdAssemblyImpl;
			if (assemblyImpl is null)
				throw new InvalidOperationException();
			AssemblyLoadedListener[] listeners;
			lock (assembliesLockObj) {
				Debug.Assert(!assemblies.Contains(assemblyImpl));
				assemblies.Add(assemblyImpl);
				assemblyImpl.IsLoadedInternal = true;
				listeners = assemblyLoadedListeners.Count == 0 ? Array.Empty<AssemblyLoadedListener>() : assemblyLoadedListeners.ToArray();
			}
			foreach (var listener in listeners)
				listener.AssemblyLoaded(assemblyImpl);
		}

		public override void Remove(DmdAssembly assembly) {
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));
			if (assembly.AppDomain != this)
				throw new InvalidOperationException();
			var assemblyImpl = assembly as DmdAssemblyImpl;
			if (assemblyImpl is null)
				throw new InvalidOperationException();
			lock (assembliesLockObj) {
				bool b = assemblies.Remove(assemblyImpl);
				Debug.Assert(b);
				assemblyImpl.IsLoadedInternal = false;

				simpleNameToAssembly.Remove(assemblyImpl.GetName().Name!);
				assemblyNameToAssembly.Remove(assemblyImpl.GetName());
			}

			var modules = assemblyImpl.GetModules();

			lock (moduleTypeLockObj) {
				foreach (var module in modules) {
					toModuleTypeDict.Remove(module);
					toModuleTypeDictIgnoreCase.Remove(module);
				}
			}

			lock (exportedTypeLockObj) {
				foreach (var module in modules) {
					toModuleExportedTypeDict.Remove(module);
					toModuleExportedTypeDictIgnoreCase.Remove(module);
				}
			}

			//TODO: Remove all its types from fullyResolvedTypes
		}

		internal bool GetIsLoaded(DmdAssemblyImpl assembly) {
			lock (assembliesLockObj)
				return assembly.IsLoadedInternal;
		}

		public override DmdAssembly[] GetAssemblies(bool includeSyntheticAssemblies) {
			lock (assembliesLockObj) {
				if (includeSyntheticAssemblies)
					return assemblies.ToArray();
				int count = 0;
				foreach (var asm in assemblies) {
					if (!asm.IsSynthetic)
						count++;
				}
				var asms = new DmdAssemblyImpl[count];
				int w = 0;
				foreach (var asm in assemblies) {
					if (!asm.IsSynthetic)
						asms[w++] = asm;
				}
				if (w != count)
					throw new InvalidOperationException();
				return asms;
			}
		}

		public override DmdAssembly? GetAssembly(string simpleName) {
			if (simpleName is null)
				throw new ArgumentNullException(nameof(simpleName));
			return GetAssemblyCore(simpleName, null);
		}

		public override DmdAssembly? GetAssembly(IDmdAssemblyName name) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			return GetAssemblyCore(name.Name ?? string.Empty, name);
		}

		static readonly Version zeroVersion = new Version(0, 0, 0, 0);
		DmdAssembly? GetAssemblyCore(string simpleName, IDmdAssemblyName? name) {
			bool onlySimpleName = name is null ||
				((name.Version is null || name.Version == zeroVersion) && string.IsNullOrEmpty(name.CultureName) && (name.GetPublicKeyToken() ?? Array.Empty<byte>()).Length == 0);
			if (onlySimpleName)
				name = null;

			DmdAssemblyImpl[] assembliesCopy;
			lock (assembliesLockObj) {
				if (name is not null) {
					if (assemblyNameToAssembly.TryGetValue(name, out var cached))
						return cached;
				}
				else {
					if (simpleNameToAssembly.TryGetValue(simpleName, out var cached))
						return cached;
				}

				assembliesCopy = assemblies.ToArray();
			}

			var assembly = GetAssemblySlowCore(assembliesCopy, simpleName, name);

			if (assembly is not null) {
				lock (assembliesLockObj) {
					if (name is not null) {
						if (assemblyNameToAssembly.TryGetValue(name, out var cached))
							return cached;
						assemblyNameToAssembly[name.AsReadOnly()] = assembly;
					}
					else {
						if (simpleNameToAssembly.TryGetValue(simpleName, out var cached))
							return cached;
						simpleNameToAssembly[simpleName] = assembly;
					}
				}
			}
			else if (CanResolveToCorLib(name)) {
				// .NET hack. We don't resolve assemblies, but some attributes used by the
				// debugger reference types in an assembly that just forwards the type to the corlib.
				// This assembly isn't normally loaded at runtime.
				// We could resolve assemblies but then it's possible that we'll resolve the wrong
				// assembly. This is the only known case where we must resolve an assembly so, for now,
				// use this hack by just returning the corlib.
				return CorLib;
			}

			return assembly;
		}

		static bool CanResolveToCorLib(IDmdAssemblyName? name) {
			if (name is null)
				return false;
			if (!string.IsNullOrEmpty(name.CultureName))
				return false;

			if (Equals(name.GetPublicKeyToken(), pkt)) {
				if (StringComparer.OrdinalIgnoreCase.Equals(name.Name, "System.Diagnostics.Debug"))
					return true;
				return false;
			}

			return false;
		}
		static readonly byte[] pkt = new byte[8] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A };

		static bool Equals(byte[]? a, byte[]? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		DmdAssemblyImpl? GetAssemblySlowCore(DmdAssemblyImpl[] assemblies, string simpleName, IDmdAssemblyName? name) {
			// Try to avoid reading the metadata in case we're debugging a program with lots of assemblies.

			// We first loop over all disk file assemblies since we can check simpleName without accessing metadata.
			foreach (var assembly in assemblies) {
				if (assembly.IsInMemory || assembly.IsDynamic)
					continue;
				if (!StringComparer.OrdinalIgnoreCase.Equals(simpleName, assembly.AssemblySimpleName))
					continue;

				// Access metadata (when calling GetName())
				if (name is null || AssemblyNameEqualityComparer.Equals(assembly.GetName(), name))
					return assembly;
			}

			// Check all in-memory and dynamic assemblies. We need to read their metadata.
			foreach (var assembly in assemblies) {
				if (!(assembly.IsInMemory || assembly.IsDynamic || assembly.AssemblySimpleName == string.Empty))
					continue;

				if (name is null) {
					if (StringComparer.OrdinalIgnoreCase.Equals(simpleName, assembly.GetName().Name))
						return assembly;
				}
				else if (AssemblyNameEqualityComparer.Equals(assembly.GetName(), name))
					return assembly;
			}
			return null;
		}

		DmdAssembly? GetAssemblyByPath(string path) {
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			lock (assembliesLockObj) {
				// We don't check synthetic assemblies, caller wants real assemblies
				foreach (var assembly in assemblies) {
					if (assembly.IsDynamic || assembly.IsInMemory || assembly.IsSynthetic)
						continue;
					if (StringComparer.OrdinalIgnoreCase.Equals(assembly.Location, path))
						return assembly;
				}
			}
			return null;
		}

		sealed class AssemblyLoadedListener : IDisposable {
			readonly DmdAppDomainImpl owner;
			public List<DmdAssembly> LoadedAssemblies { get; }
			public AssemblyLoadedListener(DmdAppDomainImpl owner) {
				this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
				LoadedAssemblies = new List<DmdAssembly>();
				owner.AddAssemblyLoadedListener(this);
			}
			public void AssemblyLoaded(DmdAssembly assembly) => LoadedAssemblies.Add(assembly);
			public void Dispose() => owner.RemoveAssemblyLoadedListener(this);
		}

		void AddAssemblyLoadedListener(AssemblyLoadedListener listener) {
			lock (assembliesLockObj)
				assemblyLoadedListeners.Add(listener);
		}

		void RemoveAssemblyLoadedListener(AssemblyLoadedListener listener) {
			lock (assembliesLockObj)
				assemblyLoadedListeners.Remove(listener);
		}

		public override DmdAssembly? Load(object? context, IDmdAssemblyName name) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			var asm = GetAssembly(name);
			if (asm is not null)
				return asm;

			var assemblyType = GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly);
			var method = assemblyType.GetMethod("Load", assemblyType, new[] { System_String }, throwOnError: true)!;

			// Load an assembly and then try to figure out which one of the 0 or more loaded assemblies
			// is the one we want. This isn't guaranteed to succeed.
			using (var listener = new AssemblyLoadedListener(this)) {
				method.Invoke(context, null, new[] { name.ToString() });
				// Dispose it so we can access its LoadedAssemblies prop
				listener.Dispose();

				var asms = listener.LoadedAssemblies.Where(a => !a.IsSynthetic && StringComparer.OrdinalIgnoreCase.Equals(a.GetName().Name, name.Name)).ToArray();
				if (asms.Length != 0) {
					if (asms.Length != 1) {
						foreach (var a in asms) {
							if (DmdMemberInfoEqualityComparer.DefaultOther.Equals(a.GetName(), name))
								return a;
						}
					}
					return asms[0];
				}

				// It probably failed to load
				return null;
			}
		}

		public override DmdAssembly? LoadFile(object? context, string path) {
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			var asm = GetAssemblyByPath(path);
			if (asm is not null)
				return asm;

			var assemblyType = GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly);
			var method = assemblyType.GetMethod("LoadFile", assemblyType, new[] { System_String }, throwOnError: true)!;
			method.Invoke(context, null, new[] { path });
			return GetAssemblyByPath(path);
		}

		public override DmdAssembly? LoadFrom(object? context, string assemblyFile) {
			if (assemblyFile is null)
				throw new ArgumentNullException(nameof(assemblyFile));
			var name = new DmdReadOnlyAssemblyName(assemblyFile);
			var asm = GetAssembly(name) ?? GetAssemblyByPath(assemblyFile);
			if (asm is not null)
				return asm;

			var assemblyType = GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly);
			var method = assemblyType.GetMethod("LoadFrom", assemblyType, new[] { System_String }, throwOnError: true)!;
			method.Invoke(context, null, new[] { name.ToString() });
			return GetAssembly(name) ?? GetAssemblyByPath(assemblyFile);
		}

		internal override DmdType? GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional, bool onlyCorLib) {
			var type = wellKnownMemberResolver.GetWellKnownType(wellKnownType, onlyCorLib);
			if (type is null && !isOptional)
				throw new ResolveException("Couldn't resolve well known type: " + wellKnownType);
			return type;
		}

		public override DmdType Intern(DmdType type, DmdMakeTypeOptions options) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			if (type.AppDomain != this)
				throw new InvalidOperationException();

			var res = type as DmdTypeBase ?? throw new ArgumentException();
			if ((options & DmdMakeTypeOptions.NoResolve) == 0)
				res = res.FullResolve() ?? res;
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakePointerType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (elementType is null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if (et is null)
				throw new ArgumentException();
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			if ((options & DmdMakeTypeOptions.NoResolve) == 0)
				et = et.FullResolve() ?? et;

			var res = new DmdPointerType(et, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeByRefType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (elementType is null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if (et is null)
				throw new ArgumentException();
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			if ((options & DmdMakeTypeOptions.NoResolve) == 0)
				et = et.FullResolve() ?? et;

			var res = new DmdByRefType(et, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (elementType is null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if (et is null)
				throw new ArgumentException();
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			if ((options & DmdMakeTypeOptions.NoResolve) == 0)
				et = et.FullResolve() ?? et;

			var res = new DmdSZArrayType(et, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			// Allow 0, it's allowed in the MD
			if (rank < 0)
				throw new ArgumentOutOfRangeException(nameof(rank));
			if (elementType is null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			if (sizes is null)
				throw new ArgumentNullException(nameof(sizes));
			if (lowerBounds is null)
				throw new ArgumentNullException(nameof(lowerBounds));
			var et = elementType as DmdTypeBase;
			if (et is null)
				throw new ArgumentException();
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			if ((options & DmdMakeTypeOptions.NoResolve) == 0)
				et = et.FullResolve() ?? et;

			var res = new DmdMDArrayType(et, rank, sizes, lowerBounds, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (genericTypeDefinition is null)
				throw new ArgumentNullException(nameof(genericTypeDefinition));
			if (genericTypeDefinition.AppDomain != this)
				throw new InvalidOperationException();
			if (typeArguments is null)
				throw new ArgumentNullException(nameof(typeArguments));
			for (int i = 0; i < typeArguments.Count; i++) {
				if (typeArguments[i].AppDomain != this)
					throw new InvalidOperationException();
			}
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}

			DmdTypeBase res;
			DmdTypeDef? gtDef;
			bool resolve = (options & DmdMakeTypeOptions.NoResolve) == 0;
			if (resolve)
				gtDef = genericTypeDefinition.Resolve() as DmdTypeDef;
			else
				gtDef = genericTypeDefinition as DmdTypeDef;
			if (gtDef is null) {
				var gtRef = genericTypeDefinition as DmdTypeRef;
				if (gtRef is null)
					throw new ArgumentException();
				if (resolve)
					typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceTypeRef(gtRef, typeArguments, customModifiers);
			}
			else {
				if (resolve)
					gtDef = (DmdTypeDef?)gtDef.FullResolve() ?? gtDef;
				if (!gtDef.IsGenericTypeDefinition)
					throw new ArgumentException();
				if (gtDef.GetGenericArguments().Count != typeArguments.Count)
					throw new ArgumentException();
				if (resolve)
					typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceType(gtDef, typeArguments, customModifiers);
			}

			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdMethodInfo MakeGenericMethod(DmdMethodInfo genericMethodDefinition, IList<DmdType> typeArguments, DmdMakeTypeOptions options) {
			if (genericMethodDefinition is null)
				throw new ArgumentNullException(nameof(genericMethodDefinition));
			if (genericMethodDefinition.AppDomain != this)
				throw new ArgumentException();
			if (!genericMethodDefinition.IsGenericMethodDefinition)
				throw new ArgumentException();
			if (typeArguments is null)
				throw new ArgumentNullException(nameof(typeArguments));
			if (typeArguments.Count == 0)
				throw new ArgumentException();
			for (int i = 0; i < typeArguments.Count; i++) {
				if (typeArguments[i].AppDomain != this)
					throw new InvalidOperationException();
			}
			var sig = genericMethodDefinition.GetMethodSignature();
			if (sig.GenericParameterCount != typeArguments.Count)
				throw new ArgumentException();

			DmdMethodInfoBase res;
			DmdMethodDef? gmDef;
			bool resolve = (options & DmdMakeTypeOptions.NoResolve) == 0;
			if (resolve)
				gmDef = genericMethodDefinition.Resolve() as DmdMethodDef;
			else
				gmDef = genericMethodDefinition as DmdMethodDef;

			if (gmDef is null) {
				var gmRef = genericMethodDefinition as DmdMethodRef;
				if (gmRef is null)
					throw new ArgumentException();
				if (resolve)
					typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdMethodSpecRef(gmRef, typeArguments);
			}
			else {
				if (gmDef.GetGenericArguments().Count != typeArguments.Count)
					throw new ArgumentException();
				if (resolve)
					typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdMethodSpec(gmDef, typeArguments);
			}

			return res;
		}

		public override DmdType MakeFunctionPointerType(DmdMethodSignature methodSignature, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (methodSignature is null)
				throw new ArgumentNullException(nameof(methodSignature));
			if (methodSignature.ReturnType.AppDomain != this)
				throw new ArgumentException();
			var parameterTypes = methodSignature.GetParameterTypes();
			for (int i = 0; i < parameterTypes.Count; i++) {
				if (parameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			var varArgsParameterTypes = methodSignature.GetVarArgsParameterTypes();
			for (int i = 0; i < varArgsParameterTypes.Count; i++) {
				if (varArgsParameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}

			var res = new DmdFunctionPointerType(this, methodSignature, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeFunctionPointerType(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (genericParameterCount < 0)
				throw new ArgumentOutOfRangeException(nameof(genericParameterCount));
			if (returnType is null)
				throw new ArgumentNullException(nameof(returnType));
			if (parameterTypes is null)
				throw new ArgumentNullException(nameof(parameterTypes));
			if (varArgsParameterTypes is null)
				throw new ArgumentNullException(nameof(varArgsParameterTypes));
			if (returnType.AppDomain != this)
				throw new ArgumentException();
			for (int i = 0; i < parameterTypes.Count; i++) {
				if (parameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			for (int i = 0; i < varArgsParameterTypes.Count; i++) {
				if (varArgsParameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}

			if ((options & DmdMakeTypeOptions.NoResolve) == 0) {
				returnType = ((DmdTypeBase)returnType).FullResolve() ?? returnType;
				parameterTypes = DmdTypeUtilities.FullResolve(parameterTypes) ?? parameterTypes;
				varArgsParameterTypes = DmdTypeUtilities.FullResolve(varArgsParameterTypes) ?? varArgsParameterTypes;
			}
			var methodSignature = new DmdMethodSignature(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes);

			var res = new DmdFunctionPointerType(this, methodSignature, customModifiers);
			lock (fullyResolvedTypesLockObj) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeGenericTypeParameter(int position, DmdType declaringType, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (declaringType is null)
				throw new ArgumentNullException(nameof(declaringType));
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			var declTypeBase = declaringType as DmdTypeBase;
			if (declTypeBase is null)
				throw new ArgumentException();
			return new DmdGenericParameterTypeImpl(this, declTypeBase, name, position, attributes, customModifiers);
		}

		public override DmdType MakeGenericMethodParameter(int position, DmdMethodBase declaringMethod, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier>? customModifiers, DmdMakeTypeOptions options) {
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (declaringMethod is null)
				throw new ArgumentNullException(nameof(declaringMethod));
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (customModifiers is not null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			return new DmdGenericParameterTypeImpl(this, declaringMethod, name, position, attributes, customModifiers);
		}

		sealed class TypeDefResolver : ITypeDefResolver {
			readonly DmdAppDomain appDomain;
			readonly bool ignoreCase;

			public TypeDefResolver(DmdAppDomain appDomain, bool ignoreCase) {
				this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
				this.ignoreCase = ignoreCase;
			}

			public DmdTypeDef? GetTypeDef(IDmdAssemblyName? assemblyName, List<string> typeNames) {
				if (typeNames.Count == 0)
					return null;
				DmdTypeDef? type;
				DmdTypeUtilities.SplitFullName(typeNames[0], out var @namespace, out var name);
				if (assemblyName is not null) {
					var assembly = (DmdAssemblyImpl?)appDomain.GetAssembly(assemblyName);
					if (!(assembly?.ManifestModule is DmdModule module))
						return null;
					var typeRef = new DmdParsedTypeRef(module, null, DmdTypeScope.Invalid, @namespace, name, null);
					type = assembly.GetType(typeRef, ignoreCase);
				}
				else {
					type = null;
					foreach (DmdAssemblyImpl assembly in appDomain.GetAssemblies()) {
						var module = assembly.ManifestModule;
						if (module is null)
							continue;
						var typeRef = new DmdParsedTypeRef(module, null, DmdTypeScope.Invalid, @namespace, name, null);
						type = assembly.GetType(typeRef, ignoreCase);
						if (type is not null)
							break;
					}
				}
				if (type is null)
					return null;
				for (int i = 1; i < typeNames.Count; i++) {
					var flags = DmdBindingFlags.Public | DmdBindingFlags.NonPublic;
					if (ignoreCase)
						flags |= DmdBindingFlags.IgnoreCase;
					type = (DmdTypeDef?)type.GetNestedType(typeNames[i], flags);
					if (type is null)
						return null;
				}
				return type;
			}
		}

		public override DmdType? GetType(string typeName, DmdGetTypeOptions options) {
			if (typeName is null)
				throw new ArgumentNullException(nameof(typeName));

			var resolver = new TypeDefResolver(this, (options & DmdGetTypeOptions.IgnoreCase) != 0);
			var type = DmdTypeNameParser.Parse(resolver, typeName);
			if (type is not null)
				return Intern(type, DmdMakeTypeOptions.NoResolve);

			if ((options & DmdGetTypeOptions.ThrowOnError) != 0)
				throw new TypeNotFoundException(typeName);
			return null;
		}

		internal DmdTypeDef? Resolve(DmdTypeRef typeRef, bool throwOnError, bool ignoreCase) {
			if (typeRef is null)
				throw new ArgumentNullException(nameof(typeRef));

			var type = ResolveCore(typeRef, ignoreCase);
			if (type is not null)
				return type;

			if (throwOnError)
				throw new TypeResolveException(typeRef);
			return null;
		}

		DmdTypeDef? ResolveCore(DmdTypeRef typeRef, bool ignoreCase) {
			var nonNestedTypeRef = DmdTypeUtilities.GetNonNestedType(typeRef);
			if (nonNestedTypeRef is null)
				return null;

			DmdModule? module;
			DmdAssembly? assembly;
			var typeScope = nonNestedTypeRef.TypeScope;
			switch (typeScope.Kind) {
			case DmdTypeScopeKind.Invalid:
				Debug.Fail("Shouldn't be here");
				return null;

			case DmdTypeScopeKind.Module:
				module = (DmdModule)typeScope.Data!;
				return Lookup(module, typeRef, ignoreCase) ?? ResolveExportedType(new[] { module }, typeRef, ignoreCase);

			case DmdTypeScopeKind.ModuleRef:
				assembly = GetAssembly((IDmdAssemblyName)typeScope.Data2!);
				if (assembly is null)
					return null;
				module = assembly.GetModule((string)typeScope.Data!);
				if (module is null)
					return null;
				return Lookup(module, typeRef, ignoreCase) ?? ResolveExportedType(new[] { module }, typeRef, ignoreCase);

			case DmdTypeScopeKind.AssemblyRef:
				assembly = GetAssembly((IDmdAssemblyName)typeScope.Data!);
				if (assembly is null)
					return null;
				return Lookup(assembly, typeRef, ignoreCase) ?? ResolveExportedType(assembly.GetModules(), typeRef, ignoreCase);

			default:
				throw new InvalidOperationException();
			}
		}

		DmdTypeDef? ResolveExportedType(DmdModule[] modules, DmdTypeRef typeRef, bool ignoreCase) {
			for (int i = 0; i < 30; i++) {
				var exportedType = FindExportedType(modules, typeRef, ignoreCase);
				if (exportedType is null)
					return null;

				var nonNested = DmdTypeUtilities.GetNonNestedType(exportedType);
				if (nonNested is null)
					return null;
				var typeScope = nonNested.TypeScope;
				if (typeScope.Kind != DmdTypeScopeKind.AssemblyRef)
					return null;
				var etAsm = GetAssembly((IDmdAssemblyName)typeScope.Data!);
				if (etAsm is null)
					return null;

				var td = Lookup(etAsm, typeRef, ignoreCase);
				if (td is not null)
					return td;

				modules = etAsm.GetModules();
			}

			return null;
		}

		DmdTypeRef? FindExportedType(IList<DmdModule> modules, DmdTypeRef typeRef, bool ignoreCase) {
			foreach (var module in modules) {
				Dictionary<DmdType, DmdTypeRef>? dict;
				do {
					lock (exportedTypeLockObj) {
						if (ignoreCase) {
							if (toModuleExportedTypeDictIgnoreCase.TryGetValue(module, out dict))
								break;
							dict = new Dictionary<DmdType, DmdTypeRef>(moduleTypeDictComparerIgnoreCase);
							toModuleExportedTypeDictIgnoreCase[module] = dict;
						}
						else {
							if (toModuleExportedTypeDict.TryGetValue(module, out dict))
								break;
							dict = new Dictionary<DmdType, DmdTypeRef>(moduleTypeDictComparer);
							toModuleExportedTypeDict[module] = dict;
						}
					}

					var types = (DmdTypeRef[])module.GetExportedTypes();

					lock (exportedTypeLockObj) {
						foreach (var type in types)
							dict[type] = type;
					}
				} while (false);

				lock (exportedTypeLockObj) {
					if (dict.TryGetValue(typeRef, out var exportedType))
						return exportedType;
				}
			}
			return null;
		}

		internal DmdTypeDef? TryLookup(DmdAssemblyImpl assembly, DmdTypeRef typeRef, bool ignoreCase) =>
			Lookup(assembly, typeRef, ignoreCase) ?? ResolveExportedType(assembly.GetModules(), typeRef, ignoreCase);

		internal DmdTypeDef? TryLookup(DmdModuleImpl module, DmdTypeRef typeRef, bool ignoreCase) =>
			Lookup(module, typeRef, ignoreCase) ?? ResolveExportedType(new[] { module }, typeRef, ignoreCase);

		DmdTypeDef? Lookup(DmdAssembly assembly, DmdTypeRef typeRef, bool ignoreCase) {
			// Most likely it's in the manifest module so we don't have to alloc an array (GetModules())
			var manifestModule = assembly.ManifestModule;
			if (manifestModule is null)
				return null;
			var type = Lookup(manifestModule, typeRef, ignoreCase);
			if (type is not null)
				return type;

			foreach (var module in assembly.GetModules()) {
				if (manifestModule == module)
					continue;
				type = Lookup(module, typeRef, ignoreCase);
				if (type is not null)
					return type;
			}
			return null;
		}

		void DmdMetadataReader_TypesUpdated(DmdModule module, DmdTypesUpdatedEventArgs e) {
			var types = new DmdTypeDef?[e.Tokens.Length];
			for (int i = 0; i < types.Length; i++)
				types[i] = module.ResolveType((int)e.Tokens[i], DmdResolveOptions.None) as DmdTypeDef;

			lock (moduleTypeLockObj) {
				((DmdModuleImpl)module).DynamicModuleVersionInternal++;
				Dictionary<DmdType, DmdTypeDef>? dict1 = null, dict2 = null;
				toModuleTypeDictIgnoreCase?.TryGetValue(module, out dict1);
				toModuleTypeDict?.TryGetValue(module, out dict2);
				Debug2.Assert(dict1 is not null || dict2 is not null);
				foreach (var type in types) {
					if (type is null)
						continue;
					if (dict1 is not null)
						dict1[type] = type;
					if (dict2 is not null)
						dict2[type] = type;
				}
			}
		}

		DmdTypeDef? Lookup(DmdModule module, DmdTypeRef typeRef, bool ignoreCase) {
			Dictionary<DmdType, DmdTypeDef>? dict;
			do {
				lock (moduleTypeLockObj) {
					if (ignoreCase) {
						if (toModuleTypeDictIgnoreCase.TryGetValue(module, out dict))
							break;
						dict = new Dictionary<DmdType, DmdTypeDef>(moduleTypeDictComparerIgnoreCase);
						toModuleTypeDictIgnoreCase[module] = dict;
					}
					else {
						if (toModuleTypeDict.TryGetValue(module, out dict))
							break;
						dict = new Dictionary<DmdType, DmdTypeDef>(moduleTypeDictComparer);
						toModuleTypeDict[module] = dict;
					}

					// Only dynamic modules can add more types at runtime
					if (module.IsDynamic) {
						// If it's the first time this code gets executed with this module
						if (toModuleTypeDictIgnoreCase.ContainsKey(module) != toModuleTypeDict.ContainsKey(module)) {
							var moduleImpl = (DmdModuleImpl)module;
							moduleImpl.MetadataReader.TypesUpdated += (s, e) => DmdMetadataReader_TypesUpdated(module, e);
						}
					}
				}

				var types = (DmdTypeDef[])module.GetTypes();

				lock (moduleTypeLockObj) {
					foreach (var type in types)
						dict[type] = type;
				}

			} while (false);

			lock (moduleTypeLockObj) {
				if (dict.TryGetValue(typeRef, out var typeDef))
					return typeDef;
			}
			return null;
		}

		internal DmdType[] GetSZArrayInterfaces(DmdType elementType) {
			var ifaces = defaultExistingWellKnownSZArrayInterfaces;
			if (ifaces is null) {
				Debug2.Assert(CorLib is not null, "CorLib hasn't been loaded yet!");
				if (CorLib is null)
					return Array.Empty<DmdType>();
				List<DmdType>? list = ObjectPools.AllocListOfType();
				foreach (var wellKnownType in possibleWellKnownSZArrayInterfaces) {
					// These interfaces should only be in corlib since the CLR needs them.
					// They're not always present so if we fail to find a type in the corlib, we don't
					// want to search the remaining assemblies (could be hundreds of assemblies).
					var iface = GetWellKnownType(wellKnownType, isOptional: true, onlyCorLib: true);
					if (iface is not null)
						list.Add(iface);
				}
				Interlocked.CompareExchange(ref defaultExistingWellKnownSZArrayInterfaces, ObjectPools.FreeAndToArray(ref list), null);
				ifaces = defaultExistingWellKnownSZArrayInterfaces!;
			}
			var res = new DmdType[ifaces.Length];
			var typeArguments = new[] { elementType };
			for (int i = 0; i < res.Length; i++)
				res[i] = MakeGenericType(ifaces[i], typeArguments, null, DmdMakeTypeOptions.None);
			return res;
		}
		volatile DmdType[]? defaultExistingWellKnownSZArrayInterfaces;
		static readonly DmdWellKnownType[] possibleWellKnownSZArrayInterfaces = new DmdWellKnownType[] {
			// Available since .NET Framework 2.0
			DmdWellKnownType.System_Collections_Generic_IList_T,
			// Available since .NET Framework 4.5
			DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T,
			// Available since .NET Framework 4.5
			DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T,
		};

		public override object? CreateInstance(object? context, DmdConstructorInfo ctor, object?[] parameters) {
			if (ctor is null)
				throw new ArgumentNullException(nameof(ctor));
			if (ctor.IsStatic)
				throw new ArgumentException();
			if (ctor.AppDomain != this)
				throw new ArgumentException();
			return runtime.Evaluator.CreateInstance(context, ctor, parameters ?? Array.Empty<object?>());
		}

		public override object? Invoke(object? context, DmdMethodBase method, object? obj, object?[]? parameters) {
			if (method is null)
				throw new ArgumentNullException(nameof(method));
			if (method.IsStatic != (obj is null))
				throw new ArgumentException();
			if (method.AppDomain != this)
				throw new ArgumentException();
			return runtime.Evaluator.Invoke(context, method, obj, parameters ?? Array.Empty<object?>());
		}

		public override object? LoadField(object? context, DmdFieldInfo field, object? obj) {
			if (field is null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj is null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			return runtime.Evaluator.LoadField(context, field, obj);
		}

		public override void StoreField(object? context, DmdFieldInfo field, object? obj, object? value) {
			if (field is null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj is null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			runtime.Evaluator.StoreField(context, field, obj, value);
		}
	}
}
