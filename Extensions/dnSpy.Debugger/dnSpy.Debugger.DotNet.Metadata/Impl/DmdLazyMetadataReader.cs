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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdLazyMetadataReader : DmdMetadataReader {
		readonly object lockObj;
		Func<DmdLazyMetadataBytes> getMetadata;
		Func<DmdModuleImpl, DmdLazyMetadataBytes, DmdMetadataReader> metadataReaderFactory;
		DmdMetadataReader MetadataReader => __metadataReader_DONT_USE ?? InitializeMetadataReader();
		volatile DmdMetadataReader __metadataReader_DONT_USE;
		volatile DmdModuleImpl module;

		public DmdLazyMetadataReader(Func<DmdLazyMetadataBytes> getMetadata, Func<DmdModuleImpl, DmdLazyMetadataBytes, DmdMetadataReader> metadataReaderFactory) {
			lockObj = new object();
			this.getMetadata = getMetadata ?? throw new ArgumentNullException(nameof(getMetadata));
			this.metadataReaderFactory = metadataReaderFactory ?? throw new ArgumentNullException(nameof(metadataReaderFactory));
		}

		DmdMetadataReader InitializeMetadataReader() {
			lock (lockObj) {
				var reader = __metadataReader_DONT_USE;
				if (reader != null)
					return reader;
				if (module == null)
					throw new InvalidOperationException();
				reader = metadataReaderFactory(module, getMetadata());
				module = null;
				getMetadata = null;
				metadataReaderFactory = null;
				reader.TypesUpdated += DmdMetadataReader_TypesUpdated;
				__metadataReader_DONT_USE = reader;
				return reader;
			}
		}

		void DmdMetadataReader_TypesUpdated(object sender, DmdTypesUpdatedEventArgs e) => typesUpdated?.Invoke(this, e);
		public override event EventHandler<DmdTypesUpdatedEventArgs> TypesUpdated {
			add => typesUpdated += value;
			remove => typesUpdated -= value;
		}
		EventHandler<DmdTypesUpdatedEventArgs> typesUpdated;

		internal void SetModule(DmdModuleImpl module) {
			lock (lockObj)
				this.module = module;
		}

		public override Guid ModuleVersionId => MetadataReader.ModuleVersionId;
		public override int MDStreamVersion => MetadataReader.MDStreamVersion;
		public override string ModuleScopeName => MetadataReader.ModuleScopeName;
		public override string ImageRuntimeVersion => MetadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo EntryPoint => MetadataReader.EntryPoint;
		public override DmdTypeDef[] GetTypes() => MetadataReader.GetTypes();
		public override DmdTypeRef[] GetExportedTypes() => MetadataReader.GetExportedTypes();
		public override DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => MetadataReader.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => MetadataReader.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => MetadataReader.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => MetadataReader.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) => MetadataReader.ResolveMethodSignature(metadataToken, genericTypeArguments, genericMethodArguments, options);
		public override byte[] ResolveSignature(int metadataToken) => MetadataReader.ResolveSignature(metadataToken);
		public override string ResolveString(int metadataToken) => MetadataReader.ResolveString(metadataToken);
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) => MetadataReader.GetPEKind(out peKind, out machine);
		public override DmdReadOnlyAssemblyName GetName() => MetadataReader.GetName();
		public override DmdReadOnlyAssemblyName[] GetReferencedAssemblies() => MetadataReader.GetReferencedAssemblies();
		public override unsafe bool ReadMemory(uint rva, void* destination, int size) => MetadataReader.ReadMemory(rva, destination, size);
		public override DmdCustomAttributeData[] ReadCustomAttributes(int metadataToken) => MetadataReader.ReadCustomAttributes(metadataToken);
		public override DmdCustomAttributeData[] ReadSecurityAttributes(int metadataToken) => MetadataReader.ReadSecurityAttributes(metadataToken);
	}
}
