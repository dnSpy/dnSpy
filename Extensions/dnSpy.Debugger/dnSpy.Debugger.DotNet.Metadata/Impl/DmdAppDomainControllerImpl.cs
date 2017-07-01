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
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAppDomainControllerImpl : DmdAppDomainController {
		public override DmdAppDomain AppDomain => appDomain;

		readonly DmdRuntimeImpl runtime;
		readonly DmdAppDomainImpl appDomain;
		readonly Func<DmdModuleImpl, DmdLazyMetadataBytes, DmdMetadataReader> metadataReaderFactory;

		public DmdAppDomainControllerImpl(DmdRuntimeImpl runtime, int id) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			appDomain = new DmdAppDomainImpl(runtime, id);
			metadataReaderFactory = CreateDmdMetadataReader;
			runtime.Add(appDomain);
		}

		DmdMetadataReader CreateDmdMetadataReader(DmdModuleImpl module, DmdLazyMetadataBytes lzmd) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (lzmd == null)
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

		public override void Remove() => runtime.Remove(appDomain);

		public override DmdAssemblyController CreateAssembly(Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) {
			if (getMetadata == null)
				throw new ArgumentNullException(nameof(getMetadata));
			if (fullyQualifiedName == null)
				throw new ArgumentNullException(nameof(fullyQualifiedName));
			if (assemblyLocation == null)
				throw new ArgumentNullException(nameof(assemblyLocation));
			var metadataReader = new DmdLazyMetadataReader(getMetadata, metadataReaderFactory);
			return new DmdAssemblyControllerImpl(appDomain, metadataReader, isInMemory, isDynamic, fullyQualifiedName, assemblyLocation);
		}

		public override DmdModuleController CreateModule(DmdAssembly assembly, Func<DmdLazyMetadataBytes> getMetadata, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			if (getMetadata == null)
				throw new ArgumentNullException(nameof(getMetadata));
			if (fullyQualifiedName == null)
				throw new ArgumentNullException(nameof(fullyQualifiedName));
			var assemblyImpl = assembly as DmdAssemblyImpl;
			if (assemblyImpl == null)
				throw new ArgumentException();
			var metadataReader = new DmdLazyMetadataReader(getMetadata, metadataReaderFactory);
			return new DmdModuleControllerImpl(assemblyImpl, metadataReader, isInMemory, isDynamic, fullyQualifiedName);
		}
	}
}
