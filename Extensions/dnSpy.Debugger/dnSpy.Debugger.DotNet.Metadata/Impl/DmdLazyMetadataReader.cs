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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdLazyMetadataReader : DmdMetadataReader {
		readonly object lockObj;
		Func<DmdLazyMetadataBytes> getMetadata;
		Func<DmdModuleImpl, DmdLazyMetadataBytes, DmdMetadataReader> metadataReaderFactory;
		DmdMetadataReader MetadataReader => __metadataReader_DONT_USE ?? InitializeMetadataReader();
		DmdMetadataReader __metadataReader_DONT_USE;
		DmdModuleImpl module;

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
				__metadataReader_DONT_USE = reader;
				return reader;
			}
		}

		internal void SetModule(DmdModuleImpl module) => this.module = module;

		public override Guid ModuleVersionId => MetadataReader.ModuleVersionId;
		public override int MDStreamVersion => MetadataReader.MDStreamVersion;
		public override string ModuleScopeName => MetadataReader.ModuleScopeName;
		public override string ImageRuntimeVersion => MetadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo EntryPoint => MetadataReader.EntryPoint;
		public override DmdType[] GetTypes() => MetadataReader.GetTypes();

		internal override DmdType ResolveTypeRef(uint rid) => MetadataReader.ResolveTypeRef(rid);
		internal override DmdType ResolveTypeDef(uint rid) => MetadataReader.ResolveTypeDef(rid);
		internal override DmdFieldInfo ResolveFieldDef(uint rid) => MetadataReader.ResolveFieldDef(rid);
		internal override DmdMethodBase ResolveMethodDef(uint rid) => MetadataReader.ResolveMethodDef(rid);
		internal override DmdMemberInfo ResolveMemberRef(uint rid, DmdType[] genericTypeArguments) => MetadataReader.ResolveMemberRef(rid, genericTypeArguments);
		internal override DmdEventInfo ResolveEventDef(uint rid) => MetadataReader.ResolveEventDef(rid);
		internal override DmdPropertyInfo ResolvePropertyDef(uint rid) => MetadataReader.ResolvePropertyDef(rid);
		internal override DmdType ResolveTypeSpec(uint rid, DmdType[] genericTypeArguments) => MetadataReader.ResolveTypeSpec(rid, genericTypeArguments);
		internal override DmdMethodBase ResolveMethodSpec(uint rid, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => MetadataReader.ResolveMethodSpec(rid, genericTypeArguments, genericMethodArguments);
		internal override byte[] ResolveFieldSignature(uint rid) => MetadataReader.ResolveFieldSignature(rid);
		internal override byte[] ResolveMethodSignature(uint rid) => MetadataReader.ResolveMethodSignature(rid);
		internal override byte[] ResolveMemberRefSignature(uint rid) => MetadataReader.ResolveMemberRefSignature(rid);
		internal override byte[] ResolveStandAloneSigSignature(uint rid) => MetadataReader.ResolveStandAloneSigSignature(rid);
		internal override byte[] ResolveTypeSpecSignature(uint rid) => MetadataReader.ResolveTypeSpecSignature(rid);
		internal override byte[] ResolveMethodSpecSignature(uint rid) => MetadataReader.ResolveMethodSpecSignature(rid);
		internal override string ResolveStringCore(uint offset) => MetadataReader.ResolveStringCore(offset);

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) => MetadataReader.GetPEKind(out peKind, out machine);
		public override DmdAssemblyName GetName() => MetadataReader.GetName();
		public override DmdType[] GetExportedTypes() => MetadataReader.GetExportedTypes();
		public override DmdAssemblyName[] GetReferencedAssemblies() => MetadataReader.GetReferencedAssemblies();
	}
}
