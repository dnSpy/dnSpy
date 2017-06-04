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
		public override int ModuleMetadataToken => MetadataReader.ModuleMetadataToken;
		public override DmdType GlobalType => MetadataReader.GlobalType;
		public override int MDStreamVersion => MetadataReader.MDStreamVersion;
		public override string ModuleScopeName => MetadataReader.ModuleScopeName;
		public override string ImageRuntimeVersion => MetadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo EntryPoint => MetadataReader.EntryPoint;
		public override DmdType[] GetTypes() => MetadataReader.GetTypes();
		public override DmdMethodBase ResolveMethod(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => MetadataReader.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
		public override DmdFieldInfo ResolveField(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => MetadataReader.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
		public override DmdType ResolveType(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => MetadataReader.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
		public override DmdMemberInfo ResolveMember(int metadataToken, DmdType[] genericTypeArguments, DmdType[] genericMethodArguments) => MetadataReader.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
		public override byte[] ResolveSignature(int metadataToken) => MetadataReader.ResolveSignature(metadataToken);
		public override string ResolveString(int metadataToken) => MetadataReader.ResolveString(metadataToken);
		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) => MetadataReader.GetPEKind(out peKind, out machine);
		public override DmdAssemblyName GetName() => MetadataReader.GetName();
		public override DmdType[] GetExportedTypes() => MetadataReader.GetExportedTypes();
		public override DmdAssemblyName[] GetReferencedAssemblies() => MetadataReader.GetReferencedAssemblies();
	}
}
