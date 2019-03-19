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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdModuleImpl : DmdModule {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdAppDomain AppDomain => Assembly.AppDomain;
		public override string FullyQualifiedName { get; }
		public override DmdAssembly Assembly => assembly;
		public override bool IsDynamic { get; }
		public override bool IsInMemory { get; }
		public override bool IsSynthetic { get; }
		public override Guid ModuleVersionId => metadataReader.ModuleVersionId;
		public override int MetadataToken => 0x00000001;
		public override DmdType GlobalType => ResolveType(0x02000001);
		public override int MDStreamVersion => metadataReader.MDStreamVersion;
		public override string ScopeName {
			get => scopeNameOverride ?? metadataReader.ModuleScopeName;
			set => scopeNameOverride = value;
		}
		public override int DynamicModuleVersion => DynamicModuleVersionInternal;
		internal volatile int DynamicModuleVersionInternal;

		internal DmdMetadataReader MetadataReader => metadataReader;
		readonly DmdAssemblyImpl assembly;
		readonly DmdMetadataReader metadataReader;
		string scopeNameOverride;

		public DmdModuleImpl(DmdAssemblyImpl assembly, DmdMetadataReader metadataReader, bool isInMemory, bool isDynamic, bool isSynthetic, string fullyQualifiedName) {
			this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			this.metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
			FullyQualifiedName = fullyQualifiedName ?? throw new ArgumentNullException(nameof(fullyQualifiedName));
			IsDynamic = isDynamic;
			IsInMemory = isInMemory;
			IsSynthetic = isSynthetic;
		}

		public override DmdType[] GetTypes() => metadataReader.GetTypes();
		public override DmdType[] GetExportedTypes() => metadataReader.GetExportedTypes();
		public override DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => metadataReader.ResolveMethodSignature(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override byte[] ResolveSignature(int metadataToken) => metadataReader.ResolveSignature(metadataToken);
		public override string ResolveString(int metadataToken) => metadataReader.ResolveString(metadataToken);
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) => metadataReader.GetPEKind(out peKind, out machine);
		public override DmdReadOnlyAssemblyName[] GetReferencedAssemblies() => metadataReader.GetReferencedAssemblies();
		public override unsafe bool ReadMemory(uint rva, void* destination, int size) => metadataReader.ReadMemory(rva, destination, size);

		sealed class TypeDefResolver : ITypeDefResolver {
			readonly DmdModuleImpl module;
			readonly bool ignoreCase;

			public TypeDefResolver(DmdModuleImpl module, bool ignoreCase) {
				this.module = module ?? throw new ArgumentNullException(nameof(module));
				this.ignoreCase = ignoreCase;
			}

			public DmdTypeDef GetTypeDef(IDmdAssemblyName assemblyName, List<string> typeNames) {
				if (typeNames.Count == 0)
					return null;

				var targetModule = module;
				var targetAssembly = targetModule.assembly;
				if (assemblyName != null && !module.assembly.AppDomainImpl.AssemblyNameEqualityComparer.Equals(targetAssembly.GetName(), assemblyName)) {
					targetAssembly = (DmdAssemblyImpl)targetAssembly.AppDomain.GetAssembly(assemblyName);
					targetModule = (DmdModuleImpl)targetAssembly?.ManifestModule;
					if (targetModule == null)
						return null;
				}

				DmdTypeDef type;
				DmdTypeUtilities.SplitFullName(typeNames[0], out string @namespace, out string name);

				var typeRef = new DmdParsedTypeRef(targetModule, null, DmdTypeScope.Invalid, @namespace, name, null);
				type = targetModule.GetType(typeRef, ignoreCase);

				if ((object)type == null)
					return null;
				for (int i = 1; i < typeNames.Count; i++) {
					var flags = DmdBindingFlags.Public | DmdBindingFlags.NonPublic;
					if (ignoreCase)
						flags |= DmdBindingFlags.IgnoreCase;
					type = (DmdTypeDef)type.GetNestedType(typeNames[i], flags);
					if ((object)type == null)
						return null;
				}
				return type;
			}
		}

		public override DmdType GetType(string typeName, DmdGetTypeOptions options) {
			if (typeName == null)
				throw new ArgumentNullException(nameof(typeName));

			var resolver = new TypeDefResolver(this, (options & DmdGetTypeOptions.IgnoreCase) != 0);
			var type = DmdTypeNameParser.Parse(resolver, typeName);
			if ((object)type != null)
				return AppDomain.Intern(type, DmdMakeTypeOptions.NoResolve);

			if ((options & DmdGetTypeOptions.ThrowOnError) != 0)
				throw new TypeNotFoundException(typeName);
			return null;
		}

		DmdTypeDef GetType(DmdTypeRef typeRef, bool ignoreCase) => assembly.AppDomainImpl.TryLookup(this, typeRef, ignoreCase);

		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			if (customAttributes != null)
				return customAttributes;
			var cas = metadataReader.ReadCustomAttributes(0x00000001);
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, cas);
			Interlocked.CompareExchange(ref customAttributes, newCAs, null);
			return customAttributes;
		}
		volatile ReadOnlyCollection<DmdCustomAttributeData> customAttributes;
	}
}
