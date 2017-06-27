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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdComMetadataReader : DmdMetadataReaderBase {
		public override Guid ModuleVersionId {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __moduleVersionId_DONT_USE;
			}
		}

		public override int MDStreamVersion {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __mdStreamVersion_DONT_USE;
			}
		}

		public override string ModuleScopeName {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __moduleScopeName_DONT_USE;
			}
		}

		public override string ImageRuntimeVersion {
			get {
				if (!modulePropsInitd)
					InitializeModuleProperties();
				return __imageRuntimeVersion_DONT_USE;
			}
		}

		void InitializeModuleProperties() {
			if (modulePropsInitd)
				return;
			COMThread(() => {
				if (modulePropsInitd)
					return;

				__imageRuntimeVersion_DONT_USE = MDAPI.GetModuleVersionString(MetaDataImport) ?? string.Empty;
				__moduleVersionId_DONT_USE = MDAPI.GetModuleMvid(MetaDataImport) ?? Guid.Empty;
				__moduleScopeName_DONT_USE = MDAPI.GetModuleName(MetaDataImport) ?? string.Empty;
				__machine_DONT_USE = MDAPI.GetModuleMachineAndPEKind(MetaDataImport, out __peKind_DONT_USE) ?? DmdImageFileMachine.I386;

				if (__imageRuntimeVersion_DONT_USE.StartsWith("v1."))
					__mdStreamVersion_DONT_USE = 0x00010000;
				else
					__mdStreamVersion_DONT_USE = 0x00020000;

				modulePropsInitd = true;
			});
		}
		bool modulePropsInitd;
		Guid __moduleVersionId_DONT_USE;
		int __mdStreamVersion_DONT_USE;
		string __moduleScopeName_DONT_USE;
		string __imageRuntimeVersion_DONT_USE;
		DmdPortableExecutableKinds __peKind_DONT_USE;
		DmdImageFileMachine __machine_DONT_USE;

		public override DmdMethodInfo EntryPoint => throw new NotImplementedException();//TODO:

		IMetaDataImport2 MetaDataImport { get; }
		IMetaDataAssemblyImport MetaDataAssemblyImport { get; }

		readonly DmdModuleImpl module;
		readonly DmdDispatcher dispatcher;

		public DmdComMetadataReader(DmdModuleImpl module, IMetaDataImport2 metaDataImport, DmdDispatcher dispatcher) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			MetaDataImport = metaDataImport ?? throw new ArgumentNullException(nameof(metaDataImport));
			MetaDataAssemblyImport = (IMetaDataAssemblyImport)metaDataImport;
			this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		void COMThread(Action action) => dispatcher.Invoke(action);

		public override DmdTypeDef[] GetTypes() => throw new NotImplementedException();//TODO:
		public override DmdTypeRef[] GetExportedTypes() => throw new NotImplementedException();//TODO:
		protected override DmdTypeRef ResolveTypeRef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdTypeDef ResolveTypeDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdFieldDef ResolveFieldDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdMethodBase ResolveMethodDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => throw new NotImplementedException();//TODO:
		protected override DmdEventDef ResolveEventDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdPropertyDef ResolvePropertyDef(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments) => throw new NotImplementedException();//TODO:
		protected override DmdTypeRef ResolveExportedType(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveFieldSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveMethodSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveMemberRefSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveStandAloneSigSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveTypeSpecSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override byte[] ResolveMethodSpecSignature(uint rid) => throw new NotImplementedException();//TODO:
		protected override string ResolveStringCore(uint offset) => throw new NotImplementedException();//TODO:

		public override void GetPEKind(out DmdPortableExecutableKinds peKind, out DmdImageFileMachine machine) {
			if (!modulePropsInitd)
				InitializeModuleProperties();
			peKind = __peKind_DONT_USE;
			machine = __machine_DONT_USE;
		}

		public override DmdAssemblyName GetName() {
			if (assemblyName == null)
				InitializeAssemblyName();
			return assemblyName.Clone();
		}
		DmdAssemblyName assemblyName;

		void InitializeAssemblyName() {
			if (assemblyName != null)
				return;
			COMThread(InitializeAssemblyName_COMThread);
		}

		void InitializeAssemblyName_COMThread() {
			dispatcher.VerifyAccess();
			if (assemblyName != null)
				return;
			var name = new DmdAssemblyName();

			const uint token = 0x20000001;
			name.Version = MDAPI.GetAssemblyVersionAndLocale(MetaDataAssemblyImport, token, out string locale) ?? new Version(0, 0, 0, 0);
			name.Name = MDAPI.GetAssemblySimpleName(MetaDataAssemblyImport, token) ?? string.Empty;
			name.CultureName = locale ?? string.Empty;
			name.HashAlgorithm = MDAPI.GetAssemblyHashAlgorithm(MetaDataAssemblyImport, token) ?? DmdAssemblyHashAlgorithm.SHA1;
			name.SetPublicKey(MDAPI.GetAssemblyPublicKey(MetaDataAssemblyImport, token) ?? Array.Empty<byte>());
			name.RawFlags = MDAPI.GetAssemblyAttributes(MetaDataAssemblyImport, token) ?? DmdAssemblyNameFlags.None;

			// PERF: Make sure the public key token is created once so it doesn't have to be recreated
			// for each caller.
			name.GetPublicKeyToken();

			assemblyName = name;
		}

		public override DmdAssemblyName[] GetReferencedAssemblies() => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadAssemblyCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadModuleCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadTypeDefCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadFieldCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadMethodCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadParamCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadEventCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadPropertyCustomAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadAssemblySecurityAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadTypeDefSecurityAttributes(uint rid) => throw new NotImplementedException();//TODO:
		protected override DmdCustomAttributeData[] ReadMethodSecurityAttributes(uint rid) => throw new NotImplementedException();//TODO:
	}
}
