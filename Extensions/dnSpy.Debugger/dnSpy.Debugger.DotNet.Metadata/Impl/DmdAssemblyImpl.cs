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
using System.IO;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAssemblyImpl : DmdAssembly {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdAppDomain AppDomain => appDomain;
		public override string Location { get; }
		public override string ImageRuntimeVersion => metadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo? EntryPoint => metadataReader.EntryPoint;
		public override bool IsLoaded => appDomain.GetIsLoaded(this);
		internal bool IsLoadedInternal { get; set; }

		internal string AssemblySimpleName {
			get {
				if (assemblySimpleName is null)
					assemblySimpleName = CalculateAssemblySimpleName();
				return assemblySimpleName;
			}
		}
		string? assemblySimpleName;
		string CalculateAssemblySimpleName() {
			// GetName() will access the metadata but we have to do it in case this is a renamed exe file.
			// Most files aren't EXEs so most of the files' metadata won't be accessed.
			if (isExe)
				return GetName().Name ?? string.Empty;

			if (IsInMemory || IsDynamic)
				return string.Empty;
			try {
				var res = Path.GetFileNameWithoutExtension(Location);
				if (res.EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
					return Path.GetFileNameWithoutExtension(res);
				return res;
			}
			catch (ArgumentException) {
			}
			return string.Empty;
		}

		public override DmdModule ManifestModule {
			get {
				lock (LockObject)
					return modules.Count == 0 ? throw new InvalidOperationException() : modules[0];
			}
		}

		internal DmdAppDomainImpl AppDomainImpl => appDomain;
		readonly DmdAppDomainImpl appDomain;
		readonly List<DmdModuleImpl> modules;
		readonly DmdMetadataReader metadataReader;
		readonly bool isExe;

		public DmdAssemblyImpl(DmdAppDomainImpl appDomain, DmdMetadataReader metadataReader, string location, string? assemblySimpleName, bool isExe) {
			modules = new List<DmdModuleImpl>();
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			this.metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
			Location = location ?? throw new ArgumentNullException(nameof(location));
			this.assemblySimpleName = assemblySimpleName;
			this.isExe = isExe;
		}

		internal void Add(DmdModuleImpl module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			lock (LockObject) {
				Debug.Assert(!modules.Contains(module));
				modules.Add(module);
			}
		}

		public override void Remove(DmdModule module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			var moduleImpl = module as DmdModuleImpl;
			if (moduleImpl is null)
				throw new InvalidOperationException();
			lock (LockObject) {
				bool b = modules.Remove(moduleImpl);
				Debug.Assert(b);
			}
		}

		public override DmdModule[] GetModules() => GetLoadedModules();
		public override DmdModule[] GetLoadedModules() {
			lock (LockObject)
				return modules.ToArray();
		}

		public override DmdModule? GetModule(string name) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			// Make a copy of it so we don't hold a lock while calling module.ScopeName
			DmdModule[] modulesCopy;
			lock (LockObject)
				modulesCopy = modules.ToArray();
			foreach (var module in modulesCopy) {
				// This is case insensitive, see AssemblyNative::GetModule(pAssembly,wszFileName,retModule) in coreclr/src/vm/assemblynative.cpp
				if (StringComparer.OrdinalIgnoreCase.Equals(module.ScopeName, name))
					return module;
			}
			return null;
		}

		public override DmdReadOnlyAssemblyName GetName() {
			if (asmName is null) {
				var newAsmName = metadataReader.GetName();
				Debug.Assert(string.IsNullOrEmpty(assemblySimpleName) || newAsmName.Name == assemblySimpleName);
				var flags = newAsmName.RawFlags;
				flags |= DmdAssemblyNameFlags.PublicKey;
				if (metadataReader.MDStreamVersion >= 0x00010000) {
					metadataReader.GetPEKind(out var peKind, out var machine);
					if ((flags & DmdAssemblyNameFlags.PA_FullMask) == DmdAssemblyNameFlags.PA_NoPlatform)
						flags = (flags & ~DmdAssemblyNameFlags.PA_FullMask) | DmdAssemblyNameFlags.PA_None;
					else
						flags = (flags & ~DmdAssemblyNameFlags.PA_FullMask) | GetProcessorArchitecture(peKind, machine);
				}
				asmName = new DmdReadOnlyAssemblyName(newAsmName.Name, newAsmName.Version, newAsmName.CultureName, flags, newAsmName.GetPublicKey(), newAsmName.GetPublicKeyToken(), newAsmName.HashAlgorithm);
			}
			return asmName;
		}
		DmdReadOnlyAssemblyName? asmName;

		static DmdAssemblyNameFlags GetProcessorArchitecture(DmdPortableExecutableKinds peKind, DmdImageFileMachine machine) {
			if ((peKind & DmdPortableExecutableKinds.PE32Plus) == 0) {
				switch (machine) {
				case DmdImageFileMachine.I386:
					if ((peKind & DmdPortableExecutableKinds.Required32Bit) != 0)
						return DmdAssemblyNameFlags.PA_x86;
					if ((peKind & DmdPortableExecutableKinds.ILOnly) != 0)
						return DmdAssemblyNameFlags.PA_MSIL;
					return DmdAssemblyNameFlags.PA_x86;

				case DmdImageFileMachine.ARM:
					return DmdAssemblyNameFlags.PA_ARM;
				}
			}
			else {
				switch (machine) {
				case DmdImageFileMachine.I386:
					if ((peKind & DmdPortableExecutableKinds.ILOnly) != 0)
						return DmdAssemblyNameFlags.PA_MSIL;
					break;

				case DmdImageFileMachine.AMD64:
					return DmdAssemblyNameFlags.PA_AMD64;

				case DmdImageFileMachine.IA64:
					return DmdAssemblyNameFlags.PA_IA64;
				}
			}

			return DmdAssemblyNameFlags.PA_None;
		}

		public override DmdType[] GetExportedTypes() {
			var list = new List<DmdType>();
			foreach (var type in metadataReader.GetTypes()) {
				if (type.IsVisible)
					list.Add(type);
			}
			foreach (var type in metadataReader.GetExportedTypes()) {
				if (!IsTypeForwarder(type))
					list.Add(type);
			}
			return list.ToArray();
		}

		public override DmdType[] GetForwardedTypes() {
			var exportedTypes = metadataReader.GetExportedTypes();
			if (exportedTypes.Length == 0)
				return Array.Empty<DmdType>();
			var res = new DmdType[exportedTypes.Length];
			int w = 0;
			foreach (var type in exportedTypes) {
				if (IsTypeForwarder(type))
					res[w++] = type;
			}
			if (res.Length != w) {
				if (w == 0)
					return Array.Empty<DmdType>();
				Array.Resize(ref res, w);
			}
			return res;
		}

		static bool IsTypeForwarder(DmdType type) {
			var nonNested = DmdTypeUtilities.GetNonNestedType(type);
			if (nonNested is null)
				return false;
			return nonNested.TypeScope.Kind == DmdTypeScopeKind.AssemblyRef;
		}

		public override DmdReadOnlyAssemblyName[] GetReferencedAssemblies() => metadataReader.GetReferencedAssemblies();
		internal DmdTypeDef? GetType(DmdTypeRef typeRef, bool ignoreCase) => appDomain.TryLookup(this, typeRef, ignoreCase);

		sealed class TypeDefResolver : ITypeDefResolver {
			readonly DmdAssemblyImpl assembly;
			readonly bool ignoreCase;

			public TypeDefResolver(DmdAssemblyImpl assembly, bool ignoreCase) {
				this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
				this.ignoreCase = ignoreCase;
			}

			public DmdTypeDef? GetTypeDef(IDmdAssemblyName? assemblyName, List<string> typeNames) {
				if (typeNames.Count == 0)
					return null;

				DmdAssemblyImpl? targetAssembly = assembly;
				if (!(assemblyName is null) && !assembly.AppDomainImpl.AssemblyNameEqualityComparer.Equals(targetAssembly.GetName(), assemblyName)) {
					targetAssembly = (DmdAssemblyImpl?)targetAssembly.AppDomain.GetAssembly(assemblyName);
					if (targetAssembly is null)
						return null;
				}

				DmdTypeDef? type;
				DmdTypeUtilities.SplitFullName(typeNames[0], out var @namespace, out string name);

				var module = targetAssembly.ManifestModule;
				if (module is null)
					return null;
				var typeRef = new DmdParsedTypeRef(module, null, DmdTypeScope.Invalid, @namespace, name, null);
				type = targetAssembly.GetType(typeRef, ignoreCase);

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
			if (!(type is null))
				return appDomain.Intern(type, DmdMakeTypeOptions.NoResolve);

			if ((options & DmdGetTypeOptions.ThrowOnError) != 0)
				throw new TypeNotFoundException(typeName);
			return null;
		}

		public override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() {
			if (!(securityAttributes is null))
				return securityAttributes;
			var cas = metadataReader.ReadSecurityAttributes(0x20000001);
			Interlocked.CompareExchange(ref securityAttributes, ReadOnlyCollectionHelpers.Create(cas), null);
			return securityAttributes!;
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData>? securityAttributes;

		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			if (!(customAttributes is null))
				return customAttributes;
			var cas = metadataReader.ReadCustomAttributes(0x20000001);
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, cas, GetSecurityAttributesData());
			Interlocked.CompareExchange(ref customAttributes, newCAs, null);
			return customAttributes!;
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData>? customAttributes;
	}
}
